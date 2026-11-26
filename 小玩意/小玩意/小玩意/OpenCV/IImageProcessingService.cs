using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace 小玩意.OpenCV
{
    interface  IImageProcessingService
    {

        event EventHandler<ImageProcessingResult> ProcessingCompleted;

        void ProcessImage(string imagePath);
        void StartCamera();
        void StopCamera();
        bool IsCameraRunning { get; }
    }

    public class ImageProcessingResult
    {
        public BitmapSource OriginalImage { get; set; }
        public BitmapSource GrayImage { get; set; }
        public BitmapSource BinaryImage { get; set; }
        public BitmapSource ProcessedImage { get; set; }
        public BitmapSource CameraImage { get; set; }
        public int DetectedDotsCount { get; set; }
    }
}
