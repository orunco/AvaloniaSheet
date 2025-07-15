using System.Collections.Generic;
using Avalonia;
using Avalonia.Input;
using AvaloniaSheetControl.Part;
using AvaloniaSheetControl.Porting;

namespace AvaloniaSheetControl;

public partial class GridCanvasX{
    private static readonly XLogger log = new();

    // 行头宽度，必须可以一次性容纳1048576的数字宽度
    private readonly double UIRowHeaderWidth = 50;

    public static readonly double RowHeightInExcelToUIFactor = 96.0 / 72.0;

    // 这里默认值采用16.5而不是15
    public static readonly double DefaultRowHeightInExcel = 16.5;

    private readonly double MinRowHeight = DefaultRowHeightInExcel *
                                           RowHeightInExcelToUIFactor;

    private List<RowSplitter> rowSplitters = new();
    private bool isRowSplitterMoving;
    private Point _rowSpiltterStartPoint; //需要记录下来

    // 像素级滚动的偏移量
    private double pixelOffsetY = 0;

    private int visualRowStartID = -1;
    private int visualRowEndID = -1;

    // 纵向滚动条滚动时
    public void OnVerticalScrollBarScroll(double newValue_0_100){
        log.Debug($"enter OnVerticalScrollBarScroll()");

        if (newValue_0_100 is < 0 or > 100){
            return;
        }

        // 使用像素级滚动
        pixelOffsetY = newValue_0_100 *
            GetTotalRowHeight() / 100;
        ViewportOffsetY = pixelOffsetY; // 保持兼容性

        log.Debug($"OnVerticalScrollBarScroll(): newValue_0_100={newValue_0_100} pixelOffsetY={pixelOffsetY}");

        CalcVisualRowRegion();
        UpdateRowSplitter();
        UpdateClearHighlightRange();

        InvalidateVisual();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e){
        log.Debug($"enter OnPointerWheelChanged()");

        base.OnPointerWheelChanged(e);

        // 使用像素级滚动
        double pixelScrollAmount = 40; // 每次滚轮滚动的像素数量，可调整

        if (e.Delta.Y < 0){
            pixelOffsetY += pixelScrollAmount;
            if (pixelOffsetY > GetTotalRowHeight()
                - Bounds.Height
                + GridCanvasX.UIColumnHeaderHeight)
                pixelOffsetY = GetTotalRowHeight()
                               - Bounds.Height
                               + GridCanvasX.UIColumnHeaderHeight;
        }
        else{
            pixelOffsetY -= pixelScrollAmount;
            if (pixelOffsetY < 0) pixelOffsetY = 0;
        }

        CalcVisualRowRegion();

        UpdateRowSplitter();
        UpdateClearHighlightRange();

        InvalidateVisual();
    }

    private void CalcVisualRowRegion(){
        log.Debug($"enter CalcVisualRowRegion(): pixelOffsetY={pixelOffsetY}, Bounds.Height={Bounds.Height}");

        ViewportOffsetY = pixelOffsetY; // 保持兼容性
        int totalRows = SheetData.GetValidRowCount();
        if (totalRows == 0){
            visualRowStartID = -1;
            visualRowEndID = -1;
            return;
        }

        double viewportTop = pixelOffsetY;
        double viewportBottom = pixelOffsetY
                                + Bounds.Height
                                - GridCanvasX.UIColumnHeaderHeight;

        // 使用二分查找找到视口起始行
        visualRowStartID = FindFirstVisibleRow(totalRows, viewportTop);

        // 如果没有找到可见行
        if (visualRowStartID == -1){
            visualRowEndID = -1;
            return;
        }

        // 顺序查找结束行
        visualRowEndID = visualRowStartID;
        double currentTop = rowHeightAccumulator.GetAccumulated(visualRowStartID);

        for (int rowID = visualRowStartID; rowID < totalRows; rowID++){
            double rowHeight = SheetData.GetRowHeightInUI((uint)rowID);
            double rowBottom = currentTop + rowHeight;

            if (currentTop >= viewportBottom){
                visualRowEndID = rowID - 1;
                break;
            }

            if (rowBottom > viewportBottom){
                visualRowEndID = rowID;
                break;
            }

            if (rowID == totalRows - 1){
                visualRowEndID = rowID;
            }

            currentTop = rowBottom;
        }

        log.Debug(
            $"CalcVisualRowRegion(): Bounds={Bounds} Row={visualRowStartID}->{visualRowEndID} viewportTop={viewportTop} viewportBottom={viewportBottom} totalRows={SheetData.GetValidRowCount()}");
    }

    private int FindFirstVisibleRow(int totalRows, double viewportTop){
        int low = 0;
        int high = totalRows - 1;
        int candidate = -1;

        // 二分法
        while (low <= high){
            int mid = (low + high) / 2;
            double rowTop = rowHeightAccumulator.GetAccumulated(mid);
            double rowBottom = rowTop + SheetData.GetRowHeightInUI((uint)mid);

            if (rowBottom <= viewportTop){
                low = mid + 1;
            }
            else{
                candidate = mid;
                high = mid - 1;
            }
        }

        return candidate;
    }

    private void UpdateRowSplitter(){
        log.Debug($"enter UpdateRowSplitter()");

        // 先清空所有的rowsplitter对象
        foreach (var oldRowSplitter in rowSplitters){
            this.Children.Remove(oldRowSplitter);
        }

        double accHeight;
        // 重新生成所有的rowsplitter对象，并赋值
        List<RowSplitter> newRowSplitters = new List<RowSplitter>();

        // 计算行分隔符的位置，考虑像素级滚动
        accHeight = GridCanvasX.UIColumnHeaderHeight;
        for (int rowID = visualRowStartID; rowID < visualRowEndID; rowID++){
            accHeight += SheetData.GetRowHeightInUI((uint)rowID);
            var rowSplitter = new RowSplitter(rowID, UIRowHeaderWidth);

            newRowSplitters.Add(rowSplitter);
            // CanvasX.InvalidateMeasureOnChildrenChanged()
            this.Children.Add(rowSplitter);

            // 设置正确的位置
            double splitterPosition = CalculateRowPositionY(rowID + 1);
            CanvasX.SetTop(rowSplitter, splitterPosition);

            // 鼠标点击三部曲：参考reogrid.SheetTabControl rightThumb代码
            rowSplitter.PointerPressed += (s, e) => {
                log.Debug($"enter PointerPressed.");

                if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed){
                    return;
                }

                e.Handled = true;
                e.Pointer.Capture(rowSplitter);
                isRowSplitterMoving = true;

                //相对Canvas的坐标，this=canvas
                _rowSpiltterStartPoint = e.GetPosition(this);
                // Console.WriteLine($"spiltterStartPoint={_rowSpiltterStartPoint}");
                base.OnPointerPressed(e);
            };

            rowSplitter.PointerMoved += (s, e) => {
                log.Debug($"enter PointerMoved.");

                if (isRowSplitterMoving){
                    //Console.WriteLine("rowSplitter.PointerMoved()");
                }
            };

            rowSplitter.PointerReleased += (s, e) => {
                log.Debug($"enter PointerReleased.");

                if (!(s is RowSplitter) ||
                    !isRowSplitterMoving){
                    return;
                }

                e.Pointer.Capture(null);
                isRowSplitterMoving = false;

                // 更新row高度
                var curPoint = e.GetPosition(this);
                // Console.WriteLine($"curPoint={curPoint}");

                // 有可能是正数，也有可能是负数
                var delta = curPoint.Y - _rowSpiltterStartPoint.Y;

                var newHeight = SheetData.GetRowHeightInUI((uint)((RowSplitter)s).RowID) + delta;

                // 不能低于最小高度
                if (newHeight <= MinRowHeight){
                    newHeight = MinRowHeight;
                }

                // Console.WriteLine($"rowid={((RowSplitter)s).RowID} newHeight={newHeight}");
                SheetData.SetRowHeightInUI((uint)((RowSplitter)s).RowID, (float)newHeight);
                rowHeightAccumulator.UpdateSize(((RowSplitter)s).RowID, newHeight); // 更新累加器
                _totalRowHeight = GetTotalRowHeight(); // 更新缓存的总高度

                base.OnPointerReleased(e);

                // 鼠标释放后需要刷新，否则这些对象还在原来的位置上
                CalcVisualRowRegion();
                UpdateRowSplitter();
                //InvalidateArrange();
            };
        }

        // 替换
        rowSplitters = newRowSplitters;
    }

    // 计算行在屏幕上的显示位置，考虑滚动偏移
    private double CalculateRowPositionY(int rowId){
        if (rowId < 0){
            return GridCanvasX.UIColumnHeaderHeight;
        }

        // 使用累加器获取累计高度
        var totalHeightBefore =
            rowHeightAccumulator.GetAccumulated(rowId);

        // 应用滚动偏移
        return GridCanvasX.UIColumnHeaderHeight
               + totalHeightBefore
               - pixelOffsetY;
    }
}