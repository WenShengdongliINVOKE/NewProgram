using System.Windows;
using 小玩意.ViewModel;

namespace 小玩意
{
    /// <summary>
    /// TCP.xaml 的交互逻辑
    /// </summary>
    public partial class TCP : Window
    {
        private static TCP tCP;

        private TCP()
        {

            InitializeComponent();
            this.DataContext = new TcpCommViwemodel();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
        public static TCP GetWindows()
        {
            if (tCP == null)
            {
                return tCP = new TCP();
            }
            else
            {
                return tCP;
            }

        }
    }
}
