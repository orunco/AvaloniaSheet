using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvaloniaSheetControl.Porting;
using HorizontalAlignment = Avalonia.Layout.HorizontalAlignment;
using Point = Avalonia.Point;

namespace AvaloniaSheetControl;

/*
AvaloniaFromGithub\samples\ControlCatalog\Pages\CustomDrawingExampleControl.cs
 */
public partial class GridCanvasX : CanvasX{
    private ISheetData _sheetData;
    public int HeadRowID = -1;
    public HashSet<int> IgnoredColumnIDs = new();

    private Point cursorPoint;
    private bool isPointerCaptured;

    private double _totalRowHeight;
    private double _totalColWidth;

    
    private readonly IPen transparentLinePen;
    private readonly IPen guideLinePen;
    private readonly IPen splitterMovingLinePen;

    private static FontFamily Default{ get; } = new(
        "Inter, -apple-system,BlinkMacSystemFont,PingFang SC, Microsoft YaHei, Segoe UI, Hiragino Sans GB, Helvetica Neue,Helvetica,Arial,sans-serif");

    private readonly Typeface _typeface = new(Default);

    private readonly TextBox editorTextBox = new(){
        Classes ={ "ClassInputEditor" }
    };

    private readonly Border LeftTopHeaderTriangle = new(){
        Child = new Polygon(){
            Points = new Points{
                new(UIColumnHeaderHeight - 2, 0),
                new(0, UIColumnHeaderHeight - 2),
                new(UIColumnHeaderHeight - 2, UIColumnHeaderHeight - 2),
            },
            Fill = SystemColors.ControlLightColor,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        },
        Background = Brushes.Transparent,
    };

    private readonly Border singleCellSelectionBorder = new(){
        BorderThickness = new Thickness(0.5),
        BorderBrush = new SolidColorBrush(new Color(0XFF, 0x21, 0x73, 0x46))
    };

    private readonly Border singleRowMoveBorder = new(){
        BorderThickness = new Thickness(0.5),
        Background = new SolidColorBrush(new Color(0x40, 0x00, 0x96, 0xFF)), // 更深的蓝色背景
        BorderBrush = new SolidColorBrush(new Color(0xFF, 0x00, 0x78, 0xD7)) // 蓝色边框
    };

/*
20240921 使用MenuFlyout做右键菜单是不可行的，因为右键点击一次出现MenuFlyout，
再次点击不会出现，再次点击才出现，不符合上下文菜单的场景，因为控件会发射
pointerRelease事件，通过devtool可以确认这一点

menuflyout适合的场景是类似word中的选择某段文字，采用
_menuFlyout.ShowAt(this, true); 主动显示体验非常好的工具栏菜单
浮动对象是非常特殊的对象，也许调试后才发现，还不如类似inputtextbox
直接加入child显示来的直接
 */
    private readonly ContextMenu _contextMenu = new(){
        Classes ={ "ClassContextMenu" },
        Items ={
            new MenuItem(){
                Header = "demo menu ",
                Icon = new Image(){
                    Width = 32,
                    Height = 32,
                    Source = new Bitmap("Themes\\Resources\\CutHS.png")
                },
            }
        }
    };


    // Items ={
    //     new MenuItem(){
    //         Header = "此行为标题",
    //     },
    //     new MenuItem(){
    //         Header = "忽略此列"
    //     },
    //     new MenuItem(){
    //         Header = "撤销忽略"
    //     }
    // }

    /*
    最重要的数据：当前viewport在整个view中的相对值是多少
    原生ScrollViewer中的Vector Offset
    ReoGrid.IViewport中的ScrollX/ScrollY

    !!!
    注意：这个X Y 坐标值是相对view的，对于view外面，还有其他比如可能存在的
    head等其他组件，需要精确剔除
    !!!
    */
    private double ViewportOffsetX{ get; set; }
    private double ViewportOffsetY{ get; set; }

    public GridCanvasX(){
        this.ContextMenu = _contextMenu;

        Children.Add(LeftTopHeaderTriangle);

        Children.Add(editorTextBox);
        Children.Add(singleCellSelectionBorder);
        Children.Add(singleRowMoveBorder);

        CanvasX.SetLeft(LeftTopHeaderTriangle, UIRowHeaderWidth - UIColumnHeaderHeight);

        // public Pen(
        //     uint color,
        //     double thickness = 1.0,
        //     IDashStyle? dashStyle = null,
        //     PenLineCap lineCap = PenLineCap.Flat,
        //     PenLineJoin lineJoin = PenLineJoin.Miter,
        //     double miterLimit = 10.0)

        transparentLinePen = new Pen(new SolidColorBrush(Colors.Transparent),
            0,
            lineCap: PenLineCap.Round);
        guideLinePen = new Pen(new SolidColorBrush(Colors.LightGray),
            0.5,
            lineCap: PenLineCap.Round);
        splitterMovingLinePen = new Pen(new SolidColorBrush(Colors.Black),
            lineCap: PenLineCap.Round,
            dashStyle: DashStyle.Dot);
    }

    // 累加器
    private CellSizeAccumulator rowHeightAccumulator;
    private CellSizeAccumulator colWidthAccumulator;
    
    public ISheetData SheetData{
        get => _sheetData;
        set{
            _sheetData = value;
            // 确保在QSheetData设置后再初始化累加器
            rowHeightAccumulator = new CellSizeAccumulator(i => value.GetRowHeightInUI((uint)i));
            rowHeightAccumulator.Initialize(value.GetValidRowCount());
            log.Debug($"RowSizeAccumulator initialized with {value.GetValidRowCount()} rows");
                   
            // 初始化列宽累加器
            colWidthAccumulator = new CellSizeAccumulator(i => value.GetColWidthInUI((uint)i));
            colWidthAccumulator.Initialize(value.GetValidColCount());
            log.Debug($"ColSizeAccumulator initialized with {value.GetValidColCount()} cols");
        }
    }

    // 构造函数内bound为空，无法计算可视区域，所以需要外部调用
    public void AfterLoad(){
        log.Debug("enter method.");

        CalcVisualRowRegion();
        CalcVisualColRegion();

        UpdateRowSplitter();
        UpdateColSplitter();
        UpdateClearHighlightRange();

        InvalidateArrange();
    }

    // 主界面目前没有托拉拽的需求
    protected override void OnPointerPressed(PointerPressedEventArgs e){
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed){
            UpdateClearHighlightRange();

            CalcClickCellRowColID(e.GetPosition(this));

            if (e.ClickCount == 2){
                // 进入编辑模式
                UpdateEditorTextBox();
                InvalidateVisual();
            }
            else if (e.ClickCount == 1){
                UpdateHighlightRange();
                InvalidateVisual();
            }
        }
        else{
            //_menuFlyout.ShowAt(this,true);

            // 右键菜单，需要特别设置
            //base.OnPointerPressed(e);
        }
    }


    // 必须有，更新了当前坐标，实现绘画托拉拽自由
    protected override void OnPointerMoved(PointerEventArgs e){
        base.OnPointerMoved(e);

        Point previousPoint = cursorPoint;

        cursorPoint = e.GetPosition(this);

        // 检查鼠标是否在滚动条区域
        bool isInScrollbarArea = cursorPoint.X > this.Bounds.Width - 26 || 
                                 cursorPoint.Y > this.Bounds.Height - 26;
 
        if(isInScrollbarArea){
            singleRowMoveBorder.IsVisible = false;
        }
        else{
            var curosrResult = CalcClickCellRowColID1(cursorPoint);
            UpdatePointerMove(curosrResult);
        }

        // 坐标以主窗口为基准，即使外面有margin之类的，坐标也是稳定的
        //Console.WriteLine($"Captured={_isPointerCaptured} _cursorPoint={_cursorPoint} ");
        //Console.WriteLine($"DesiredSize={this.DesiredSize} bound={this.Bounds} {this.IsHitTestVisible}  ");

        if (isPointerCaptured){
            //Console.WriteLine($"_isPointerCaptured={_isPointerCaptured}");
        }

        // 文本框显示
        //SetTop(textbox, _cursorPoint.Y);
        //SetLeft(textbox, _cursorPoint.X);
        InvalidateArrange(); // 改变了位置信息，InvalidateVisual无效
    }
    //
    // protected override void OnPointerReleased(PointerReleasedEventArgs e){
    //     e.Pointer.Capture(null);
    //     isPointerCaptured = false;
    //     Console.WriteLine($"OnPointerReleased {e.GetPosition(this)}");
    //     base.OnPointerReleased(e);
    // }

    protected override void OnSizeChanged(SizeChangedEventArgs args){
        log.Debug("enter method.");

        base.OnSizeChanged(args);

        // 到这里，计算出了可视区域
        CalcVisualRowRegion();
        CalcVisualColRegion();

        UpdateRowSplitter();
        UpdateColSplitter();

        InvalidateVisual();
    }

    private int clickCellRowID = -1;
    private int clickCellColID = -1;
    private double singleCellSelectionBorderTop = 0;
    private double singleCellSelectionBorderLeft = 0;

    private void CalcClickCellRowColID(Point clickPoint){
        if (SheetData.GetValidRowCount() == 0 ||
            SheetData.GetValidColCount() == 0){
            visualColStartID = -1;
            visualColEndID = -1;
            return;
        }

        if (visualColStartID == -1 ||
            visualColEndID == -1){
            return;
        }

        clickCellRowID = -1;
        clickCellColID = -1;

        double acc = UIColumnHeaderHeight;
        for (var i = visualRowStartID; i <= visualRowEndID; i++){
            if (acc <= clickPoint.Y &&
                clickPoint.Y < acc + SheetData.GetRowHeightInUI((uint)i)){
                clickCellRowID = i;
                singleCellSelectionBorderTop = acc;
                break;
            }

            acc += SheetData.GetRowHeightInUI((uint)i);
        }

        acc = UIRowHeaderWidth;
        for (var i = visualColStartID; i <= visualColEndID; i++){
            if (acc <= clickPoint.X &&
                clickPoint.X < acc + SheetData.GetColWidthInUI((uint)i)){
                clickCellColID = i;
                singleCellSelectionBorderLeft = acc;
                break;
            }

            acc += SheetData.GetColWidthInUI((uint)i);
        }
    }

    // 这个代码是重复的，逻辑有差异
    private (int rowID, int colID, double top, double left) CalcClickCellRowColID1(Point clickPoint){
        int rowid = -1;
        int colid = -1;
        double top = 0;
        double left = 0;

        if (SheetData.GetValidRowCount() == 0 ||
            SheetData.GetValidColCount() == 0){
            return (rowid, colid, top, left);
        }

        if (visualColStartID == -1 ||
            visualColEndID == -1){
            return (rowid, colid, top, left);
        }

        double acc = UIColumnHeaderHeight;
        for (var i = visualRowStartID; i <= visualRowEndID; i++){
            if (acc <= clickPoint.Y &&
                clickPoint.Y < acc + SheetData.GetRowHeightInUI((uint)i)){
                rowid = i;
                top = acc;
                break;
            }

            acc += SheetData.GetRowHeightInUI((uint)i);
        }


        acc = UIRowHeaderWidth;
        for (var i = visualColStartID; i <= visualColEndID; i++){
            if (acc <= clickPoint.X &&
                clickPoint.X < acc + SheetData.GetColWidthInUI((uint)i)){
                colid = i;
                left = acc;
                break;
            }

            acc += SheetData.GetColWidthInUI((uint)i);
        }

        return (rowid, colid, top, left);
    }

    private void UpdatePointerMove((int rowID, int colID, double top, double left) re){
        if (re.rowID == -1 || re.colID == -1){
            singleRowMoveBorder.IsVisible = false;
        }
        else{
            singleRowMoveBorder.IsVisible = true;
            singleRowMoveBorder.Width = this.Bounds.Width;
            singleRowMoveBorder.Height = SheetData.GetRowHeightInUI((uint)re.rowID);

            // 这里CalculateRowPositionY计算量是可以接受的，采用了累加器，否则约到后面越慢
            CanvasX.SetTop(singleRowMoveBorder, CalculateRowPositionY(re.rowID));
            CanvasX.SetLeft(singleRowMoveBorder, 0);
        }
    }
    
        
    // 计算总行高（视图层职责）
    private double GetTotalRowHeight() => 
        rowHeightAccumulator.GetAccumulated(
            SheetData.GetValidRowCount());
    
    // 计算总列宽（视图层职责）
    private double GetTotalColWidth() => 
        colWidthAccumulator.GetAccumulated(
            SheetData.GetValidColCount());
}