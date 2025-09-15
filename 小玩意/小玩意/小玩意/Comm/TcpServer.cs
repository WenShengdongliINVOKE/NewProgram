using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace 小玩意.Comm
{
    public class TcpServer : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly ConcurrentDictionary<Guid, TcpClient> _clients = new();
        private bool _isRunning;

        public event Action<Guid>? ClientConnected;
        public event Action<Guid>? ClientDisconnected;
        public event Action<Guid, string>? MessageReceived;
        public TcpServer(IPAddress ip, int port)
        {
            _listener = new TcpListener(ip, port);
        }
        /// <summary>
        /// 服务器启动
        /// </summary>
        /// <returns></returns>
        public async Task StartAsync()
        {
            _isRunning = true;
            _listener.Start();
            Console.WriteLine($"Server started on {_listener.LocalEndpoint}");

            try
            {
                while (_isRunning)
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    // _ = HandleClientAsync(client);
                }
            }
            catch (Exception ex) when (ex is ObjectDisposedException || ex is InvalidOperationException)
            {
                // Server stopped
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tcpClient"></param>
        /// <returns></returns>
        private async Task HandleClientAsync(TcpClient tcpClient)
        {
            var clientId = Guid.NewGuid();
            _clients[clientId] = tcpClient;
            ClientConnected?.Invoke(clientId);

            //try
            //{
            //    using var stream = tcpClient.GetStream();
            //    var buffer = new byte[4096];
            //    var messageBuilder = new StringBuilder();

            //    while (_isRunning)
            //    {
            //        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            //        if (bytesRead == 0) break;

            //        messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
            //        while (messageBuilder.ToString().IndexOf("\n", StringComparison.Ordinal) is int delimiterIndex && delimiterIndex != -1)
            //        {
            //            var message = messageBuilder.ToString(0, delimiterIndex);
            //            messageBuilder.Remove(0, delimiterIndex + 1);
            //            MessageReceived?.Invoke(clientId, message);
            //        }
            //    }
            //}
            //finally
            //{
            //    tcpClient.Dispose();
            //    _clients.TryRemove(clientId, out _);
            //    ClientDisconnected?.Invoke(clientId);
            //}
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task BroadcastAsync(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            var data = Encoding.UTF8.GetBytes($"{message}\n");

            foreach (var client in _clients.Values)
            {
                try
                {
                    // await client.GetStream().WriteAsync(data, 0, data.Length);
                }
                catch
                {
                    // Handle write failures
                }
            }
        }
        public void Stop()
        {
            _isRunning = false;
            _listener.Stop();
        }
        public void Dispose()
        {
            Stop();
            foreach (var client in _clients.Values)
            {
                client.Dispose();
            }
            GC.SuppressFinalize(this);
        }
    }
}
