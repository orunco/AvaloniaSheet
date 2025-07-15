using System;
using System.Collections.Generic;
using Avalonia;
using AvaloniaSheetControl.Part;
using AvaloniaSheetControl.Porting;

namespace AvaloniaSheetControl;

// 目前没有实现逐像素和CalcVisualColRegion的二分法查找
public partial class GridCanvasX{
    // 列头高度 可以容纳单行字母即可
    private static readonly double UIColumnHeaderHeight = 22;
    public static readonly double DefaultColWidthInExcel = 9;
    public static readonly double ColWidthInExcelToUIFactor = 6 * 96.0 / 72.0;

    private readonly double MinColWidth = DefaultColWidthInExcel *
                                          ColWidthInExcelToUIFactor;

    private List<ColSplitter> colSplitters = new();
    private bool isColSplitterMoving;
    private Point _colSpiltterStartPoint; //需要记录下来

    private int visualColStartID = -1;
    private int visualColEndID = -1;

    public void OnHorizontalScrollBarScroll(double newValue_0_100){
        if (newValue_0_100 is < 0 or > 100){
            return;
        }

        // 计算出当前viewPort所在的坐标值，注意，这里不包括最后一行
        ViewportOffsetX = newValue_0_100 * GetTotalColWidth() / 100;

        // Console.WriteLine($"newValue_0_100={newValue_0_100} ViewportOffsetY={ViewportOffsetY}");

        CalcVisualColRegion();
        UpdateColSplitter();
        UpdateClearHighlightRange();

        InvalidateVisual();
    }

    private void CalcVisualColRegion(){
        if (SheetData.GetValidRowCount() == 0 ||
            SheetData.GetValidColCount() == 0){
            visualColStartID = -1;
            visualColEndID = -1;
            return;
        }

        // 输入：起点：ViewportOffsetX
        //      画框：当前绘图窗口bounds为viewport
        // 输出：计算出visibleRegion，要求完全覆盖画框

        double accWidth = 0;

        for (var i = 0; i < SheetData.GetValidColCount(); i++){
            // 一旦累计值大于等于X
            if (accWidth >= ViewportOffsetX){
                visualColStartID = i;
                break;
            }
            else{
                accWidth += SheetData.GetColWidthInUI((uint)i);
            }

            // 尾部边界条件
            if (i == SheetData.GetValidColCount() - 1){
                visualColStartID = i;
            }
        }

        // 从visualColStart开始计算到结尾
        accWidth = 0;
        for (int i = visualColStartID; i < SheetData.GetValidColCount(); i++){
            // 宁可更多，所以是先累计高度
            accWidth += SheetData.GetColWidthInUI((uint)i);

            // 一旦累计值大于等于
            if (accWidth >= Bounds.Width){
                visualColEndID = i;
                break;
            }

            // 尾部边界条件
            if (i == SheetData.GetValidColCount() - 1){
                visualColEndID = i;
            }
        }

        //Console.WriteLine($"NewMethod() Bounds={Bounds} Col={visualColStart}->{visualColEnd}");
    }

    private void UpdateColSplitter(){
        // 先清空所有的colsplitter对象
        foreach (var oldColSplitter in colSplitters){
            this.Children.Remove(oldColSplitter);
        }

        double accWidth;

        // 重新生成所有的colsplitter对象，并赋值
        List<ColSplitter> newColSplitters = new List<ColSplitter>();

        accWidth = UIRowHeaderWidth;
        for (int colID = visualColStartID; colID < visualColEndID; colID++){
            accWidth += SheetData.GetColWidthInUI((uint)colID);
            var colSplitter = new ColSplitter(colID, UIColumnHeaderHeight);

            newColSplitters.Add(colSplitter);
            // CanvasX.InvalidateMeasureOnChildrenChanged()
            this.Children.Add(colSplitter);

            // 设置正确的位置
            CanvasX.SetLeft(colSplitter, accWidth);

            // 鼠标点击三部曲：参考reogrid.SheetTabControl rightThumb代码
            colSplitter.PointerPressed += (s, e) => {
                if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed){
                    return;
                }

                e.Handled = true;
                e.Pointer.Capture(colSplitter);
                isColSplitterMoving = true;

                //相对Canvas的坐标，this=canvas
                _colSpiltterStartPoint = e.GetPosition(this);
                // Console.WriteLine($"spiltterStartPoint={_colSpiltterStartPoint}");
                base.OnPointerPressed(e);
            };

            colSplitter.PointerMoved += (s, e) => {
                if (isColSplitterMoving){
                    //Console.WriteLine("colSplitter.PointerMoved()");
                }
            };

            colSplitter.PointerReleased += (s, e) => {
                if (!isColSplitterMoving){
                    return;
                }

                e.Pointer.Capture(null);
                isColSplitterMoving = false;

                // 更新col高度
                var curPoint = e.GetPosition(this);
                // Console.WriteLine($"curPoint={curPoint}");

                // 有可能是正数，也有可能是负数
                var delta = curPoint.X - _colSpiltterStartPoint.X;

                var newWidth = SheetData.GetColWidthInUI((uint)((ColSplitter)s).ColID) + delta;

                // 不能低于最小宽度
                if (newWidth <= MinColWidth){
                    newWidth = MinColWidth;
                }

                Console.WriteLine($"colid={((ColSplitter)s).ColID} newWidth={newWidth}");
                SheetData.SetColWidthInUI((uint)((ColSplitter)s).ColID, (float)newWidth);
                colWidthAccumulator.UpdateSize(((ColSplitter)s).ColID, newWidth); // 更新累加器
                _totalColWidth = GetTotalColWidth(); // 更新缓存的总宽度
                
                base.OnPointerReleased(e);

                // 鼠标释放后需要刷新，否则这些对象还在原来的位置上
                CalcVisualColRegion();
                UpdateColSplitter();
                UpdateClearHighlightRange();
                //InvalidateArrange();
            };
        }

        // 替换
        colSplitters = newColSplitters;
    }
}