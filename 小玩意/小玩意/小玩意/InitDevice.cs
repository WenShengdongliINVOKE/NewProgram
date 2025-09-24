using NLog;
using S7.Net;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using 小玩意.Comm;
using 小玩意.Model;

namespace 小玩意
{
    /// <summary>
    /// 这个类是加载外部设备信息的 ABB机器人的加载比较特殊，
    /// 它是通过自己的SDK去扫描环境内的机器人的 
    /// 所以不在这里加载直接在机器人的通讯类里面加载
    /// 这个类负责加载所有外部设备和配置文件信息 每一种信息都有一个静态变量存储
    /// </summary>
    public class InitDevice
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// 设备信息 包含通讯参数
        /// </summary>
        public static List<S7Model>? DeviceModels= new List<S7Model>();
       
        /// <summary>
        /// 所有西门子PLC数据点信息
        /// </summary>
        public static List<List<Tuple<string, Siemens.Type, string>>>? SiemensValuePairs= new List<List<Tuple<string, Siemens.Type, string>>>();
        /// <summary>
        /// 所有ABB机器人数据点信息
        /// </summary>
        public static List<Tuple<string ,string,string,string>> AbbValuePairs= new List<Tuple<string, string, string, string>>();

        /// <summary>
        /// 所有三菱PLC数据点信息
        /// </summary>
        public static List<List<Tuple<string, SanLing.Type, string>>>? SanLingValuePairs = new List<List<Tuple<string, SanLing.Type, string>>>();
        /// <summary>
        /// 初始化所有配置信息
        /// </summary>
        public InitDevice()
        {
            var Result = ReadExecl.ReadIniFile();

            if (Result.Sheet1.FirstOrDefault(o=>o.Item5!=null).Item5== "Siemens")
            {
                foreach (var item in Result.Sheet1)
                {
                    DeviceModels.Add(new S7Model()
                    {
                        S7Address = item.Item3,
                        S7Rack = 0,
                        S7Slot = 0,
                        CpuType = CpuType.S71200
                    });
                }
            }
            else if (Result.Sheet1.FirstOrDefault(o => o.Item5 != null).Item5 == "SanLing")
            {
                foreach (var item in Result.Sheet1)
                {
                    
                    DeviceModels.Add(new S7Model()
                    {
                        S7Address = item.Item3,
                        S7Rack = 0,
                        S7Slot = 0,
                        CpuType = CpuType.S71200
                    });
                }

            }
           
                foreach (var item in Result.Sheet2)
                {
                    if (item.Item5.Contains("Siemens"))
                    {
                        SiemensValuePairs.Add(new List<Tuple<string, Siemens.Type, string>>() { new Tuple<string, Siemens.Type, string>(item.Item1, GetSiemensDataType(item.Item2), item.Item3) });
                    }
                    else if (item.Item5.Contains("SanLing"))
                    {
                        SanLingValuePairs.Add(new List<Tuple<string, SanLing.Type, string>>() { new Tuple<string, SanLing.Type, string>(item.Item1, GetSanLingDataType(item.Item2), item.Item3) });

                    }
                }
            foreach (var item in Result.Sheet3)
            {
                AbbValuePairs.Add(new Tuple<string, string, string, string>(item.Item1,item.Item2,item.Item3,item.Item4) );
            }

        }
        /// <summary>
        /// 加载界面PLC信息  这里同时去读取配置文件
        /// </summary>
        /// <returns> 第一个返回值List<S7Model>是用来实际通讯的PLC设备信息 这里配置了多少个就代表实际有多少个PLC设备 // 第二个返回值ObservableCollection<S7Model> 这个是用来绑定到界面的设备数量 配置了多少就new多少个  </returns>
        //public static Tuple<List<S7Model>, ObservableCollection<S7Model>, List<List<Tuple<string, Siemens.Type, string>>>> GetPlcDevice()
        //{
        //    ObservableCollection<S7Model> S7modes = new ObservableCollection<S7Model>();
        //    List<S7Model> S7modeslist = new List<S7Model>();
        //    List<List<Tuple<string, Siemens.Type, string>>> ValuePairs = new List<List<Tuple<string, Siemens.Type, string>>>();
        //    var Result = ReadExecl.ReadIniFile();

        //    foreach (var item in Result.Sheet1)
        //    {
        //        S7modes.Add(new S7Model()
        //        {
        //            S7Address = item.Item3,
        //            S7Rack = Convert.ToInt16(item.Item4),
        //            S7Slot = Convert.ToInt16(item.Item5),
        //            CpuType = CpuType.S71200
        //        });
        //        S7modeslist.Add(new S7Model()
        //        {
        //            S7Address = item.Item3,
        //            S7Rack = Convert.ToInt16(item.Item4),
        //            S7Slot = Convert.ToInt16(item.Item5),
        //            CpuType = CpuType.S71200
        //        });
        //    }

        //    foreach (var item in Result.Sheet2)
        //    {
        //        ValuePairs.Add(new List<Tuple<string, Siemens.Type, string>>() { new Tuple<string, Siemens.Type, string>(item.Item1, GetDataType(item.Item2), item.Item3) });
        //    }

        //    return Tuple.Create(S7modeslist, S7modes, ValuePairs);
        //}

        /// <summary>
        /// 获取数据类型
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private static Siemens.Type GetSiemensDataType(ReadOnlySpan<char> str) => str switch
        {
            var s when s.Equals("Bool", StringComparison.OrdinalIgnoreCase) => Siemens.Type.Bool,
            var s when s.Equals("Int16", StringComparison.OrdinalIgnoreCase) => Siemens.Type.Int16,
            var s when s.Equals("Int32", StringComparison.OrdinalIgnoreCase) => Siemens.Type.Int32,
            var s when s.Equals("String", StringComparison.OrdinalIgnoreCase) => Siemens.Type.String,
            var s when s.Equals("Real", StringComparison.OrdinalIgnoreCase) => Siemens.Type.Real,
            _ => throw new ArgumentException($"不支持的数据类型: {str.ToString()}")
        };

        /// <summary>
        /// 获取数据类型
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private static SanLing.Type GetSanLingDataType(ReadOnlySpan<char> str) => str switch
        {
            var s when s.Equals("Bool", StringComparison.OrdinalIgnoreCase) => SanLing.Type.Bool,
            var s when s.Equals("Int16", StringComparison.OrdinalIgnoreCase) => SanLing.Type.Int16,
            var s when s.Equals("Int32", StringComparison.OrdinalIgnoreCase) => SanLing.Type.Int32,
            var s when s.Equals("String", StringComparison.OrdinalIgnoreCase) => SanLing.Type.String,
            var s when s.Equals("Real", StringComparison.OrdinalIgnoreCase) => SanLing.Type.Real,
            _ => throw new ArgumentException($"不支持的数据类型: {str.ToString()}")
        };



        /// <summary>
        /// 这个是一个通用的对象属性序列化方法
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string Serialize(object obj)
        {
            var res = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p =>
            {
                var attr = p.GetCustomAttribute<BrowsableAttribute>();
                if (attr is not null) return attr.Browsable;
                return true;
            }).Select(pp => new { Key = pp.Name, Value = pp.GetValue(obj) }).Select(o => $"{o.Key}:{o.Value}");
            return string.Join(Environment.NewLine, res);
        }
        public static ObservableCollection<TcpModel> GetTcpDevice()
        {

            ObservableCollection<TcpModel> tcpModels;
            //TODO:后续补充读取数据库或者配置文件代码
            if (true)
            {
                tcpModels = new ObservableCollection<TcpModel>()
               {
                new TcpModel() { TcpAddress = "192.168.0.11", TcpPort = 1 },
                 new TcpModel() { TcpAddress = "192.168.0.12", TcpPort = 1 },
                  new TcpModel() { TcpAddress = "192.168.0.13", TcpPort = 1 },
                   new TcpModel() { TcpAddress = "192.168.0.14", TcpPort = 1 },
                    new TcpModel() { TcpAddress = "192.168.0.15", TcpPort = 1 },
                      new TcpModel() { TcpAddress = "192.168.0.16", TcpPort = 1 }
               };
            }
            return tcpModels;
        }


    }
}
