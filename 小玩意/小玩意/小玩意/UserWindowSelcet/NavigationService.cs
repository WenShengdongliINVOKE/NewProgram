using System.Windows.Controls;

namespace 小玩意.UserWindowSelcet
{
    /// <summary>
    /// 界面导航服务
    /// </summary>
    class NavigationService : INavigationService
    {
        private readonly Stack<UserControl> _navigationStack = new Stack<UserControl>();
        private ContentControl _contentControl;

        public event EventHandler NavigationChanged;

        public void Initialize(ContentControl contentControl)
        {
            _contentControl = contentControl;
        }

        public void NavigateTo<T>() where T : UserControl
        {
            UserControl page = Activator.CreateInstance<T>();
            _navigationStack.Push(page);
            _contentControl.Content = page;
            NavigationChanged?.Invoke(this, EventArgs.Empty);
        }

        public void GoBack()
        {
            if (_navigationStack.Count > 1)
            {
                _navigationStack.Pop(); // 移除当前页面
                UserControl previousPage = _navigationStack.Peek();
                _contentControl.Content = previousPage;
                NavigationChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
