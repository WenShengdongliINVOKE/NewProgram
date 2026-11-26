using CommunityToolkit.Mvvm.ComponentModel;
using NLog;
using NPOI.SS.Formula.Functions;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using 小玩意.Comm;
using 小玩意.Model;

namespace 小玩意.ViewModel
{
    class AbbRobotViewModel : ObservableObject, INotifyPropertyChanged
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();


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
            set
            {
                SetProperty(ref _selectedSide, value);
                OnPropertyChanged(nameof(SelectedSide));
            }

        }
       
        AbbRobotCommunication abbRobotCommunication;

        List< List<Tuple<string, string, string,string>>> ValuePairs = new List<List<Tuple<string, string, string, string>>>();
        public AbbRobotViewModel()
        {

            RobotModels = new ObservableCollection<AbbRobotModel>();

            // 作用：点位集合必须先实例化。
            // 目的：界面点位列表绑的就是它，为 null 时列表永远空；
            //      读取循环里对它做 foreach 会抛空引用，而那个 catch 又会弹窗，
            //      一旦配置里能有数据，就会变成"无限循环 + 无限弹窗"，页面根本没法用。
            AbbRobotValueModels = new ObservableCollection<AbbRobotValueModel>();

            Task.Factory.StartNew(() =>
            {
                abbRobotCommunication = new AbbRobotCommunication();
                int i = 1;
                foreach (var item in abbRobotCommunication.controllerInfo)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        RobotModels.Add(new AbbRobotModel() { Id = i++.ToString(), IPAddress = item.IPAddress.ToString(), Name = item.Name, IsVirtual = item.IsVirtual, SystemVersion = item.Version.ToString() });
                    }
                );
                }
            }
            );
           
            GetSelectABBRobotAllData();
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
                        if (ValuePairs.Any())
                        {


                            if (SelectedSide != null)
                            {
                                abbRobotCommunication.SeclcetRobot(SelectedSide.Name);
                            }

                            //var ss = ValuePairs.FirstOrDefault(o => o.All(o => o.Item1 == SelectedSide.IPAddress));
                            var ReadAllPlc = abbRobotCommunication.GetAllAbbRobotDataValue(ValuePairs.FirstOrDefault(o => o.All(o => o.Item1 == SelectedSide.Name)));
                            //if ((Tmp) || Tmpnums != SelectedSide.Name)
                            //{
                            //    Tmp = false;
                            //    Tmpnums = SelectedSide.Name;
                            //    //异步切换UI线程防止出现跨线程异常
                            //    Application.Current.Dispatcher.Invoke(() =>
                            //    {
                            //        if (AbbRobotValueModels.Any())
                            //        {
                            //            AbbRobotValueModels.Clear();
                            //        }

                            //        foreach (var item in ReadAllPlc)
                            //        {
                            //            AbbRobotValueModels.Add(new AbbRobotValueModel() { Address = item.Address, Name = item.Name, Value = item.Value });
                            //        }
                            //    });
                            //}
                            //异步切换UI线程防止出现跨线程异常
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                //每次读取机器人数据后 更新界面数据
                                foreach (var ReadAllPlcValue in AbbRobotValueModels)
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
                        // 作用：只记日志，不弹窗。
                        // 目的：这是常驻循环，机器人短暂掉线时每一轮都会失败一次，
                        //      弹窗会在一分钟内堆满屏幕，把真正的错误信息盖住。
                        logger.Error(ex, "读取机器人数据出错");
                    }

                    // 作用：每轮读取之间停一下。
                    // 目的：原循环没有任何等待，等于同步死循环，会把一个 CPU 核跑满；
                    //      高频读取还会占满控制器连接，其他操作拿不到响应。
                    await Task.Delay(500);
                }


            });
        }
    }
}
