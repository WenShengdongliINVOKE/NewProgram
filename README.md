# 小玩意 · 工业设备上位机

> **仓库结构**：解决方案 `小玩意.sln` 位于 `小玩意/` 目录，项目代码位于 `小玩意/小玩意/` 目录。
> 下文提及的相对路径（`Comm/`、`ini/`、`lib/` 等）均相对于项目目录 `小玩意/小玩意/`。

基于 .NET 8 + WPF 的产线设备上位机，用于统一接入 PLC、机器人、运动控制器与视觉相机，并向上对接 MES。

---

## 功能概览

| 模块 | 页面 | 说明 |
|---|---|---|
| 设备接入 | `ConnectDevice.xaml` | 左侧按品牌展开的导航树，右侧显示通讯参数、可连接设备与实时点位 |
| PLC 信息 | `PlcMessagePage.xaml` | 独立查看 PLC 的设备列表与点位数据（与连接设备页共用同一面板控件） |
| 机器人 | `AbbRobot.xaml` | 扫描并列出网段内的 ABB 控制器，展示 RAPID 变量与信号 |
| 轴控制 | `AxisControl.xaml` | 正运动控制器的单轴调试：使能、点动、绝对定位、回零、实时位置 |
| 视觉识别 | `VisionRecognition.xaml` | OpenCV 灰度化 / 二值化 / 连通域检测，支持相机实时与本地图片 |
| 天气 | `Weather.xaml` | 车间天气展示（附带效果） |

---

## 技术栈

- **框架**：.NET 8（`net8.0-windows`）+ WPF
- **MVVM**：CommunityToolkit.Mvvm
- **PLC 通讯**：S7netplus（西门子）、HslCommunication（三菱 / 松下 / 汇川 / 欧姆龙 / Modbus）
- **Modbus RTU**：EasyModbusTCP + 自行拼装的报文与 CRC16 校验
- **机器视觉**：OpenCvSharp4
- **机器人**：ABB RobotStudio SDK（`lib/` 下的三个 DLL）
- **运动控制**：正运动 Zmotion SDK（`Zmotion/` 下的 `zauxdll.dll`、`zmotion.dll`）
- **日志**：NLog
- **表格读写**：ClosedXML、EPPlus、NPOI

---

## 目录结构

```
小玩意.sln
小玩意/
├── Comm/              通讯层
│   ├── Siemens.cs              西门子 S7
│   ├── SanLing.cs              三菱 MC（内部实为 PanasonicMcNet，见「已知限制」）
│   ├── ModbusComm.cs           Modbus RTU
│   ├── TcpClient.cs            TCP 文本客户端
│   ├── TcpServer.cs            TCP 服务端
│   ├── AbbRobotCommunication.cs ABB 机器人
│   ├── MesWebSocketClient.cs   MES 长连接客户端
│   ├── IPlccommiunctionCommand.cs  通讯统一接口
│   └── Message.cs / CheckSum.cs    Modbus 报文与校验
├── PlcModel/          PLC 品牌层（每个品牌一个类，实现 IPlcCommunicationModel）
├── ViewModel/         各页面的视图模型
├── Controls/          可复用控件（PlcDetailPanel）
├── Model/             数据模型
├── OpenCV/            图像处理
├── Zmotion/           正运动 SDK 与封装
├── ini/               配置文件目录
├── lib/               第三方 DLL（ABB SDK）
├── GIF/               提示动画素材
└── Styles.xaml        全局样式
```

---

## 配置文件

程序从 `ini/配置文件.xlsx` 读取设备与点位配置。文件共三张有效工作表，**顺序不能调整**（`ReadIniFile` 按工作表索引读取）：

### 1. 设备表

| 设备名称 | 连接方式 | 设备IP | 设备类型 | 设备型号 |
|---|---|---|---|---|
| PLC1 | 以太网 | 192.168.0.10 | S71200 | Siemens |

> 代码只取 **C 列（IP）** 与 **E 列（设备型号）**。E 列填 `Siemens` 或 `SanLing`，判断时用精确相等，大小写必须一致。

### 2. 变量表

| 变量名称 | 数据类型 | DB地址 | 设备名称 | 设备型号 |
|---|---|---|---|---|
| 启动信号 | Bool | DB1.0.0 | PLC1 | Siemens |

> 数据类型只能是 `Bool` / `Int16` / `Int32` / `String` / `Real`，其它值会在解析时抛异常。

### 3. 机器人点位

| 机器人名称 | 数据名称 | 数据类型 | 所属程序 |
|---|---|---|---|

> 当前版本的读取代码尚未启用，先按格式填写。

仓库内提供了 `ini/PLC配置表.xlsx` 作为模板，字段含义与约束见其中的「填写说明」表。

**路径解析顺序**：优先当前工作目录下的 `小玩意\ini\配置文件.xlsx`，找不到则回退到 exe 同级的 `ini\配置文件.xlsx`。

---

## 构建与运行

### 环境要求

- Windows 10/11
- .NET 8 SDK
- Visual Studio 2022（可选，用于调试）

### 命令

```bash
dotnet build 小玩意.sln
```

首次运行前请确认：

1. `ini/配置文件.xlsx` 已就位（构建时会把源码 `ini` 同步到输出目录）
2. 若使用轴控制，`zauxdll.dll` 与 `zmotion.dll` 会随构建复制到 exe 同级目录

---

## 已知限制

| 项 | 说明 |
|---|---|
| 松下 / 汇川 / 欧姆龙 / 禾川 | 品牌类已建立，但通讯实现尚未完成，`GetPlcModels()` 目前返回空集合 |
| 三菱 | `SanLing.cs` 内部实际实例化的是 `PanasonicMcNet`（松下协议），接入真实三菱机型前需确认并改用 `MelsecMcNet` |
| 配置层 | 只解析出西门子设备，其余品牌尚无独立配置来源；三菱设备被塞进 `S7Model` 且缺少端口字段 |
| 机器人点位表 | `ReadExecl.cs` 中读取第 3 个工作表的代码被注释，`Sheet3` 恒为空 |
| 导航按钮 | 顶部的「轴信息」「加工中心信息」尚未绑定页面，点击会提示页面不存在 |
| 回零模式 | `AxisControl` 的回零模式默认 1，必须按具体机型手册确认，填错可能导致轴朝错误方向找原点 |

---

## 开发约定

- 注释使用中文，统一写明「作用」与「目的」两件事，避免只复述代码动作。
- 通讯类统一实现 `IPlccommiunctionCommand`，界面层只面向接口编程。
- PLC 品牌类统一实现 `IPlcCommunicationModel`，新增品牌时导航自动出现新条目，无需改动 XAML。
- 界面配色遵循 `Styles.xaml` 中的暖色渐变体系：页面底色 `#F5F5F5`、卡片白底圆角 8、主按钮使用 `ModernButtonStyle1`。
