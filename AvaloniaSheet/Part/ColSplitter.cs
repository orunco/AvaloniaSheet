using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace AvaloniaSheetControl.Part;

public class ColSplitter : Border{
    public int ColID{ get; }

    public ColSplitter(int colId, double colHeaderHeight){
        Width = 5;
        Height = colHeaderHeight;
        Cursor = new Cursor(StandardCursorType.SizeWestEast);
        ColID = colId;
#if DEBUG
        Background = Brushes.Transparent;
        ToolTip.SetTip(this, $"colID={colId}");
#else
        Background = Brushes.Transparent;   //必须有值，否则不响应事件
#endif
    }
}