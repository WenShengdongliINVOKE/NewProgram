using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace 小玩意.ViewModel
{
    /// <summary>
    /// 左侧导航树中的一个节点。
    /// 作用：同时充当「分组标题」和「品牌条目」两种角色，供 TreeView 直接绑定。
    /// 目的：导航内容由数据驱动（品牌清单来自反射扫描 IPlcCommunicationModel 实现类），
    ///      界面只负责画，以后新增品牌不必再改 XAML。
    /// </summary>
    public class DeviceNavItem : ObservableObject
    {
        /// <summary>节点类型，决定右侧面板显示哪些内容</summary>
        public DeviceNavKind Kind { get; init; }

        /// <summary>主标题，如「西门子」「通用通讯」</summary>
        public string Title { get; init; } = string.Empty;

        /// <summary>副标题，如「S7 协议」「2 台设备」</summary>
        public string Subtitle { get; init; } = string.Empty;

        /// <summary>
        /// 左侧小方块里的短标记，如「S7」「MC」「TCP」。
        /// 作用：让操作员一眼分辨品牌，不必逐条读文字。
        /// 目的：导航栏宽度有限，用 2~3 个字符比图标更省空间，也不需要额外图片资源。
        /// </summary>
        public string Badge { get; init; } = string.Empty;

        /// <summary>右侧面板的详细说明文字</summary>
        public string Description { get; init; } = string.Empty;

        /// <summary>协议名称，显示在右侧面板的标签上</summary>
        public string Protocol { get; init; } = string.Empty;

        /// <summary>
        /// 对应的品牌信息，仅 Kind 为 Plc 时有值。
        /// 作用：右侧面板据此判断该品牌有没有通讯实现、要不要显示连接按钮。
        /// </summary>
        public PlcBrandInfo? Brand { get; init; }

        /// <summary>子节点。分组节点用它装品牌条目</summary>
        public ObservableCollection<DeviceNavItem> Children { get; } = new();

        /// <summary>
        /// 是否展开。
        /// 作用：与 TreeViewItem.IsExpanded 双向绑定。
        /// 目的：放进模型而不是靠界面状态，是为了让「默认展开 PLC 分组」这种初始状态可设置。
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }
        private bool _isExpanded = true;

        /// <summary>
        /// 是否选中。
        /// 作用：与 TreeViewItem.IsSelected 双向绑定，表示当前在右侧打开的是哪一项。
        /// 目的：TreeView.SelectedItem 是只读依赖属性、无法直接双向绑定，
        ///      绑到模型上才能既知道「谁被选中」，又能由代码把选中项改回去。
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
        private bool _isSelected;

        /// <summary>是否有子节点（XAML 里用来决定要不要画展开箭头）</summary>
        public bool HasChildren => Children.Count > 0;
    }

    /// <summary>
    /// 导航节点类型。
    /// 作用：区分分组、PLC 品牌、机器人、通用通讯四类。
    /// 目的：右侧面板对四类内容展示的字段不同（品牌要看实现状态，通用通讯要看端口），
    ///      用一个枚举判断比反复比较字符串可靠。
    /// </summary>
    public enum DeviceNavKind
    {
        /// <summary>分组标题，本身不可作为设备打开</summary>
        Group = 0,

        /// <summary>PLC 品牌，对应一个 IPlcCommunicationModel 实现类</summary>
        Plc = 1,

        /// <summary>机器人设备</summary>
        Robot = 2,

        /// <summary>通用通讯方式（TCP 客户端、Modbus RTU 等）</summary>
        Transport = 3
    }
}
