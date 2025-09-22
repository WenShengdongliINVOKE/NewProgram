using DocumentFormat.OpenXml.Bibliography;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace 小玩意.Model
{
    class WeatherInfo
    {
        public string city { get; set; }
        public string country { get; set; }
        public double temperature { get; set; }     // 温度（℃）
        public int humidity { get; set; }           // 湿度（%）
        public double windSpeed { get; set; }       // 风速（m/s）
        public string description { get; set; }     // 天气描述（如：多云）
        public string iconCode { get; set; }
        public DateTime dateTime { get; set; }      // 获取时间

        public string City
        {
            get => city;
            set { city = value; OnPropertyChanged(nameof(City)); }
        }

        public string Country
        {
            get => country;
            set { country = value; OnPropertyChanged(nameof(Country)); }
        }

        public double Temperature
        {
            get => temperature;
            set { temperature = value; OnPropertyChanged(nameof(Temperature)); }
        }

        public int Humidity
        {
            get => humidity;
            set { humidity = value; OnPropertyChanged(nameof(Humidity)); }
        }

        public double WindSpeed
        {
            get => windSpeed;
            set { windSpeed = value; OnPropertyChanged(nameof(WindSpeed)); }
        }

        public string Description
        {
            get => description;
            set { description = value; OnPropertyChanged(nameof(Description)); }
        }

        public string IconCode
        {
            get => iconCode;
            set { iconCode = value; OnPropertyChanged(nameof(IconCode)); }
        }

        public DateTime DateTime
        {
            get => dateTime;
            set { dateTime = value; OnPropertyChanged(nameof(DateTime)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

