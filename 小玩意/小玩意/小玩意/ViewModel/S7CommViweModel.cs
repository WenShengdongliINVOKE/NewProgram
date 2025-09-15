using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        public ICommand ItemSelectCommand { get; }

        /// <summary>
        /// 
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
        List<List<Tuple<string, Siemens.Type, string, string>>> ValuePairs = new List<List<Tuple<string, Siemens.Type, string, string>>>();
        /// <summary>
        /// 当前选中PLC的索引值
        /// </summary>
        public int SelectedIndex { get; set; } = 0;

        public S7CommViweModel()
        {
            ConnCommand = new RelayCommand(S7ConnCommand);
            ItemSelectCommand = new RelayCommand(GetSelectedSide);
            (S7Models, MyDevice) = InitDevice.GetPlcDevice();
            //ErrorViewModel.Errornotice("1",false,1);          
            S7ConnCommand();
            GetSelectPLCAllData();

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


                    ValuePairs.Add(new List<Tuple<string, Siemens.Type, string, string>>() { new Tuple<string, Siemens.Type, string, string>("192.168.0.11", Siemens.Type.Bool, "名称1", "DB1") });
                    ValuePairs.Add(new List<Tuple<string, Siemens.Type, string, string>>() { new Tuple<string, Siemens.Type, string, string>("192.168.0.12", Siemens.Type.Bool, "名称2", "DB2") });
                    ValuePairs.Add(new List<Tuple<string, Siemens.Type, string, string>>() { new Tuple<string, Siemens.Type, string, string>("192.168.0.13", Siemens.Type.Bool, "名称3", "DB3") });
                    ValuePairs.Add(new List<Tuple<string, Siemens.Type, string, string>>() { new Tuple<string, Siemens.Type, string, string>("192.168.0.14", Siemens.Type.Bool, "名称4", "DB4") });
                    //ValuePairs.Add(new Tuple<Siemens.Type, string, string>(Siemens.Type.Bool, "名称2", "DB12"));
                    //ValuePairs.Add(new Tuple<Siemens.Type, string, string>(Siemens.Type.Bool, "名称3", "DB13"));
                    //ValuePairs.Add(new Tuple<Siemens.Type, string, string>(Siemens.Type.Bool, "名称4", "DB14"));
                    //ValuePairs.Add(new Tuple<Siemens.Type, string, string>(Siemens.Type.Bool, "名称5", "DB15"));
                    //ValuePairs.Add(new Tuple<Siemens.Type, string, string>(Siemens.Type.Bool, "名称6", "DB16"));
                    //ValuePairs.Add(new Tuple<Siemens.Type, string, string>(Siemens.Type.Bool, "名称7", "DB17"));


                    MyDataValue = new ObservableCollection<S7ValueModel>();

                }
                else
                {
                    ErrorViewModel.Errornotice("PLC地址不能为空！", true, 1);
                }
            }
            );



        }
        bool Tmp = true;
        int Tmpnums;
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
                    // var ss = ValuePairs.FirstOrDefault(o => o.All(o => o.Item1 == SelectedSide.S7Address));
                    var ReadAllPlc = await _siemens.FirstOrDefault(o => o._address == SelectedSide.S7Address).GetAllPlcDataAddress(ValuePairs.FirstOrDefault(o => o.All(o => o.Item1 == SelectedSide.S7Address)));
                    if ((Tmp) || Tmpnums != SelectedIndex)
                    {
                        Tmp = false;
                        Tmpnums = SelectedIndex;
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
                catch (Exception ex)
                {

                    ErrorViewModel.Errornotice(ex.Message, true, 1);
                }
            }


        });
        }
        /// <summary>
        /// 获取当前选中PLC信息
        /// </summary>
        private void GetSelectedSide()
        {
            if (SelectedSide != null)
            {
                //MessageBox.Show(SelectedSide);
            }
        }

    }
}
