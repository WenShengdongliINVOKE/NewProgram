using ABB.Robotics.Controllers;
using ABB.Robotics.Controllers.Discovery;
using ABB.Robotics.Controllers.IOSystemDomain;
using ABB.Robotics.Controllers.MotionDomain;
using ABB.Robotics.Controllers.RapidDomain;
using System.Windows;

namespace 小玩意
{
    /// <summary>
    /// 注意，此类使用时初始化一次即可，所有的机器人对象都会存储在 ControllerInfo 字段中 使用ConnRobot 方法传入下标连接需要连接的机器人 
    /// </summary>
    internal class AbbRobotCommunication
    { /// <summary>
      /// 编号
      /// </summary>
        public int? Id { get; set; }
        /// <summary>
        /// 系统名称
        /// </summary>
        public string? SystemName { get; set; }
        /// <summary>
        /// 系统版本
        /// </summary>
        public string? SystemVersion { get; set; }
        /// <summary>
        /// IP地址
        /// </summary>
        public string? IpAddress { get; set; }
        /// <summary>
        /// 是否为虚拟机
        /// </summary>
        public bool? IsVirtual { get; set; }
        /// <summary>
        /// x坐标
        /// </summary>
        public double X;
        /// <summary>
        /// y坐标
        /// </summary>
        public double Y;
        /// <summary>
        /// z坐标
        /// </summary>
        public double Z;

        public static ABB.Robotics.Controllers.RapidDomain.Task? tRob1;//
        public static List<ABB.Robotics.Controllers.RapidDomain.Task>? TRobt = [];
        /// <summary>
        /// GetABBRobot方法扫描到的机器人对象 此对象仅为  GetAllAbbRobotDataValue 方法使用
        /// </summary>
        private Controller? controller;
        /// <summary>
        ///  所有扫描到的机器人对象
        /// </summary>
        private List<Controller>? listcontroller = new List<Controller>();
        /// <summary>
        /// 机器人数量
        /// </summary>
        public int count { get; set; }
        /// <summary>
        /// 扫描到的机器人队列
        /// </summary>
        public ControllerInfo[] controllerInfo;

        public List<AbbRobotCommunication> abbRobots { get; set; }
        /// <summary>
        /// 初始化并扫描环境内的机器人
        /// </summary>
        public AbbRobotCommunication()
        {
            Tuple<List<AbbRobotCommunication>, ControllerInfo[]> tuple = GetABBRobot();
            this.controllerInfo = tuple.Item2;
            //this.abbRobots = tuple.Item1;
            this.count = controllerInfo.Length;
            ConnAllRobot();
        }
        /// <summary>
        /// 连接机器人
        /// </summary>
        /// <param name="index">选择要链接的机器人</param>
        /// <returns></returns>
        public bool ConnRobot(int index = 0)
        {

            controller = Controller.Connect(controllerInfo[index], ConnectionType.Standalone, false);
            if (controller != null)
            {
                if (controller.Connected == true)
                {
                    controller.Logon(UserInfo.DefaultUser);
                    tRob1 = controller.Rapid.GetTask("T_ROB1");
                    return true;
                }
                return false;
            }
            return false;


        }

        /// <summary>
        /// 连接所有机器人
        /// </summary>
        /// <returns></returns>
        public bool ConnAllRobot()
        {
            foreach (var item in controllerInfo)
            {
                listcontroller.Add(Controller.Connect(item, ConnectionType.Standalone, false));
            }
            //controller = Controller.Connect(controllerInfo[index], ConnectionType.Standalone, false);
            if (listcontroller != null)
            {
                try
                {
                    foreach (var item in listcontroller)
                    {
                        if (item.Connected == true)
                        {
                            item.Logon(UserInfo.DefaultUser);
                            TRobt.Add(tRob1 = item.Rapid.GetTask("T_ROB1"));
                            //continue;
                        }
                        //return false;
                    }
                }
                catch (Exception ex)
                {
                    //TODO: 此处需要增加日志
                    //throw;
                }
            }
            return false;


        }


        // public static ABB.Robotics.Controllers.RapidDomain.Task tRob1 = null;//
        //  public static NetworkScanner scanner = new NetworkScanner();
        // public static Controller controller =new Controller();
        //public static ControllerInfoCollection controllers = new ControllerInfoCollection();
        //此对象是一个数组  每项中信息可能有用的是如下几类：
        //1，IP地址 2，系统名称 3，控制器名称 4，版本 5，标识码 6，是否为虚拟机



        /// <summary>
        /// 扫描机器人
        /// </summary>
        /// <returns>返回扫描到的所有机器人信息 Tuple<List<AbbRobotCommunication>返回一些基础信息，ControllerInfo[]返回详细信息 </returns>
         private Tuple<List<AbbRobotCommunication>, ControllerInfo[]> GetABBRobot()
        {

            NetworkScanner scanner = new NetworkScanner();
            ControllerInfo[] controllers;/*= new ControllerInfo[10];*/

            //用于接收所返回的机器人信息类
           
            List<AbbRobotCommunication> abbRobotCommunications = new List<AbbRobotCommunication>();
            //List<Controller> controllerList = new List<Controller>();
            //abbRobotCommunications.Add(new AbbRobotCommunication());
            try
            {   //扫描环境内的机器人
                scanner.Scan();
                controllers = new ControllerInfo[scanner.GetControllers(NetworkScannerSearchCriterias.Virtual).Length];
                controllers = scanner.GetControllers(NetworkScannerSearchCriterias.Virtual);
                foreach (ControllerInfo controller in controllers)
                {
                    this.SystemVersion = controller.Version.ToString();
                    this.SystemVersion = controller.Version.ToString();
                    this.SystemName = controller.SystemName;
                    this.IpAddress = controller.IPAddress.ToString();
                    this.Id = +1;
                    abbRobotCommunications.Add(this);

                }
                return Tuple.Create(abbRobotCommunications, controllers);



            }
            catch (System.Exception ex)
            {
                //TODO: 此处需要增加日志

                ErrorBox.Show($"未扫描到机器人 错误信息：{ex.ToString()}", true);
                return Tuple.Create(new List<AbbRobotCommunication>(), new ControllerInfo[scanner.GetControllers(NetworkScannerSearchCriterias.Virtual).Length]);

            }


        }




        //
        //
        /// <summary>
        /// 读取数据
        /// </summary>
        /// <param name="controller">目标对象</param>
        /// <param name="abbName">机器人名</param>
        /// <param name="programName">目标程序</param>
        /// <param name="dataName">数据名称</param>
        /// <returns></returns>
        public object ReadAbbDataNum(string abbName, string programName, string dataName, object value)
        {

            try
            {
                if (this.controller.IsMaster == false) MessageBox.Show("ABB主机请求失败,请先连接机器人再读取数据");
                RapidData rd = controller.Rapid.GetRapidData(abbName, programName, dataName);
                var obj = rd.Value.ToString().Trim();
                return obj;
            }
            catch (Exception ex)
            {
                //TODO: 此处需要增加日志

                ErrorBox.Show(ex.ToString(), true);
                return "未连接到机器人";
            }


        }
        /// <summary>
        /// 读取数组
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="abbName"></param>
        /// <param name="programName"></param>
        /// <param name="dataName"></param>
        /// <param name="array"></param>
        /// <returns></returns>
        public ArrayData ReadAbbDataArray(string abbName, string programName, string dataName)
        {

            try
            {
                if (controller.IsMaster == false) MessageBox.Show("ABB主机请求失败,请先连接机器人再读取数据");
                RapidData rd = controller.Rapid.GetRapidData(abbName, programName, dataName);
                if (rd != null)
                {
                    var array = (ArrayData)rd.Value;
                    return array;
                }
                return null;
            }
            catch (Exception ex)
            {
                //TODO: 此处需要增加日志

                MessageBox.Show(ex.ToString());
                return null;
            }


        }
        /// <summary>
        /// 获取当前选择的机器人坐标
        /// </summary>
        /// <param name="controller">目标对象</param>
        /// <param name="abbName">机器人名</param>
        /// <param name="programName">程序名</param>
        /// <param name="dataName">数据名</param>
        /// <returns></returns>
        public AbbRobotCommunication GetAbbXYZ(string abbName, string programName, string dataName)
        {
            
            if (controller.IsMaster == false && controller.OperatingMode == ControllerOperatingMode.Auto)
            {
                try
                {
                    using (var m = Mastership.Request(controller))
                    {
                        if (controller.IsMaster == false) MessageBox.Show("ABB主机请求失败,请先连接机器人再读取数据");
                        RapidData rd = controller.Rapid.GetRapidData(abbName, programName, dataName);
                        RobTarget rb = (RobTarget)rd.Value;
                        this.X = rb.Trans.X;
                        this.Y = rb.Trans.Y;
                        this.Z = rb.Trans.Z;
                    }
                }
                catch (Exception ex)
                {
                    //TODO: 此处需要增加日志

                    MessageBox.Show(ex.ToString());
                }
            }
            return this;
        }



        /// <summary>
        /// 读取Bool量
        /// </summary>
        /// <param name="controller">目标对象</param>
        /// <param name="rapidBool">写入的值</param>
        /// <returns></returns>
        public Signal ReadAbbBool(string str)
        {

            Signal dopick = controller.IOSystem.GetSignal(str);

            return dopick;

        }
        /// <summary>
        /// 写入bool量
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public void WriteAbbBools(string str, string str1)
        {
            try
            {
                Signal signal = controller.IOSystem.GetSignal(str);
                DigitalSignal sig = (DigitalSignal)signal;
                if (str1 == "1")
                {
                    sig.Set();
                }
                else
                {
                    sig.Reset();
                }
                Signal signal2 = controller.IOSystem.GetSignal(str);
                GroupSignal group = (GroupSignal)signal2;
                group.Value = Convert.ToSingle(str1);
            }
            catch (Exception ex)
            {
                //TODO: 此处需要增加日志

                MessageBox.Show(ex.ToString());
            }
        }

        /// <summary>
        /// 写入Num数据
        /// </summary>
        /// <param name="controller">目标对象</param>
        /// <param name="str">写入的值</param>
        public void WriteAbbNum(string robotName, string programName, string dataName, string str)
        {
            try
            {   //获取写入Rapid权限
                using (Mastership.Request(controller.Rapid))
                {
                    RapidData Rd = controller.Rapid.GetRapidData(robotName, programName, dataName);
                    Num num = (Num)Rd.Value;
                    num.FillFromString2(str);
                    Rd.Value = num;
                    MessageBox.Show($"数据{num}写入成功");
                }
                ;
            }
            catch (Exception ex)
            {
                //TODO: 此处需要增加日志

                MessageBox.Show(ex.ToString());
            }
        }
        /// <summary>
        /// 像数组写入   2025.9.2 LSY
        /// </summary>
        /// <param name"robotName">机器人名称</param>
        /// <param name="programName">程序名称 指明你要向机器人的哪段程序的变量写入 因为ABB机器人的每个子程序都可以单独设置自己的局部变量</param>
        /// <param name="dataName">数据名称 </param>
        /// <param name="str"> 写入的值</param>
        public void WriteAbbArray(string robotName, string programName, string dataName, string str)
        {
            try
            {
                using (Mastership.Request(controller.Rapid))
                {
                    RapidData Rd = controller.Rapid.GetRapidData(robotName, programName, dataName);
                    if (Rd.IsArray)
                    {
                        ArrayData array = (ArrayData)Rd.Value;
                        array.FillFromString(str);
                    }
                    MessageBox.Show("数值已修改");
                }
                ;
            }
            catch (Exception ex)
            {
                //TODO: 此处需要增加日志

                MessageBox.Show(ex.ToString());
            }
        }
        /// <summary>
        /// 向数组中的某一个写入
        /// </summary>
        /// <param name="robotName">机器人名称</param>
        /// <param name="programName">程序名称</param>
        /// <param name="dataName">数据名</param>
        /// <param name="str">写入的数据</param>
        /// <param name="number">下标</param>
        public void WriteAbbArray(string robotName, string programName, string dataName, string str, int number)
        {
            try
            {
                using (Mastership.Request(controller.Rapid))
                {
                    RapidData Rd = controller.Rapid.GetRapidData();
                    if (Rd.IsArray)
                    {
                        Num num = (Num)Rd.ReadItem(number);
                        num.Value = Convert.ToInt32(str);
                    }
                    MessageBox.Show("数值已修改");
                }
                ;
            }
            catch (Exception ex)
            {
                //TODO: 此处需要增加日志

                MessageBox.Show(ex.ToString());
            }
        }
        /// <summary>
        /// 获取当前世界坐标数据
        /// </summary>
        public void GetRobotRealWorldTimeGestures()
        {


            RobTarget robTarget = controller.MotionSystem.ActiveMechanicalUnit.GetPosition(CoordinateSystemType.World);

            this.X = robTarget.Trans.X;
            this.Y = robTarget.Trans.Y;
            this.Z = robTarget.Trans.Z;
            robTarget.Rot.ToEulerAngles(out this.X, out this.Y, out this.Z);

        }
        /// <summary>
        /// 获取工具坐标系
        /// </summary>
        private void GetRobotToolRealTimeGestures()
        {
            RobTarget robTarget = controller.MotionSystem.ActiveMechanicalUnit.GetPosition(CoordinateSystemType.World);
            this.X = robTarget.Trans.X;
            this.Y = robTarget.Trans.Y;
            this.Z = robTarget.Trans.Z;
            robTarget.Rot.ToEulerAngles(out this.X, out this.Y, out this.Z);

        }

        /// <summary>
        /// 获取已知所有数据
        /// </summary>
        /// <param name="name"></param>
        public void GetAllAbbRobotDataValue(Tuple<string,string ,string,string> RobotDataValue,int Value )
        {
            //获取时使用
            controller = listcontroller.FirstOrDefault(o => o.Name == RobotDataValue.Item1);
            ReadAbbDataNum(RobotDataValue.Item1,RobotDataValue.Item2,RobotDataValue.Item3,Value);


        }
    }
}
