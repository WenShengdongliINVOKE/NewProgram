using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
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

        public List<Siemens>? _siemens = new List<Siemens>();
        public List<S7Model>? S7Models = new List<S7Model>();
        public ICommand? s7command { get; set; }
        public ICommand? tCPcommand { get; set; }
        public ICommand? rS485command { get; set; }
        public ICommand? rs485TCPcommand { get; set; }

        public ICommand? abbcommand { get; set; }
        ErrorViewModel errorViewModel = new ErrorViewModel();
        public ConnectDeviceViweModel()
        {
            s7command = new RelayCommand(S7ConnCommand);
            abbcommand = new RelayCommand(AbbConnComm);
            
            (S7Models, _) = InitDevice.GetPlcDevice();
        }
        //[ObservableProperty]
        //private string textbolck = "2123";
        //public string TextBolck { get => textbolck;   set => SetProperty(ref textbolck, value); }




        private void AbbConnComm()
        { 
             
             AbbRobot model = new AbbRobot();


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
                        //address = item.S7Address;
                        //rack = Convert.ToInt16(item.S7Lock);
                        //slot = Convert.ToInt16(item.S7Slot);

                        _siemens.Add(new Siemens(item.CpuType, item.S7Address, item.S7Rack, item.S7Slot) { _cpuType = item.CpuType, _address = item.S7Address, _rack = item.S7Rack, _slot = item.S7Slot });
                        _siemens.Add(new Siemens(item.CpuType, item.S7Address, item.S7Rack, item.S7Slot));
                        _siemens.Add(new Siemens(item.CpuType, item.S7Address, item.S7Rack, item.S7Slot));
                        _siemens.Add(new Siemens(item.CpuType, item.S7Address, item.S7Rack, item.S7Slot));
                        //Siemens.WritePlcInt32(item.S7Address, "22");
                        //Siemens.ReadPlc(11, "");
                        //List<string> strings = new List<string>();
                        //List<string> strings1 = new List<string>();
                        //List<string> strings2 = new List<string>();
                        //List<string> strings3 = new List<string>();
                        //strings.Add("1");
                        //strings.Add("8");
                        //strings1.Add("3");
                        //strings1.Add("47");
                        //strings2.Add("52");
                        //strings2.Add("6");
                        //strings3.Add("72");
                        //strings3.Add("7");

                    }


                        //ValuePairs.Add(new List<Tuple<string, Siemens.Type, string, string>>() { new Tuple<string, Siemens.Type, string, string>("192.168.0.11", Siemens.Type.Bool, "名称1", "DB1") });
                        //ValuePairs.Add(new List<Tuple<string, Siemens.Type, string, string>>() { new Tuple<string, Siemens.Type, string, string>("192.168.0.12", Siemens.Type.Bool, "名称2", "DB2") });
                        //ValuePairs.Add(new List<Tuple<string, Siemens.Type, string, string>>() { new Tuple<string, Siemens.Type, string, string>("192.168.0.13", Siemens.Type.Bool, "名称3", "DB3") });
                        //ValuePairs.Add(new List<Tuple<string, Siemens.Type, string, string>>() { new Tuple<string, Siemens.Type, string, string>("192.168.0.14", Siemens.Type.Bool, "名称4", "DB4") });
                        //ValuePairs.Add(new Tuple<Siemens.Type, string, string>(Siemens.Type.Bool, "名称2", "DB12"));
                        //ValuePairs.Add(new Tuple<Siemens.Type, string, string>(Siemens.Type.Bool, "名称3", "DB13"));
                        //ValuePairs.Add(new Tuple<Siemens.Type, string, string>(Siemens.Type.Bool, "名称4", "DB14"));
                        //ValuePairs.Add(new Tuple<Siemens.Type, string, string>(Siemens.Type.Bool, "名称5", "DB15"));
                        //ValuePairs.Add(new Tuple<Siemens.Type, string, string>(Siemens.Type.Bool, "名称6", "DB16"));
                        //ValuePairs.Add(new Tuple<Siemens.Type, string, string>(Siemens.Type.Bool, "名称7", "DB17"));


                        //MyDataValue = new ObservableCollection<S7ValueModel>();
                    }
                    catch (Exception ex     )
                    {

                        ErrorViewModel.Errornotice("初始化PLC出错！请检查配置清单！",true,2);
                    }
                }
                else
                {
                    ErrorViewModel.Errornotice("PLC地址不能为空！", true, 1);
                }
            }
            );



        }

        private void CilikeButtonS7()
        {
           
            S7Communication s7Comm = S7Communication.GetWindows();
            //s7Comm.();

        }
    }
}
