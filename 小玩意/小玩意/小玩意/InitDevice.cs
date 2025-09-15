using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Input;
using 小玩意.Model;

namespace 小玩意
{
    public static class InitDevice
    {
        /// <summary>
        /// 加载界面PLC信息
        /// </summary>
        /// <returns> 第一个返回值List<S7Model>是用来实际通讯的PLC设备信息 这里配置了多少个就代表实际有多少个PLC设备 // 第二个返回值ObservableCollection<S7Model> 这个是用来绑定到界面的设备数量 配置了多少就new多少个  </returns>
        public static Tuple<List<S7Model>, ObservableCollection<S7Model>> GetPlcDevice()
        {
            ObservableCollection<S7Model> S7modes;
            List<S7Model> S7modeslist = new List<S7Model>();
            //<List<S7Model>, ObservableCollection<S7Model>>? tuple;
            //TODO:后续补充读取数据库或者配置文件代码
            //S7modes 这个是用来绑定到界面的设备数量 配置了多少就new多少个 
            S7modes = new ObservableCollection<S7Model>()
               {

                new S7Model() { CpuType=S7.Net.CpuType.S71200,  S7Address = "192.168.0.11", S7Rack = 0,S7Slot = 1 },
                 new S7Model() { CpuType=S7.Net.CpuType.S71200, S7Address = "192.168.0.12", S7Rack = 0,S7Slot = 1 },
                 // new S7Model() { CpuType=S7.Net.CpuType.S71200, S7Address = "192.168.0.13", S7Rack = 0,S7Slot = 1 },
                 //  new S7Model() { CpuType=S7.Net.CpuType.S71200, S7Address = "192.168.0.14", S7Rack = 0,S7Slot = 1 },
                 //   new S7Model() { CpuType=S7.Net.CpuType.S71200, S7Address = "192.168.0.15", S7Rack = 0,S7Slot = 1 },
                 //     new S7Model() { CpuType=S7.Net.CpuType.S71200, S7Address = "192.168.0.16", S7Rack = 0,S7Slot = 1 }
               };


            //这个是用来实际通讯的PLC设备信息 这里配置了多少个就代表实际有多少个PLC设备
            S7modeslist.Add(new S7Model() { CpuType = S7.Net.CpuType.S71200, S7Address = "192.168.0.11", S7Rack = 0, S7Slot = 1 });
            S7modeslist.Add(new S7Model() { CpuType = S7.Net.CpuType.S71200, S7Address = "192.168.0.12", S7Rack = 0, S7Slot = 1 });
            //S7modeslist.Add(new S7Model() { CpuType = S7.Net.CpuType.S71200, S7Address = "192.168.0.13", S7Rack = 0, S7Slot = 1 });
            //S7modeslist.Add(new S7Model() { CpuType = S7.Net.CpuType.S71200, S7Address = "192.168.0.14", S7Rack = 0, S7Slot = 1 });
            //S7modeslist.Add(new S7Model() { CpuType = S7.Net.CpuType.S71200, S7Address = "192.168.0.15", S7Rack = 0, S7Slot = 1 });
            //S7modeslist.Add(new S7Model() { CpuType = S7.Net.CpuType.S71200, S7Address = "192.168.0.16", S7Rack = 0, S7Slot = 1 });

            //var str = Serialize(new S7Model() { CpuType = S7.Net.CpuType.S71200, S7Address = "192.168.0.11", S7Rack = 0, S7Slot = 1 });
            return Tuple.Create(S7modeslist, S7modes);
        }

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
            }).Select(pp => new { Key = pp.Name,Value = pp.GetValue(obj) }).Select(o=>$"{o.Key}:{o.Value}");
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
