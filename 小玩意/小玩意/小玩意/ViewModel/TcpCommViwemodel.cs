using CommunityToolkit.Mvvm.ComponentModel;
using NLog;
using System.Collections.ObjectModel;
using 小玩意.Model;

namespace 小玩意.ViewModel
{
    public class TcpCommViwemodel : ObservableObject
    {

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private string _addressValue;

        public string AddressValue { get => _addressValue; set => SetProperty(ref _addressValue, value); }

        private string port;
        public string Port { get => port; set => SetProperty(ref port, value); }




        public ObservableCollection<TcpModel> myData { get; set; }
        public ObservableCollection<TcpModel> MyData
        {
            get { return myData; }
            set
            {
                if (myData != value)
                {
                    myData = value;
                    OnPropertyChanged(nameof(MyData));
                }
            }
        }
        public TcpCommViwemodel()
        {
            MyData = InitDevice.GetTcpDevice();
        }
    }
}
