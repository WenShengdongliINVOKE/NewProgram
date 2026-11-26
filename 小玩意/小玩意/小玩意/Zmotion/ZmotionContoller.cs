using cszmcaux;
using NLog;
using System.IO.Ports;
using System.Text;

namespace 小玩意.Zmotion
{
    /// <summary>
    /// 正运动（Zmotion）控制器通讯类。
    /// 作用：封装控制器的连接与单轴操作（读位置、使能、点动、绝对定位、回零、停止）。
    /// 目的：SDK 是 C 风格接口，所有函数都要传句柄、靠返回码判断成败、失败也不抛异常；
    ///      如果让界面直接调，每个按钮都得自己判返回码和异常，很快会写漏。
    ///      统一收在这一层之后，界面只需要看 bool。
    /// </summary>
    internal sealed class ZmotionContoller : IDisposable
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 控制器句柄。
        /// 作用：后续每一个轴操作都要把它传给 SDK。
        /// 目的：SDK 没有对象概念，一切都挂在这个句柄上；句柄为 Zero 时调任何轴函数
        ///      都只会静默返回错误码，所以每次操作前必须配合 IsConnected 一起判断。
        /// </summary>
        private IntPtr _handle = IntPtr.Zero;

        /// <summary>SDK 返回码 0 表示成功，其余一律视为失败</summary>
        private const int SuccessCode = 0;

        /// <summary>控制器 IP 地址</summary>
        public string Ip { get; }

        /// <summary>
        /// 控制器端口。
        /// 作用：记录调用方传入的端口，便于日志排查。
        /// 目的：正运动的以太网连接函数只接受 IP，端口由 SDK 内部固定；
        ///      保留这个字段是为了兼容原有构造签名，不参与连接。
        /// </summary>
        public int Port { get; }

        /// <summary>当前是否已连上控制器</summary>
        public bool IsConnected { get; private set; }

        private readonly int _baudRate;
        private readonly string _portName = string.Empty;
        private readonly Parity _parity;
        private readonly StopBits _stopBits;
        private readonly int _dataBits;

        /// <summary>
        /// 初始化以太网连接参数。
        /// 作用：只保存 IP 与端口，不发起连接。
        /// 目的：原实现在构造函数里直接连接且完全不看返回码，连不上时对象也是"成功"创建的，
        ///      调用方无法判断状态。改成显式 Connect() 后，连接成败一目了然。
        /// </summary>
        /// <param name="ip">控制器 IP</param>
        /// <param name="port">端口，仅记录不参与连接</param>
        public ZmotionContoller(string ip, int port = 0)
        {
            Ip = ip;
            Port = port;
        }

        /// <summary>
        /// 初始化串口连接参数。
        /// 作用：保存串口参数并设置 SDK 的默认串口波特率。
        /// 目的：部分老机型只支持串口；当前项目走以太网，这里保留能力但未实现打开串口，
        ///      避免以后要接串口时再改构造签名影响调用方。
        /// </summary>
        public ZmotionContoller(int baudRate, int dataBits, Parity parity, StopBits stopBits)
        {
            _baudRate = baudRate;
            _dataBits = dataBits;
            _parity = parity;
            _stopBits = stopBits;
            Ip = string.Empty;
            zmcaux.ZAux_SetComDefaultBaud((uint)_baudRate, (uint)_dataBits, (uint)_parity, (uint)_stopBits);
        }

        #region 连接与释放

        /// <summary>
        /// 连接控制器。
        /// 作用：按 IP 建立以太网连接并保存句柄。
        /// 目的：把"连不上"和"连上了但后面操作失败"区分开——
        ///      现场排查时这两种情况的原因完全不同，混在一起会浪费大量时间。
        /// </summary>
        /// <returns>true 表示连接成功</returns>
        public bool Connect()
        {
            if (IsConnected) return true;

            if (string.IsNullOrWhiteSpace(Ip))
            {
                logger.Warn("正运动控制器连接被忽略：IP 为空");
                return false;
            }

            try
            {
                int code = zmcaux.ZAux_OpenEth(Ip, out IntPtr handle);
                if (code != SuccessCode)
                {
                    logger.Error($"连接正运动控制器失败：{Ip}，返回码 {code}");
                    _handle = IntPtr.Zero;
                    IsConnected = false;
                    return false;
                }

                _handle = handle;
                IsConnected = true;
                logger.Info($"正运动控制器连接成功：{Ip}");
                return true;
            }
            catch (DllNotFoundException ex)
            {
                // 作用：单独识别 dll 缺失，给出可执行的提示。
                // 目的：这个异常的表现和"网络连不上"完全一样（都是连接失败），
                //      但原因完全不同；不点明的话现场会去查网线和 IP，白费工夫。
                logger.Error(ex, "找不到 zauxdll.dll，请确认运行目录下已包含 Zmotion 的驱动库");
                IsConnected = false;
                return false;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"连接正运动控制器异常：{Ip}");
                IsConnected = false;
                return false;
            }
        }

        /// <summary>
        /// 断开连接。
        /// 作用：关闭 SDK 句柄并复位状态。
        /// 目的：句柄不关闭会一直占用控制器的连接数，反复重连后控制器会拒绝新连接。
        /// </summary>
        public void Disconnect()
        {
            if (_handle != IntPtr.Zero)
            {
                try
                {
                    zmcaux.ZAux_Close(_handle);
                }
                catch (Exception ex)
                {
                    logger.Debug(ex, "关闭正运动控制器句柄时异常，已忽略");
                }
            }

            _handle = IntPtr.Zero;
            IsConnected = false;
            logger.Info($"正运动控制器已断开：{Ip}");
        }

        /// <summary>释放资源，等同断开连接</summary>
        public void Dispose() => Disconnect();

        #endregion

        #region 轴操作

        /// <summary>
        /// 读取轴的当前位置（指令位置）。
        /// 作用：取当前位置用于界面显示。
        /// 目的：用指令位置而不是编码器位置，是因为操作员核对"走到哪了"看的是指令值；
        ///      两者在正常运行时一致，只有在丢步、报警等异常时才出现偏差。
        /// </summary>
        /// <param name="axis">轴号，从 0 开始</param>
        /// <param name="position">读到的位置</param>
        /// <returns>true 表示读取成功</returns>
        public bool TryGetPosition(int axis, out float position)
        {
            position = 0f;

            if (!IsConnected || _handle == IntPtr.Zero)
            {
                return false;
            }

            try
            {
                return zmcaux.ZAux_Direct_GetDpos(_handle, axis, ref position) == SuccessCode;
            }
            catch (Exception ex)
            {
                logger.Debug(ex, $"读取轴 {axis} 位置异常");
                return false;
            }
        }

        /// <summary>
        /// 读取轴使能状态。
        /// 作用：查询该轴当前是否已上使能（伺服是否抱闸松开）。
        /// 目的：未使能就发运动指令，控制器不会动但也不报错，表现为"点了没反应"；
        ///      界面必须能显示真实使能状态，操作员才知道是不是漏了上电。
        /// </summary>
        public bool TryGetAxisEnable(int axis, out bool enabled)
        {
            enabled = false;

            if (!IsConnected || _handle == IntPtr.Zero) return false;

            try
            {
                int value = 0;
                if (zmcaux.ZAux_Direct_GetAxisEnable(_handle, axis, ref value) != SuccessCode) return false;

                enabled = value != 0;
                return true;
            }
            catch (Exception ex)
            {
                logger.Debug(ex, $"读取轴 {axis} 使能状态异常");
                return false;
            }
        }

        /// <summary>
        /// 设置轴使能。
        /// 作用：给轴上下使能（驱动器伺服使能）。
        /// 目的：上使能是运动的前置条件，且在故障复位后必须重新使能，所以单独暴露成方法。
        /// </summary>
        public bool SetAxisEnable(int axis, bool enabled)
            => Execute($"轴 {axis} {(enabled ? "上" : "下")}使能",
                       () => zmcaux.ZAux_Direct_SetAxisEnable(_handle, axis, enabled ? 1 : 0));

        /// <summary>
        /// 设置运行速度。
        /// 作用：设置该轴的运动速度，单位由控制器配置决定（通常为 units/s）。
        /// 目的：速度是轴参数而不是指令参数，设一次就一直生效；
        ///      如果每次运动前不设，用的会是上一次残留的值，容易超速。
        /// </summary>
        public bool SetSpeed(int axis, float speed)
            => Execute($"设置轴 {axis} 速度 {speed}",
                       () => zmcaux.ZAux_Direct_SetSpeed(_handle, axis, speed));

        /// <summary>设置加速度</summary>
        public bool SetAccel(int axis, float accel)
            => Execute($"设置轴 {axis} 加速度 {accel}",
                       () => zmcaux.ZAux_Direct_SetAccel(_handle, axis, accel));

        /// <summary>设置减速度</summary>
        public bool SetDecel(int axis, float decel)
            => Execute($"设置轴 {axis} 减速度 {decel}",
                       () => zmcaux.ZAux_Direct_SetDecel(_handle, axis, decel));

        /// <summary>
        /// 点动（连续运动）。
        /// 作用：让轴按指定方向持续运动，直到调用 Stop。
        /// 目的：点动是调试和手动对位最常用的动作，方向 1 为正、-1 为负；
        ///      它不会自己停，所以界面上必须有对应的停止按钮。
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="direction">方向，1 正向 / -1 负向</param>
        public bool Jog(int axis, int direction)
            => Execute($"轴 {axis} 点动（方向 {direction}）",
                       () => zmcaux.ZAux_Direct_Single_Vmove(_handle, axis, direction));

        /// <summary>
        /// 停止运动。
        /// 作用：以减速方式停止该轴当前的运动。
        /// 目的：用减速停止（模式 2）而不是立即取消（模式 1），
        ///      是为了避免急停带来的机械冲击和丢步；真需要急停的场合应该走硬件急停回路。
        /// </summary>
        public bool Stop(int axis)
            => Execute($"轴 {axis} 停止",
                       () => zmcaux.ZAux_Direct_Single_Cancel(_handle, axis, 2));

        /// <summary>
        /// 绝对定位。
        /// 作用：把轴运动到指定的绝对坐标。
        /// 目的：与点动相对，定位是产线正常动作；目标值必须是绝对坐标，
        ///      传相对距离会走到完全错误的位置，所以方法名里明确写 Absolute。
        /// </summary>
        public bool MoveAbsolute(int axis, float position)
            => Execute($"轴 {axis} 绝对定位到 {position}",
                       () => zmcaux.ZAux_Direct_Single_MoveAbs(_handle, axis, position));

        /// <summary>
        /// 执行回零。
        /// 作用：按控制器内置的回零模式寻找原点。
        /// 目的：回零模式（imode）与驱动器接线、原点开关类型强相关，不同机型取值不同，
        ///      所以模式由调用方传入而不是写死；传错模式会朝错误方向找原点，可能撞限位。
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="mode">回零模式，取值参考控制器手册</param>
        public bool Home(int axis, int mode)
            => Execute($"轴 {axis} 回零（模式 {mode}）",
                       () => zmcaux.ZAux_Direct_Single_Datum(_handle, axis, mode));

        /// <summary>
        /// 把当前位置设为指定值。
        /// 作用：直接改写指令位置。
        /// 目的：手工对位后需要把当前位置定义成某个坐标（例如零点），
        ///      但这是"软清零"，不经过回零动作；误用会让坐标基准错位，所以只做低层能力，不放到显眼按钮上。
        /// </summary>
        public bool SetPosition(int axis, float position)
            => Execute($"轴 {axis} 位置设为 {position}",
                       () => zmcaux.ZAux_Direct_SetDpos(_handle, axis, position));

        /// <summary>
        /// 读取轴状态字。
        /// 作用：取控制器内部的轴状态位。
        /// 目的：状态字里包含报警、限位、运动中、到位等标志，
        ///      界面据此显示状态，比只看位置能不能变化更早发现问题。
        /// </summary>
        public bool TryGetAxisStatus(int axis, out int status)
        {
            status = 0;

            if (!IsConnected || _handle == IntPtr.Zero) return false;

            try
            {
                return zmcaux.ZAux_Direct_GetAxisStatus(_handle, axis, ref status) == SuccessCode;
            }
            catch (Exception ex)
            {
                logger.Debug(ex, $"读取轴 {axis} 状态异常");
                return false;
            }
        }

        #endregion

        #region 原有辅助方法

        /// <summary>
        /// 设置控制器 IP。
        /// 作用：修改控制器自身的 IP 地址。
        /// 目的：现场更换网段时用；注意改完需要重新连接，当前句柄会失效。
        /// </summary>
        public int SetIP(string ip) => zmcaux.ZAux_SetIp(_handle, ip);

        /// <summary>
        /// 搜索网段内的控制器。
        /// 作用：广播查找在线控制器。
        /// 目的：不知道控制器 IP 时先搜一下，比逐个猜地址快得多。
        /// </summary>
        public int SearchIP()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append('\0', 255);
            return zmcaux.ZAux_SearchEthlist(builder, 255, 1000);
        }

        #endregion

        /// <summary>
        /// 统一执行 SDK 调用。
        /// 作用：检查连接状态、调用 SDK、把返回码翻译成 bool 并记日志。
        /// 目的：SDK 的每个函数都是「返回 0 表示成功、非 0 表示失败且不抛异常」，
        ///      如果每个方法各写一遍判断，漏判一处就会出现"操作失败了但界面没提示"。
        /// </summary>
        /// <param name="actionName">动作名称，用于日志</param>
        /// <param name="sdkCall">实际的 SDK 调用</param>
        private bool Execute(string actionName, Func<int> sdkCall)
        {
            if (!IsConnected || _handle == IntPtr.Zero)
            {
                logger.Warn($"{actionName} 被忽略：控制器未连接");
                return false;
            }

            try
            {
                int code = sdkCall();
                if (code != SuccessCode)
                {
                    logger.Warn($"{actionName} 失败，返回码 {code}");
                    return false;
                }

                logger.Info($"{actionName} 成功");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"{actionName} 异常");
                return false;
            }
        }
    }
}
