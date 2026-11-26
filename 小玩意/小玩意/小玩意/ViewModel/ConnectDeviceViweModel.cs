using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NLog;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using 小玩意.Comm;
using 小玩意.Model;
using 小玩意.PlcModel;

namespace 小玩意.ViewModel
{
    /// <summary>
    /// 连接设备页的视图模型。
    /// 作用：构建左侧导航树（PLC 品牌 / 机器人 / 通用通讯），并驱动右侧的品牌详情面板。
    /// 目的：让「有哪些设备可连」由代码里的品牌类决定，界面只负责展示；
    ///      这样新增一个 PLC 品牌时，界面完全不用改。
    /// </summary>
    public partial class ConnectDeviceViweModel : ObservableObject
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 已建立的西门子连接实例。
        /// 作用：保存连接对象，供页面切换后继续使用。
        /// 目的：S7CommViweModel 会读取这个静态列表来刷新点位，改成实例字段会导致那边拿不到数据。
        /// </summary>
        public static List<Siemens>? _siemens = new List<Siemens>();

        /// <summary>
        /// 配置是否已加载。
        /// 作用：保证 InitDevice 只执行一次。
        /// 目的：InitDevice 是往静态列表里追加数据的，页面每打开一次就 new 一次，
        ///      设备数量会成倍膨胀（这也是原来界面里设备重复的根源）。
        /// </summary>
        private static bool _configLoaded;

        /// <summary>左侧导航的三个分组</summary>
        public ObservableCollection<DeviceNavItem> NavGroups { get; } = new();

        /// <summary>连接当前选中品牌的命令</summary>
        public ICommand ConnectCommand { get; }

        private DeviceNavItem? _selectedItem;

        /// <summary>
        /// 当前选中的导航项。
        /// 作用：右侧面板的所有内容都跟着它变。
        /// 目的：把「选中了什么」保存在视图模型里，界面切换、刷新时才不会丢失当前上下文。
        /// </summary>
        public DeviceNavItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (!SetProperty(ref _selectedItem, value)) return;

                RaiseSelectionChanged();
                WakeUpPlcDetail();
            }
        }

        public ConnectDeviceViweModel()
        {
            LoadConfigOnce();
            BuildNavigation();

            ConnectCommand = new RelayCommand(ConnectSelected, () => IsPlcSelected);

            logger.Info("连接设备页初始化完成");
        }

        #region 右侧面板的派生属性

        /// <summary>是否有选中项，用来在「空状态提示」和「详情面板」之间切换</summary>
        public bool HasSelection => SelectedItem is not null;

        public string SelectedTitle => SelectedItem?.Title ?? "未选择设备";

        public string SelectedSubtitle => SelectedItem?.Subtitle ?? string.Empty;

        public string SelectedProtocol => SelectedItem?.Protocol ?? string.Empty;

        public string SelectedDescription => SelectedItem?.Description ?? "从左侧选择一项，查看它的通讯方式和设备清单。";

        /// <summary>导航徽标文字，右侧面板顶部也复用一个，保持视觉一致</summary>
        public string SelectedBadge => SelectedItem?.Badge ?? "--";

        /// <summary>选中的是不是 PLC 品牌（决定「连接」按钮是否可用）</summary>
        public bool IsPlcSelected => SelectedItem?.Kind == DeviceNavKind.Plc;

        /// <summary>选中的是分组标题</summary>
        public bool IsGroupSelected => SelectedItem?.Kind == DeviceNavKind.Group;

        /// <summary>
        /// 该品牌是否已有通讯实现。
        /// 作用：控制右侧面板显示「可以连接」还是「通讯类待实现」。
        /// </summary>
        public bool HasCommunicationImpl => SelectedItem?.Brand?.HasCommunicationImpl == true;

        public string ImplementationText => SelectedItem switch
        {
            { Kind: DeviceNavKind.Group } => "分组",
            { Brand.HasCommunicationImpl: true } => "通讯类已实现",
            { Kind: DeviceNavKind.Plc } => "通讯类待实现",
            { Kind: DeviceNavKind.Robot } => "由 RobotStudio SDK 提供",
            _ => "通用通讯"
        };

        private string _connectionStateText = "未连接";

        /// <summary>连接状态文字，连接动作结束后更新</summary>
        public string ConnectionStateText
        {
            get => _connectionStateText;
            private set => SetProperty(ref _connectionStateText, value);
        }

        /// <summary>导航栏底部的统计文字</summary>
        public string NavSummary
        {
            get
            {
                int brandCount = NavGroups.Where(g => g.Kind == DeviceNavKind.Group)
                                          .SelectMany(g => g.Children)
                                          .Count();
                return $"{brandCount} 个品牌 / {NavGroups.Count} 类设备";
            }
        }

        #endregion

        #region 导航构建

        /// <summary>
        /// 构建左侧导航树。
        /// 作用：把 PLC 品牌、机器人、通用通讯三类内容装进三个可展开的分组。
        /// 目的：品牌清单来自 PlcBrandCatalog 的反射结果，所以这里不需要写死任何品牌名，
        ///      新增品牌类后导航会自动多一项。
        /// </summary>
        private void BuildNavigation()
        {
            NavGroups.Clear();

            // PLC 分组：内容完全由实现了 IPlcCommunicationModel 的类决定
            // 先把品牌清单取出来，因为分组标题上的数量要在创建时一次性写进去（Subtitle 是 init-only）
            List<PlcBrandInfo> brands = PlcBrandCatalog.Discover();

            var plcGroup = new DeviceNavItem
            {
                Kind = DeviceNavKind.Group,
                Title = "PLC 设备",
                Badge = "PLC",
                Subtitle = $"{brands.Count} 个品牌",
                Protocol = "品牌导航",
                Description = "按品牌展开，选择具体 PLC 后可在右侧查看连接参数与设备清单。",
                IsExpanded = true
            };

            foreach (PlcBrandInfo brand in brands)
            {
                plcGroup.Children.Add(new DeviceNavItem
                {
                    Kind = DeviceNavKind.Plc,
                    Title = brand.DisplayName,
                    Subtitle = brand.Protocol,
                    Badge = brand.Badge,
                    Protocol = brand.Protocol,
                    Description = brand.Description,
                    Brand = brand
                });
            }

            // 机器人分组：机器人不是 PLC，走 RobotStudio SDK，单独归一类
            var robotGroup = new DeviceNavItem
            {
                Kind = DeviceNavKind.Group,
                Title = "机器人",
                Badge = "ABB",
                Subtitle = "1 台设备",
                Protocol = "RobotStudio SDK",
                Description = "ABB 机器人通过 RobotStudio SDK 通讯，不参与 PLC 的点位轮询。",
                IsExpanded = true
            };

            robotGroup.Children.Add(new DeviceNavItem
            {
                Kind = DeviceNavKind.Robot,
                Title = "ABB 机器人",
                Subtitle = "RobotStudio SDK",
                Badge = "ABB",
                Protocol = "RobotStudio SDK",
                Description = "支持连接控制器、读写 RAPID 变量与信号；具体点位在机器人页面配置。"
            });

            // 通用通讯分组：这类不是某个品牌，而是协议通道
            var transportGroup = new DeviceNavItem
            {
                Kind = DeviceNavKind.Group,
                Title = "通用通讯",
                Badge = "TCP",
                Subtitle = "2 种方式",
                Protocol = "协议通道",
                Description = "与具体品牌无关的通讯通道，用于对接自定义设备或第三方系统。",
                IsExpanded = true
            };

            transportGroup.Children.Add(new DeviceNavItem
            {
                Kind = DeviceNavKind.Transport,
                Title = "TCP 客户端",
                Subtitle = "文本帧 + 换行分隔",
                Badge = "TCP",
                Protocol = "TCP",
                Description = "以 UTF-8 文本按换行分隔收发，适合对接自定义上位机或 MES 网关。"
            });

            transportGroup.Children.Add(new DeviceNavItem
            {
                Kind = DeviceNavKind.Transport,
                Title = "Modbus RTU",
                Subtitle = "串口通讯",
                Badge = "485",
                Protocol = "Modbus RTU",
                Description = "基于串口的 Modbus 通讯，需指定串口号、波特率、校验位与停止位。"
            });

            NavGroups.Add(plcGroup);
            NavGroups.Add(robotGroup);
            NavGroups.Add(transportGroup);
        }

        #endregion

        #region 选中与连接

        private S7CommViweModel? _plcDetail;

        /// <summary>
        /// PLC 详细信息面板的视图模型。
        /// 作用：承载下方 PlcDetailPanel 要显示的设备列表与实时点位。
        /// 目的：只在选中 PLC 品牌时才创建——它内部会启动常驻读取循环，
        ///      提前创建等于让它在后台空转、白占一个 PLC 连接。
        /// </summary>
        public S7CommViweModel? PlcDetail
        {
            get => _plcDetail;
            private set => SetProperty(ref _plcDetail, value);
        }

        /// <summary>
        /// 当前选中的是不是西门子品牌。
        /// 作用：决定下方是显示详细信息面板，还是显示"暂无数据"的提示。
        /// 目的：其余品牌的通讯类还没实现，点位数据无从获取；
        ///      硬显示一个空面板会让操作员以为连上了却读不到数，反而更难判断。
        /// </summary>
        public bool IsSiemensSelected
            => SelectedItem is { Kind: DeviceNavKind.Plc } && SelectedItem.Brand?.ModelType == typeof(SiemensPlc);

        /// <summary>
        /// 唤醒或收起 PLC 详细信息面板。
        /// 作用：选中西门子品牌时创建并启动详情模型；切到别的品牌时停掉并释放。
        /// 目的：详情模型里有 while 常驻读取循环，必须在切走时停掉，
        ///      否则每切换一次品牌就多一个循环，互相抢同一个 PLC 连接，读出来的值会来回跳。
        /// </summary>
        private void WakeUpPlcDetail()
        {
            OnPropertyChanged(nameof(IsSiemensSelected));

            if (!IsSiemensSelected)
            {
                _plcDetail?.Stop();
                PlcDetail = null;
                return;
            }

            // 只在没有实例时创建；已有实例继续用，避免同品牌内来回切换反复起循环
            PlcDetail ??= new S7CommViweModel();
        }

        /// <summary>
        /// 选中项变化后，通知所有派生属性刷新。
        /// 作用：一次性把右侧面板依赖的属性都标脏。
        /// 目的：派生属性是计算得到的，不会自动收到变更通知；
        ///      漏掉任何一个，界面就会出现「标题变了但说明还是上一个品牌」这种错位。
        /// </summary>
        private void RaiseSelectionChanged()
        {
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(SelectedTitle));
            OnPropertyChanged(nameof(SelectedSubtitle));
            OnPropertyChanged(nameof(SelectedProtocol));
            OnPropertyChanged(nameof(SelectedDescription));
            OnPropertyChanged(nameof(SelectedBadge));
            OnPropertyChanged(nameof(IsPlcSelected));
            OnPropertyChanged(nameof(IsGroupSelected));
            OnPropertyChanged(nameof(HasCommunicationImpl));
            OnPropertyChanged(nameof(ImplementationText));
            // 切换品牌后状态归零，否则会显示上一个品牌的连接结果，容易误判
            ConnectionStateText = IsPlcSelected ? "未连接" : "—";

            (ConnectCommand as RelayCommand)?.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// 连接当前选中的品牌。
        /// 作用：按品牌分派到各自的连接逻辑。
        /// 目的：界面上只有一个连接按钮，避免为每个品牌各做一个按钮；
        ///      新增品牌时只需要在这里加一条分支，界面不用动。
        /// </summary>
        private void ConnectSelected()
        {
            if (SelectedItem?.Brand is not { } brand) return;

            if (!brand.HasCommunicationImpl)
            {
                // 作用：没有通讯实现时明确告知，而不是让按钮静默无反应。
                // 目的：操作员点了没反应会以为程序卡死，进而反复点击或直接报修。
                ConnectionStateText = "通讯类尚未实现";
                ErrorViewModel.Errornotice($"{brand.DisplayName} 的通讯类尚未实现，暂时无法连接。", true, 1);
                return;
            }

            if (brand.ModelType == typeof(SiemensPlc))
            {
                ConnectSiemens();
                return;
            }

            if (brand.ModelType == typeof(SanLingPlc))
            {
                // 作用：三菱暂不连接，只说明原因。
                // 目的：配置表里没有记录三菱的 IP 和端口（设备表目前只有西门子字段），
                //      硬拿西门子的地址去接三菱会连到错误设备上，比连不上更危险。
                ConnectionStateText = "配置缺少三菱设备";
                ErrorViewModel.Errornotice("配置清单里还没有三菱 PLC 的地址与端口，请先补充配置。", true, 1);
                return;
            }

            ConnectionStateText = "该品牌无连接入口";
        }

        /// <summary>
        /// 连接配置里的全部西门子设备。
        /// 作用：遍历配置表创建通讯对象，并在后台线程执行以免卡住界面。
        /// 目的：Siemens 的构造函数里就会发起连接并同步等待结果，
        ///      放在 UI 线程会直接假死到超时；后台线程连完再回 UI 刷新状态。
        /// </summary>
        private void ConnectSiemens()
        {
            var devices = InitDevice.DeviceModels;
            if (devices is null || devices.Count == 0)
            {
                ConnectionStateText = "配置中没有设备";
                ErrorViewModel.Errornotice("配置清单里没有可用的西门子设备，请检查 ini\\配置文件.xlsx。", true, 1);
                return;
            }

            ConnectionStateText = "正在连接…";

            Task.Factory.StartNew(() =>
            {
                _siemens ??= new List<Siemens>();

                // 先清掉同地址的旧实例，避免反复点击连接后列表里堆出重复连接
                _siemens.RemoveAll(p => devices.Any(d => d.S7Address == p._address));

                foreach (S7Model device in devices)
                {
                    try
                    {
                        _siemens.Add(new Siemens(device.CpuType, device.S7Address!, device.S7Rack, device.S7Slot));
                    }
                    catch (Exception ex)
                    {
                        // 单台失败不中断其余设备，配置里某一台地址写错不该让整条线都连不上
                        logger.Error(ex, $"连接西门子 PLC 失败：{device.S7Address}");
                    }
                }

                int okCount = _siemens.Count(p => p.iSconn);

                // 回到 UI 线程刷新：ObservableCollection 绑定在界面上，跨线程修改会抛异常
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    ConnectionStateText = okCount > 0 ? $"已连接 {okCount} / {devices.Count} 台" : "连接失败";
                });

                logger.Info($"西门子 PLC 连接完成：成功 {okCount} / 共 {devices.Count} 台");
            });
        }

        #endregion

        /// <summary>
        /// 加载配置，仅执行一次。
        /// 作用：调用 InitDevice 读取 ini\配置文件.xlsx。
        /// 目的：InitDevice 会往静态列表里追加，重复执行会让设备数量翻倍；
        ///      同时把异常挡住，配置缺失时页面仍能打开，只是设备清单为空。
        /// </summary>
        private static void LoadConfigOnce()
        {
            if (_configLoaded) return;
            _configLoaded = true;

            try
            {
                _ = new InitDevice();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "读取设备配置失败");
                ErrorViewModel.Errornotice("读取设备配置失败，请检查 ini\\配置文件.xlsx。", true, 1);
            }
        }
    }
}
