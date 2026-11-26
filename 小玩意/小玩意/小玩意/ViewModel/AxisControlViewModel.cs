using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NLog;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using 小玩意.Zmotion;

namespace 小玩意.ViewModel
{
    /// <summary>
    /// 轴控制页面的视图模型。
    /// 作用：连接正运动控制器、维护轴列表与实时位置，并把界面上的点动/定位/回零动作转成 SDK 调用。
    /// 目的：界面只负责画按钮，所有「能不能动、动了会怎样」的判断都收在这里；
    ///      否则每个按钮的事件里都要重复判连接、判使能，很容易漏掉某一个。
    /// </summary>
    public partial class AxisControlViewModel : ObservableObject, IDisposable
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 控制器实例。
        /// 作用：持有与正运动控制器的连接。
        /// 目的：IP 被修改后旧句柄就失效了，所以这里允许重建；不能声明成只读。
        /// </summary>
        private ZmotionContoller? _controller;

        /// <summary>
        /// 位置刷新定时器。
        /// 作用：周期性读取各轴位置与使能状态。
        /// 目的：位置必须实时刷新，操作员点动时才能看到轴在动；
        ///      但也不能太快，150ms 是显示流畅与通讯负担之间的折中值。
        /// </summary>
        private readonly DispatcherTimer _refreshTimer;

        /// <summary>轴列表</summary>
        public ObservableCollection<AxisItem> Axes { get; } = new();

        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }
        public ICommand EnableCommand { get; }
        public ICommand DisableCommand { get; }
        public ICommand JogForwardCommand { get; }
        public ICommand JogBackwardCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand MoveCommand { get; }
        public ICommand HomeCommand { get; }

        private string _controllerIp = "192.168.0.100";

        /// <summary>
        /// 控制器 IP。
        /// 作用：连接时使用，也允许操作员按现场情况修改。
        /// 目的：不同产线的控制器网段不一样，写死在代码里意味着每个现场都要重新编译。
        /// </summary>
        public string ControllerIp
        {
            get => _controllerIp;
            set => SetProperty(ref _controllerIp, value);
        }

        private AxisItem? _selectedAxis;

        /// <summary>当前选中的轴，右侧操作面板的所有动作都作用于它</summary>
        public AxisItem? SelectedAxis
        {
            get => _selectedAxis;
            set
            {
                if (!SetProperty(ref _selectedAxis, value)) return;

                OnPropertyChanged(nameof(SelectedAxisTitle));
                MoveTarget = value?.Position ?? 0f;
                NotifyCommandsCanExecute();
            }
        }

        public string SelectedAxisTitle => SelectedAxis is null ? "未选择轴" : $"轴 {SelectedAxis.AxisNo}";

        private bool _isConnected;

        /// <summary>控制器是否已连接</summary>
        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (!SetProperty(ref _isConnected, value)) return;

                OnPropertyChanged(nameof(ConnectionBadgeText));
                NotifyCommandsCanExecute();
            }
        }

        /// <summary>连接状态徽标文字</summary>
        public string ConnectionBadgeText => IsConnected ? "已连接" : "未连接";

        private string _statusText = "未连接控制器";

        /// <summary>底部状态提示，动作结果都写在这里</summary>
        public string StatusText
        {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
        }

        private float _jogSpeed = 10f;

        /// <summary>点动与定位使用的速度</summary>
        public float JogSpeed
        {
            get => _jogSpeed;
            set => SetProperty(ref _jogSpeed, value);
        }

        private float _acceleration = 100f;

        /// <summary>加速度与减速度（正运动默认两者分开设置，这里界面共用一个值）</summary>
        public float Acceleration
        {
            get => _acceleration;
            set => SetProperty(ref _acceleration, value);
        }

        private float _moveTarget;

        /// <summary>绝对定位的目标坐标</summary>
        public float MoveTarget
        {
            get => _moveTarget;
            set => SetProperty(ref _moveTarget, value);
        }

        private int _homeMode = 1;

        /// <summary>
        /// 回零模式。
        /// 作用：传给控制器的回零方式编号。
        /// 目的：模式与驱动器接线、原点开关类型强相关，各机型取值不同，
        ///      所以做成可填参数而不是写死；填错会朝错误方向找原点，可能撞限位。
        /// </summary>
        public int HomeMode
        {
            get => _homeMode;
            set => SetProperty(ref _homeMode, value);
        }

        public AxisControlViewModel()
        {
            CreateAxes();

            ConnectCommand = new RelayCommand(Connect, () => !IsConnected);
            DisconnectCommand = new RelayCommand(Disconnect, () => IsConnected);
            EnableCommand = new RelayCommand(() => SetEnable(true), () => CanOperateAxis);
            DisableCommand = new RelayCommand(() => SetEnable(false), () => CanOperateAxis);
            JogForwardCommand = new RelayCommand(() => Jog(1), () => CanOperateAxis);
            JogBackwardCommand = new RelayCommand(() => Jog(-1), () => CanOperateAxis);
            StopCommand = new RelayCommand(Stop, () => IsConnected);
            MoveCommand = new RelayCommand(MoveAbsolute, () => CanOperateAxis);
            HomeCommand = new RelayCommand(Home, () => CanOperateAxis);

            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
            _refreshTimer.Tick += (_, __) => RefreshAxisValues();

            // 默认选中第一个轴，避免打开页面后右侧一片空白，操作员还得先点一下
            SelectedAxis = Axes.FirstOrDefault();
        }

        /// <summary>是否可以对轴下发动作：已连接且选中了轴</summary>
        private bool CanOperateAxis => IsConnected && SelectedAxis is not null;

        /// <summary>
        /// 创建轴条目。
        /// 作用：按固定数量生成轴列表。
        /// 目的：正运动控制器最多支持 8 轴，先按 8 个全部列出，
        ///      未接电机的轴读状态会失败并显示"未连接"，操作员一眼能看出哪几轴是空的。
        /// </summary>
        private void CreateAxes()
        {
            for (int i = 0; i < 8; i++)
            {
                Axes.Add(new AxisItem { AxisNo = i });
            }
        }

        /// <summary>
        /// 连接控制器。
        /// 作用：按当前 IP 建立连接，成功后启动位置刷新。
        /// 目的：连接失败不弹模态框，只把原因写到状态栏——轴控制页是常驻调试页，
        ///      弹窗会挡住操作，而且连不上时操作员往往要反复试几次。
        /// </summary>
        private void Connect()
        {
            if (string.IsNullOrWhiteSpace(ControllerIp))
            {
                StatusText = "请先填写控制器 IP";
                return;
            }

            // IP 改过就换一个新实例：句柄是跟着 IP 走的，复用旧句柄会连到原来的控制器上
            if (_controller is null || !string.Equals(_controller.Ip, ControllerIp, StringComparison.Ordinal))
            {
                _controller?.Dispose();
                _controller = new ZmotionContoller(ControllerIp);

                // 轴号与轴的对应关系在建连接前就要设给界面，避免连上后第一帧读到旧值
                SelectedAxis ??= Axes.FirstOrDefault();
            }

            StatusText = $"正在连接 {ControllerIp} …";

            if (_controller.Connect())
            {
                IsConnected = true;
                StatusText = $"已连接 {ControllerIp}";
                _refreshTimer.Start();
            }
            else
            {
                IsConnected = false;
                StatusText = $"连接失败：{ControllerIp}。请检查 IP、网线与驱动库（zauxdll.dll）是否就位。";
            }
        }

        /// <summary>断开控制器连接</summary>
        private void Disconnect()
        {
            _refreshTimer.Stop();
            _controller?.Disconnect();
            IsConnected = false;

            foreach (AxisItem axis in Axes)
            {
                axis.StatusText = "未连接";
                axis.IsEnabled = false;
            }

            StatusText = "已断开控制器";
        }

        /// <summary>
        /// 刷新所有轴的位置与使能状态。
        /// 作用：定时把 SDK 里的实时值同步到界面。
        /// 目的：读取失败时不清零、保留上一次的值，是因为偶尔一次通讯超时很常见，
        ///      清成 0 会让位置显示来回跳，操作员会误以为轴在动。
        /// </summary>
        private void RefreshAxisValues()
        {
            if (_controller is not { IsConnected: true }) return;

            foreach (AxisItem axis in Axes)
            {
                if (_controller.TryGetPosition(axis.AxisNo, out float position))
                {
                    axis.Position = position;
                }

                if (_controller.TryGetAxisEnable(axis.AxisNo, out bool enabled))
                {
                    axis.IsEnabled = enabled;
                    axis.StatusText = enabled ? "已使能" : "未使能";
                }
                else
                {
                    axis.StatusText = "无响应";
                }
            }
        }

        /// <summary>
        /// 上/下使能。
        /// 作用：对当前选中的轴切换伺服使能状态。
        /// 目的：使能是所有运动的前置条件；未使能下发运动指令控制器不会动作也不报错，
        ///      表现为"点了没反应"，所以这里失败时要明确提示。
        /// </summary>
        private void SetEnable(bool enable)
        {
            if (!EnsureReady(out AxisItem axis)) return;

            bool ok = _controller!.SetAxisEnable(axis.AxisNo, enable);
            axis.IsEnabled = enable;
            axis.StatusText = enable ? "已使能" : "未使能";

            StatusText = ok
                ? $"轴 {axis.AxisNo} {(enable ? "上使能" : "下使能")}成功"
                : $"轴 {axis.AxisNo} {(enable ? "上使能" : "下使能")}失败";
        }

        /// <summary>
        /// 点动。
        /// 作用：让选中轴按方向持续运动。
        /// 目的：每次点动前重设速度与加减速，是为了让界面上填的参数立即生效；
        ///      只在连接时设一次的话，操作员改了速度却要重连才生效，很容易误判成参数没起作用。
        /// </summary>
        private void Jog(int direction)
        {
            if (!EnsureReady(out AxisItem axis)) return;

            _controller!.SetSpeed(axis.AxisNo, JogSpeed);
            _controller.SetAccel(axis.AxisNo, Acceleration);
            _controller.SetDecel(axis.AxisNo, Acceleration);

            bool ok = _controller.Jog(axis.AxisNo, direction);
            StatusText = ok
                ? $"轴 {axis.AxisNo} 正在{(direction > 0 ? "正向" : "负向")}点动，点「停止」结束"
                : $"轴 {axis.AxisNo} 点动失败";
        }

        /// <summary>停止当前轴的运动</summary>
        private void Stop()
        {
            if (SelectedAxis is null) return;

            bool ok = _controller?.Stop(SelectedAxis.AxisNo) == true;
            StatusText = ok ? $"轴 {SelectedAxis.AxisNo} 已停止" : $"轴 {SelectedAxis.AxisNo} 停止失败";
        }

        /// <summary>绝对定位到目标坐标</summary>
        private void MoveAbsolute()
        {
            if (!EnsureReady(out AxisItem axis)) return;

            _controller!.SetSpeed(axis.AxisNo, JogSpeed);
            _controller.SetAccel(axis.AxisNo, Acceleration);
            _controller.SetDecel(axis.AxisNo, Acceleration);

            bool ok = _controller.MoveAbsolute(axis.AxisNo, MoveTarget);
            StatusText = ok
                ? $"轴 {axis.AxisNo} 正在移动到 {MoveTarget:F3}"
                : $"轴 {axis.AxisNo} 定位失败";
        }

        /// <summary>执行回零</summary>
        private void Home()
        {
            if (!EnsureReady(out AxisItem axis)) return;

            bool ok = _controller!.Home(axis.AxisNo, HomeMode);
            StatusText = ok
                ? $"轴 {axis.AxisNo} 开始回零（模式 {HomeMode}）"
                : $"轴 {axis.AxisNo} 回零失败，请确认回零模式 {HomeMode} 是否适用于该机型";
        }

        /// <summary>
        /// 动作前的统一检查。
        /// 作用：确认已连接、选中了轴、且该轴已经使能。
        /// 目的：这三项是下发运动指令的必要条件，任何一项不满足都会让动作静默失效；
        ///      在这里一次性给出明确提示，比在每个动作里各判一遍更不容易漏。
        /// </summary>
        private bool EnsureReady(out AxisItem axis)
        {
            axis = SelectedAxis!;

            if (_controller is not { IsConnected: true })
            {
                StatusText = "控制器未连接";
                return false;
            }

            if (SelectedAxis is null)
            {
                StatusText = "请先在左侧选择一个轴";
                return false;
            }

            if (!SelectedAxis.IsEnabled)
            {
                StatusText = $"轴 {SelectedAxis.AxisNo} 尚未使能，请先点「上使能」";
                return false;
            }

            return true;
        }

        /// <summary>通知所有命令重新求值可用状态</summary>
        private void NotifyCommandsCanExecute()
        {
            (ConnectCommand as RelayCommand)?.NotifyCanExecuteChanged();
            (DisconnectCommand as RelayCommand)?.NotifyCanExecuteChanged();
            (EnableCommand as RelayCommand)?.NotifyCanExecuteChanged();
            (DisableCommand as RelayCommand)?.NotifyCanExecuteChanged();
            (JogForwardCommand as RelayCommand)?.NotifyCanExecuteChanged();
            (JogBackwardCommand as RelayCommand)?.NotifyCanExecuteChanged();
            (StopCommand as RelayCommand)?.NotifyCanExecuteChanged();
            (MoveCommand as RelayCommand)?.NotifyCanExecuteChanged();
            (HomeCommand as RelayCommand)?.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// 暂停位置刷新。
        /// 作用：页面切走时停掉定时器。
        /// 目的：主窗口会缓存页面，切走并不会销毁实例；定时器不停就会在看不见的页面上
        ///      继续占用控制器通讯，导致其他页面响应变慢。
        /// </summary>
        public void PauseRefresh() => _refreshTimer.Stop();

        /// <summary>
        /// 恢复位置刷新。
        /// 作用：页面重新显示时，如果仍处于连接状态就重新开始轮询。
        /// 目的：与 PauseRefresh 配对，保证来回切页面时刷新能自动恢复，
        ///      不需要操作员重新点一次连接。
        /// </summary>
        public void ResumeRefresh()
        {
            if (IsConnected) _refreshTimer.Start();
        }

        /// <summary>
        /// 释放资源。
        /// 作用：停掉刷新定时器并关闭控制器连接。
        /// 目的：页面切走后定时器如果还在跑，会持续占用控制器连接数；
        ///      连接不释放的话，反复进出页面最终会导致控制器拒绝新连接。
        /// </summary>
        public void Dispose()
        {
            _refreshTimer.Stop();
            _controller?.Dispose();
            _controller = null;
            IsConnected = false;
            logger.Info("轴控制页面已释放资源");
        }
    }

    /// <summary>
    /// 轴列表中的一项。
    /// 作用：承载单个轴的编号、实时位置、使能状态。
    /// 目的：位置需要每秒刷新多次，做成可通知对象后界面才能自动更新；
    ///      直接用 float 数组绑不上去。
    /// </summary>
    public class AxisItem : ObservableObject
    {
        /// <summary>轴号，从 0 开始，与控制器内部编号一致</summary>
        public int AxisNo { get; init; }

        /// <summary>列表里显示的名称</summary>
        public string DisplayName => $"轴 {AxisNo}";

        private float _position;

        /// <summary>当前位置</summary>
        public float Position
        {
            get => _position;
            set => SetProperty(ref _position, value);
        }

        private bool _isEnabled;

        /// <summary>是否已使能</summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        private string _statusText = "未连接";

        /// <summary>状态文字</summary>
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }
    }
}
