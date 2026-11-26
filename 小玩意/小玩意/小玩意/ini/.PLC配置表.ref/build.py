# -*- coding: utf-8 -*-
"""
生成 PLC 配置表模板。
结构严格对应 ReadExecl.ReadIniFile 的解析规则：
  - 第 1 个工作表 = 设备表，代码取 C 列（设备IP）与 E 列（设备型号）
  - 第 2 个工作表 = 变量表，代码取 A（变量名称）、B（数据类型）、C（DB地址）、E（设备型号）
  - 第 3 个工作表 = 机器人点位表，代码取 A~D 列
注意：工作表顺序不能变，ReadIniFile 是按索引取表的。
"""
try:
    import openpyxl
except ImportError:
    import subprocess, sys
    subprocess.check_call([sys.executable, "-m", "pip", "install", "--quiet", "openpyxl>=3.1.0"])
    import openpyxl

from openpyxl import Workbook
from openpyxl.styles import Alignment, Border, Font, PatternFill, Side
from openpyxl.utils import get_column_letter
from openpyxl.worksheet.datavalidation import DataValidation


def xl_color(css_hex: str) -> str:
    value = css_hex.removeprefix("#").upper()
    if len(value) != 6:
        raise ValueError(f"Expected #RRGGBB, got: {css_hex}")
    return "FF" + value


XL_HEADER_BG = xl_color("#4472C4")
XL_HEADER_FG = xl_color("#FFFFFF")
XL_BORDER = xl_color("#BFBFBF")
XL_STRIPE = xl_color("#F2F6FC")

thin = Side(style="thin", color=XL_BORDER)
BORDER_ALL = Border(left=thin, right=thin, top=thin, bottom=thin)

HEADER_FONT = Font(name="微软雅黑", size=10, bold=True, color=XL_HEADER_FG)
BODY_FONT = Font(name="微软雅黑", size=10)
HEADER_FILL = PatternFill("solid", fgColor=XL_HEADER_BG)

CENTER = Alignment(horizontal="center", vertical="center")
LEFT = Alignment(horizontal="left", vertical="center")

# 数据验证预留到第 200 行，方便用户持续往下填
VALIDATE_LAST_ROW = 200


def build_table(ws, headers, widths, rows, validations):
    """铺一张配置表：表头 + 样例数据 + 边框 + 列宽 + 冻结 + 筛选 + 下拉"""
    col_count = len(headers)

    # 表头
    for idx, name in enumerate(headers, start=1):
        cell = ws.cell(row=1, column=idx, value=name)
        cell.font = HEADER_FONT
        cell.fill = HEADER_FILL
        cell.alignment = CENTER
        cell.border = BORDER_ALL
    ws.row_dimensions[1].height = 24

    # 列宽
    for idx, width in enumerate(widths, start=1):
        ws.column_dimensions[get_column_letter(idx)].width = width

    # 样例数据
    for r, row in enumerate(rows, start=2):
        for c, value in enumerate(row, start=1):
            cell = ws.cell(row=r, column=c, value=value)
            cell.font = BODY_FONT
            cell.border = BORDER_ALL
            cell.alignment = LEFT if c == 1 else CENTER
            # 隔行浅底，纯长表更容易跟行
            if r % 2 == 0:
                cell.fill = PatternFill("solid", fgColor=XL_STRIPE)

    # 数据验证（下拉）：限制成代码能识别的取值，避免填错后解析抛异常
    for col_letter, options in validations.items():
        dv = DataValidation(
            type="list",
            formula1='"' + ",".join(options) + '"',
            allow_blank=True,
            showDropDown=False,   # False 表示显示下拉箭头（openpyxl 该参数语义为反）
        )
        dv.error = "请从下拉列表中选择，手填的值程序无法识别"
        dv.errorTitle = "取值不在允许范围"
        ws.add_data_validation(dv)
        dv.add(f"{col_letter}2:{col_letter}{VALIDATE_LAST_ROW}")

    # 冻结表头 + 自动筛选
    ws.freeze_panes = "A2"
    ws.auto_filter.ref = f"A1:{get_column_letter(col_count)}{max(len(rows) + 1, 2)}"

    # 打印：横向、一页宽
    ws.page_setup.orientation = "landscape"
    ws.page_setup.fitToWidth = 1
    ws.sheet_properties.pageSetUpPr.fitToPage = True


wb = Workbook()

# ============ Sheet1 设备表（代码取 C 列 IP、E 列型号） ============
ws1 = wb.active
ws1.title = "设备表"
build_table(
    ws1,
    ["设备名称", "连接方式", "设备IP", "设备类型", "设备型号"],
    [16, 14, 18, 14, 16],
    [
        ["PLC1", "以太网", "192.168.0.10", "S71200", "Siemens"],
        ["PLC2", "以太网", "192.168.0.20", "S71200", "Siemens"],
        ["PLC3", "以太网", "192.168.0.30", "FX5U", "SanLing"],
    ],
    {"E": ["Siemens", "SanLing"]},
)

# ============ Sheet2 变量表（代码取 A/B/C/E 列） ============
ws2 = wb.create_sheet("变量表")
build_table(
    ws2,
    ["变量名称", "数据类型", "DB地址", "设备名称", "设备型号"],
    [22, 14, 16, 16, 16],
    [
        ["启动信号", "Bool", "DB1.0.0", "PLC1", "Siemens"],
        ["运行速度", "Int16", "DB1.2", "PLC1", "Siemens"],
        ["产量计数", "Int32", "DB1.4", "PLC1", "Siemens"],
        ["温度设定", "Real", "DB1.8", "PLC1", "Siemens"],
        ["物料编码", "String", "DB1.12", "PLC1", "Siemens"],
    ],
    {"B": ["Bool", "Int16", "Int32", "String", "Real"], "E": ["Siemens", "SanLing"]},
)

# ============ Sheet3 机器人点位表（代码取 A~D 列） ============
ws3 = wb.create_sheet("机器人点位")
build_table(
    ws3,
    ["机器人名称", "数据名称", "数据类型", "所属程序"],
    [20, 20, 16, 20],
    [
        ["IRB1200", "nCycleCount", "num", "T_ROB1"],
        ["IRB1200", "bStart", "bool", "T_ROB1"],
        ["IRB6700", "robtarget_Home", "robtarget", "T_ROB1"],
    ],
    {},
)

# ============ Sheet4 填写说明（ReadIniFile 不读它，纯粹给人看） ============
ws4 = wb.create_sheet("填写说明")
ws4.column_dimensions["A"].width = 18
ws4.column_dimensions["B"].width = 92

notes = [
    ("工作表顺序", "设备表、变量表、机器人点位 三张表的顺序不能调整，程序按第 1/2/3 个工作表依次读取。本说明表放在最后，不影响解析。"),
    ("设备表·C 列", "填 PLC 的 IP 地址，必填。程序用它建立连接。"),
    ("设备表·E 列", "只能填 Siemens 或 SanLing。程序用这一列判断整张设备表属于哪个品牌，必须完全一致（区分大小写）。"),
    ("变量表·A 列", "填变量名称，用于界面显示。建议用中文业务名，不要填 IP。"),
    ("变量表·B 列", "只能填 Bool / Int16 / Int32 / String / Real 五选一。填其它值会导致读取时直接抛异常。"),
    ("变量表·C 列", "填 DB 地址，格式如 DB1.0.0（Bool）、DB1.2（Int16/Int32）、DB1.8（Real）。"),
    ("变量表·E 列", "填 Siemens 或 SanLing，需与设备表中的型号一致，否则该条点位会被忽略。"),
    ("机器人点位", "当前版本的读取代码尚未启用，先按格式填好，等通讯实现后即可生效。"),
    ("文件位置", r"程序从运行目录下的 ini\配置文件.xlsx 读取，不是从源码目录读。修改后需替换到 bin\Debug\net8.0-windows\ini\ 下。"),
]

ws4["A1"] = "填写说明"
ws4["A1"].font = Font(name="微软雅黑", size=12, bold=True)
ws4["B1"] = "PLC 配置表的字段含义与填写约束"
ws4["B1"].font = Font(name="微软雅黑", size=10, color="FF6B7280")
ws4.row_dimensions[1].height = 24

for r, (topic, text) in enumerate(notes, start=2):
    a = ws4.cell(row=r, column=1, value=topic)
    a.font = Font(name="微软雅黑", size=10, bold=True)
    a.alignment = Alignment(horizontal="left", vertical="top")
    a.border = BORDER_ALL

    b = ws4.cell(row=r, column=2, value=text)
    b.font = BODY_FONT
    b.alignment = Alignment(horizontal="left", vertical="top", wrap_text=True)
    b.border = BORDER_ALL
    # 每条说明高度按文字长度粗估，避免长句被压成一行
    ws4.row_dimensions[r].height = max(20, 16 * (len(text) // 44 + 1))

wb.properties.title = "PLC配置表"
wb.save(r"D:\Program\小玩意\小玩意\小玩意\ini\PLC配置表.xlsx")
print("saved")
