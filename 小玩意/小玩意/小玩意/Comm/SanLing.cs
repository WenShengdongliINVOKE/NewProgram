using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using HslCommunication;
using HslCommunication.Profinet.Melsec;
using OfficeOpenXml.Drawing.Slicer.Style;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 小玩意.Model;
using 小玩意.ViewModel;

namespace 小玩意.Comm
{
    class SanLing : IDisposable, IPlccommiunctionCommand
    {
        /// <summary>
        /// 数据类型 枚举划分
        /// </summary>
        public enum Type
        {
            Bool = 0,
            Int16 = 1,
            Int32 = 2,
            String = 3,
            Real = 4,

        }
        private string _address;

        private int _port;
        // 实例化对象，指定PLC的ip地址和端口号
        MelsecMcNet melsecMc;
        OperateResult conn;


        public SanLing(string address, int port)
        {
            _address = address;
            _port = port;
            melsecMc = new MelsecMcNet(_address, _port);
        }
        public bool Connect()
        {
            conn = melsecMc.ConnectServer();
            if (conn.IsSuccess)
            {
                ///TODO这里要加日志
                return true;
            }
            ///这里要加日志
            return false;
        }




        public bool WritePlcInt32(string DBaddress, string value)
        {
            if (conn.IsSuccess)
            {
                melsecMc.Write(DBaddress, value);
                return true;
            }
            ///TODO 加日志
            return false;
        }

        public bool WritePlcInt16(string DBaddress, string value)
        {
            if (conn.IsSuccess)
            {
                melsecMc.Write(DBaddress, value);
                return true;
            }
            ///TODO 加日志
            return false;
            //throw new NotImplementedException();
        }

        public bool WritePlcByte(string DBaddress, string value)
        {
            if (conn.IsSuccess)
            {
                melsecMc.Write(DBaddress, value);
                return true;
            }
            ///TODO 加日志
            return false;
        }
        /// <summary>
        /// 暂未实现 在SanLing中弃用
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public object ReadPlc<T>(T type, string address)
        {
            object d = new object();
            if (this.melsecMc != null)
            {

                if (!this.conn.IsSuccess)
                {
                    //PLC未连接，正在尝试重连！
                    conn = this.melsecMc.ConnectServer();
                    if (!conn.IsSuccess)
                    {
                        ErrorViewModel.Errornotice("重连失败",true,3);
                        return null;
                    }

                }
               
                   
                    switch (type)
                    {
                        case true:
                            d = melsecMc.ReadBool(address);
                            break;
                        case var ttype when type is Int16:
                            d = melsecMc.ReadInt16(address);
                            break;
                        case var ttype when type is Int32:
                            d = melsecMc.ReadInt32(address);
                            break;
                        case var ttype when type is float:
                            d = melsecMc.ReadFloat(address);
                            break;
                        case var ttype when type is string:
                            d = melsecMc.ReadString(address, 30);
                            break;
                        default:
                            break;
                    }
                
                //T d = (T)this._plc.Read(address);
                return d;
            }
            else
            {
                //TODO: 此处需要增加日志

                //PLC未初始化!
                return d;
            }


            //throw new NotImplementedException();
            //if (conn.IsSuccess)
            //{
            //    T t = (T)melsecMc.Read(address, 100);
            //    return 
            //}
            /////TODO 加日志
            //return false;
        }

        /// <summary>
        /// 读取已知地址的数据
        /// </summary>
        /// <param name="DataAddresss"></param>
        /// <returns></returns>
        public async Task<List<Tuple<string,string,string>>> GetAllPlcDataAddress(List<Tuple<string, SanLing.Type, string, string>> DataAddresss)
        {
            /// <summary>
            /// 存储返回的数据  数据名称和DB地址还有数据值
            /// </summary>
            List<Tuple<string, string, string>> addrssList = new List<Tuple<string, string, string>>();
            //这里按照数据类型去分类 把相同类型全都放到一个类型去做读取操作
            //if (false)  //没有PLC实物 连接时  这里不执行读取PLC操作 这里考虑加锁？
            //{
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
                    var ReadValue = melsecMc.ReadBool(item.Item3);
                        addrssList.Add(new Tuple<string, string, string>(item.Item1.ToString(), item.Item2.ToString(), ReadValue.ToString()));
                }
                    foreach (var item in Int16)
                {
                    var ReadValue = melsecMc.ReadInt16(item.Item3);
                    addrssList.Add(new Tuple<string, string, string>(item.Item1.ToString(), item.Item2.ToString(), ReadValue.ToString()));
                }
                foreach (var item in Int32)
                {
                    var ReadValue = melsecMc.ReadInt32(item.Item3);
                    //addrssList.Add(new Tuple<string, string, string>(item.Item1.ToString(), item.Item2.ToString(), ReadValue.ToString()));

                }
                foreach (var item in Real)
                {
                    var ReadValue = melsecMc.ReadFloat(item.Item3);
                    //addrssList.Add(new Tuple<string, string, string>(item.Item1.ToString(), item.Item2.ToString(), ReadValue.ToString()));
                }
                foreach (var item in String)
                {
                    var ReadValue = melsecMc.ReadString( item.Item3,30);
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
            //}
            return addrssList;


            //throw new NotImplementedException();
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            if (conn.IsSuccess)
            {
                melsecMc.Dispose();
            }
            else
            {
                ///这里加日志
            }
        }

    }
}
