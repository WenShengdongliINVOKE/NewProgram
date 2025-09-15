using System.Windows.Input;

namespace 小玩意
{
    public class GridCell
    {
        public enum CellType { TextBlock, Button }

        public CellType Type { get; set; }     // 控件类型
        public string? Content { get; set; }    // 显示内容
        public ICommand? Command { get; set; } // 按钮绑定的命令（可选）
    }
}
