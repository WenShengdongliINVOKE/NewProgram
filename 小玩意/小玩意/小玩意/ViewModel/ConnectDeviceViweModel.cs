using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NLog;
using System.Windows.Input;
using 小玩意.Comm;
using 小玩意.Model;

namespace 小玩意.ViewModel
{
    /// <summary>
    /// 这个类只负责所有设备的各种通讯连接 数据展示交互等功能由其它类完成
    /// </summary>
    partial class ConnectDeviceViweModel : ObservableObject
    {

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public static List<Siemens>? _siemens = new List<Siemens>();
        public List<S7Model>? S7Models = new List<S7Model>();
        public ICommand? s7command { get; set; }
        public ICommand? tCPcommand { get; set; }
        public ICommand? rS485command { get; set; }
        public ICommand? rs485TCPcommand { get; set; }

      
        ErrorViewModel errorViewModel = new ErrorViewModel();
        /// <summary>
        /// 初始化所有配置信息
        /// </summary>
        InitDevice InitDevice = new InitDevice();
        public ConnectDeviceViweModel()
        {
            s7command = new RelayCommand(S7ConnCommand);
        }


        /// <summary>
        /// 与PLC建立通讯
        /// </summary>
        private void S7ConnCommand()
        {
            Task.Factory.StartNew(async () =>
            {

                if (S7Models != null)
                {
                    try
                    {
                        foreach (var item in S7Models)
                        {
                            _siemens.Add(new Siemens(item.CpuType, item.S7Address, item.S7Rack, item.S7Slot));
                         
                        }

                    }
                    catch (Exception ex)
                    {
                        
                        ErrorViewModel.Errornotice("初始化PLC出错！请检查配置清单！", true, 2);
                    }
                }
                else
                {
                    ErrorViewModel.Errornotice("PLC地址不能为空！", true, 1);
                }
            }
            );



        }


    }
}
