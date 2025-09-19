using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using 小玩意.Model;
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

