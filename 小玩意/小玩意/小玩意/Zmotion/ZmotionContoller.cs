using cszmcaux;
using System.IO.Ports;
using System.Text;

namespace 小玩意.Zmotion
{
    class ZmotionContoller
    {
        enum ZmotionCommnuicationType
        {
            TCP = 1,
            SerialPort = 2,

        }


        IntPtr _intPtr;
        private string _ip;
        private int _port;


        private int _baudRate;
        private string _portName;
        private Parity _parity;
        private StopBits _stopBits;
        private int _dataBits;
        /// <summary>
        /// TCP 初始化
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        public ZmotionContoller(string ip, int port)
        {
            _ip = ip;
            _port = port;
            zmcaux.ZAux_OpenEth(_ip, out _intPtr);
        }
        /// <summary>
        /// 串口初始化
        /// </summary>
        /// <param name="baudRate"></param>
        /// <param name="dataBits"></param>
        /// <param name="parity"></param>
        /// <param name="stopBits"></param>
        public ZmotionContoller(int baudRate, int dataBits, Parity parity, StopBits stopBits)
        {
            _baudRate = baudRate;
            _stopBits = stopBits;
            _dataBits = dataBits;
            _parity = parity;
            zmcaux.ZAux_SetComDefaultBaud((uint)_baudRate, (uint)_dataBits, (uint)_parity, (uint)_stopBits);

        }
        /// <summary>
        /// 设置IP
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public int SetIP(string ip)
        {

            return zmcaux.ZAux_SetIp(_intPtr, ip);
        }

        /// <summary>
        /// 搜索IP
        /// </summary>
        /// <returns></returns>
        public int SearchIP()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append('\0', 255);
            return zmcaux.ZAux_SearchEthlist(stringBuilder, 255, 1000);
        }


    }
}
