using 小玩意.Model;

namespace 小玩意.Comm
{
     public interface IPlccommiunctionCommand
    {
        public bool WritePlcInt32(string DBaddress, string value);
        public bool WritePlcInt16(string DBaddress, string value);

        public bool WritePlcByte(string DBaddress, string value);
        public T ReadPlc<T>(T type, string address);
        //public Task<List<T>> GetAllPlcDataAddress(List<Tuple<string, Siemens.Type, string, string>> DataAddresss);
        //public void WritePlcInt32();
        //public void WritePlcInt32();
    }
}
