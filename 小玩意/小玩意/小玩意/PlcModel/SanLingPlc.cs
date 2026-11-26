using NLog;
using 小玩意.Comm;

namespace 小玩意.PlcModel
{
    /// <summary>
    /// 三菱（Mitsubishi）PLC。
    /// 作用：保存一台三菱 PLC 的连接参数，并负责产出与它通讯的通讯对象。
    /// 目的：与其他品牌统一到 IPlcCommunicationModel，上层轮询代码不必再按品牌分支。
    /// 协议：MC 协议。
    /// 注意：当前 Comm/SanLing.cs 内部实际实例化的是 HslCommunication 的 PanasonicMcNet
    ///      （松下 MC 协议），与"三菱"这个名字对不上。接真实三菱设备前必须先确认：
    ///      · 若现场是 FX5U / Q 系列三菱，应改用 MelsecMcNet；
    ///      · 若现场确实是松下设备，请直接用 PanasonicPlc 而不是本类。
    ///      这一条不能靠猜，接错协议的表现是"能连上但读出来的值全是 0 或乱码"。
    /// </summary>
    internal class SanLingPlc : IPlcCommunicationModel
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>PLC 的 IP 地址</summary>
        public string Ip { get; }

        /// <summary>
        /// 通讯端口。
        /// 作用：指定 MC 协议的 TCP 端口。
        /// 目的：三菱 MC 协议常用 6000，而松下 MEWTOCOL-TCP 用 5007、Modbus 用 502；
        ///      端口和协议是绑定的，填错的表现是连接直接被拒绝，所以做成参数由现场决定。
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// 初始化三菱 PLC 的连接参数。
        /// 作用：保存 IP 与端口，并在入口处拦掉空 IP。
        /// 目的：配置有误时在初始化阶段就报出来，不必等产线运行时才发现连不上。
        /// </summary>
        /// <param name="ip">PLC 的 IP 地址</param>
        /// <param name="port">MC 协议端口，默认 6000</param>
        public SanLingPlc(string ip, int port = 6000)
        {
            if (string.IsNullOrWhiteSpace(ip))
            {
                throw new ArgumentException("三菱 PLC 的 IP 不能为空", nameof(ip));
            }

            Ip = ip;
            Port = port;
        }

        /// <summary>
        /// 获取本品牌的通讯对象。
        /// 作用：用保存的连接参数创建一个 SanLing 通讯实例。
        /// 目的：让上层用同一套代码驱动所有品牌。
        /// 注意：SanLing 与西门子不同，构造时**不会**连接，必须再调用一次 Connect() 才能读写。
        ///      未连接时其内部 conn 字段为 null，直接调 ReadPlc 会抛空引用异常。
        ///      这里只负责创建对象、不代为连接，是因为连接是网络阻塞操作，
        ///      该在后台线程何时发起应由调用方统一安排，藏在本方法里容易让界面莫名卡顿。
        /// </summary>
        /// <returns>通讯对象集合；创建失败时返回空集合</returns>
        public List<IPlccommiunctionCommand> GetPlcModels()
        {
            try
            {
                var plc = new SanLing(Ip, Port);
                logger.Info($"三菱 PLC 通讯对象已创建：{Ip}:{Port}（需再调用 Connect() 才能读写）");
                return new List<IPlccommiunctionCommand> { plc };
            }
            catch (Exception ex)
            {
                // 作用：创建失败时返回空集合，并把原因记进日志。
                // 目的：单台设备失败不应影响同一产线上的其他设备继续工作。
                logger.Error(ex, $"创建三菱 PLC 通讯对象失败：{Ip}:{Port}");
                return new List<IPlccommiunctionCommand>();
            }
        }
    }
}
