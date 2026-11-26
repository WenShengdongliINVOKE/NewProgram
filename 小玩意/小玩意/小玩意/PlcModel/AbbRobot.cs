namespace 小玩意.PlcModel
{
    /// <summary>
    /// ABB 机器人（机器人设备，**不是 PLC**）。
    /// 作用：作为机器人设备在设备清单里的占位类型，用于和其他设备一起呈现在界面上。
    /// 目的：产线设备清单里既有 PLC 也有机器人，界面需要一个统一的条目来展示；
    ///      但机器人走的是 RobotStudio SDK（见 Comm/AbbRobotCommunication.cs），
    ///      没有「寄存器地址」这个概念，也不适合套 PLC 那套读写接口。
    /// 因此本类**刻意不实现 IPlcCommunicationModel**：
    ///      若强行实现，GetPlcModels() 只能返回空集合或抛异常，
    ///      反而会让上层误以为机器人是可以按点位轮询的设备。
    /// 建议：本类更适合放在 Comm/ 或 Model/ 下，当前留在 PlcModel 里是历史位置，
    ///      后续整理时再迁移，避免有人把它当 PLC 品牌类处理。
    /// </summary>
    internal class AbbRobot
    {
        /// <summary>机器人控制器名称（RobotStudio 里注册的名字），用于在多个控制器中定位</summary>
        public string? ControllerName { get; set; }

        /// <summary>控制器 IP 地址</summary>
        public string? Ip { get; set; }
    }
}
