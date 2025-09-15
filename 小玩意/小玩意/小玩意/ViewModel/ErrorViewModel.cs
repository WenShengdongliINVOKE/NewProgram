using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;

namespace 小玩意.ViewModel
{
    /// <summary>
    /// 报错弹窗
    /// </summary>
    internal class ErrorViewModel : ObservableObject
    {
        private static string _path1 = @"C:\Users\25062\Desktop\New\小玩意\小玩意\小玩意\GIF\摸鱼.gif";
        public string Path1 { get => _path1; set => SetProperty(ref _path1, value); }

        private static string _path2 = @"C:\Users\25062\Desktop\New\小玩意\小玩意\小玩意\GIF\摸鱼.gif";
        public string Path2 { get => _path2; set => SetProperty(ref _path2, value); }

        private static string _errorText;
        public string ErrorText { get => _errorText; set => SetProperty(ref _errorText, value); }

        private static Brush _colors = new SolidColorBrush(Colors.Red);
        public Brush Color { get => _colors; set => SetProperty(ref _colors, value); }



        public ErrorViewModel()
        {

        }
        /// <summary>
        /// 报错提示框
        /// </summary>
        /// <param name="str">提示内容</param>
        /// <param name="b">报警还是正常内容</param>
        /// <param name="number">选择不同的动画 1一般错误 2预料之中的错误 3严重错误</param>
        public static void Errornotice(string str, bool b, int number)
        {
            if (!b)
            {
                SetGIFWindow(number);
                _errorText = str;
                _colors = new SolidColorBrush(Colors.Gray);
            }
            else
            {
                SetGIFWindow(number);
                _errorText = str;
                _colors = new SolidColorBrush(Colors.Red);
            }
            ErrorBox errorBox = new ErrorBox();
            errorBox.Show();
        }
        /// <summary>
        /// \\bin\\Debug\\net8.0-windows
        /// </summary>
        /// <param name="number"></param> 
        private static void SetGIFWindow(int number)
        {
            string currentDirectory = Environment.CurrentDirectory;
            var ss = currentDirectory.Substring(0, currentDirectory.Length - 24);

            switch (number)
            {
                case 1:
                    _path1 = ss + "GIF\\边哭边吃橘子.gif";
                    _path2 = ss + "GIF\\搞点欧润菊吃吃.gif";
                    break;
                case 2:
                    _path1 = ss + "GIF\\边哭边吃橘子.gif";
                    _path2 = ss + "GIF\\踹手手.gif";
                    break;
                case 3:
                    _path1 = ss + "GIF\\边哭边吃橘子.gif";
                    _path2 = ss + "GIF\\哭着拿橘子.gif";
                    break;
                case 4:
                    _path1 = ss + "GIF\\边哭边吃橘子.gif";
                    _path2 = ss + "GIF\\两只手抓.gif";
                    break;
                case 5:
                    _path1 = ss + "GIF\\边哭边吃橘子.gif";
                    _path2 = "GIF\\";
                    break;
                case 6:
                    _path1 = ss + "GIF\\提可乐.gif";
                    _path2 = ss + "GIF\\两只手抓.gif";
                    break;
                case 7:
                    _path1 = ss + "GIF\\提可乐.gif";
                    _path2 = ss + "GIF\\搞点欧润菊吃吃.gif";
                    break;
                case 8:
                    _path1 = ss + "GIF\\提可乐.gif";
                    _path2 = "GIF\\";
                    break;
                case 9:
                    _path1 = ss + "GIF\\提可乐.gif";
                    _path2 = "GIF\\";
                    break;
                default:
                    _path1 = ss + "GIF\\提可乐.gif";
                    _path2 = "GIF\\";
                    break;
            }

        }


    }
}
