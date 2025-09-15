using S7.Net;
using System.Text;
using System.Windows;
using 小玩意.Model;
using 小玩意.ViewModel;

namespace 小玩意.Comm
{
    public class Siemens :IPlccommiunctionCommand
    {
        public enum Type
        {
            Bool = 0,
            Int16 = 1,
            Int32 = 2,
            String = 3,
            Real = 4,

        }

        public bool iSconn;
        public CpuType _cpuType;

        public string _address;
        public short _rack;
        public short _slot;
        public Plc? _plc;
        public string? value;


        /// <summary>
        /// 初始化PLC
        /// </summary>
        /// <param name="cpuType">CPU类型</param>
        /// <param name="address">地址</param>
        /// <param name="rack">机架号</param>
        /// <param name="slot"></param>
        public Siemens(CpuType cpuType, string address, short rack, short slot)
        {
            if (false)
            {
                try
                {

                    this._cpuType = cpuType;
                    this._address = address;
                    this._rack = rack;
                    this._slot = slot;
                    this.value = "";
                    this._plc = new Plc(this._cpuType, this._address, this._rack, this._slot);
                    if (this._plc != null)
                    {
                        Application.Current.Dispatcher.Invoke(new Action(async () =>
                        {
                            await this._plc.OpenAsync();
                            if (this._plc.IsConnected)
                            {
                                this.iSconn = this._plc.IsConnected;
                            }
                            else
                            {
                                this.iSconn = this._plc.IsConnected;
                            }
                        }));
                    }

                }
                catch (Exception ex)
                {

                    //TODO:需要添加日志
                }
            }

        }
        /// <summary>
        /// 向PLC写入Int类型
        /// </summary>
        /// <param name="DBaddress">DB地址</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public bool WritePlcInt32(string DBaddress, string value)
        {
            if (this._plc != null)
            {
                if (!this.iSconn)
                {
                    //PLC未连接，正在尝试重连！
                    this._plc.Open();
                    WritePlcInt32(DBaddress, value);
                    return true;
                }

                this._plc.Write(DBaddress, value);
                return true;
            }
            else
            {
                //TODO: 此处需要增加日志

                //PLC未初始化!
                return false;
            }
        }
        /// <summary>
        /// 向PLC写入Int16位
        /// </summary>
        /// <param name="DBaddress"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool WritePlcInt16(string DBaddress, string value)
        {
            if (this._plc != null)
            {
                if (!this.iSconn)
                {
                    //PLC未连接，正在尝试重连！
                    this._plc.Open();
                    WritePlcInt16(DBaddress, value);
                    return true;
                }

                this._plc.Write(DBaddress, value);
                return true;
            }
            else
            {
                //TODO: 此处需要增加日志

                //PLC未初始化!
                return false;
            }
        }
        /// <summary>
        /// 写入Byte
        /// </summary>
        /// <param name="DBaddress"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool WritePlcByte(string DBaddress, string value)
        {

            if (this._plc != null)
            {
                Tuple<int, int> tuple = GetString(DBaddress);
                if (!this.iSconn)
                {
                    //PLC未连接，正在尝试重连！
                    this._plc.Open();
                    WritePlcByte(DBaddress, value);
                    return true;
                }

                this._plc.WriteBytes(DataType.Memory, tuple.Item1, tuple.Item2, Encoding.UTF8.GetBytes(value));
                return true;
            }
            else
            {
                //TODO: 此处需要增加日志

                //PLC未初始化!
                return false;
            }

        }
        /// <summary>
        /// 读取PLC内部数据  因为使用了类型推断来读取数据 所以在传参时需要传入一个目标参数 T 决定了读取和返回的数据类型
        /// </summary>
        /// <typeparam name="T">T作为类型推断根据传入的类型返回数据</typeparam>
        /// <param name="value">类型</param>
        /// <param name="address">地址</param>
        /// <returns></returns>
        public T ReadPlc<T>(T type, string address)
        {
            if (this._plc != null)
            {

                if (!this.iSconn)
                {
                    //PLC未连接，正在尝试重连！
                    this._plc.Open();
                    T t = ReadPlc(type, address);
                    return t;
                }

                T d = (T)this._plc.Read(address);
                return d;
            }
            else
            {
                //TODO: 此处需要增加日志

                //PLC未初始化!
                return default(T);
            }
        }

        /// <summary>
        /// 字符串处理
        /// </summary>
        /// <param name="strvalue"></param>
        /// <returns></returns>
        private Tuple<int, int> GetString(string strvalue)
        {
            //首先提取我需要的字符串
            var str = _address.Split('.');
            //再次拆解
            var db = Convert.ToInt32(str[0].Where(s => int.TryParse($"{s}", out int _)));
            var startByte = Convert.ToInt32(str[1].Where(s => int.TryParse($"{s}", out int _)));
            return Tuple.Create(db, startByte);
        }

        /// <summary>
        /// 获取当前PLC所有的地址数据   返回包含了 名称  地址 值  的 元组数组
        /// </summary>
        /// <param name="DataAddresss">Type是类型   List<Tuple<string,string>> 第一个string是变量名称，第二个是变量地址 </param>
        public async Task<List<S7ValueModel>> GetAllPlcDataAddress(List<Tuple<string, Siemens.Type, string, string>> DataAddresss)
        {
            /// <summary>
            /// 存储返回的数据  数据名称和DB地址还有数据值
            /// </summary>
            List<S7ValueModel> addrssList = new List<S7ValueModel>();
            //这里按照数据类型去分类 把相同类型全都放到一个类型去做读取操作
            await Task.Factory.StartNew(() =>
            {
                try
                {
                    var Bool = DataAddresss.FindAll(o => o.Item2 == Type.Bool);
                    var Int16 = DataAddresss.FindAll(o => o.Item2 == Type.Int16);
                    var Int32 = DataAddresss.FindAll(o => o.Item2 == Type.Int32);
                    var Real = DataAddresss.FindAll(o => o.Item2 == Type.Real);
                    var String = DataAddresss.FindAll(o => o.Item2 == Type.String);
                    foreach (var item in Bool)
                    {
                        var ReadValue = ReadPlc(true, item.Item3);
                        addrssList.Add(new S7ValueModel() { Name = item.Item1.ToString(), Address = item.Item2.ToString(), Value = ReadValue.ToString() });
                    }
                    foreach (var item in Int16)
                    {
                        var ReadValue = ReadPlc(1, item.Item3);
                        //addrssList.Add(new Tuple<string, string, string>(item.Item1.ToString(), item.Item2.ToString(), ReadValue.ToString()));
                    }
                    foreach (var item in Int32)
                    {
                        var ReadValue = ReadPlc(1, item.Item3);
                        //addrssList.Add(new Tuple<string, string, string>(item.Item1.ToString(), item.Item2.ToString(), ReadValue.ToString()));

                    }
                    foreach (var item in Real)
                    {
                        var ReadValue = ReadPlc(1.1, item.Item3);
                        //addrssList.Add(new Tuple<string, string, string>(item.Item1.ToString(), item.Item2.ToString(), ReadValue.ToString()));
                    }
                    foreach (var item in String)
                    {
                        var ReadValue = ReadPlc("", item.Item3);
                        //addrssList.Add(new Tuple<string, string, string>(item.Item1.ToString(), item.Item2.ToString(), ReadValue.ToString()));
                    }

                }
                catch (Exception ex)
                {
                    //TODO: 此处需要增加日志

                    ErrorViewModel.Errornotice($"读取数据失败！Siemens.cs{ex.ToString()}", true, 1);
                    //throw;
                }
            }
            );
            return addrssList;
            // 这里按上面分好的类型去读取PLC的所有地址数据 每一种类型按分类去读取 每次读取一个
            // Task.Factory.StartNew(() =>
            //{ 
            //    int j = 0;
            //    while (true)
            //    {
            //        //foreach (var item in addrssList.Keys)
            //        //{
            //        //    if (item==Type.Int32.ToString())
            //        //    {
            //        //        foreach (var item in collection)
            //        //        {

            //        //        }
            //        //    }
            //        //}
            //        addrssList[DataAddresss[Type.Bool][j].Item2] = ReadPlc(true, addrssList[DataAddresss[Type.Bool][j].Item2]).ToString();
            //        addrssList[DataAddresss[Type.Int16][j].Item2] = ReadPlc(1, addrssList[DataAddresss[Type.Bool][j].Item2]).ToString();
            //        addrssList[DataAddresss[Type.Int32][j].Item2] = ReadPlc(10, addrssList[DataAddresss[Type.Bool][j].Item2]).ToString();
            //        addrssList[DataAddresss[Type.String][j].Item2] = ReadPlc("", addrssList[DataAddresss[Type.Bool][j].Item2]).ToString();
            //        addrssList[DataAddresss[Type.Real][j].Item2] = ReadPlc(1.1, addrssList[DataAddresss[Type.Bool][j].Item2]).ToString();
            //        j++;
            //        if (j == addrssList.Count) j = 0;
            //    }
            //});

        }
    }
}
