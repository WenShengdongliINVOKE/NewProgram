using System.Windows;
using 小玩意.ViewModel;

namespace 小玩意
{
    /// <summary>
    /// ErrorBox.xaml 的交互逻辑
    /// </summary>
    public partial class ErrorBox : Window
    {
        public ErrorBox()
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
            this.DataContext = new ErrorViewModel();

        }



        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        public static void Show(string str, bool b)
        {
            ErrorBox errorBox = new ErrorBox();
            errorBox.ShowDialog();
            ErrorViewModel.Errornotice(str, b, 1);
        }
    }
}
