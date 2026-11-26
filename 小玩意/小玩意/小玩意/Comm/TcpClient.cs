using NLog;
using System.Net.Sockets;
using System.Text;
using 小玩意.ViewModel;

namespace 小玩意.Comm
{
    /// <summary>
    /// TCP 客户端通讯类。
    /// 作用：以「UTF-8 文本 + 换行符分隔」的帧格式与远端服务端收发报文。
    /// 目的：现场既有西门子、三菱这类走专用协议的 PLC，也有走裸 TCP 文本协议的设备。
    ///      让本类实现 IPlccommiunctionCommand 之后，界面层可以用同一套读写方法驱动所有设备，
    ///      不必为每种协议各写一条分支——那种写法每新增一种设备都要改一次界面代码，最容易漏改。
    /// </summary>
    public class TcpClient : IDisposable, IPlccommiunctionCommand
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly System.Net.Sockets.TcpClient _client = new();
        private NetworkStream? _stream;
        private bool _isConnected;

        /// <summary>
        /// 最近一次收到的完整报文。
        /// 作用：把接收线程解析出来的最后一条消息存下来，供同步的 ReadPlc 取用。
        /// 目的：TCP 是服务端主动推送的流式协议，没有「按地址去读」的语义，
        ///      ReadPlc 只能读到最新一条报文；如果这里不留一份，
        ///      ReadPlc 就没有任何数据源，只能永远返回默认值。
        /// </summary>
        private string _lastMessage = string.Empty;

        /// <summary>
        /// 收到一条完整报文时触发。
        /// 作用：把接收线程读到的消息抛给订阅者。
        /// 目的：读取动作发生在后台线程，界面不能直接拿数据，
        ///      用事件让订阅者自己决定要不要切回 UI 线程，避免本类反向依赖界面。
        /// 用法：使用前在外面写 MessageReceived += 你的方法。
        /// </summary>
        public event Action<string>? MessageReceived;

        /// <summary>
        /// 连接断开时触发。
        /// 作用：通知订阅者连接已不可用。
        /// 目的：断线后界面需要改状态、停轮询；如果没人通知，
        ///      界面会一直显示「已连接」，操作员会误以为看到的是实时数据。
        /// </summary>
        public event Action? Disconnected;

        #region 原有 TCP 读写逻辑（暂存：等接口改造稳定后再统一重构）

        /// <summary>
        /// 连接服务器
        /// </summary>
        /// <param name="host">服务端地址</param>
        /// <param name="port">服务端端口</param>
        /// <param name="timeout">连接超时时间，单位毫秒</param>
        /// <returns></returns>
        public async Task ConnectAsync(string host, int port, int timeout = 5000)
        {
            var connectTask = _client.ConnectAsync(host, port);
            var timeoutTask = Task.Delay(timeout);

            if (await Task.WhenAny(connectTask, timeoutTask) == timeoutTask)
            {
                // 注意：这里只弹了提示，既没有 return 也没有抛异常，
                // 紧接着的 GetStream() 在连接尚未建立时会抛 InvalidOperationException。
                // 目前保持原有行为不动，等统一改造连接流程时一并处理。
                ErrorViewModel.Errornotice("连接超时，请检查服务器地址和端口是否正确。", true, 1);
            }

            _stream = _client.GetStream();
            _isConnected = true;
            // 丢弃返回的 Task 而不 await，是刻意为之：接收循环要一直跑在后台，
            // 不能阻塞调用方；异常由循环内部自己收口（见 ReceiveMessagesAsync 的 catch）。
            _ = ReceiveMessagesAsync();
        }

        /// <summary>
        /// 读取数据
        /// 作用：在后台循环里持续读取网络流，并按换行符拆成一条条完整报文。
        /// 目的：TCP 是字节流，一次 Read 可能只读到半条报文，也可能一次读到多条，
        ///      所以必须自己攒一个缓冲区按分隔符切分；否则上层拿到的会是残缺的 JSON/命令。
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
                    // 读到 0 字节代表对端已正常关闭连接，继续循环只会立刻再读到 0，形成空转
                    if (bytesRead == 0) break;

                    messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                    while (messageBuilder.ToString().IndexOf("\n", StringComparison.Ordinal) is int delimiterIndex
                        && delimiterIndex != -1)
                    {
                        var message = messageBuilder.ToString(0, delimiterIndex);
                        messageBuilder.Remove(0, delimiterIndex + 1);
                        // 存一份最新报文，供同步的 ReadPlc 读取；
                        // 只保留最后一条是有意为之，历史报文对「当前值」类点位没有意义。
                        _lastMessage = message;
                        MessageReceived?.Invoke(message);
                    }
                }
            }
            catch
            {
                // 作用：吞掉接收循环中的异常，防止后台任务把整个程序带崩。
                // 目的：这里是自发自收的后台线程，没有调用方可以接住异常，只能就地收口。
                //      TODO 后续要补日志，否则断线时现场只能看到「数据不刷新」，查不到原因。
            }
            finally
            {
                // 以下一行是历史遗留：断线后原本要主动断开并通知界面。
                // 暂时注掉，是因为 Disconnect() 会再触发 Disconnected 事件，
                // 若在接收线程里同步执行，订阅方又去操作界面容易重入；
                // 等异常处理和重连逻辑补齐后再放开。
                //Disconnect();
            }
        }

        /// <summary>
        /// 发送信息
        /// 作用：以异步方式把文本按「UTF-8 + 换行符」的帧格式写到网络流。
        /// 目的：保留给已经在用 async 的调用方（如界面里的 await 按钮命令），
        ///      不强制它们改成同步调用。
        /// </summary>
        /// <param name="message">要发送的文本，不含结尾换行符</param>
        /// <returns></returns>
        public async Task SendAsync(string message)
        {
            if (!_isConnected || string.IsNullOrEmpty(message)) return;

            var data = Encoding.UTF8.GetBytes($"{message}\n");
            await _stream!.WriteAsync(data, 0, data.Length);
            // 作用：TODO 后续补日志与异常处理。
            // 目的：发送失败时目前会静默丢弃，现场表现为「指令没反应」，
            //      必须留下记录才能判断是网络问题还是设备没应答。
        }

        #endregion

        #region IPlccommiunctionCommand 实现（TCP 版）

        /// <summary>
        /// 写入 32 位整数。
        /// 作用：把 value 校验为 int 之后，按文本帧发给服务端。
        /// 目的：TCP 没有寄存器地址的概念，DBaddress 只是调用方传下来的点位标识，
        ///      真正决定写入内容的是 value。这里先做数值校验再发送，
        ///      是为了避免一个拼错的字符串被原样发出去——设备侧解析失败一般不会回执，
        ///      现场只能看到「写不进去」，很难反推是上位机发错了内容。
        /// </summary>
        /// <param name="DBaddress">点位标识，仅用于日志定位，不参与发送</param>
        /// <param name="value">要写入的数值文本</param>
        /// <returns>true 表示已写入网络流</returns>
        public bool WritePlcInt32(string DBaddress, string value)
        {
            if (!int.TryParse(value, out int intValue))
            {
                logger.Warn($"TCP 写入 Int32 失败：\"{value}\" 不是合法整数（点位 {DBaddress}）");
                return false;
            }

            return SendText(intValue.ToString(), DBaddress);
        }

        /// <summary>
        /// 写入 16 位整数。
        /// 作用：把 value 校验为 short 之后，按文本帧发给服务端。
        /// 目的：与 WritePlcInt32 同理，只是把取值范围收窄到 16 位；
        ///      如果这里不校验而是统一按 32 位发，超出范围的数值会被静默截断，
        ///      设备拿到的值和上位机显示的值对不上，排查起来非常费时。
        /// </summary>
        /// <param name="DBaddress">点位标识，仅用于日志定位，不参与发送</param>
        /// <param name="value">要写入的数值文本</param>
        /// <returns>true 表示已写入网络流</returns>
        public bool WritePlcInt16(string DBaddress, string value)
        {
            if (!short.TryParse(value, out short shortValue))
            {
                logger.Warn($"TCP 写入 Int16 失败：\"{value}\" 不是合法 16 位整数（点位 {DBaddress}）");
                return false;
            }

            return SendText(shortValue.ToString(), DBaddress);
        }

        /// <summary>
        /// 写入字节（原样透传）。
        /// 作用：把 value 当作原始文本直接发送，不做任何数值解析。
        /// 目的：TCP 设备多数接收自定义文本命令或字符串，这个方法是「原样透传」的出口；
        ///      如果这里也做强转数值，所有字符串类点位都会直接写失败。
        /// </summary>
        /// <param name="DBaddress">点位标识，仅用于日志定位，不参与发送</param>
        /// <param name="value">要原样发送的文本</param>
        /// <returns>true 表示已写入网络流</returns>
        public bool WritePlcByte(string DBaddress, string value)
        {
            return SendText(value, DBaddress);
        }

        /// <summary>
        /// 读取数据（读最新报文）。
        /// 作用：把最近收到的一条报文按 T 的类型解析后返回。
        /// 目的：接口要求同步返回 T，而 TCP 是「服务端主动推送」的流式协议，
        ///      没有地址可以按地址去读，所以只能读缓存下来的最新报文。
        ///      若还没收到过任何数据就返回 default(T)，让调用方拿到一眼能看出来的空值，
        ///      而不是把上一次的旧数据当成当前值继续显示。
        /// </summary>
        /// <typeparam name="T">由调用方传入的示例值推断，决定解析目标类型</typeparam>
        /// <param name="type">类型示例值，只用于推断 T，本身不参与解析</param>
        /// <param name="address">点位标识，仅用于日志定位</param>
        /// <returns>解析成功返回读到的值，否则返回 default(T)</returns>
        public T ReadPlc<T>(T type, string address)
        {
            string raw = _lastMessage.Trim();

            if (string.IsNullOrEmpty(raw))
            {
                logger.Warn($"TCP 暂无可读报文（点位 {address}），返回默认值");
                return default(T)!;
            }

            // 装箱后按运行时类型分支：T 是泛型参数，无法直接 switch，
            // 只能先转成 object 再做模式匹配。
            object? boxed = type;
            object? parsed = null;

            if (boxed is bool)
            {
                // 作用：把常见的开关量写法统一归一成 bool。
                // 目的：不同设备回 "1" / "true" / "on" 都可能出现，
                //      只认其中一种会让部分设备的点位永远读成 false。
                parsed = raw == "1"
                         || raw.Equals("true", StringComparison.OrdinalIgnoreCase)
                         || raw.Equals("on", StringComparison.OrdinalIgnoreCase);
            }
            else if (boxed is short)
            {
                if (short.TryParse(raw, out short shortValue)) parsed = shortValue;
            }
            else if (boxed is int)
            {
                if (int.TryParse(raw, out int intValue)) parsed = intValue;
            }
            else if (boxed is long)
            {
                if (long.TryParse(raw, out long longValue)) parsed = longValue;
            }
            else if (boxed is float)
            {
                if (float.TryParse(raw, out float floatValue)) parsed = floatValue;
            }
            else if (boxed is double)
            {
                if (double.TryParse(raw, out double doubleValue)) parsed = doubleValue;
            }
            else if (boxed is string)
            {
                parsed = raw;
            }
            else
            {
                logger.Warn($"TCP 不支持的数据类型 {typeof(T).Name}（点位 {address}）");
                return default(T)!;
            }

            if (parsed is T result) return result;

            logger.Warn($"TCP 报文 \"{raw}\" 无法解析为 {typeof(T).Name}（点位 {address}）");
            return default(T)!;
        }

        /// <summary>
        /// 以同步方式发送一条文本报文。
        /// 作用：把文本按「UTF-8 + 换行符」的帧格式写到网络流，并返回是否发送成功。
        /// 目的：IPlccommiunctionCommand 定义的是同步返回 bool 的方法，而本类原有的 SendAsync 是异步的。
        ///      如果在 UI 线程上对 SendAsync 用 .Wait() / .Result 阻塞等待，
        ///      异步续体会被派回 UI 线程，而 UI 线程正被阻塞，两者互相等待就是死锁；
        ///      所以这里直接用 NetworkStream 的同步 Write，不经过 async 状态机，从根上避开死锁。
        /// </summary>
        /// <param name="text">要发送的文本，不含结尾换行符</param>
        /// <param name="address">点位标识，仅用于日志定位</param>
        /// <returns>true 表示已写入网络流；false 表示未连接、内容为空或发送异常</returns>
        private bool SendText(string text, string address)
        {
            if (!_isConnected || _stream is null)
            {
                logger.Warn($"TCP 未连接，写入被丢弃（点位 {address}）");
                return false;
            }

            if (string.IsNullOrEmpty(text))
            {
                logger.Warn($"TCP 写入内容为空，已忽略（点位 {address}）");
                return false;
            }

            try
            {
                // 结尾的换行符是收发双方约定的帧分隔符，必须与 ReceiveMessagesAsync 的拆包逻辑一致，
                // 少发这个字符对端会一直等不到完整报文。
                var data = Encoding.UTF8.GetBytes($"{text}\n");
                _stream.Write(data, 0, data.Length);
                logger.Info($"TCP 发送成功（点位 {address}）：{text}");
                return true;
            }
            catch (Exception ex)
            {
                // 作用：把发送异常转成 false 返回，而不是向上抛。
                // 目的：接口约定用 bool 表达成败，调用方（界面）只需要知道成没成功；
                //      异常细节留在日志里，避免一层层往上抛最终变成未捕获异常弹框。
                logger.Error(ex, $"TCP 发送失败（点位 {address}）");
                return false;
            }
        }

        #endregion

        /// <summary>
        /// 断开连接
        /// 作用：置连接标志、关闭网络流与套接字，并通知订阅者。
        /// 目的：先改标志再关流，是为了让接收循环下一轮判断时能正常退出；
        ///      如果只关流不改标志，接收循环会持续抛异常，日志被刷屏。
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
        /// 关闭连接并释放资源
        /// 作用：断开连接后释放底层套接字，并抑制终结器。
        /// 目的：套接字持有非托管句柄，不显式释放要等 GC 终结器才回收，
        ///      频繁重连时会耗尽端口或句柄；抑制终结器则是因为资源已经手工释放过，再释放一次属多余开销。
        /// </summary>
        public void Dispose()
        {
            Disconnect();
            _client.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
