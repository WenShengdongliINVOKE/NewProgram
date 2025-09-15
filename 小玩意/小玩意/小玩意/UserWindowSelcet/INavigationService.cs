using System.Windows.Controls;

namespace 小玩意.UserWindowSelcet
{
    /// <summary>
    /// 界面导航服务接口
    /// </summary>
    interface INavigationService
    {
        void NavigateTo<T>() where T : UserControl;
        void GoBack();
        event EventHandler NavigationChanged;
    }
}
