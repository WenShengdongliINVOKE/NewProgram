using CommunityToolkit.Mvvm.ComponentModel;
using DocumentFormat.OpenXml.Drawing.Charts;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using 小玩意.Comm;
using 小玩意.Model;

namespace 小玩意.ViewModel
{
    class AbbRobotViewModel : ObservableObject, INotifyPropertyChanged
    {
        ObservableCollection<AbbRobotModel> _robotModels;
        public ObservableCollection<AbbRobotModel> RobotModels
        {
            get => _robotModels;
            set
            {
                SetProperty(ref _robotModels, value);
                OnPropertyChanged(nameof(RobotModels));
            }
        }
        ObservableCollection<AbbRobotValueModel> _abbRobotValueModels;
        public ObservableCollection<AbbRobotValueModel> AbbRobotValueModels
        {
            get => _abbRobotValueModels;
            set
            {
                SetProperty(ref _abbRobotValueModels, value);
                OnPropertyChanged(nameof(AbbRobotValueModels));
            }
        }

        private AbbRobotModel _selectedSide;
        public AbbRobotModel SelectedSide
        {
            get => _selectedSide;
            set {
                SetProperty(ref _selectedSide, value);
                OnPropertyChanged(nameof(SelectedSide));
            }
        
        }

        AbbRobotCommunication abbRobotCommunication;

        public AbbRobotViewModel()
        {

            RobotModels = new ObservableCollection<AbbRobotModel>();
            Task.Factory.StartNew(() =>
            {
                abbRobotCommunication = new AbbRobotCommunication();
                int i = 1;
                foreach (var item in abbRobotCommunication.controllerInfo)
                {
                    RobotModels.Add(new AbbRobotModel() { Id = i++.ToString(), IPAddress = item.IPAddress.ToString(), Name = item.Name, IsVirtual = item.IsVirtual, SystemVersion = item.Version.ToString() });
                }
            }
            );

            ReadExecl.ReadIniFile();
        }

       

        private void GetAbbRobotDataValue()
        {



        }

        bool Tmp = true;
        string Tmpnums = string.Empty;
        /// <summary>
        /// 向界面加载当前选中机器人的所有数据 默认优先加载第一个
        /// </summary>
        private async Task GetSelectABBRobotAllData()
        {

            await Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    try
                    {
                        //var ss = ValuePairs.FirstOrDefault(o => o.All(o => o.Item1 == SelectedSide.S7Address));
                        var ReadAllPlc = await abbRobotCommunication.GetAllAbbRobotDataValue() ;
                        if ((Tmp) || Tmpnums != SelectedSide.Name)
                        {
                            Tmp = false;
                            Tmpnums = SelectedSide.Name;
                            //异步切换UI线程防止出现跨线程异常
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                if (AbbRobotValueModels.Any())
                                {
                                    AbbRobotValueModels.Clear();
                                }

                                foreach (var item in ReadAllPlc)
                                {
                                    AbbRobotValueModels.Add(new AbbRobotValueModel() { Address = item.Address, Name = item.Name, Value = item.Value });
                                }
                            });
                        }
                        //异步切换UI线程防止出现跨线程异常
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            //每次读取PLC数据后 更新界面数据
                            foreach (var ReadAllPlcValue in AbbRobotValueModels)
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
    }
}
