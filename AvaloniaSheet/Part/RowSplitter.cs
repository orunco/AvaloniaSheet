using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace AvaloniaSheetControl.Part;

/*
三种方法解决事件内存泄露问题：
1、主动调用自定义的unload函数，传入 -= 操作
2、弱引用事件
3、匿名方法或lambda表达式绑定事件，这样当MainWindow不再使用时，事件处理器会随之自动被垃圾收集器回收

同excel，RowSplitter位于rowid所在行的下方，比如最后一行的下方有最后一个RowSplitter
excel做的比较精细化，是放在中心点位置上的
 */
public class RowSplitter : Border{
    public int RowID{ get; }

    public RowSplitter(int rowId,double rowHeaderWidth){
        Width = rowHeaderWidth;
        Height = 5;
        Cursor = new Cursor(StandardCursorType.SizeNorthSouth);
        RowID = rowId;
#if DEBUG
        Background = Brushes.Transparent;
        ToolTip.SetTip(this, $"rowID={rowId}");
#else
        Background = Brushes.Transparent;   //必须有值，否则不响应事件
#endif
    }
}