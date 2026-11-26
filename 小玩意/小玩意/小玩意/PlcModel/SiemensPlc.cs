using NLog;
using S7.Net;
using 小玩意.Comm;

namespace 小玩意.PlcModel
{
    /// <summary>
    /// 西门子（Siemens）PLC。
    /// 作用：保存一台西门子 PLC 的连接参数，并负责产出与它通讯的通讯对象。
    /// 目的：把「品牌 + 连接参数 → 通讯对象」这一步从界面层挪到品牌层，
    ///      界面只按 IPlcCommunicationModel 统一处理，以后新增品牌不必再改界面代码。
    /// 协议：S7 协议，由 Comm/Siemens.cs 基于 S7netplus 实现。
    ///      S7 连接必须给齐 CPU 型号、机架号、槽号三项，少一项或填错都连不上。
    /// </summary>
    internal class SiemensPlc : IPlcCommunicationModel
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// CPU 型号。
        /// 作用：告诉通讯层按哪个 S7 子系列的规则去握手。
        /// 目的：1200/1500 与 300/400 的机架槽号取值规则不同，型号填错时
        ///      报错往往只有一句"连接失败"，现场很难判断到底错在哪一项。
        /// </summary>
        public CpuType CpuType { get; }

        /// <summary>PLC 的 IP 地址</summary>
        public string Ip { get; }

        /// <summary>机架号：S7-1200/1500 固定 0，S7-300/400 多数也是 0</summary>
        public short Rack { get; }

        /// <summary>槽号：S7-1200/1500 一般为 1，S7-300/400 视 CPU 实际插槽而定</summary>
        public short Slot { get; }

        /// <summary>
        /// 初始化西门子 PLC 的连接参数。
        /// 作用：保存 CPU 型号、IP、机架号、槽号。
        /// 目的：参数构造时一次定好并只读，避免运行中被改动；
        ///      这类参数被改坏后的表现是"连接正常但读出来的数据全错"，比连不上更难查。
        /// </summary>
        /// <param name="cpuType">CPU 型号</param>
        /// <param name="ip">PLC 的 IP 地址</param>
        /// <param name="rack">机架号，默认 0</param>
        /// <param name="slot">槽号，默认 0</param>
        public SiemensPlc(CpuType cpuType, string ip, short rack = 0, short slot = 0)
        {
            if (string.IsNullOrWhiteSpace(ip))
            {
                throw new ArgumentException("西门子 PLC 的 IP 不能为空", nameof(ip));
            }

            CpuType = cpuType;
            Ip = ip;
            Rack = rack;
            Slot = slot;
        }

        /// <summary>
        /// 获取本品牌的通讯对象。
        /// 作用：用保存的连接参数创建一个 Siemens 通讯实例。
        /// 目的：上层拿到集合就能直接读写，不必自己拼构造参数，也就不会出现
        ///      "界面里传 3 个参数、另一个地方传 4 个"这种不一致。
        /// 注意：Comm/Siemens.cs 的构造函数内部就会发起连接并用 Dispatcher 同步等待结果，
        ///      因此本方法会一直阻塞到连接成败有结论为止。请放在后台线程调用，
        ///      不要直接写在界面按钮的 Click 里，否则界面会假死到超时。
        /// </summary>
        /// <returns>通讯对象集合；创建失败时返回空集合</returns>
        public List<IPlccommiunctionCommand> GetPlcModels()
        {
            try
            {
                var plc = new Siemens(CpuType, Ip, Rack, Slot);
                logger.Info($"西门子 PLC 通讯对象已创建：{Ip}（{CpuType}，机架 {Rack}，槽 {Slot}）");
                return new List<IPlccommiunctionCommand> { plc };
            }
            catch (Exception ex)
            {
                // 作用：创建失败时返回空集合，并把原因记进日志。
                // 目的：一台设备初始化失败不该让整个设备列表崩掉——
                //      同一条产线上还有别的品牌、别的设备要照常工作。
                logger.Error(ex, $"创建西门子 PLC 通讯对象失败：{Ip}");
                return new List<IPlccommiunctionCommand>();
            }
        }
    }
}
