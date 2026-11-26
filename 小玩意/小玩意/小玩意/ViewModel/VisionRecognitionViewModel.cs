using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using NLog;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using 小玩意.OpenCV;

namespace 小玩意.ViewModel
{
    /// <summary>
    /// 视觉识别页面的视图模型。
    /// 作用：驱动摄像头取图与图像处理，并把处理结果的四路图像和识别数量送到界面。
    /// 目的：OpenCVTool 只负责算，不管展示；这一层负责把「算出来的 BitmapSource」变成
    ///      界面能直接绑定的属性，并提供启动/停止/加载图片这三个动作。
    /// </summary>
    public partial class VisionRecognitionViewModel : ObservableObject, IDisposable
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 图像处理服务。
        /// 作用：封装 OpenCV 的灰度、二值化、形态学与连通域检测。
        /// 目的：界面不应该出现任何 OpenCV 类型（Mat / Scalar），
        ///      全部收敛在服务内部，以后换算法实现也不影响界面。
        /// </summary>
        private readonly OpenCVTool _vision = new();

        public ICommand StartCameraCommand { get; }
        public ICommand StopCameraCommand { get; }
        public ICommand LoadImageCommand { get; }

        private BitmapSource? _originalImage;

        /// <summary>原始图像（相机模式下是当前帧，图片模式下是读入的文件）</summary>
        public BitmapSource? OriginalImage
        {
            get => _originalImage;
            private set => SetProperty(ref _originalImage, value);
        }

        private BitmapSource? _grayImage;

        /// <summary>灰度图</summary>
        public BitmapSource? GrayImage
        {
            get => _grayImage;
            private set => SetProperty(ref _grayImage, value);
        }

        private BitmapSource? _binaryImage;

        /// <summary>二值化结果（检测就是以它为基础做的）</summary>
        public BitmapSource? BinaryImage
        {
            get => _binaryImage;
            private set => SetProperty(ref _binaryImage, value);
        }

        private BitmapSource? _processedImage;

        /// <summary>标注了检测框的结果图</summary>
        public BitmapSource? ProcessedImage
        {
            get => _processedImage;
            private set => SetProperty(ref _processedImage, value);
        }

        private int _detectedCount;

        /// <summary>识别到的点位数</summary>
        public int DetectedCount
        {
            get => _detectedCount;
            private set
            {
                if (!SetProperty(ref _detectedCount, value)) return;
                OnPropertyChanged(nameof(DetectedCountText));
            }
        }

        public string DetectedCountText => DetectedCount > 0 ? $"识别到 {DetectedCount} 个点位" : "暂无识别结果";

        private bool _isCameraRunning;

        /// <summary>相机是否正在采集</summary>
        public bool IsCameraRunning
        {
            get => _isCameraRunning;
            private set
            {
                if (!SetProperty(ref _isCameraRunning, value)) return;

                OnPropertyChanged(nameof(CameraStateText));
                NotifyCommandsCanExecute();
            }
        }

        public string CameraStateText => IsCameraRunning ? "采集中" : "已停止";

        private string _statusText = "相机未启动。可以启动相机实时检测，也可以加载一张本地图片做离线检测。";

        /// <summary>底部状态提示</summary>
        public string StatusText
        {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
        }

        public VisionRecognitionViewModel()
        {
            StartCameraCommand = new RelayCommand(StartCamera, () => !IsCameraRunning);
            StopCameraCommand = new RelayCommand(StopCamera, () => IsCameraRunning);
            LoadImageCommand = new RelayCommand(LoadImage, () => !IsCameraRunning);

            // 订阅处理结果：服务每算完一帧就推一次，这里转成界面可绑定的属性
            _vision.ProcessingCompleted += OnProcessingCompleted;
        }

        /// <summary>
        /// 收到一帧处理结果。
        /// 作用：把结果对象里的四路图像与数量写到视图模型属性上。
        /// 目的：事件触发点在 UI 线程（相机由 DispatcherTimer 驱动），可以直接赋值；
        ///      但如果以后改成后台线程处理，这里就必须切回 UI 线程，所以下方做了线程判断，
        ///      避免以后改动驱动方式时踩跨线程访问界面的坑。
        /// </summary>
        private void OnProcessingCompleted(object? sender, ImageProcessingResult result)
        {
            if (!System.Windows.Application.Current.Dispatcher.CheckAccess())
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => OnProcessingCompleted(sender, result));
                return;
            }

            // 相机模式下 CameraImage 是当前帧，图片模式下它为空，用 OriginalImage 兜底
            OriginalImage = result.CameraImage ?? result.OriginalImage;
            GrayImage = result.GrayImage;
            BinaryImage = result.BinaryImage;
            ProcessedImage = result.ProcessedImage;
            DetectedCount = result.DetectedDotsCount;
        }

        /// <summary>
        /// 启动相机。
        /// 作用：打开默认摄像头并开始逐帧检测。
        /// 目的：失败时只更新状态文字、不弹模态框——相机被其他程序占用是很常见的情况，
        ///      弹窗会打断操作，而且操作员往往要再试一次。
        /// </summary>
        private void StartCamera()
        {
            try
            {
                _vision.StartCamera();
                IsCameraRunning = true;
                StatusText = "相机已启动，正在实时检测。";
            }
            catch (Exception ex)
            {
                // 作用：把失败原因翻译成人能看懂的话。
                // 目的：异常信息是"无法打开摄像头"，但现场真正的原因通常是设备被占用，
                //      不点明的话操作员会去查驱动，白费时间。
                IsCameraRunning = false;
                StatusText = $"启动相机失败：{ex.Message}。请确认摄像头已连接且未被其他程序占用。";
                logger.Error(ex, "启动摄像头失败");
            }
        }

        /// <summary>停止相机</summary>
        private void StopCamera()
        {
            try
            {
                _vision.StopCamera();
                StatusText = "相机已停止。";
            }
            catch (Exception ex)
            {
                logger.Error(ex, "停止摄像头失败");
                StatusText = $"停止相机时出错：{ex.Message}";
            }
            finally
            {
                // 无论底层是否报错，界面都必须回到"已停止"，否则按钮状态会卡在"采集中"
                IsCameraRunning = false;
            }
        }

        /// <summary>
        /// 加载本地图片做离线检测。
        /// 作用：弹出文件对话框，选中的图片交给 OpenCVTool 处理。
        /// 目的：没有相机或现场不方便取实时画面时，用历史图片验证算法阈值是否合适，
        ///      这也是调二值化阈值时最常用的方式。
        /// </summary>
        private void LoadImage()
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择要检测的图片",
                Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp|所有文件|*.*"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                _vision.ProcessImage(dialog.FileName);
                StatusText = $"已处理：{System.IO.Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                StatusText = $"处理图片失败：{ex.Message}";
                logger.Error(ex, $"处理图片失败：{dialog.FileName}");
            }
        }

        /// <summary>
        /// 页面离开时停止采集。
        /// 作用：关闭摄像头。
        /// 目的：摄像头是独占设备，隐藏的页面一直占着它，其他程序（或另一台工位机）就打不开；
        ///      同时也避免不可见时继续跑图像处理白耗 CPU。
        /// </summary>
        public void StopIfRunning()
        {
            if (IsCameraRunning) StopCamera();
        }

        private void NotifyCommandsCanExecute()
        {
            (StartCameraCommand as RelayCommand)?.NotifyCanExecuteChanged();
            (StopCameraCommand as RelayCommand)?.NotifyCanExecuteChanged();
            (LoadImageCommand as RelayCommand)?.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// 释放资源。
        /// 作用：解除事件订阅并释放 OpenCV 工具（会顺带停止相机）。
        /// 目的：不解除订阅的话，工具对象被释放后仍持有本对象的引用，
        ///      页面缓存机制下会让整页内存长期无法回收。
        /// </summary>
        public void Dispose()
        {
            _vision.ProcessingCompleted -= OnProcessingCompleted;
            _vision.Dispose();
            logger.Info("视觉识别页面已释放资源");
        }
    }
}
