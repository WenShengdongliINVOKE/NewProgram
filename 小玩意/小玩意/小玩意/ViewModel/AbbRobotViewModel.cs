using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
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

        public AbbRobotViewModel()
        {
            
            RobotModels = new ObservableCollection<AbbRobotModel>();
            Task.Factory.StartNew(() =>
            {
                AbbRobotCommunication abbRobotCommunication = new AbbRobotCommunication();
                int i = 1;
                foreach (var item in abbRobotCommunication.controllerInfo)
                {
                    RobotModels.Add(new AbbRobotModel() { Id = i++.ToString(), IPAddress = item.IPAddress.ToString(), Name = item.Name, IsVirtual = item.IsVirtual, SystemVersion = item.Version.ToString() });
                }
            }
            );
        }

        private void GetAbbRobotDataValue() { 
        
          

        }
    }
}
