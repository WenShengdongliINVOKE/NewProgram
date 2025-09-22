using System.Windows.Controls;
using 小玩意.ViewModel;

namespace 小玩意
{
    /// <summary>
    /// ConnectDevice.xaml 的交互逻辑
    /// </summary>
    public partial class ConnectDevice : Page
    {
        public ConnectDevice()
        {
            InitializeComponent();
            this.DataContext = new ConnectDeviceViweModel();
        }
    }
}
