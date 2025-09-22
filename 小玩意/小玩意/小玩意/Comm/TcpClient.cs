using System.Net.Sockets;
using System.Text;
using 小玩意.ViewModel;

namespace 小玩意.Comm
{
    public class TcpClient : IDisposable
    {
        private readonly System.Net.Sockets.TcpClient _client = new();
        private NetworkStream? _stream;
        private bool _isConnected;
        /// <summary>
        /// 使用时 请在外面 MessageReceived += 你的方法 ; 通过对委托的封装来让订阅者可以接受到发布者所发送的消息
        /// </summary>
        public event Action<string>? MessageReceived;
        public event Action? Disconnected;
        /// <summary>
        /// 连接服务器
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        /// <exception cref="TimeoutException"></exception>
        public async Task ConnectAsync(string host, int port, int timeout = 5000)
        {
            var connectTask = _client.ConnectAsync(host, port);
            var timeoutTask = Task.Delay(timeout);

            if (await Task.WhenAny(connectTask, timeoutTask) == timeoutTask)
            {
                ErrorViewModel.Errornotice("连接超时，请检查服务器地址和端口是否正确。", true, 1);
            }

            _stream = _client.GetStream();
            _isConnected = true;
            _ = ReceiveMessagesAsync();
        }
        /// <summary>
        /// 读取数据
        /// </summary>
        /// <returns></returns>
        private async Task ReceiveMessagesAsync()
        {
            var buffer = new byte[4096];
            var messageBuilder = new StringBuilder();

            try
            {
                while (_isConnected)
                {
                    var bytesRead = await _stream!.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                    while (messageBuilder.ToString().IndexOf("\n", StringComparison.Ordinal) is int delimiterIndex
                        && delimiterIndex != -1)
                    {
                        var message = messageBuilder.ToString(0, delimiterIndex);
                        messageBuilder.Remove(0, delimiterIndex + 1);
                        MessageReceived?.Invoke(message);
                    }
                }
            }
            catch
            {
                //TODO: Add logging 加日志和异常处理
                // Handle read errors
            }
            finally
            {
                //Disconnect();
            }
        }
        /// <summary>
        /// 发送信息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task SendAsync(string message)
        {
            if (!_isConnected || string.IsNullOrEmpty(message)) return;

            var data = Encoding.UTF8.GetBytes($"{message}\n");
            await _stream!.WriteAsync(data, 0, data.Length);
            //TODO :加日志 加异常
        }
        /// <summary>
        /// 
        /// </summary>
        public void Disconnect()
        {
            if (!_isConnected) return;

            _isConnected = false;
            _stream?.Close();
            _client.Close();
            Disconnected?.Invoke();
        }
        /// <summary>
        /// 关闭连接
        /// </summary>
        public void Dispose()
        {
            Disconnect();
            _client.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
