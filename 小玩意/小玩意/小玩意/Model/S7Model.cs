using S7.Net;
using System.ComponentModel;
namespace 小玩意.Model
{
    public class S7Model
    {
        [Browsable(true)]
        public CpuType CpuType { get; set; }
        public string? S7Address { get; set; }
        public short S7Rack { get; set; }
        public short S7Slot { get; set; }

    }
}
