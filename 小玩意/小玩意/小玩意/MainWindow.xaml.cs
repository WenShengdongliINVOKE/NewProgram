using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using 小玩意.ViewModel;

namespace 小玩意
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // 定义全局Mutex名称（确保唯一性）
        private const string MutexName = "Global\\YourCompanyName_YourAppName_UniqueMutexIdentifier";

        // 静态Mutex变量
        private static Mutex mutex;


        public MainWindow()
        {

            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
          
            this.MaxHeight = SystemParameters.PrimaryScreenHeight;
            // 默认导航到首页
            NavigateToPage("ConnectDevice.xaml", null);

        }
        /// <summary>
        /// 绑定到按钮的导航事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NavigateToPage(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            string pageName = $"{button.CommandParameter.ToString()}.xaml";
            NavigateToPage(pageName, button);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pageName"></param>
        /// <param name="navButton"></param>
        private void NavigateToPage(string pageName, Button navButton)
        {
            // 高亮当前选中的导航按钮
            HighlightNavButton(navButton);

            // 使用动画过渡效果导航到页面
            DoubleAnimation fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.2)
            };

            fadeOut.Completed += (s, _) =>
            {
                MainFrame.Navigate(new System.Uri(pageName, System.UriKind.Relative));

                DoubleAnimation fadeIn = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.3)
                };
                MainFrame.BeginAnimation(OpacityProperty, fadeIn);
            };

            MainFrame.BeginAnimation(OpacityProperty, fadeOut);
        }
        private void HighlightNavButton(Button activeButton)
        {
            // 重置所有按钮样式
            ConnectDevice.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF000000"));
            S7Communication.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF000000"));
            AboutBtn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF000000"));
            ContactBtn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF000000"));

            // 高亮当前按钮
            if (activeButton != null)
            {
                activeButton.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1E88E5"));
            }
        }

    }
}