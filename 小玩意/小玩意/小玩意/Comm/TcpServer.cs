using System.Net;
using System.Net.Sockets;
using System.Text;
using 小玩意.ViewModel;

namespace 小玩意.Comm
{
    public class TcpServer : IDisposable
    {
        // 服务器监听Socket
        private Socket _listenerSocket;
        // 存储所有已连接的客户端
        private readonly Dictionary<string, ClientHandler> _clients = new Dictionary<string, ClientHandler>();
        // 同步锁，确保线程安全
        private readonly object _lock = new object();
        // 取消令牌源，用于停止服务器
        private CancellationTokenSource _cts = new CancellationTokenSource();
        // 服务器运行状态标志
        private bool _isRunning;

        // 事件：当有客户端连接时触发
        public event Action<string>? ClientConnected;
        // 事件：当有客户端断开连接时触发
        public event Action<string>? ClientDisconnected;
        // 事件：当收到客户端消息时触发
        public event Action<string, string>? MessageReceived;
        // 事件：当发生错误时触发
        public event Action<string, Exception>? ErrorOccurred;


        /// <summary>
        /// 启动服务器并开始监听指定端口
        /// </summary>
        /// <param name="port">要监听的端口号</param>
        /// <returns>表示异步启动操作的任务</returns>
        public async Task StartAsync(int port)
        {
            if (_isRunning)
                ErrorViewModel.Errornotice("服务器已在运行中", false, 1);

            _listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // 绑定到本地IP和端口
            _listenerSocket.Bind(new IPEndPoint(IPAddress.Any, port));

            // 开始监听，设置最大挂起连接数为100
            _listenerSocket.Listen(100);

            _isRunning = true;

            // 开始接受客户端连接
            _ = AcceptClientsAsync(_cts.Token);
        }

        /// <summary>
        /// 异步接受客户端连接
        /// </summary>
        /// <param name="cancellationToken">取消令牌，用于停止接受连接</param>
        /// <returns>表示异步接受连接操作的任务</returns>
        private async Task AcceptClientsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _isRunning)
            {
                try
                {
                    // 异步接受客户端连接
                    var clientSocket = await _listenerSocket.AcceptAsync();

                    // 生成客户端唯一标识符
                    var clientId = GenerateClientId(clientSocket);

                    // 创建客户端处理器
                    var handler = new ClientHandler(clientSocket, clientId);

                    // 注册事件处理程序
                    handler.MessageReceived += (msg) => MessageReceived?.Invoke(clientId, msg);
                    handler.Disconnected += () => OnClientDisconnected(clientId);
                    handler.ErrorOccurred += (ex) => ErrorOccurred?.Invoke(clientId, ex);

                    // 添加到客户端字典
                    lock (_lock)
                    {
                        _clients[clientId] = handler;
                    }

                    // 开始接收数据
                    _ = handler.StartReceivingAsync(cancellationToken);

                    // 触发客户端连接事件
                    ClientConnected?.Invoke(clientId);
                }
                catch (ObjectDisposedException)
                {
                    // 监听器已被释放，正常退出
                    break;
                }
                catch (Exception ex)
                {
                    // 处理接受连接时可能发生的异常
                    ErrorOccurred?.Invoke("Accept", ex);
                }
            }
        }

        /// <summary>
        /// 生成客户端唯一标识符
        /// </summary>
        /// <param name="clientSocket">客户端Socket</param>
        /// <returns>客户端唯一标识符</returns>
        private string GenerateClientId(Socket clientSocket)
        {
            var endpoint = clientSocket.RemoteEndPoint as IPEndPoint;
            return $"{endpoint?.Address}:{endpoint?.Port}-{Guid.NewGuid().ToString("N").Substring(0, 8)}";
        }

        /// <summary>
        /// 处理客户端断开连接事件
        /// </summary>
        /// <param name="clientId">断开连接的客户端标识符</param>
        private void OnClientDisconnected(string clientId)
        {
            // 从客户端字典中移除
            lock (_lock)
            {
                _clients.Remove(clientId);
            }

            // 触发客户端断开连接事件
            ClientDisconnected?.Invoke(clientId);
        }

        /// <summary>
        /// 向所有客户端发送消息
        /// </summary>
        /// <param name="message">要发送的消息</param>
        /// <returns>表示异步发送操作的任务</returns>
        public async Task BroadcastAsync(string message)
        {
            if (!_isRunning) return;

            List<ClientHandler> clientsCopy;
            lock (_lock)
            {
                clientsCopy = new List<ClientHandler>(_clients.Values);
            }

            // 创建发送任务列表
            var sendTasks = new List<Task>();
            foreach (var client in clientsCopy)
            {
                sendTasks.Add(client.SendAsync(message));
            }

            // 等待所有发送任务完成
            await Task.WhenAll(sendTasks);
        }

        /// <summary>
        /// 向特定客户端发送消息
        /// </summary>
        /// <param name="clientId">目标客户端标识符</param>
        /// <param name="message">要发送的消息</param>
        /// <returns>表示异步发送操作的任务</returns>
        public async Task SendToClientAsync(string clientId, string message)
        {
            if (!_isRunning) return;

            ClientHandler handler;
            lock (_lock)
            {
                if (!_clients.TryGetValue(clientId, out handler))
                    throw new InvalidOperationException($"找不到标识符为 '{clientId}' 的客户端");
            }

            await handler.SendAsync(message);
        }

        /// <summary>
        /// 断开与特定客户端的连接
        /// </summary>
        /// <param name="clientId">要断开的客户端标识符</param>
        public void DisconnectClient(string clientId)
        {
            ClientHandler handler;
            lock (_lock)
            {
                if (!_clients.TryGetValue(clientId, out handler))
                    return;

                _clients.Remove(clientId);
            }

            handler.Disconnect();
        }

        /// <summary>
        /// 停止服务器并断开所有客户端连接
        /// </summary>
        public void Stop()
        {
            if (!_isRunning) return;

            _isRunning = false;
            _cts.Cancel();

            // 断开所有客户端连接
            List<ClientHandler> clientsCopy;
            lock (_lock)
            {
                clientsCopy = new List<ClientHandler>(_clients.Values);
                _clients.Clear();
            }

            foreach (var client in clientsCopy)
            {
                client.Disconnect();
            }

            _listenerSocket?.Close();
        }

        /// <summary>
        /// 获取当前连接的客户端数量
        /// </summary>
        public int ClientCount
        {
            get
            {
                lock (_lock)
                {
                    return _clients.Count;
                }
            }
        }

        /// <summary>
        /// 获取所有连接的客户端标识符列表
        /// </summary>
        /// <returns>客户端标识符列表</returns>
        public List<string> GetClientIds()
        {
            lock (_lock)
            {
                return new List<string>(_clients.Keys);
            }
        }

        /// <summary>
        /// 获取服务器运行状态
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Stop();
            _cts.Dispose();
            _listenerSocket?.Dispose();
        }




        //#region
        //private Socket _tcpServer;
        //private string _ip;
        //private int _port;
        //Socket socket_commu;
        //public TcpServer(string ip, int port)
        //{
        //    _ip = ip;
        //    _port = port;
        //    _tcpServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //}
        ///// <summary>
        ///// 启动服务器 并阻塞等待客户端连接
        ///// </summary>
        //public void ServerStart()
        //{

        //        //解析IP加端口
        //        IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(_ip), _port);
        //        //绑定端口
        //        _tcpServer.Bind(ipEndPoint);
        //        //开始监听设定最多10个排队连接请求
        //        _tcpServer.Listen(10);
        //        //TODO:  这里要加日志    Console.WriteLine("服务器已启动，等待客户端连接...");
        //        //等待客户端连接(这是一个阻塞方法)
        //        socket_commu = _tcpServer.Accept();
        //        //Task.Run(() => AcceptClientsAsync());

        //}
        ///// <summary>
        ///// 接收数据 
        ///// </summary>
        //private void ReceiveAsync()
        //{
        //    byte[] buffer = new byte[1024 * 1024];
        //    while (true)
        //    {
        //       var num= _tcpServer.Receive(buffer);
        //       var ReceiveStr =  Encoding.UTF8.GetString(buffer, 0, num);
        //        ReceiveStr.Replace("\n", "").Replace("\r","");
        //        if (ReceiveStr!=null)
        //        {
        //            MessageReceived.Invoke(ReceiveStr);
        //        }
        //    }



        //}
        //private void SendAsync(string msg)
        //{
        //    if (socket_commu != null)
        //    {
        //        byte[] buffer = Encoding.UTF8.GetBytes(msg + "\n");
        //        socket_commu.Send(buffer);
        //        //TODO: 这里要加日志
        //    }
        //    else
        //    {
        //        //TODO: 这里要加日志
        //        ErrorViewModel.Errornotice("未连接到服务器，发送失败！", true, 1);
        //    }
        //}
        //#endregion




        //// TCP 监听器，用于接受客户端连接
        //private TcpListener? _listener;
        //// 存储所有已连接的客户端
        //private readonly List<ClientHandler> _clients = new List<ClientHandler>();
        //// 取消令牌源，用于停止服务器
        //private CancellationTokenSource? _cancellationTokenSource;
        //// 服务器运行状态标志
        //private bool _isRunning;

        //// 事件：当有客户端连接时触发
        //public event Action<TcpClient>? ClientConnected;
        //// 事件：当有客户端断开连接时触发
        //public event Action<TcpClient>? ClientDisconnected;
        //// 事件：当收到客户端消息时触发
        //public event Action<TcpClient, string>? MessageReceived;

        ///// <summary>
        ///// 启动服务器并开始监听指定端口
        ///// </summary>
        ///// <param name="port">要监听的端口号</param>
        ///// <returns>表示异步启动操作的任务</returns>
        //public async Task StartAsync(int port)
        //{
        //    if (_isRunning)
        //        throw new InvalidOperationException("服务器已经在运行中");

        //    _cancellationTokenSource = new CancellationTokenSource();
        //    _listener = new TcpListener(IPAddress.Any, port);
        //    _listener.Start();
        //    _isRunning = true;

        //    // 开始接受客户端连接
        //    _ = AcceptClientsAsync(_cancellationTokenSource.Token);
        //}

        ///// <summary>
        ///// 异步接受客户端连接
        ///// </summary>
        ///// <param name="cancellationToken">取消令牌，用于停止接受连接</param>
        ///// <returns>表示异步接受连接操作的任务</returns>
        //private async Task AcceptClientsAsync(CancellationToken cancellationToken)
        //{
        //    while (!cancellationToken.IsCancellationRequested && _isRunning)
        //    {
        //        try
        //        {
        //            // 异步接受客户端连接
        //            var client = await _listener!.AcceptTcpClientAsync();

        //            // 创建客户端处理器
        //            var handler = new ClientHandler(client);
        //            handler.MessageReceived += (msg) => OnClientMessageReceived(handler.Client, msg);
        //            handler.Disconnected += () => OnClientDisconnected(handler.Client);

        //            // 添加到客户端列表
        //            lock (_clients)
        //            {
        //                _clients.Add(handler);
        //            }

        //            // 启动消息接收
        //            _ = handler.StartReceivingAsync();

        //            // 触发客户端连接事件
        //            ClientConnected?.Invoke(client);
        //        }
        //        catch (ObjectDisposedException)
        //        {
        //            // 监听器已被释放，正常退出
        //            break;
        //        }
        //        catch (Exception ex)
        //        {
        //            // 处理接受连接时可能发生的异常
        //            // TODO: 添加日志记录
        //            Console.WriteLine($"接受客户端连接时发生错误: {ex.Message}");
        //        }
        //    }
        //}

        ///// <summary>
        ///// 处理客户端消息接收事件
        ///// </summary>
        ///// <param name="client">发送消息的客户端</param>
        ///// <param name="message">接收到的消息</param>
        //private void OnClientMessageReceived(TcpClient client, string message)
        //{
        //    // 触发消息接收事件
        //    MessageReceived?.Invoke(client, message);
        //}

        ///// <summary>
        ///// 处理客户端断开连接事件
        ///// </summary>
        ///// <param name="client">断开连接的客户端</param>
        //private void OnClientDisconnected(TcpClient client)
        //{
        //    // 从客户端列表中移除
        //    lock (_clients)
        //    {
        //        _clients.RemoveAll(h => h.Client == client);
        //    }

        //    // 触发客户端断开连接事件
        //    ClientDisconnected?.Invoke(client);
        //}

        ///// <summary>
        ///// 向所有客户端发送消息
        ///// </summary>
        ///// <param name="message">要发送的消息</param>
        ///// <returns>表示异步发送操作的任务</returns>
        //public async Task BroadcastAsync(string message)
        //{
        //    if (!_isRunning) return;

        //    List<ClientHandler> clientsCopy;
        //    lock (_clients)
        //    {
        //        clientsCopy = new List<ClientHandler>(_clients);
        //    }

        //    // 创建发送任务列表
        //    var sendTasks = new List<Task>();
        //    foreach (var client in clientsCopy)
        //    {
        //        sendTasks.Add(client.SendAsync(message));
        //    }

        //    // 等待所有发送任务完成
        //    await Task.WhenAll(sendTasks);
        //}

        ///// <summary>
        ///// 向特定客户端发送消息
        ///// </summary>
        ///// <param name="client">目标客户端</param>
        ///// <param name="message">要发送的消息</param>
        ///// <returns>表示异步发送操作的任务</returns>
        //public async Task SendToClientAsync(TcpClient client, string message)
        //{
        //    if (!_isRunning) return;

        //    ClientHandler? handler;
        //    lock (_clients)
        //    {
        //        handler = _clients.Find(h => h.Client == client);
        //    }

        //    if (handler != null)
        //    {
        //        await handler.SendAsync(message);
        //    }
        //}

        ///// <summary>
        ///// 停止服务器并断开所有客户端连接
        ///// </summary>
        //public void Stop()
        //{
        //    if (!_isRunning) return;

        //    _isRunning = false;
        //    _cancellationTokenSource?.Cancel();

        //    // 断开所有客户端连接
        //    List<ClientHandler> clientsCopy;
        //    lock (_clients)
        //    {
        //        clientsCopy = new List<ClientHandler>(_clients);
        //        _clients.Clear();
        //    }

        //    foreach (var client in clientsCopy)
        //    {
        //        client.Disconnect();
        //    }

        //    _listener?.Stop();
        //}

        ///// <summary>
        ///// 获取当前连接的客户端数量
        ///// </summary>
        //public int ClientCount
        //{
        //    get
        //    {
        //        lock (_clients)
        //        {
        //            return _clients.Count;
        //        }
        //    }
        //}

        ///// <summary>
        ///// 获取服务器运行状态
        ///// </summary>
        //public bool IsRunning => _isRunning;

        ///// <summary>
        ///// 释放资源
        ///// </summary>
        //public void Dispose()
        //{
        //    Stop();
        //    _cancellationTokenSource?.Dispose();
        //    _listener = null;
        //    GC.SuppressFinalize(this);
        //}

        ///// <summary>
        ///// 内部类，用于处理单个客户端连接
        ///// </summary>
        //private class ClientHandler
        //{
        //    // 客户端连接
        //    public TcpClient Client { get; }
        //    // 网络流，用于读取和写入数据
        //    private NetworkStream? _stream;
        //    // 连接状态标志
        //    private bool _isConnected;

        //    // 事件：当接收到消息时触发
        //    public event Action<string>? MessageReceived;
        //    // 事件：当客户端断开连接时触发
        //    public event Action? Disconnected;

        //    public ClientHandler(TcpClient client)
        //    {
        //        Client = client;
        //        _stream = client.GetStream();
        //        _isConnected = true;
        //    }

        //    /// <summary>
        //    /// 开始异步接收消息
        //    /// </summary>
        //    /// <returns>表示异步接收操作的任务</returns>
        //    public async Task StartReceivingAsync()
        //    {
        //        var buffer = new byte[4096];
        //        var messageBuilder = new StringBuilder();

        //        try
        //        {
        //            while (_isConnected && _stream != null)
        //            {
        //                var bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
        //                if (bytesRead == 0) break;

        //                messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));

        //                while (messageBuilder.ToString().IndexOf("\n", StringComparison.Ordinal) is int delimiterIndex
        //                    && delimiterIndex != -1)
        //                {
        //                    var message = messageBuilder.ToString(0, delimiterIndex);
        //                    messageBuilder.Remove(0, delimiterIndex + 1);
        //                    MessageReceived?.Invoke(message);
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            // 处理读取过程中可能发生的异常
        //            Console.WriteLine($"从客户端接收消息时发生错误: {ex.Message}");
        //        }
        //        finally
        //        {
        //            Disconnect();
        //        }
        //    }

        //    /// <summary>
        //    /// 异步发送消息到客户端
        //    /// </summary>
        //    /// <param name="message">要发送的消息</param>
        //    /// <returns>表示异步发送操作的任务</returns>
        //    public async Task SendAsync(string message)
        //    {
        //        if (!_isConnected || string.IsNullOrEmpty(message) || _stream == null) return;

        //        try
        //        {
        //            var data = Encoding.UTF8.GetBytes($"{message}\n");
        //            await _stream.WriteAsync(data, 0, data.Length);
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"向客户端发送消息时发生错误: {ex.Message}");
        //            Disconnect();
        //        }
        //    }

        //    /// <summary>
        //    /// 断开与客户端的连接
        //    /// </summary>
        //    public void Disconnect()
        //    {
        //        if (!_isConnected) return;

        //        _isConnected = false;
        //        _stream?.Close();
        //        Client.Close();
        //        Disconnected?.Invoke();
        //    }
        //}

        /// <summary>
        /// 内部类，用于处理单个客户端连接
        /// </summary>
        private class ClientHandler : IDisposable
        {
            /// <summary>
            /// socket 对象
            /// </summary>
            private readonly Socket _clientSocket;
            private readonly string _clientId;
            /// <summary>
            /// 连接状态标志
            /// </summary>
            private bool _isConnected;
            /// <summary>
            /// 监听消息事件 通过对委托的封装来让订阅者可以接受到发布者所发送的消息
            /// </summary>
            public event Action<string>? MessageReceived;
            /// <summary>
            /// 断开连接事件 客户端断开连接时触发
            /// </summary>
            public event Action? Disconnected;
            /// <summary>
            /// 错误事件 发生错误时触发
            /// </summary>
            public event Action<Exception>? ErrorOccurred;

            public bool IsConnected => _isConnected && _clientSocket.Connected;

            public ClientHandler(Socket clientSocket, string clientId)
            {
                _clientSocket = clientSocket;
                _clientId = clientId;
                _isConnected = true;
            }

            /// <summary>
            /// 开始异步接收数据
            /// </summary>
            /// <param name="cancellationToken">取消令牌</param>
            /// <returns>表示异步接收操作的任务</returns>
            public async Task StartReceivingAsync(CancellationToken cancellationToken)
            {
                var buffer = new byte[4096];
                var messageBuilder = new StringBuilder();

                try
                {
                    while (_isConnected && !cancellationToken.IsCancellationRequested)
                    {
                        // 异步接收数据
                        var bytesRead = await _clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                        if (bytesRead == 0) break;

                        messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));

                        // 处理消息分隔符（这里使用换行符）
                        while (messageBuilder.ToString().IndexOf('\n') is int delimiterIndex && delimiterIndex >= 0)
                        {
                            var message = messageBuilder.ToString(0, delimiterIndex);
                            messageBuilder.Remove(0, delimiterIndex + 1);
                            MessageReceived?.Invoke(message);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // 操作被取消，正常退出
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(ex);
                }
                finally
                {
                    Disconnect();
                }
            }

            /// <summary>
            /// 异步发送消息
            /// </summary>
            /// <param name="message">要发送的消息</param>
            /// <returns>表示异步发送操作的任务</returns>
            public async Task SendAsync(string message)
            {
                if (!_isConnected)
                    throw new InvalidOperationException("连接未建立");

                try
                {
                    var data = Encoding.UTF8.GetBytes($"{message}\n");
                    await _clientSocket.SendAsync(new ArraySegment<byte>(data), SocketFlags.None);
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(ex);
                    Disconnect();
                    throw;
                }
            }

            /// <summary>
            /// 断开连接
            /// </summary>
            public void Disconnect()
            {
                if (!_isConnected) return;

                _isConnected = false;
                try
                {
                    _clientSocket.Shutdown(SocketShutdown.Both);
                    _clientSocket.Close();
                }
                catch
                {
                    // 忽略关闭过程中的异常
                }
                finally
                {
                    Disconnected?.Invoke();
                }
            }

            /// <summary>
            /// 释放资源
            /// </summary>
            public void Dispose()
            {
                Disconnect();
                _clientSocket.Dispose();
            }

        }
    }
}
