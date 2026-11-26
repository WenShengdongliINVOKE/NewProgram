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
          
            // 作用：先把点位集合实例化出来。
            // 目的：界面点位列表直接绑这个集合，为 null 时列表永远是空的；
            //      读取循环里的 MyDataValue.Any() 也会抛空引用被 catch 吞掉，
            //      表现为"页面什么都读不出来，日志里也没有明显错误"。
            MyDataValue = new ObservableCollection<S7ValueModel>();

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

            // 作用：默认选中第一台设备。
            // 目的：点位读取是围绕"当前选中设备"进行的，不预选的话打开面板后点位区一直空着，
            //      操作员会以为设备没读到数据，其实只是没选中设备。
            SelectedSide = MyDevice.FirstOrDefault();
        }


        /// <summary>
        /// 与PLC建立通讯
        /// </summary>
        private void S7ConnCommand()
        {
            Task.Factory.StartNew(async () =>
           {

               if (InitDevice.DeviceModels is { Count: > 0 })
               {
                   foreach (var item in InitDevice.DeviceModels!)
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
        /// 停止标志。
        /// 作用：让常驻读取循环在面板关闭后退出。
        /// 目的：这个循环是 while(true)，本身没有退出条件；嵌入「连接设备」页之后，
        ///      每切换一次品牌就可能起一个循环，几个来回就会把 CPU 和一个 PLC 连接吃满，
        ///      而且新旧循环会抢同一个连接，读出来的值会来回跳。
        /// </summary>
        private volatile bool _stopped;

        /// <summary>
        /// 停止数据读取。
        /// 作用：置位停止标志，循环在下一轮检查时退出。
        /// 目的：宿主页面切走或切换品牌时必须调用，否则后台会一直空转读 PLC。
        /// </summary>
        public void Stop() => _stopped = true;
        /// <summary>
        /// 向界面加载当前选中PLC的所有数据 默认优先加载第一个
        /// </summary>
        private async Task GetSelectPLCAllData()
        {

            await Task.Factory.StartNew(async () =>
        {
            while (!_stopped)
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

                // 作用：每轮读取之间停一下。
                // 目的：原循环没有任何等待，是一段同步死循环，会把一个 CPU 核跑满；
                //      同时高频请求会让 PLC 通讯模块的请求队列堆积，整机响应都跟着变慢。
                //      300ms 对界面刷新足够，也给了控制器喘息时间。
                await Task.Delay(300);
            }


        });
        }
       

    }
}
