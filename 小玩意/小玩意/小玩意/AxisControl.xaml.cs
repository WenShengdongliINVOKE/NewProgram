using System.Windows.Controls;
using 小玩意.ViewModel;

namespace 小玩意
{
    /// <summary>
    /// 轴控制页面的交互逻辑。
    /// 作用：创建视图模型，并在页面显示/隐藏时启停位置轮询。
    /// 目的：主窗口会缓存页面实例，切走并不会销毁对象；如果不在这里暂停，
    ///      看不见的页面会一直向控制器读数据，把通讯带宽占掉、拖慢其他页面。
    /// </summary>
    public partial class AxisControl : Page
    {
        private readonly AxisControlViewModel _viewModel = new();

        public AxisControl()
        {
            InitializeComponent();
            DataContext = _viewModel;

            Loaded += (_, __) => _viewModel.ResumeRefresh();
            Unloaded += (_, __) => _viewModel.PauseRefresh();
        }
    }
}
