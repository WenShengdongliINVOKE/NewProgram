using NLog;
using System.Reflection;
using 小玩意.Comm;
using 小玩意.PlcModel;

namespace 小玩意.ViewModel
{
    /// <summary>
    /// 一个 PLC 品牌的展示信息。
    /// 作用：把「某个实现了 IPlcCommunicationModel 的类型」和它的中文名、协议、说明绑在一起。
    /// 目的：左侧导航和右侧面板都只认这个对象，不必各自去猜类型名怎么显示；
    ///      以后新增品牌类，只要实现接口，导航里就会自动多出一项。
    /// </summary>
    public sealed class PlcBrandInfo
    {
        /// <summary>实现 IPlcCommunicationModel 的具体类型</summary>
        public Type ModelType { get; init; } = typeof(object);

        /// <summary>中文显示名，如「西门子」</summary>
        public string DisplayName { get; init; } = string.Empty;

        /// <summary>协议名称，如「S7 协议」</summary>
        public string Protocol { get; init; } = string.Empty;

        /// <summary>导航栏里的短标记，如「S7」</summary>
        public string Badge { get; init; } = string.Empty;

        /// <summary>右侧面板的详细说明</summary>
        public string Description { get; init; } = string.Empty;

        /// <summary>
        /// 该品牌在 Comm 目录下是否已有对应的通讯实现类。
        /// 作用：决定右侧面板显示「可以连接」还是「通讯类待实现」。
        /// 目的：四个新补的品牌目前只有参数没有通讯实现，如果界面不给提示，
        ///      操作员点了连接没反应会以为是程序坏了。
        /// </summary>
        public bool HasCommunicationImpl { get; init; }

        /// <summary>排序序号，保证导航里的品牌顺序固定，不随反射顺序变化</summary>
        public int SortIndex { get; init; }
    }

    /// <summary>
    /// PLC 品牌目录。
    /// 作用：反射扫描程序集里所有实现 IPlcCommunicationModel 的类，产出品牌清单。
    /// 目的：导航栏的内容直接由「有哪些品牌类」决定，新增一个品牌不必再改界面 XAML；
    ///      如果手工在 XAML 里写死六个品牌，每加一个都要改两处，很容易漏。
    /// </summary>
    internal static class PlcBrandCatalog
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 品牌的中文名、协议、短标记、说明。
        /// 作用：反射只能拿到类型名（SiemensPlc），拿不到中文名和协议，靠这张表补齐。
        /// 目的：把展示文案集中在一处，改文案不用翻界面代码；
        ///      表里没有的类型也不是错误，会自动退化成类型名显示，不会漏掉新品牌。
        /// </summary>
        private static readonly Dictionary<string, (string Display, string Protocol, string Badge, string Description)> Metadata = new()
        {
            ["SiemensPlc"] = ("西门子", "S7 协议", "S7", "基于 S7netplus。需 CPU 型号、机架号、槽号三项，缺一项都连不上。"),
            ["SanLingPlc"] = ("三菱", "MC 协议", "MC", "当前通讯实现内部实际为 PanasonicMcNet（松下协议），接入真实三菱机型前需先确认。"),
            ["PanasonicPlc"] = ("松下", "MEWTOCOL-TCP", "PA", "FP 系列常用，默认端口 5007。通讯类尚未实现。"),
            ["InovancePlc"] = ("汇川", "Modbus TCP", "IN", "AM600 / H5U 系列常用，默认端口 502。通讯类尚未实现。"),
            ["OmronPlc"] = ("欧姆龙", "FINS/TCP", "OM", "需网络号、节点号、单元号三段地址，默认端口 9600。通讯类尚未实现。"),
            ["HechuanPlc"] = ("禾川", "Modbus TCP", "HC", "无专用驱动，走通用 Modbus TCP，默认端口 502。通讯类尚未实现。")
        };

        /// <summary>
        /// 导航里品牌的固定顺序。
        /// 作用：按现场接线习惯排列，西门子和三菱排前面。
        /// 目的：反射返回的类型顺序不保证稳定，不指定的话每次启动导航顺序都可能变，操作员会找不到东西。
        /// </summary>
        private static readonly string[] Order =
        {
            "SiemensPlc", "SanLingPlc", "PanasonicPlc", "InovancePlc", "OmronPlc", "HechuanPlc"
        };

        /// <summary>
        /// 所有通讯实现类，只反射一次。
        /// 作用：缓存 IPlccommiunctionCommand 的实现类型列表。
        /// 目的：每个品牌都要判断「有没有通讯实现」，逐个反射扫描程序集会明显拖慢页面首次打开。
        /// </summary>
        private static readonly Lazy<Type[]> CommandTypes = new(() =>
        {
            try
            {
                return typeof(IPlccommiunctionCommand).Assembly.GetTypes()
                    .Where(t => !t.IsInterface && !t.IsAbstract && typeof(IPlccommiunctionCommand).IsAssignableFrom(t))
                    .ToArray();
            }
            catch (ReflectionTypeLoadException ex)
            {
                logger.Warn(ex, "扫描通讯实现类时部分类型加载失败，已忽略失败项");
                return ex.Types.Where(t => t is not null).Cast<Type>()
                    .Where(t => !t.IsInterface && !t.IsAbstract && typeof(IPlccommiunctionCommand).IsAssignableFrom(t))
                    .ToArray();
            }
        });

        /// <summary>
        /// 扫描并返回全部 PLC 品牌。
        /// 作用：找出程序集里所有实现了 IPlcCommunicationModel 的具体类，补上展示信息后按固定顺序返回。
        /// 目的：让导航栏内容跟着代码走——新增一个品牌类，界面自动多一项，不需要改 XAML。
        /// </summary>
        /// <returns>按 Order 排好序的品牌清单</returns>
        public static List<PlcBrandInfo> Discover()
        {
            var result = new List<PlcBrandInfo>();

            Type[] types;
            try
            {
                types = typeof(IPlcCommunicationModel).Assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // 作用：某些类型加载失败时，用能加载出来的那部分继续。
                // 目的：不让一个坏类型导致整个导航栏空白——导航没了，整个页面就等于废了。
                logger.Warn(ex, "扫描 PLC 品牌时部分类型加载失败，已忽略失败项");
                types = ex.Types.Where(t => t is not null).Cast<Type>().ToArray();
            }

            foreach (Type type in types)
            {
                if (type.IsInterface || type.IsAbstract) continue;
                if (!typeof(IPlcCommunicationModel).IsAssignableFrom(type)) continue;

                if (!Metadata.TryGetValue(type.Name, out var meta))
                {
                    // 未登记的类用类型名兜底，保证新品牌至少能出现在导航里，只是没有中文说明
                    string fallback = type.Name.EndsWith("Plc", StringComparison.Ordinal)
                        ? type.Name[..^3]
                        : type.Name;
                    meta = (fallback, "未登记协议", fallback[..Math.Min(2, fallback.Length)], "该品牌尚未在 PlcBrandCatalog 中登记说明。");
                    logger.Warn($"PLC 品牌 {type.Name} 未登记展示信息，已使用类型名兜底");
                }

                int index = Array.IndexOf(Order, type.Name);

                result.Add(new PlcBrandInfo
                {
                    ModelType = type,
                    DisplayName = meta.Display,
                    Protocol = meta.Protocol,
                    Badge = meta.Badge,
                    Description = meta.Description,
                    HasCommunicationImpl = HasCommunicationImpl(type.Name),
                    SortIndex = index >= 0 ? index : 99
                });
            }

            var ordered = result.OrderBy(b => b.SortIndex).ThenBy(b => b.DisplayName).ToList();
            logger.Info($"PLC 品牌发现完成，共 {ordered.Count} 个：{string.Join("、", ordered.Select(b => b.DisplayName))}");
            return ordered;
        }

        /// <summary>
        /// 判断某品牌有没有对应的通讯实现类。
        /// 作用：按「品牌前缀匹配通讯类名」的方式判断，例如 SiemensPlc 对应 Comm 里的 Siemens。
        /// 目的：比起在每个品牌里手写一个「已实现/未实现」的布尔值，反射匹配在补上通讯类后会自动变成 true，
        ///      不需要有人记得回来改标志位。
        /// </summary>
        /// <param name="modelTypeName">品牌类的类型名，如 SiemensPlc</param>
        private static bool HasCommunicationImpl(string modelTypeName)
        {
            string brand = modelTypeName.Replace("Plc", string.Empty, StringComparison.Ordinal);
            return CommandTypes.Value.Any(t => t.Name.StartsWith(brand, StringComparison.OrdinalIgnoreCase));
        }
    }
}
