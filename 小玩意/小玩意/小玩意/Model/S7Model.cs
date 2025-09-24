using S7.Net;
using System.ComponentModel;
namespace 小玩意.Model
{
    public class S7Model
    {
        /// <summary>
        /// 默认是1200系列
        /// </summary>
        [Browsable(true)]
        public CpuType CpuType { get; set; } = CpuType.S71200;
        public string? S7Address { get; set; }
        /// <summary>
        /// 默认为0
        /// </summary>
        public short S7Rack { get; set; } = 0;
        /// <summary>
        /// 默认为0
        /// </summary>
        public short S7Slot { get; set; } = 0;
        public S7Model()
        {

        }

    }
}
