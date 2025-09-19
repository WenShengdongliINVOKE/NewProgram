using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 小玩意.Model
{
    class SaveModel
    {
        /// <summary>
        /// 标题行
        /// </summary>
        public List<string> Title_Row { get; set; }
        /// <summary>
        /// 数据行
        /// </summary>
        public List<List<string>> Data_Row { get; set; }
    }
}
