using System.Windows.Controls;
using 小玩意.ViewModel;

namespace 小玩意
{
    /// <summary>
    /// 视觉识别页面的交互逻辑。
    /// 作用：创建视图模型，并在页面离开时停止相机。
    /// 目的：摄像头是独占设备，页面隐藏后如果还在采集，其他程序就再也打不开摄像头；
    ///      同时也能避免不可见时继续跑图像处理白耗 CPU。
    /// </summary>
    public partial class VisionRecognition : Page
    {
        private readonly VisionRecognitionViewModel _viewModel = new();

        public VisionRecognition()
        {
            InitializeComponent();
            DataContext = _viewModel;

            Unloaded += (_, __) => _viewModel.StopIfRunning();
        }
    }
}
