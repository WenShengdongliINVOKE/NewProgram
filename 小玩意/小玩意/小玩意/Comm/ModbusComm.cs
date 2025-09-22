using System.IO.Ports;
using 小玩意.ViewModel;

namespace 小玩意.Comm
{
    /// <summary>
    /// ModbusRTU通讯类
    /// </summary>
    internal class ModbusComm : IDisposable
    {

        private int _baudRate;

        private string _portName;
        private Parity _parity;
        private StopBits _stopBits;
        private int _dataBits;

        public int Timeout { get; set; } = 1000;
        public int RetryCount { get; set; } = 3;
        private SerialPort SerialPort { get; set; }


        /// <summary>
        /// 初始化ModeBus通讯对象
        /// </summary>
        /// <param name="_baudRate">波特率</param>
        /// <param name="_portName">串口号</param>
        /// <param name="_parity">校验位</param>
        /// <param name="_stopBits">停止位</param>
        /// <param name="_dataBits">数据大小</param>
        /// <param name="_readTimeout">超时时间</param>
        public ModbusComm(int _baudRate, string _portName, Parity _parity
                             , StopBits _stopBits, int _dataBits, int _readTimeout)
        {
            SerialPort = new SerialPort();
            this._baudRate = _baudRate;
            this._portName = _portName;
            this._parity = _parity;
            this._stopBits = _stopBits;
            this._dataBits = _dataBits;

            if (SerialPort.IsOpen!)
            {
                SerialPort.BaudRate = _baudRate;
                SerialPort.PortName = _portName;
                SerialPort.Parity = _parity;
                SerialPort.StopBits = _stopBits;
                SerialPort.DataBits = _dataBits;
            }
            //SerialPort.rea = _readTimeout;



        }
        /// <summary>
        /// 写入单个寄存器
        /// </summary>
        /// <param name="slaveAddress">从站地址</param>
        /// <param name="mode">命令模式</param>
        /// <param name="startAddress">起始地址</param>
        /// <param name="str">写入的数据</param>
        /// <param name="serialPort">通讯对象实例</param>
        /// <returns></returns>
        public byte[] WriteSingleRegister(int slaveAddress, byte mode, short startAddress, string str)
        {
            ///存储发送的报文
            byte[] message = null;
            if (SerialPort != null)
            {
                List<ushort> registers = new List<ushort>();
                for (int i = 0; i < str.Length; i++)
                {
                    registers.Add(str[i]);
                }
                message = Message.GetReadMessage(slaveAddress, mode, startAddress, short.Parse(str));
                //发送命令
                ExecuteWithRetry(() => SerialPort.Write(message, 0, message.Length));
                return message;
            }
            return null;
        }

        /// <summary>
        /// 写入单个线圈
        /// </summary>
        /// <param name="slaveAddress">从站地址</param>
        /// <param name="mode">命令模式</param>
        /// <param name="startAddress">起始地址</param>
        /// <param name="str">写入的数据</param>
        /// <param name="serialPort">通讯对象实例</param>
        /// <returns></returns>
        public byte[] WriteSingleCoil(int slaveAddress, byte mode, short startAddress, string str)
        {
            ///存储发送的报文
            byte[] message = null;
            bool value = false;
            if (SerialPort != null)
            {
                if (string.Equals(str, "True", StringComparison.OrdinalIgnoreCase) || str == "1")
                {
                    value = true;
                }
                else if (string.Equals(str, "False", StringComparison.OrdinalIgnoreCase) || str == "0")
                {
                    value = false;
                }
                message = Message.MessageGeneration(slaveAddress, mode, startAddress, value);


                // byte [] result = GetReadMessage(slaveAddress, mode, startAddress, (bool)textBox2.Text);
                //发送命令
                ExecuteWithRetry(() => SerialPort.Write(message, 0, message.Length));
                return message;
            }
            return null;
        }

        /// <summary>
        /// 批量写入线圈
        /// </summary>
        /// <param name="slaveAddress">从站地址</param>
        /// <param name="mode">命令模式</param>
        /// <param name="startAddress">起始地址</param>
        /// <param name="str">写入的数据</param>
        /// <param name="serialPort">通讯对象实例</param>
        /// <returns></returns>
        public byte[] WriteArrayCoil(int slaveAddress, byte mode, short startAddress, string str)
        {
            ///存储发送的报文
            byte[] message = null;

            //转换输入的值为大写并去除空格
            str = str.ToUpper();
            str = str.Replace(" ", "");


            if (SerialPort != null)
            {
                //生成写入多个线圈的报文
                List<bool> value = new List<bool>();

                bool temp = false;
                if (string.Equals(str, "True", StringComparison.OrdinalIgnoreCase) || str == "1")
                {
                    temp = true;
                }
                else if (string.Equals(str, "False", StringComparison.OrdinalIgnoreCase) || str == "0")
                {
                    temp = false;
                }
                else
                {
                    ErrorViewModel.Errornotice("输入的数值只能是 1 ，0  或 True , False ", true, 1);
                }

                value.Add(temp);

                message = Message.MessageGeneration(slaveAddress, mode, startAddress, value);
                //发送命令
                ExecuteWithRetry(() => SerialPort.Write(message, 0, message.Length));
                return message;
            }
            return null;
        }

        /// <summary>
        /// 批量写入寄存器
        /// </summary>
        /// <param name="slaveAddress">从站地址</param>
        /// <param name="mode">命令模式</param>
        /// <param name="startAddress">起始地址</param>
        /// <param name="str">写入的数据</param>
        /// <param name="serialPort">通讯对象实例</param>
        /// <returns></returns>
        public byte[] WriteArrayRegister(int slaveAddress, byte mode, short startAddress, string str)
        {
            ///存储发送的报文
            byte[] message = null;

            str = str.Replace(" ", "");
            //生成写入多个寄存器的报文
            if (SerialPort != null)
            {
                try
                {
                    short value = short.Parse(str);
                    message = Message.MessageGeneration(slaveAddress, mode, startAddress, value);
                }
                catch (Exception)
                {
                    ErrorViewModel.Errornotice("输入有误 ", true, 1);
                    return null;
                }
                //发送命令
                ExecuteWithRetry(() => SerialPort.Write(message, 0, message.Length));
                return message;
            }
            return null;
        }

        /// <summary>
        /// 读取已知所有地址的所有数据
        /// </summary>
        /// <param name="slaveAddress"></param>
        /// <param name="startAddress"></param>
        /// <param name="numberOfRegisters"></param>
        public void ReadAllData(int slaveAddress, short startAddress, short numberOfRegisters)
        {
            List<byte[]> bytes = new List<byte[]>();
            bytes = ReadRegister(slaveAddress, startAddress, numberOfRegisters, true);
            bytes = ReadPutRegister(slaveAddress, startAddress, numberOfRegisters);
            bytes = ReadOutCoil(slaveAddress, startAddress, numberOfRegisters);
            bytes = ReadPutCoil(slaveAddress, startAddress, numberOfRegisters);
        }

        /// <summary>
        /// 读输入寄存器
        /// </summary>
        /// <param name="serialPort">通讯对象</param>
        /// <param name="slaveAddress"></param>
        /// <param name="mode"></param>
        /// <param name="startAddress"></param>
        /// <param name="numberOfRegisters"></param>
        /// <returns></returns>
        private List<byte[]> ReadPutRegister(
             int slaveAddress, short startAddress, short numberOfRegisters)
        {
            byte mode;
            byte[] ReadData = null;
            byte[] data = null;
            List<byte[]> bytes = new List<byte[]>();
            if (SerialPort != null)
            {
                mode = 0x04;
                data = Message.GetReadMessage(slaveAddress, mode, startAddress, numberOfRegisters);
                //bytes.Add(data);
                SerialPort.Write(data, 0, data.Length);

                Thread.Sleep(20);
                SerialPort.Read(ReadData, 0, SerialPort.BytesToRead);

                bytes.Add(data);
                bytes.Add(ReadData);
                return bytes;
            }
            return bytes;
        }
        /// <summary>
        /// 读保持型寄存器
        /// </summary>
        /// <param name="serialPort"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="startAddress"></param>
        /// <param name="numberOfRegisters"></param>
        /// <returns></returns>
        public List<byte[]> ReadRegister(
             int slaveAddress, short startAddress, short numberOfRegisters, bool b)
        {
            byte mode;
            byte[] ReadData = new byte[13];
            byte[] data = null;
            List<byte[]> bytes = new List<byte[]>();
            mode = 0x03;
            data = Message.GetReadMessage(slaveAddress, mode, startAddress, numberOfRegisters);
            if (b)
            {
                SerialPort.Write(data, 0, data.Length);
            }

            Thread.Sleep(100);

            SerialPort.Read(ReadData, 0, SerialPort.BytesToRead);

            bytes.Add(data);
            bytes.Add(ReadData);
            return bytes;
        }
        /// <summary>
        /// 读输出线圈
        /// </summary>
        /// <param name="serialPort"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="startAddress"></param>
        /// <param name="numberOfRegisters"></param>
        /// <returns></returns>
        public List<byte[]> ReadOutCoil(
             int slaveAddress, short startAddress, short numberOfRegisters)
        {
            byte mode;
            byte[] ReadData = null;
            byte[] data = null;
            List<byte[]> bytes = new List<byte[]>();
            mode = 0x02;
            data = Message.GetReadMessage(slaveAddress, mode, startAddress, numberOfRegisters);
            bytes.Add(data);
            SerialPort.Write(data, 0, data.Length);

            Thread.Sleep(1000);
            SerialPort.Read(ReadData, 0, SerialPort.BytesToRead);
            data[1] = 0x06;
            bytes.Add(data);
            bytes.Add(ReadData);
            return bytes;
        }
        /// <summary>
        /// 读输入线圈
        /// </summary>
        /// <param name="serialPort"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="startAddress"></param>
        /// <param name="numberOfRegisters"></param>
        /// <returns></returns>
        private List<byte[]> ReadPutCoil(
             int slaveAddress, short startAddress, short numberOfRegisters)
        {
            byte mode;
            byte[] ReadData = null;
            byte[] data = null;
            List<byte[]> bytes = new List<byte[]>();
            mode = 0x01;
            data = Message.GetReadMessage(slaveAddress, mode, startAddress, numberOfRegisters);
            bytes.Add(data);
            SerialPort.Write(data, 0, data.Length);

            Thread.Sleep(1000);
            SerialPort.Read(ReadData, 0, SerialPort.BytesToRead);
            data[1] = 0x06;
            bytes.Add(data);
            bytes.Add(ReadData);
            return bytes;
        }


        /// <summary>
        /// 向串口写入字符串数据
        /// </summary>
        /// <param name="Message"></param>
        /// <returns></returns>
        public bool WriteStringData(string Message)
        {
            if (SerialPort.IsOpen)
            {
                SerialPort.WriteLine(Message);
                Thread.Sleep(200);
                var ReadString = SerialPort.ReadExisting();
                //SerialPort.DataReceived += ReadString;
            }
            else
            {

                ErrorViewModel.Errornotice("串口未连接，正在重新初始化串口 初始换完成后将重新发送", true, 2);
                Thread.Sleep(500);
                SerialPort = new SerialPort(this._portName, this._baudRate, this._parity, this._dataBits, this._stopBits);
                SerialPort.Open();
                if (SerialPort.IsOpen)
                {
                    SerialPort.WriteLine(Message);
                }
                else
                {
                    ErrorViewModel.Errornotice("串口打开失败，请检查硬件连接是否完好", true, 2);
                }

                return false;
            }

            return true;
        }
        private void ExecuteWithRetry(Action action)
        {
            if (!SerialPort.IsOpen)
            {
                throw new Exception("未连接到Modbus设备");
            }

            int attempt = 0;
            Exception lastException = null;

            while (attempt < RetryCount)
            {
                try
                {
                    action();
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    attempt++;

                    if (attempt < RetryCount)
                    {
                        Thread.Sleep(100); // 等待一段时间后重试
                    }
                }
            }

            throw new Exception($"Modbus操作失败，重试{RetryCount}次后仍失败", lastException);
        }


        public void Dispose()
        {
            this.SerialPort?.Dispose();
            ErrorViewModel.Errornotice("串口已关闭", false, 6);
        }
    }
}
