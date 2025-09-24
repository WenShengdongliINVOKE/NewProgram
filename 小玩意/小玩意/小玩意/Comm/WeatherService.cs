using Newtonsoft.Json.Linq;
using NLog;
using System.Net.Http;
using 小玩意.Model;

namespace 小玩意.Comm
{
    class WeatherService
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        // 构造函数：传入你的 OpenWeatherMap API Key
        public WeatherService(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
        }

        // 获取指定城市的当天天气
        public async Task<WeatherInfo> GetWeatherAsync(string cityName)
        {
            try
            {
                // OpenWeatherMap API 地址（获取当前天气）
                string url = $"http://api.openweathermap.org/data/2.5/weather?q={cityName}&appid={_apiKey}&units=metric&lang=zh_cn";

                HttpResponseMessage response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();

                // 解析 JSON
                JObject data = JObject.Parse(json);

                var weather = new WeatherInfo
                {
                    City = data["name"].ToString(),
                    Country = data["sys"]["country"].ToString(),
                    Temperature = (double)data["main"]["temp"],
                    Humidity = (int)data["main"]["humidity"],
                    WindSpeed = (double)data["wind"]["speed"],
                    Description = data["weather"][0]["description"].ToString(),
                    DateTime = DateTime.Now
                };

                return weather;
            }
            catch (HttpRequestException httpEx)
            {
                throw new Exception($"网络请求失败: {httpEx.Message}");
            }
            catch (Newtonsoft.Json.JsonException jsonEx)
            {
                throw new Exception($"解析天气数据失败: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                // 城市未找到、API 密钥错误等
                throw new Exception($"获取天气失败: {ex.Message}");
            }
        }
    }
}
