using System.Windows.Controls;
using 小玩意.ViewModel;

namespace 小玩意
{
    /// <summary>
    /// PLC 信息页的交互逻辑。
    /// 作用：创建点位监控的视图模型。
    /// 目的：页面只负责展示设备与点位，连接动作统一放在「连接设备」页，
    ///      避免两个页面各自连接一次、互相把对方的连接顶掉。
    /// </summary>
    public partial class PlcMessagePage : Page
    {
        /// <summary>
        /// 页面单例。
        /// 作用：缓存已经创建过的页面实例。
        /// 目的：点位模型内部有常驻的读取循环，每次导航都 new 一个会起出多条循环，
        ///      同一条数据被处理多次，还会成倍占用 PLC 连接数。
        /// </summary>
        private static PlcMessagePage? _instance;

        public PlcMessagePage()
        {
            InitializeComponent();
            DataContext = new S7CommViweModel();
        }

        /// <summary>
        /// 获取页面实例（不存在则创建）。
        /// 作用：给外部提供一个统一的取页面入口。
        /// 目的：配合上面的单例字段，保证无论从哪里发起导航，拿到的都是同一个页面。
        /// </summary>
        public static PlcMessagePage GetWindow() => _instance ??= new PlcMessagePage();
    }
}
