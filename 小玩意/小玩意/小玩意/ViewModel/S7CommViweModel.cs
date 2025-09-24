using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NLog;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using 小玩意.Comm;
using 小玩意.Model;

namespace 小玩意.ViewModel
{
    public partial class S7CommViweModel : ObservableObject, INotifyPropertyChanged
    {

        //object obj;
        // 实现 INotifyPropertyChanged 接口
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public ICommand ConnCommand { get; }
      


        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// 当前选中设备的索引
        /// </summary>
        private S7Model _selectedSide;
        public S7Model SelectedSide
        {
            get => _selectedSide;
            set =>

                SetProperty(ref _selectedSide, value);


        }
        /// <summary>
        /// 注意！ 使用 ObservableCollection 这个集合时 需要先实例化 PLC设备集合 这里配置了多少个就代表实际有多少个PLC设备
        /// </summary>
        public ObservableCollection<S7Model>? _myDevice;
        public ObservableCollection<S7Model>? MyDevice
        {
            get => _myDevice;
            set
            {
                SetProperty(ref _myDevice, value);
                if (_myDevice != null && _myDevice.Count > 0)
                {
                    SelectedSide = _myDevice[0];
                }
            }
        }

        /// <summary>
        /// 注意！ 使用 ObservableCollection 这个集合时 需要先实例化 PLC数据集合 这里是用来绑定到界面的数据 里面包含了名称 地址  当前值
        /// </summary>
        public ObservableCollection<S7ValueModel>? _myDataValue;
        /// <summary>
        /// PLC数据集合 这里是用来绑定到界面的数据 里面包含了名称 地址  当前值
        /// </summary>
        public ObservableCollection<S7ValueModel>? MyDataValue { get => _myDataValue; set => SetProperty(ref _myDataValue, value); }
        /// <summary>
        /// PLC设备信息集合 这里配置了多少个就代表实际有多少个PLC设备
        /// </summary>
        public List<S7Model>? S7Models = new List<S7Model>();
        /// <summary>
        /// 西门子PLC通讯类集合 这里会根据S7Models的数量 实例化多少个Siemens对象
        /// </summary>
        public List<Siemens>? _siemens = new List<Siemens>();

        /// <summary>
        /// 这里是一个二维集合 外层List代表PLC设备数量 内层List代表每个PLC设备下的所有的地址的 名称  数据类型 地址 当前值
        /// </summary>
        List<List<Tuple<string, Siemens.Type, string>>> ValuePairs = new List<List<Tuple<string, Siemens.Type, string>>>();
        /// <summary>
        /// 当前选中PLC的索引值
        /// </summary>
        public int SelectedIndex { get; set; } = 0;

        private static S7CommViweModel s7CommViweModel;

        private static readonly object _obj = new object();



        public S7CommViweModel()
        {
            ConnCommand = new RelayCommand(S7ConnCommand);
          
            GetPlcDevice();
            Thread.Sleep(100);
            GetSelectPLCAllData();
           

        }
        /// <summary>
        /// 获取PLC设备信息 并加载到界面
        /// </summary>
        private void GetPlcDevice()
        {
            //MyDevice = InitDevice.InterfaceModels;
            MyDevice = new ObservableCollection<S7Model>();
            foreach (var item in InitDevice.DeviceModels)
            {
                MyDevice.Add(new S7Model() { S7Address = item.S7Address, S7Rack = item.S7Rack, S7Slot = item.S7Slot, CpuType = item.CpuType });
            }
            ValuePairs = InitDevice.SiemensValuePairs;
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
                   foreach (var item in S7Models)
                   {
                       _siemens.Add(new Siemens(item.CpuType, item.S7Address, item.S7Rack, item.S7Slot) { _cpuType = item.CpuType, _address = item.S7Address, _rack = item.S7Rack, _slot = item.S7Slot });
                   }

               }
               else
               {
                   logger.Info("初始化PLC出错！请检查配置清单！");
                   ErrorViewModel.Errornotice("PLC地址不能为空！", true, 1);
               }
           }
           );



        }
        bool Tmp = true;
        string Tmpnums = string.Empty;
        /// <summary>
        /// 向界面加载当前选中PLC的所有数据 默认优先加载第一个
        /// </summary>
        private async Task GetSelectPLCAllData()
        {

            await Task.Factory.StartNew(async () =>
        {
            while (true)
            {
                try
                {
                   
                    //根据当前选择的PLC对象去找它的实例 找到此实例之后 调用它自己的 GetAllPlcDataAddress 方法
                 
                    if (ValuePairs.Any() && ConnectDeviceViweModel._siemens.Any())
                    {
                        var ReadAllPlc = await ConnectDeviceViweModel._siemens.FirstOrDefault(o => o._address == SelectedSide.S7Address).GetAllPlcDataAddress(ValuePairs.FirstOrDefault(o => o.All(o => o.Item1 == SelectedSide.S7Address)));
                       
                        if ((Tmp) || Tmpnums != SelectedSide.S7Address)
                        {
                            Tmp = false;
                            Tmpnums = SelectedSide.S7Address;
                            //异步切换UI线程防止出现跨线程异常
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                if (MyDataValue.Any())
                                {
                                    MyDataValue.Clear();
                                }

                                foreach (var item in ReadAllPlc)
                                {
                                    MyDataValue.Add(new S7ValueModel() { Address = item.Address, Name = item.Name, Value = item.Value });
                                }
                            });
                        }

                        //异步切换UI线程防止出现跨线程异常
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            //每次读取PLC数据后 更新界面数据
                            foreach (var ReadAllPlcValue in MyDataValue)
                            {
                                ReadAllPlcValue.Name = ReadAllPlcValue.Name;
                                ReadAllPlcValue.Address = ReadAllPlcValue.Address;
                                ReadAllPlcValue.Value = ReadAllPlcValue.Value;
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "读取PLC数据出错！");
                    //ErrorViewModel.Errornotice(ex.Message, true, 1);
                }
            }


        });
        }
       

    }
}
