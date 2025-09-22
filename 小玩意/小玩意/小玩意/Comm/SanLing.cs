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
        public T ReadPlc<T>(T type, string address)
        {
            throw new NotImplementedException();
            //if (conn.IsSuccess)
            //{
            //    T t = (T)melsecMc.Read(address, 100);
            //    return 
            //}
            /////TODO 加日志
            //return false;
        }

        public async Task<List<SanLingValueModel>> GetAllPlcDataAddress(List<Tuple<string, Siemens.Type, string, string>> DataAddresss)
        {
            /// <summary>
            /// 存储返回的数据  数据名称和DB地址还有数据值
            /// </summary>
            List<SanLingValueModel> addrssList = new List<SanLingValueModel>();
            //这里按照数据类型去分类 把相同类型全都放到一个类型去做读取操作
            //if (false)  //没有PLC实物 连接时  这里不执行读取PLC操作 这里考虑加锁？
            //{
            await Task.Factory.StartNew(() =>
            {
                try
                {
                    foreach (var item in DataAddresss)
                    {
                        var ReadValue = melsecMc.Read( item.Item3,10);
                        addrssList.Add(new SanLingValueModel() { Name = item.Item1.ToString(), Address = item.Item2.ToString(), Value = ReadValue.ToString() });
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
