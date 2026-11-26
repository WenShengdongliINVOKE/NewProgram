using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using 小玩意.ViewModel;

namespace 小玩意.OpenCV
{
    internal class OpenCVTool : IImageProcessingService, IDisposable
    {
        private VideoCapture _capture;
        /// <summary>
        /// 摄像头定时器 用于定时读取摄像头帧
        /// </summary>
        private DispatcherTimer _cameraTimer;
        private bool _isDisposed = false;
        /// <summary>
        /// 处理完成事件 完成后将处理过的图像更新到界面
        /// </summary>
        public event EventHandler<ImageProcessingResult> ProcessingCompleted;
        public bool IsCameraRunning { get; private set; }

        public OpenCVTool()
        {
            InitializeTimer();
        }
        private void InitializeTimer()
        {
            _cameraTimer = new DispatcherTimer();
            _cameraTimer.Interval = TimeSpan.FromMilliseconds(60);
            _cameraTimer.Tick += OnCameraTimerTick;
        }


        /// <summary>
        /// 加载图像文件
        /// </summary>
        /// <param name="imagePath">文件路径</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public void ProcessImage(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
               ErrorViewModel.Errornotice("图像路径不能为空",false,1);

            try
            {
                using (var originalImage = Cv2.ImRead(imagePath, ImreadModes.Color))
                {
                    if (originalImage.Empty())
                        throw new InvalidOperationException("无法加载图像文件");

                    //处理图像
                    var result = ProcessFrame(originalImage, null);
                    //处理完成后更新到界面
                    ProcessingCompleted?.Invoke(this, result);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"处理图像时出错: {ex.Message}", ex);
            }
        }

        public void StartCamera()
        {
            if (IsCameraRunning) return;

            try
            {
                _capture = new VideoCapture(0);

                if (!_capture.IsOpened())
                    throw new InvalidOperationException("无法打开摄像头");

                _capture.Set(VideoCaptureProperties.FrameWidth, 640);
                _capture.Set(VideoCaptureProperties.FrameHeight, 480);

                IsCameraRunning = true;
                _cameraTimer.Start();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"开启摄像头时出错: {ex.Message}", ex);
            }
        }

        public void StopCamera()
        {
            if (!IsCameraRunning) return;

            _cameraTimer.Stop();
            IsCameraRunning = false;

            _capture?.Release();
            _capture?.Dispose();
            _capture = null;
        }

        private void OnCameraTimerTick(object sender, EventArgs e)
        {
            if (!IsCameraRunning || _capture == null) return;

            try
            {
                using (var frame = new Mat())
                {
                    bool success = _capture.Read(frame);

                    if (!success || frame.Empty())
                    {
                        StopCamera();
                        return;
                    }

                    var result = ProcessFrame(frame, frame);
                    ProcessingCompleted?.Invoke(this, result);
                }
            }
            catch (Exception ex)
            {
                // 作用：出错时停止相机，只记日志，不再向外抛异常。
                // 目的：本方法由 DispatcherTimer 的 Tick 调用，而 Tick 里抛出的异常没有任何调用方
                //      可以接住，会直接终结进程——表现为"摄像头一抖动程序就没了"。
                //      这里把异常就地收口，并保留 StopCamera 让相机回到可重试的干净状态。
                NLog.LogManager.GetCurrentClassLogger().Error(ex, "处理摄像头帧时出错，已停止相机");
                StopCamera();
            }
        }
        /// <summary>
        /// 处理图像帧的核心方法
        /// 执行灰度转换、二值化、形态学操作和黑点检测
        /// </summary>
        /// <param name="originalFrame">原始图像帧</param>
        /// <param name="cameraFrame">摄像头原始帧（可为null）</param>
        /// <returns>处理结果</returns>
        private ImageProcessingResult ProcessFrame(Mat originalFrame, Mat cameraFrame)
        {
            using (var grayFrame = new Mat())
            using (var binaryFrame = new Mat())
            using (var processedFrame = originalFrame.Clone())
            {
                // 转换为灰度图
                Cv2.CvtColor(originalFrame, grayFrame, ColorConversionCodes.BGR2GRAY);

                // 二值化
                Cv2.Threshold(grayFrame, binaryFrame, 50, 255, ThresholdTypes.BinaryInv);

                // 形态学操作
                using (var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp. Size(3, 3)))
                using (var morph = new Mat())
                {
                    Cv2.MorphologyEx(binaryFrame, morph, MorphTypes.Open, kernel, iterations: 2);

                    // 连通组件分析
                    var dotsCount = DetectBlackDots(morph, processedFrame);

                    return new ImageProcessingResult
                    {
                        OriginalImage = ConvertToBitmapSource(originalFrame),
                        GrayImage = ConvertToBitmapSource(grayFrame),
                        BinaryImage = ConvertToBitmapSource(binaryFrame),
                        ProcessedImage = ConvertToBitmapSource(processedFrame),
                        CameraImage = cameraFrame != null ? ConvertToBitmapSource(cameraFrame) : null,
                        DetectedDotsCount = dotsCount
                    };
                }
            }
        }

        private int DetectBlackDots(Mat binaryImage, Mat outputImage)
        {
            using (var labels = new Mat())
            using (var stats = new Mat())
            using (var centroids = new Mat())
            {
                int numComponents = Cv2.ConnectedComponentsWithStats(binaryImage, labels, stats, centroids);
                var rnd = new Random();
                int dotsCount = 0;

                for (int i = 1; i < numComponents; i++)
                {
                    int area = stats.At<int>(i, (int)ConnectedComponentsTypes.Area);
                    int x = stats.At<int>(i, (int)ConnectedComponentsTypes.Left);
                    int y = stats.At<int>(i, (int)ConnectedComponentsTypes.Top);
                    int width = stats.At<int>(i, (int)ConnectedComponentsTypes.Width);
                    int height = stats.At<int>(i, (int)ConnectedComponentsTypes.Height);

                    if (area > 50 && area < 10000)
                    {
                        dotsCount++;
                        var color = new Scalar(rnd.Next(256), rnd.Next(256), rnd.Next(256));
                        Cv2.Rectangle(outputImage, new OpenCvSharp. Rect(x, y, width, height), color, 2);
                        Cv2.PutText(outputImage, $"Dot {dotsCount}", new OpenCvSharp.Point(x, y - 10),
                                    HersheyFonts.HersheySimplex, 0.5, color, 1);
                    }
                }

                return dotsCount;
            }
        }

        private BitmapSource ConvertToBitmapSource(Mat mat)
        {
            if (mat.Empty()) return null;

            // 调整图像大小以适应显示
            using (var resized = new Mat())
            {
                Cv2.Resize(mat, resized, new OpenCvSharp.Size(280, 280));
                return BitmapSourceConverter.ToBitmapSource(resized);
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                StopCamera();
                _cameraTimer?.Stop();
                _capture?.Dispose();
                _isDisposed = true;
            }
        }
    }


}
