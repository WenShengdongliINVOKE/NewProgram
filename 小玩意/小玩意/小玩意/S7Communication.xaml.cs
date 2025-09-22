using System.Windows.Controls;
using 小玩意.ViewModel;

namespace 小玩意
{
    /// <summary>
    /// S7Communication.xaml 的交互逻辑
    /// </summary>
    public partial class S7Communication : Page
    {
        private static S7Communication? s7Comm;
        public S7Communication()
        {
            InitializeComponent();
            this.DataContext = new S7CommViweModel();
        }
        public static S7Communication GetWindows()
        {
            if (s7Comm == null)
            {

                return s7Comm = new S7Communication();
            }
            else
            {
                return s7Comm;
            }

        }



        /// <summary>
        /// 选中事件 这里根据选中PLC的不同来加载不同PLC的数据 已弃用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGridText_SelectedCellsChanged(object sender, System.Windows.Controls.SelectedCellsChangedEventArgs e)
        {
            //S7CommViweModel s7CommViweModel = this.DataContext as S7CommViweModel;
            //var ss = sender.ToString();
            //var SelectNumber = Convert.ToInt32(sender.ToString().Substring(sender.ToString().Length - 1, 1));
            //s7CommViweModel.SelectedIndex = SelectNumber - 1;
        }
    }
}
