using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using AvaloniaSheetControl.Porting;

namespace AvaloniaSheetControl;

public partial class GridCanvasX{
    public override void Render(DrawingContext dc){
        // 渲染函数不能出现任何Children.Add(contorl)
        // 提示System.InvalidOperationException: Visual was invalidated during the render pass
        // 所以所有的对象都要在初始化时和事件中准备妥当，这里仅仅是画画
        // base.Render(dc) 应该是在绘制contorl类
        base.Render(dc);

        // 渲染函数不能出现任何invalid
        // InvalidateVisual();

        // 加速绘画，需要去掉ScrollBar的宽度
        var localBounds = new Rect(new Size(
            Bounds.Width,
            Bounds.Height));
        var clip = dc.PushClip(localBounds);

        //Console.WriteLine($"Bounds={Bounds} Row={visualRowStart}->{visualRowEnd}");

        // ----1----
        // 1.1 整个控件画背景颜色，一个是debug，二来是鼠标的滚轮事件必须有要对象才能接受
        // 空的地方是无法出发滚轮事件的，非常奇怪，相当于这个背景颜色作为mask
        dc.DrawRectangle(Brushes.Transparent, guideLinePen, localBounds, 1.0d);

        // 1.2 列头底色
        dc.DrawRectangle(SystemColors.ControlColor,
            transparentLinePen,
            new Rect(0, 0, localBounds.Width, GridCanvasX.UIColumnHeaderHeight));

        // 1.2 行头底色
        dc.DrawRectangle(SystemColors.ControlColor,
            transparentLinePen,
            new Rect(0, 0, UIRowHeaderWidth, localBounds.Height));

        // 2 画横线 
        double accHeight = 0;

        // 2.1 画列头区域
        // 考虑像素级滚动的起始位置
        accHeight = GridCanvasX.UIColumnHeaderHeight;
        dc.DrawLine(guideLinePen,
            new Point(0, accHeight),
            new Point(localBounds.Width, accHeight));

        // 2.2 画表格横线
        if (visualRowStartID != -1 && visualRowEndID != -1){
            // 计算第一行显示位置，考虑像素级偏移
            accHeight = CalculateRowPositionY(visualRowStartID);

            for (var rowID = visualRowStartID; rowID <= visualRowEndID; rowID++){
                // 2.2.1 高亮标记headrow背景（用户设置数据） 
                if (HeadRowID == rowID){
                    dc.DrawRectangle(SystemColors.PaleBlueColorBrush,
                        transparentLinePen,
                        new Rect(
                            UIRowHeaderWidth,
                            accHeight,
                            localBounds.Width - UIRowHeaderWidth,
                            // 使用实际行高度，不受滚动影响
                            SheetData.GetRowHeightInUI((uint)rowID)));
                }

                accHeight += SheetData.GetRowHeightInUI((uint)rowID);
                dc.DrawLine(guideLinePen,
                    new Point(0, accHeight),
                    new Point(localBounds.Width, accHeight));
            }
        }

        // 3 画垂直线
        double accWidth = 0;

        // 3.1 画列头
        accWidth += UIRowHeaderWidth;
        dc.DrawLine(guideLinePen,
            new Point(accWidth, 0),
            new Point(accWidth, localBounds.Height));

        // 3.2 画表格垂直线
        for (var colID = visualColStartID; colID <= visualColEndID; colID++){
            accWidth += SheetData.GetColWidthInUI((uint)colID);
            dc.DrawLine(guideLinePen,
                new Point(accWidth, 0),
                new Point(accWidth, localBounds.Height));
        }

        // 4、文字
        // 4.1 行表头文字，数字序号，考虑像素级滚动
        accHeight = CalculateRowPositionY(visualRowStartID);
        if (visualRowStartID != -1){
            for (int rowID = visualRowStartID; rowID <= visualRowEndID; rowID++){
                var formattedText = CreateFormattedText($"{rowID + 1}");
                // 计算垂直居中和靠右对齐位置
                double rowHeight = SheetData.GetRowHeightInUI((uint)rowID);
                double textHeight = formattedText.Height;
                double centerY = accHeight + (rowHeight - textHeight) / 2;
                double rightX = UIRowHeaderWidth - formattedText.Width - 5; // 5像素右边距

                dc.DrawText(formattedText, new Point(rightX, centerY));

                accHeight += rowHeight;
            }
        }

        // 4.2 列表头文字，字母序号
        accWidth = UIRowHeaderWidth;
        if (visualColStartID != -1){
            for (int colID = visualColStartID; colID <= visualColEndID; colID++){
                var formattedText = CreateFormattedText($"{ReferenceHelper.ToColumnLetter(colID)}"); // 人类可读+1
                // 计算居中位置
                double colWidth = SheetData.GetColWidthInUI((uint)colID);
                double textWidth = formattedText.Width;
                double centerX = accWidth + (colWidth - textWidth) / 2;

                dc.DrawText(formattedText, new Point(centerX, 0));
                accWidth += SheetData.GetColWidthInUI((uint)colID);
            }
        }

        // 4.3 填充内容
        // 使用计算函数获取正确的起始位置，考虑像素级滚动
        accHeight = CalculateRowPositionY(visualRowStartID);

        // 定义单元格内容区域（排除行头和列头）
        var contentAreaRect = new Rect(
            UIRowHeaderWidth,
            GridCanvasX.UIColumnHeaderHeight,
            localBounds.Width - UIRowHeaderWidth,
            localBounds.Height - GridCanvasX.UIColumnHeaderHeight);

        // 为单元格内容创建裁剪区域，防止内容溢出到行头和列头
        using (var contentClip = dc.PushClip(contentAreaRect)){
            if (visualRowStartID != -1 && visualColStartID != -1 && visualRowEndID != -1 && visualColEndID != -1){
                for (int rowID = visualRowStartID; rowID <= visualRowEndID; rowID++){
                    var curRowHeight = SheetData.GetRowHeightInUI((uint)rowID);

                    // 如果行位置小于列头高度，就跳过这一行的渲染
                    if (accHeight + curRowHeight <= GridCanvasX.UIColumnHeaderHeight)
                        continue;

                    accWidth = UIRowHeaderWidth;
                    for (int colID = visualColStartID; colID <= visualColEndID; colID++){
                        var curColWidth = SheetData.GetColWidthInUI((uint)colID);

                        // 计算单元格的可见区域
                        var cellRect = new Rect(
                            accWidth,
                            accHeight,
                            curColWidth,
                            curRowHeight);

                        var cellValue = SheetData.GetCellText(rowID, colID);
                        if (string.IsNullOrEmpty(cellValue)){
                            // 性能优化：不要渲染空的内容
                        }
                        else if (rowID == clickCellRowID &&
                                 colID == clickCellColID &&
                                 editorTextBox.IsVisible){
                            // 进入编辑模式不要渲染，否则文字重叠
                        }
                        else{
                            // 使用剪裁确保只渲染单元格可见部分
                            // 这里的cellRect是相对于整个画布的，我们需要确保它不会渲染到列头区域上方
                            using var cellClip = dc.PushClip(cellRect);
                            var textLayout = new TextLayout(
                                cellValue
                                , _typeface
                                , 12
                                , Brushes.Black
                                , TextAlignment.Left
                                , TextWrapping.Wrap,
                                maxWidth: curColWidth,
                                maxHeight: curRowHeight);

                            // 在单元格内绘制文本
                            textLayout.Draw(dc, new Point(
                                accWidth,
                                accHeight));
                        }

                        accWidth += curColWidth;
                    }

                    // Console.WriteLine($"rowID={rowID} height={SheetData.GetRowHeightInExcel((uint)rowID)}");
                    accHeight += curRowHeight; // 累加行高
                }
            }
        }

        // 画行的移动横线，和表头列头无关
        if (isRowSplitterMoving){
            dc.DrawLine(splitterMovingLinePen,
                new Point(0, cursorPoint.Y),
                new Point(localBounds.Width, cursorPoint.Y));
        }

        if (isColSplitterMoving){
            dc.DrawLine(splitterMovingLinePen,
                new Point(cursorPoint.X, 0),
                new Point(cursorPoint.X, localBounds.Height));
        }

        // end drawing the world 

        // this is prime time to draw gui stuff 

        //context.DrawLine(_pen, _cursorPoint + new Vector(-200, 0), _cursorPoint + new Vector(20, 0));

        // 这里就等价于画列宽线拖拉效果
        // dc.DrawLine(_pen, _cursorPoint + new Vector(0, -200), _cursorPoint + new Vector(0, 200));

        clip.Dispose();

        // oh and draw again when you can, no rush, right? 
        // 要么不间断的绘画，很消耗资源；要么每个操作后自己InvalidateXXX()
        // Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Background);
    }

    private static FormattedText CreateFormattedText(string textToFormat, double size = 12){
        return new FormattedText(textToFormat,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            size,
            Brushes.Black);
    }
}