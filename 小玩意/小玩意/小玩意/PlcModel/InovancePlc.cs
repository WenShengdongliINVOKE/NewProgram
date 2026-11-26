using NLog;
using 小玩意.Comm;

namespace 小玩意.PlcModel
{
    /// <summary>
    /// 汇川（Inovance）PLC。
    /// 作用：保存一台汇川 PLC 的连接参数，并负责产出与它通讯的通讯对象。
    /// 目的：与松下、欧姆龙等品牌统一到 IPlcCommunicationModel 之后，
    ///      上层轮询逻辑不区分品牌，只有在需要品牌专有功能时才向下转型。
    /// 协议：AM600 / H5U 等系列以 Modbus TCP 为主。本项目已引用 HslCommunication，
    ///      汇川专用通讯类是 HslCommunication.Profinet.Inovance.InovanceTcpNet，
    ///      它比通用 Modbus 多了汇川的地址软元件解析（如 M127、D100 这类写法）。
    /// </summary>
    internal class InovancePlc : IPlcCommunicationModel
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>PLC 的 IP 地址</summary>
        public string Ip { get; }

        /// <summary>
        /// 通讯端口。
        /// 作用：指定 TCP 连接端口。
        /// 目的：Modbus TCP 标准端口是 502，但汇川部分机型出厂会改成 502 以外的值，
        ///      现场按实际填写；写死常量会导致"同一套程序换个车间就连不上"。
        /// </summary>
        public int Port { get; }

        /// <summary>站号（单元号）：Modbus 从站地址，多台设备共用一条链路时用来区分</summary>
        public byte Station { get; }

        /// <summary>
        /// 初始化汇川 PLC 的连接参数。
        /// 作用：保存 IP、端口、站号，并在入口处拦掉空 IP。
        /// 目的：把参数校验放在构造函数，配置写错时在初始化阶段就报出来，
        ///      而不是等到产线运行时才发现连不上。
        /// </summary>
        /// <param name="ip">PLC 的 IP 地址</param>
        /// <param name="port">通讯端口，默认 502</param>
        /// <param name="station">从站地址，默认 1</param>
        public InovancePlc(string ip, int port = 502, byte station = 1)
        {
            if (string.IsNullOrWhiteSpace(ip))
            {
                throw new ArgumentException("汇川 PLC 的 IP 不能为空", nameof(ip));
            }

            Ip = ip;
            Port = port;
            Station = station;
        }

        /// <summary>
        /// 获取本品牌的通讯对象。
        /// 作用：把连接参数转成上层可以直接使用的通讯对象集合。
        /// 目的：让上层用同一套轮询代码驱动所有品牌，新增品牌不影响既有逻辑。
        /// </summary>
        /// <returns>通讯对象集合；当前版本返回空集合</returns>
        public List<IPlccommiunctionCommand> GetPlcModels()
        {
            // 现状：汇川通讯类还未实现。HslCommunication 已经带了 InovanceTcpNet，
            // 实现时直接用即可，不需要自己拼 Modbus 报文。
            // 先返回空集合而不是抛异常，避免只接了其他品牌的现场因为这里没写完而无法启动。
            logger.Warn($"汇川 PLC（{Ip}:{Port}，站号 {Station}）的通讯类尚未实现，本次未产生通讯对象");
            return new List<IPlccommiunctionCommand>();
        }
    }
}
