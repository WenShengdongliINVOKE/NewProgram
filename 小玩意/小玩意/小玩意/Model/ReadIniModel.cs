using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 小玩意.Model
{
    /// <summary>
    /// 用于读取INI文件的模型类 此类为每一种配置项目单独创建一个属性 方便后续扩展
    /// </summary>
    class ReadIniModel
    {

        public List<Tuple<string, string, string, string, string>> Sheet1 { get; set; } = new List<Tuple<string, string, string, string, string>>();
        public List<Tuple<string, string, string, string, string>> Sheet2 { get; set; } = new List<Tuple<string, string, string, string, string>>();
        public List<Tuple<string, string, string, string, string>> Sheet3 { get; set; } = new List<Tuple<string, string, string, string, string>>();
        public List<Tuple<string, string, string, string, string>> Sheet4 { get; set; } = new List<Tuple<string, string, string, string, string>>();
        public List<Tuple<string, string, string, string, string>> Sheet5 { get; set; } = new List<Tuple<string, string, string, string, string>>();
        public List<Tuple<string, string, string, string, string>> Sheet6 { get; set; } = new List<Tuple<string, string, string, string, string>>();
        public List<Tuple<string, string, string, string, string>> Sheet7 { get; set; } = new List<Tuple<string, string, string, string, string>>();
    }
}
