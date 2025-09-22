using System.Windows.Controls;
using 小玩意.ViewModel;

namespace 小玩意
{
    /// <summary>
    /// AbbRobot.xaml 的交互逻辑
    /// </summary>
    public partial class AbbRobot : Page
    {



        public AbbRobot()
        {
            InitializeComponent();
            this.DataContext = new AbbRobotViewModel();
        }


    }

}

