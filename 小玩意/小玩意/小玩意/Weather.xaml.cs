using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using 小玩意.Comm;
using 小玩意.Model;
using 小玩意.ViewModel;

namespace 小玩意
{
    /// <summary>
    /// Weather.xaml 的交互逻辑
    /// </summary>
    public partial class Weather : Page
    {
        private WeatherService _weatherService;
        private WeatherInfo _weatherInfo;
        private DispatcherTimer _timer; // 用于更新时间或动画
        private string currentWeatherType = "";
        public Weather()
        {
            InitializeComponent();
            string apiKey = Environment.GetEnvironmentVariable("OPENWEATHER_API_KEY") ?? "your_api_key_here";
            _weatherService = new WeatherService(apiKey);

            _weatherInfo = new WeatherInfo();
            this.DataContext = _weatherInfo;
            Button_Search_Click(null,null);
        }


        private async void Button_Search_Click(object sender, RoutedEventArgs e)
        {
            string city = "内江市";
            if (string.IsNullOrEmpty(city))
            {
                MessageBox.Show("请输入城市名称！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var weather = await _weatherService.GetWeatherAsync(city);

                _weatherInfo.City = weather.City;
                _weatherInfo.Country = weather.Country;
                _weatherInfo.Temperature = Math.Round(weather.Temperature, 1);
                _weatherInfo.Humidity = weather.Humidity;
                _weatherInfo.WindSpeed = Math.Round(weather.WindSpeed, 1);
                _weatherInfo.Description = weather.Description;
                _weatherInfo.DateTime = weather.DateTime;

                SetWeatherIcon(weather.Description);
                ApplyWeatherEffect(weather.Description); // 👈 应用动态效果
            }
            catch (Exception ex)
            {
                ErrorViewModel.Errornotice($"❌ 获取天气失败：{ex.Message} 😅🙌 其实是没有钱去调用付费API啦。。。",false,2);
            }
        }

        private void SetWeatherIcon(string desc)
        {
            if (desc.Contains("晴")) txtIcon.Text = "☀️";
            else if (desc.Contains("多云") || desc.Contains("阴")) txtIcon.Text = "⛅";
            else if (desc.Contains("雨")) txtIcon.Text = "🌧️";
            else if (desc.Contains("雪")) txtIcon.Text = "❄️";
            else if (desc.Contains("雷")) txtIcon.Text = "⛈️";
            else txtIcon.Text = "🌤️";
        }

        // 根据天气类型应用动态背景效果
        private void ApplyWeatherEffect(string description)
        {
            // 清除旧效果
            WeatherEffectCanvas.Children.Clear();

            if (description.Contains("晴"))
            {
                SetupSunnyEffect();
            }
            else if (description.Contains("雨"))
            {
                SetupRainEffect();
            }
            else if (description.Contains("雪"))
            {
                SetupSnowEffect();
            }
            else if (description.Contains("多云") || description.Contains("阴"))
            {
                SetupCloudyEffect();
            }
            else
            {
                MainGrid.Background = new SolidColorBrush(Color.FromRgb(15, 26, 47)); // 默认深蓝
            }
        }

        #region 🌞 晴天效果：渐变 + 白云飘动
        private void SetupSunnyEffect()
        {
            // 渐变背景
            var brush = new LinearGradientBrush();
            brush.StartPoint = new Point(0, 0);
            brush.EndPoint = new Point(1, 1);
            brush.GradientStops.Add(new GradientStop(Colors.LightSkyBlue, 0));
            brush.GradientStops.Add(new GradientStop(Colors.Yellow, 0.5));
            brush.GradientStops.Add(new GradientStop(Colors.Orange, 1));
            MainGrid.Background = brush;

            // 创建白云
            for (int i = 0; i < 3; i++)
            {
                var cloud = new Path
                {
                    Fill = new SolidColorBrush(Colors.White),
                    Data = Geometry.Parse("M0,10 C2,-2 8,-2 10,2 C13,-2 18,-2 20,2 C24,-2 30,-2 32,4 C36,0 42,0 44,6 C50,2 58,2 60,10 L60,20 L0,20 Z"),
                    Width = 60,
                    Height = 30,
                    Opacity = 0.8,
                    RenderTransform = new TranslateTransform((i + 1) * 150, 80)
                };

                WeatherEffectCanvas.Children.Add(cloud);

                // 动画：白云从右向左缓慢移动
                var anim = new DoubleAnimation();
                anim.From = 800;
                anim.To = -100;
                anim.Duration = TimeSpan.FromSeconds(60 + i * 20);
                anim.RepeatBehavior = RepeatBehavior.Forever;
                cloud.RenderTransform.BeginAnimation(TranslateTransform.XProperty, anim);
            }
        }
        #endregion

        #region 🌧️ 雨天效果：雨滴动画
        private void SetupRainEffect()
        {
            MainGrid.Background = new SolidColorBrush(Color.FromRgb(60, 80, 100));

            Random rand = new Random();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _timer.Tick += (s, e) =>
            {
                if (WeatherEffectCanvas.ActualWidth == 0) return;

                var raindrop = new Rectangle
                {
                    Width = 2,
                    Height = 10,
                    Fill = new SolidColorBrush(Colors.LightBlue),
                    Opacity = 0.7
                };

                Canvas.SetLeft(raindrop, rand.NextDouble() * WeatherEffectCanvas.ActualWidth);
                Canvas.SetTop(raindrop, 0);
                WeatherEffectCanvas.Children.Add(raindrop);

                // 雨滴下落动画
                var anim = new DoubleAnimation();
                anim.To = WeatherEffectCanvas.ActualHeight;
                anim.Duration = TimeSpan.FromSeconds(1);
                anim.Completed += (a, b) => WeatherEffectCanvas.Children.Remove(raindrop);
                raindrop.BeginAnimation(Canvas.TopProperty, anim);
            };
            _timer.Start();
        }
        #endregion

        #region ❄️ 雪天效果：雪花飘落
        private void SetupSnowEffect()
        {
            MainGrid.Background = new SolidColorBrush(Color.FromRgb(40, 44, 52));

            Random rand = new Random();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _timer.Tick += (s, e) =>
            {
                if (WeatherEffectCanvas.ActualWidth == 0) return;

                var snowflake = new TextBlock
                {
                    Text = "❅",
                    FontSize = rand.Next(10, 20),
                    Foreground = Brushes.White,
                    Opacity = 0.8
                };

                Canvas.SetLeft(snowflake, rand.NextDouble() * WeatherEffectCanvas.ActualWidth);
                Canvas.SetTop(snowflake, 0);
                WeatherEffectCanvas.Children.Add(snowflake);

                double duration = rand.NextDouble() * 3 + 3; // 3~6秒飘落
                double sway = rand.Next(-50, 50); // 左右飘动

                var animTop = new DoubleAnimation(0, WeatherEffectCanvas.ActualHeight, new Duration(TimeSpan.FromSeconds(duration)));
                var animLeft = new DoubleAnimationUsingPath
                {
                    Duration = new Duration(TimeSpan.FromSeconds(duration)),
                    //PathGeometry = Geometry.Parse($"M0,0 C{sway}, {duration / 2 * 100}, {sway / 2}, {duration * 100}, {sway / 2}, {duration * 100}")
                };
                animLeft.Source = PathAnimationSource.X;

                animTop.Completed += (a, b) =>
                {
                    WeatherEffectCanvas.Children.Remove(snowflake);
                };

                snowflake.BeginAnimation(Canvas.TopProperty, animTop);
                snowflake.BeginAnimation(Canvas.LeftProperty, animLeft);
            };
            _timer.Start();
        }
        #endregion

        #region ⛅ 阴天/多云效果：灰云移动
        private void SetupCloudyEffect()
        {
            MainGrid.Background = new SolidColorBrush(Color.FromRgb(50, 55, 65));

            var brush = new RadialGradientBrush();
            brush.GradientStops.Add(new GradientStop(Colors.Gray, 0));
            brush.GradientStops.Add(new GradientStop(Colors.DarkGray, 1));
            MainGrid.Background = brush;

            for (int i = 0; i < 4; i++)
            {
                var cloud = new Path
                {
                    Fill = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                    Data = Geometry.Parse("M0,15 C5,5 15,5 20,8 C28,3 38,3 42,10 C50,5 60,5 65,12 C75,8 85,8 90,15 L90,30 L0,30 Z"),
                    Width = 90,
                    Height = 30,
                    Opacity = 0.6,
                    RenderTransform = new TranslateTransform(i * 200, 100)
                };

                WeatherEffectCanvas.Children.Add(cloud);

                var anim = new DoubleAnimation();
                anim.From = 800;
                anim.To = -150;
                anim.Duration = TimeSpan.FromSeconds(40);
                anim.RepeatBehavior = RepeatBehavior.Forever;
                cloud.RenderTransform.BeginAnimation(TranslateTransform.XProperty, anim);
            }
        }
        #endregion

        // 窗口加载时初始化
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 可在此处自动查询默认城市
        }
    }
}
