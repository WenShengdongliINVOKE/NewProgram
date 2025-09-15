using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace 小玩意
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Mutex _mutex = null;
        private const string MutexName = "Global\\YourUniqueAppMutexName";
        // 引入Windows API函数
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern int RegisterWindowMessage(string lpString);

        private const int SW_RESTORE = 9;
        private static int WM_SHOWME;

        private void Application_Startup(object sender, StartupEventArgs e)
        {

            // 注册自定义窗口消息
            WM_SHOWME = RegisterWindowMessage("WM_SHOWME_" + MutexName);

            // 尝试创建互斥体
            bool createdNew;
            _mutex = new Mutex(true, MutexName, out createdNew);

            if (!createdNew)
            {
                // 互斥体已存在，说明程序已在运行
                MessageBox.Show("应用程序已经在运行中！", "提示",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                // 查找并激活已运行的实例
                Process current = Process.GetCurrentProcess();
                foreach (Process process in Process.GetProcessesByName(current.ProcessName))
                {
                    if (process.Id != current.Id)
                    {
                        // 发送自定义消息给已运行的实例
                        IntPtr hWnd = process.MainWindowHandle;
                        if (hWnd != IntPtr.Zero)
                        {
                            // 激活窗口
                            PostMessage(hWnd, WM_SHOWME, IntPtr.Zero, IntPtr.Zero);
                            SetForegroundWindow(hWnd);
                            ShowWindow(hWnd, SW_RESTORE);
                        }
                        break;
                    }
                }

                // 立即关闭应用程序
                Shutdown();
                return;
            }

            // 只有单实例运行时才创建主窗口
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
        }
        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            // 释放互斥体
            if (_mutex != null)
            {
                _mutex.ReleaseMutex();
                _mutex.Dispose();
                _mutex = null;
            }
        }
    }

}
