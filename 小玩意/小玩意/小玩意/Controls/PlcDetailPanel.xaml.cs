using System.Windows.Controls;

namespace 小玩意.Controls
{
    /// <summary>
    /// PLC 详细信息面板。
    /// 作用：展示已连接设备列表与该设备的实时点位数据。
    /// 目的：同一块内容在两处复用——「连接设备」页选中品牌后的下方区域，
    ///      以及独立的 PLC 信息页；抽成控件后只维护一份，
    ///      否则改一处列宽要改两个文件，迟早不一致。
    /// 说明：本控件不设置 DataContext，由宿主页面绑定传入视图模型。
    /// </summary>
    public partial class PlcDetailPanel : UserControl
    {
        public PlcDetailPanel()
        {
            InitializeComponent();
        }
    }
}
