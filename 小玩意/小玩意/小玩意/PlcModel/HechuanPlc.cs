using NLog;
using 小玩意.Comm;

namespace 小玩意.PlcModel
{
    /// <summary>
    /// 禾川（Hechuan）PLC。
    /// 作用：保存一台禾川 PLC 的连接参数，并负责产出与它通讯的通讯对象。
    /// 目的：禾川在 HslCommunication 里没有专用通讯类，只能走通用 Modbus TCP；
    ///      把这一点写在品牌类里，上层就不用关心"这个品牌有没有专用驱动"。
    /// 协议：Modbus TCP。使用 HslCommunication.Profinet.ModbusTcp.ModbusTcpNet 即可。
    /// </summary>
    internal class HechuanPlc : IPlcCommunicationModel
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>PLC 的 IP 地址</summary>
        public string Ip { get; }

        /// <summary>
        /// 通讯端口。
        /// 作用：指定 Modbus TCP 连接端口。
        /// 目的：标准端口 502，但禾川部分机型出厂默认不是 502（有的用 5000 段），
        ///      现场必须按实际配置填写，所以不写死。
        /// </summary>
        public int Port { get; }

        /// <summary>站号：Modbus 从站地址，多台设备共用一条链路时用来区分</summary>
        public byte Station { get; }

        /// <summary>
        /// 初始化禾川 PLC 的连接参数。
        /// 作用：保存 IP、端口、站号，并在入口处拦掉空 IP。
        /// 目的：配置错误尽早暴露在初始化阶段，而不是等产线运行时才表现为"读不到数"。
        /// </summary>
        /// <param name="ip">PLC 的 IP 地址</param>
        /// <param name="port">通讯端口，默认 502</param>
        /// <param name="station">从站地址，默认 1</param>
        public HechuanPlc(string ip, int port = 502, byte station = 1)
        {
            if (string.IsNullOrWhiteSpace(ip))
            {
                throw new ArgumentException("禾川 PLC 的 IP 不能为空", nameof(ip));
            }

            Ip = ip;
            Port = port;
            Station = station;
        }

        /// <summary>
        /// 获取本品牌的通讯对象。
        /// 作用：把连接参数转成上层可以直接使用的通讯对象集合。
        /// 目的：与其他品牌保持同一调用方式，上层轮询逻辑不需要为禾川写特例。
        /// </summary>
        /// <returns>通讯对象集合；当前版本返回空集合</returns>
        public List<IPlccommiunctionCommand> GetPlcModels()
        {
            // 现状：禾川通讯类还未实现。实现时用通用 ModbusTcpNet，
            // 按 Modbus 的功能码去读保持寄存器/线圈即可，不需要品牌专用驱动。
            // 先返回空集合而不是抛异常，保证其他品牌已接线的现场不受影响。
            logger.Warn($"禾川 PLC（{Ip}:{Port}，站号 {Station}）的通讯类尚未实现，本次未产生通讯对象");
            return new List<IPlccommiunctionCommand>();
        }
    }
}
