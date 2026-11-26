using NLog;
using 小玩意.Comm;

namespace 小玩意.PlcModel
{
    /// <summary>
    /// 欧姆龙（Omron）PLC。
    /// 作用：保存一台欧姆龙 PLC 的连接参数，并负责产出与它通讯的通讯对象。
    /// 目的：欧姆龙走 FINS 协议，地址结构比其他品牌多出网络号、节点号、单元号三层，
    ///      把这些参数收在品牌类里，上层就不必为欧姆龙单独写一套连接代码。
    /// 协议：FINS/TCP。本项目已引用 HslCommunication，
    ///      对应通讯类是 HslCommunication.Profinet.Omron.OmronFinsNet。
    /// </summary>
    internal class OmronPlc : IPlcCommunicationModel
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>PLC 的 IP 地址</summary>
        public string Ip { get; }

        /// <summary>
        /// 通讯端口。
        /// 作用：指定 FINS/TCP 连接端口。
        /// 目的：FINS/TCP 的默认端口是 9600，与常见的 502/5007 完全不同，
        ///      照抄其他品牌的默认值会一直连不上，所以这里单独给默认值。
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// 网络号（FINS 地址第一段）。
        /// 作用：标识设备所在的 FINS 网络。
        /// 目的：单机直连时固定为 0；接入多级网络（如通过 Controller Link 中转）时必须填实际网络号，
        ///      填错的表现是"连接成功但读写目标地址报错"。
        /// </summary>
        public byte NetworkNo { get; }

        /// <summary>
        /// 节点号（FINS 地址第二段）。
        /// 作用：标识网络内的具体设备。
        /// 目的：通常等于 PLC IP 的最后一段（如 192.168.1.10 对应节点号 10），
        ///      由构造方按现场实际情况传入，不在这里猜测。
        /// </summary>
        public byte NodeNo { get; }

        /// <summary>
        /// 单元号（FINS 地址第三段）。
        /// 作用：标识 CPU 单元或扩展单元。
        /// 目的：多数机型 CPU 单元号为 0；接了扩展机架时才需要改，默认 0 即可。
        /// </summary>
        public byte UnitNo { get; }

        /// <summary>
        /// 初始化欧姆龙 PLC 的连接参数。
        /// 作用：保存 IP、端口以及 FINS 的三段地址。
        /// 目的：FINS 地址信息缺一不可，统一在构造时给齐，
        ///      避免实现通讯类时才发现"参数不够用"而回头改所有调用点。
        /// </summary>
        /// <param name="ip">PLC 的 IP 地址</param>
        /// <param name="port">FINS/TCP 端口，默认 9600</param>
        /// <param name="networkNo">网络号，默认 0</param>
        /// <param name="nodeNo">节点号，默认 1</param>
        /// <param name="unitNo">单元号，默认 0</param>
        public OmronPlc(string ip, int port = 9600, byte networkNo = 0, byte nodeNo = 1, byte unitNo = 0)
        {
            if (string.IsNullOrWhiteSpace(ip))
            {
                throw new ArgumentException("欧姆龙 PLC 的 IP 不能为空", nameof(ip));
            }

            Ip = ip;
            Port = port;
            NetworkNo = networkNo;
            NodeNo = nodeNo;
            UnitNo = unitNo;
        }

        /// <summary>
        /// 获取本品牌的通讯对象。
        /// 作用：把连接参数转成上层可以直接使用的通讯对象集合。
        /// 目的：让 FINS 的地址差异停留在品牌类内部，上层轮询代码感知不到。
        /// </summary>
        /// <returns>通讯对象集合；当前版本返回空集合</returns>
        public List<IPlccommiunctionCommand> GetPlcModels()
        {
            // 现状：欧姆龙通讯类还未实现。实现时用 HslCommunication 的 OmronFinsNet，
            // 并把 NetworkNo / NodeNo / UnitNo 依次赋给它对应的属性。
            // 先返回空集合而不是抛异常，保证其他品牌已接线的现场不受影响。
            logger.Warn($"欧姆龙 PLC（{Ip}:{Port}，网络 {NetworkNo}/节点 {NodeNo}/单元 {UnitNo}）的通讯类尚未实现，本次未产生通讯对象");
            return new List<IPlccommiunctionCommand>();
        }
    }
}
