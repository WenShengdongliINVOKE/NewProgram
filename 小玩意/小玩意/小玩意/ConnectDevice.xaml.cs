using System.Windows;
using System.Windows.Controls;
using 小玩意.ViewModel;

namespace 小玩意
{
    /// <summary>
    /// ConnectDevice.xaml 的交互逻辑。
    /// 作用：初始化视图模型，并把导航树的选中事件转交给视图模型。
    /// 目的：TreeView.SelectedItem 是只读依赖属性，无法在 XAML 里双向绑定，
    ///      只能用事件把「选了哪一项」写回视图模型；其余逻辑全部留在视图模型里，界面不做业务判断。
    /// </summary>
    public partial class ConnectDevice : Page
    {
        public ConnectDevice()
        {
            InitializeComponent();
            DataContext = new ConnectDeviceViweModel();
        }

        /// <summary>
        /// 导航树选中项变化。
        /// 作用：把当前选中节点交给视图模型，由它刷新右侧面板。
        /// 目的：分成「分组标题」和「品牌条目」两类节点，选中分组时右侧仍要显示分组说明，
        ///      所以这里不做过过滤，所有节点都往下传，显示什么由视图模型按节点类型决定。
        /// </summary>
        private void DeviceTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is ConnectDeviceViweModel viewModel)
            {
                viewModel.SelectedItem = e.NewValue as DeviceNavItem;
            }
        }
    }
}
