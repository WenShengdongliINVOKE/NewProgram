using NLog;
using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;

namespace 小玩意.Comm
{
    /// <summary>
    /// MES 系统 WebSocket 客户端。
    /// 作用：与 MES 服务端建立长连接，负责收发报文，并在断线后自动重连。
    /// 目的：MES 一般通过 WebSocket 长连接推送工单、过站结果等数据，而现场网络抖动、MES 重启都很常见；
    ///      如果每次断线都要人工重连，产线就会停在那里等操作员发现，所以把重连逻辑收在本类内部，
    ///      上层只管订阅消息，不必关心当前连没连上。
    /// 说明：本类是「客户端」，连接的是 MES 提供的 ws:// 或 wss:// 地址。
    ///      由于报文格式尚未确定，收发都以原始 JSON 字符串为单位；
    ///      等协议定稿后只需在调用侧组装内容（或改用 SendJsonAsync 传对象），本类不用改。
    /// </summary>
    public sealed class MesWebSocketClient : IDisposable
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>服务端地址，形如 ws://192.168.1.10:8080/mes</summary>
        private readonly string _endpoint;

        /// <summary>预解析好的 Uri，避免每次重连重复解析；解析失败在构造函数里就暴露，不留到运行期</summary>
        private readonly Uri _uri;

        /// <summary>
        /// 当前连接对象。
        /// 作用：持有实际的 WebSocket 连接。
        /// 目的：ClientWebSocket 关闭或中断后**不能复用**，重连时必须换一个新实例；
        ///      所以这里不能声明成 readonly，否则重连只能抛 ObjectDisposedException。
        /// </summary>
        private ClientWebSocket? _socket;

        /// <summary>
        /// 取消源。
        /// 作用：Dispose 时触发取消，让接收循环和重连循环一起退出。
        /// 目的：两个循环都是 while 死循环，没有取消信号就只能靠套接字异常退出，
        ///      进程关闭时会残留后台线程。
        /// </summary>
        private CancellationTokenSource? _cts;

        /// <summary>
        /// 发送闸门。
        /// 作用：保证同一时刻只有一个发送操作在执行。
        /// 目的：ClientWebSocket 明确不允许并发 SendAsync，两个线程同时发会抛
        ///      InvalidOperationException（"已有发送操作正在进行"）。而 MES 报文往往由
        ///      多个业务点（过站、报警、定时上报）同时触发，不串行化就是偶发崩溃。
        /// </summary>
        private readonly SemaphoreSlim _sendLock = new(1, 1);

        private Task? _maintainTask;
        private volatile bool _disposed;

        /// <summary>服务端地址</summary>
        public string Endpoint => _endpoint;

        /// <summary>
        /// 当前是否已连接。
        /// 作用：供界面或业务判断能不能发报文。
        /// 目的：断线期间 SendAsync 会被丢弃，如果界面不显示状态，
        ///      操作员会以为数据已经发出去了。
        /// </summary>
        public bool IsConnected => _socket is { State: WebSocketState.Open };

        /// <summary>
        /// 断线后的重连间隔。
        /// 作用：控制重连频率，默认 5 秒。
        /// 目的：间隔太短会在 MES 宕机时把对端连接队列打满；
        ///      太长则 MES 恢复后上位机迟迟连不上，产线等待时间变长。
        /// </summary>
        public TimeSpan ReconnectInterval { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// 单次连接超时时间，默认 10 秒。
        /// 作用：限制一次 ConnectAsync 最多占用多久。
        /// 目的：TCP 连接在网线拔掉等情况下可能长时间无响应，
        ///      不设超时会让重连循环一直卡在这一步，表现成"再也不重连了"。
        /// </summary>
        public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// JSON 序列化设置。
        /// 作用：统一控制发送报文里的日期格式、空值处理、命名策略等。
        /// 目的：报文格式还没定，但一旦定了（比如要求 ISO8601 或不发 null 字段），
        ///      改这一处就能影响所有发送，不必去每个调用点逐个改。
        /// </summary>
        public JsonSerializerSettings JsonSettings { get; set; } = new()
        {
            // 忽略 null 字段：MES 侧多数按固定字段解析，多余的空字段容易触发校验失败
            NullValueHandling = NullValueHandling.Ignore,
            // 统一用 ISO8601：跨系统传输时避免 "2026/11/26 17:00:00" 这类区域相关格式被对端解析成别的时刻
            DateFormatHandling = DateFormatHandling.IsoDateFormat
        };

        /// <summary>收到一条完整文本报文时触发。注意：回调在后台线程，操作界面需自行切回 UI 线程</summary>
        public event Action<string>? MessageReceived;

        /// <summary>连接建立成功时触发</summary>
        public event Action? Connected;

        /// <summary>连接断开时触发，参数为断开原因</summary>
        public event Action<string>? Disconnected;

        /// <summary>发生异常时触发（连接失败、发送失败等），供上层记录或提示</summary>
        public event Action<Exception>? Error;

        /// <summary>
        /// 初始化 MES 客户端。
        /// 作用：保存并校验服务端地址。
        /// 目的：地址写错（少个 w、用了 http://）在运行期只表现为"连不上"，
        ///      现场很难判断是网络问题还是配置写错；在构造函数里直接拦下，问题暴露得最早。
        /// </summary>
        /// <param name="endpoint">MES 的 WebSocket 地址，必须以 ws:// 或 wss:// 开头</param>
        public MesWebSocketClient(string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentException("MES 地址不能为空", nameof(endpoint));
            }

            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? uri) ||
                (uri.Scheme != "ws" && uri.Scheme != "wss"))
            {
                throw new ArgumentException($"MES 地址格式不正确，应以 ws:// 或 wss:// 开头：{endpoint}", nameof(endpoint));
            }

            _endpoint = endpoint;
            _uri = uri;
        }

        /// <summary>
        /// 启动连接并保持。
        /// 作用：启动后台任务，完成首次连接、常驻接收、断线重连这一整套循环。
        /// 目的：调用方（程序启动流程）只需调用一次，之后不必再管连接状态。
        ///      首次连接失败也进入重连循环而不抛异常，是因为上位机通常比 MES 先启动，
        ///      开机瞬间连不上属正常情况，不能让程序因此起不来。
        /// </summary>
        public void Start()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(MesWebSocketClient));

            // 幂等：重复调用直接返回，避免起出第二个接收循环把同一条报文处理两遍
            if (_maintainTask is not null) return;

            _cts = new CancellationTokenSource();
            _maintainTask = Task.Run(() => MaintainConnectionAsync(_cts.Token));
            logger.Info($"MES 客户端已启动：{_endpoint}");
        }

        /// <summary>
        /// 发送一条已组装好的 JSON 文本。
        /// 作用：把字符串按 UTF-8 文本帧发给 MES。
        /// 目的：报文格式未定时，这是最通用的出口——调用方自己拼 JSON 也行、
        ///      用 SendJsonAsync 传对象也行，本类不限制报文结构。
        /// </summary>
        /// <param name="json">完整 JSON 文本</param>
        /// <param name="token">取消标记</param>
        /// <returns>true 表示已交给底层发送；false 表示未连接或发送失败</returns>
        public async Task<bool> SendAsync(string json, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                logger.Warn("MES 发送内容为空，已忽略");
                return false;
            }

            // 先取局部引用再判断状态：即使这期间连接被重连流程换掉，
            // 本次发送仍然对着当时那个连接对象，不会出现"检查时非空、使用时为 null"的空引用。
            if (_socket is not { State: WebSocketState.Open } socket)
            {
                logger.Warn($"MES 未连接，报文未发送：{json}");
                return false;
            }

            await _sendLock.WaitAsync(token);
            try
            {
                var data = Encoding.UTF8.GetBytes(json);
                // endOfMessage 传 true：本类按「一条 JSON 一帧」发送，不做分片
                await socket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, true, token);
                logger.Info($"MES 发送报文：{json}");
                return true;
            }
            catch (Exception ex)
            {
                // 作用：发送异常转成 false 返回，同时抛给 Error 事件。
                // 目的：接口层用返回值表达成败，异常细节留给日志和订阅者；
                //      这里不向上抛，避免调用方每个业务点都要包 try-catch。
                logger.Error(ex, $"MES 报文发送失败：{json}");
                SafeRaise(() => Error?.Invoke(ex), nameof(Error));
                return false;
            }
            finally
            {
                _sendLock.Release();
            }
        }

        /// <summary>
        /// 序列化对象后发送。
        /// 作用：把业务对象按 JsonSettings 转成 JSON 再发出去。
        /// 目的：等 MES 报文格式定了之后，调用方只需定义好实体类就能发，
        ///      不用每个业务点手拼字符串（手拼最容易漏引号、拼错字段名）。
        /// </summary>
        /// <typeparam name="T">报文实体类型</typeparam>
        /// <param name="payload">报文对象</param>
        /// <param name="token">取消标记</param>
        /// <returns>true 表示已交给底层发送</returns>
        public Task<bool> SendJsonAsync<T>(T payload, CancellationToken token = default)
        {
            if (payload is null)
            {
                logger.Warn("MES 发送对象为 null，已忽略");
                return Task.FromResult(false);
            }

            string json = JsonConvert.SerializeObject(payload, JsonSettings);
            return SendAsync(json, token);
        }

        /// <summary>
        /// 连接维持主循环。
        /// 作用：不断重复「连接 → 收消息 → 断开 → 等待 → 再连接」。
        /// 目的：把连接的生命周期集中在一处，业务侧只订阅事件；
        ///      如果把重连散落在各处，很容易出现两个重连任务同时跑、互相打断的情况。
        /// </summary>
        private async Task MaintainConnectionAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await ConnectOnceAsync(token);

                    // 正常情况下会一直停在这里收消息，直到对端关闭或网络中断才返回
                    await ReceiveLoopAsync(token);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    // 主动关闭（Dispose）导致的取消，属于正常退出，不记错误日志以免刷屏
                    break;
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"MES 连接异常：{_endpoint}");
                    SafeRaise(() => Error?.Invoke(ex), nameof(Error));
                }
                finally
                {
                    CleanupSocket("连接已结束，等待重连");
                }

                if (token.IsCancellationRequested) break;

                try
                {
                    await Task.Delay(ReconnectInterval, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            logger.Info("MES 连接维持循环已退出");
        }

        /// <summary>
        /// 执行一次连接。
        /// 作用：创建新的 WebSocket 实例并完成握手。
        /// 目的：必须先 new 新实例再连——ClientWebSocket 一旦关闭就无法再次 ConnectAsync，
        ///      复用旧实例重连必然抛异常，这是实现自动重连时最容易踩的坑。
        /// </summary>
        private async Task ConnectOnceAsync(CancellationToken token)
        {
            _socket = new ClientWebSocket();

            // 内置心跳：由 WebSocket 协议层周期性发 ping。
            // 目的：现场的交换机、防火墙常会掐掉长时间无数据的空闲连接，
            //      有心跳才能让 NAT/防火墙认为连接还活着，否则会"看着连着但收不到数据"。
            _socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);

            // 连接超时与外部取消合并：任一条件成立都能中断握手
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeoutCts.CancelAfter(ConnectTimeout);

            logger.Info($"正在连接 MES：{_endpoint}");
            await _socket.ConnectAsync(_uri, timeoutCts.Token);
            logger.Info($"MES 连接成功：{_endpoint}");

            SafeRaise(() => Connected?.Invoke(), nameof(Connected));
        }

        /// <summary>
        /// 接收循环。
        /// 作用：持续读取服务端推送的报文，拼接完整后逐条抛出。
        /// 目的：WebSocket 必须有人一直读，否则接收缓冲区满了之后对端发送会被阻塞，
        ///      表现为「MES 那边卡住」。所以即使业务暂时不处理消息，这个循环也不能停。
        /// </summary>
        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            // 取局部引用：CleanupSocket 可能在其他线程把 _socket 置空，
            // 直接用字段会在循环中途遇到 null。
            ClientWebSocket? socket = _socket;
            if (socket is null) return;

            var buffer = new byte[8 * 1024];
            var builder = new StringBuilder();

            while (!token.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                builder.Clear();

                WebSocketReceiveResult result;
                // 作用：把分片拼成一条完整消息。
                // 目的：一条逻辑消息可能被拆成多个帧到达（报文较大时必然发生），
                //      只读一次就拿去解析，会得到半截 JSON，反序列化必然失败且现象随机。
                do
                {
                    result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        logger.Warn($"MES 主动关闭连接：{result.CloseStatus} {result.CloseStatusDescription}");
                        return;
                    }

                    builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                }
                while (!result.EndOfMessage);

                string message = builder.ToString();
                if (string.IsNullOrEmpty(message)) continue;

                logger.Debug($"MES 收到报文：{message}");
                SafeRaise(() => MessageReceived?.Invoke(message), nameof(MessageReceived));
            }
        }

        /// <summary>
        /// 释放当前连接。
        /// 作用：尽量走一次关闭握手，然后释放套接字，并通知订阅者。
        /// 目的：握手能让 MES 立刻知道是上位机主动离开（而不是网络故障），
        ///      避免 MES 侧把这条连接当成异常、触发告警；但握手不能无限等，
        ///      网络已断时 CloseAsync 会一直挂着，所以限时 2 秒。
        /// </summary>
        private void CleanupSocket(string reason)
        {
            ClientWebSocket? socket = _socket;
            _socket = null;
            if (socket is null) return;

            try
            {
                if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                {
                    using var closeCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    // 这里在后台任务里同步等待，不存在 UI 同步上下文，不会死锁
                    socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "client closing", closeCts.Token)
                          .GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                // 关闭握手失败不影响后续重连，降级为 Debug，避免每次断线都刷一条错误日志
                logger.Debug(ex, "MES 关闭握手未完成，直接释放连接");
            }
            finally
            {
                socket.Dispose();
                logger.Info($"MES 连接已释放：{reason}");
                SafeRaise(() => Disconnected?.Invoke(reason), nameof(Disconnected));
            }
        }

        /// <summary>
        /// 安全触发事件。
        /// 作用：包一层 try-catch 再调用订阅者。
        /// 目的：事件触发点都在后台循环里，只要有一个订阅者抛异常，
        ///      接收循环就会被打断、连接看起来"卡死"却没有任何提示；
        ///      这里把异常隔离掉，保证循环继续跑。
        /// </summary>
        private void SafeRaise(Action? action, string eventName)
        {
            if (action is null) return;

            try
            {
                action();
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"MES 事件 {eventName} 的订阅者抛出异常，已忽略");
            }
        }

        /// <summary>
        /// 释放资源。
        /// 作用：取消后台循环、等待其退出、释放连接与同步对象。
        /// 目的：不先取消就直接 Dispose 套接字，会和接收线程里的 ReceiveAsync 撞车，
        ///      抛出难以复现的 ObjectDisposedException；先停循环再释放才安全。
        /// 注意：本方法最多阻塞约 3 秒，建议在窗口关闭、程序退出时调用。
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _cts?.Cancel();

            try
            {
                // 等后台循环收尾，最多 3 秒；超时就往下走，不能因为一个卡住的循环让程序关不掉
                _maintainTask?.Wait(TimeSpan.FromSeconds(3));
            }
            catch (AggregateException ex)
            {
                // 循环内部的异常已在各处记录过，这里只是等待时的包装异常，无需再报
                logger.Debug(ex, "等待 MES 后台循环退出时捕获到异常，已忽略");
            }

            CleanupSocket("客户端释放");

            _cts?.Dispose();
            _cts = null;
            _sendLock.Dispose();

            logger.Info("MES 客户端已释放");
        }
    }
}
