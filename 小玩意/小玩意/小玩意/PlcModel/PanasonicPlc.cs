using NLog;
using 小玩意.Comm;

namespace 小玩意.PlcModel
{
    /// <summary>
    /// 松下（Panasonic）PLC。
    /// 作用：保存一台松下 PLC 的连接参数，并负责产出与它通讯的通讯对象。
    /// 目的：界面层只面向 IPlcCommunicationModel 编程，每新增一个品牌只多一个这样的类，
    ///      界面、轮询、报警逻辑一行都不用改；否则每接一个品牌就要在界面里加一段 if 分支。
    /// 协议：FP 系列常用 MEWTOCOL-TCP。本项目已引用 HslCommunication，
    ///      对应的通讯类是 HslCommunication.Profinet.Panasonic.PanasonicMcNet
    ///      （Comm/SanLing.cs 目前实际用的就是它，类名和品牌对不上，属于待整理的历史包袱）。
    /// </summary>
    internal class PanasonicPlc : IPlcCommunicationModel
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>PLC 的 IP 地址</summary>
        public string Ip { get; }

        /// <summary>
        /// 通讯端口。
        /// 作用：指定 TCP 连接端口。
        /// 目的：5007 是松下 MEWTOCOL-TCP 的常用端口，但现场可能改过；
        ///      做成可传参而不是写死常量，换机型或换配置时不用改代码。
        /// </summary>
        public int Port { get; }

        /// <summary>站号：多台 PLC 挂在同一网络时用来区分设备</summary>
        public short Station { get; }

        /// <summary>
        /// 初始化松下 PLC 的连接参数。
        /// 作用：保存 IP、端口、站号，并在入口处拦掉空 IP。
        /// 目的：参数在构造时一次定好并只读，避免运行过程中被误改，
        ///      那种情况的表现是"连接正常但读写全错"，非常难排查。
        /// </summary>
        /// <param name="ip">PLC 的 IP 地址</param>
        /// <param name="port">通讯端口，默认 5007</param>
        /// <param name="station">站号，默认 1</param>
        public PanasonicPlc(string ip, int port = 5007, short station = 1)
        {
            if (string.IsNullOrWhiteSpace(ip))
            {
                throw new ArgumentException("松下 PLC 的 IP 不能为空", nameof(ip));
            }

            Ip = ip;
            Port = port;
            Station = station;
        }

        /// <summary>
        /// 获取本品牌的通讯对象。
        /// 作用：把连接参数转成上层可以直接使用的通讯对象集合。
        /// 目的：上层只需要「拿到一串 IPlccommiunctionCommand 去轮询」，
        ///      不必知道底下是松下的 MEWTOCOL 还是别的什么协议。
        /// </summary>
        /// <returns>通讯对象集合；当前版本返回空集合</returns>
        public List<IPlccommiunctionCommand> GetPlcModels()
        {
            // 现状：松下的通讯实现还没独立出来，需要先把 Comm/SanLing.cs 里那段
            // PanasonicMcNet 逻辑拆成独立的松下通讯类，这里才能产出真实对象。
            // 先返回空集合而不是抛异常，是为了让只接了其他品牌的现场仍能正常启动，
            // 不至于因为某个品牌没写完就整个程序起不来。
            logger.Warn($"松下 PLC（{Ip}:{Port}，站号 {Station}）的通讯类尚未实现，本次未产生通讯对象");
            return new List<IPlccommiunctionCommand>();
        }
    }
}
