using System;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;


namespace AvaloniaSheetControl;

[TemplatePart(Name = PART_GridCanvas, Type = typeof(GridCanvasX))]
[TemplatePart(Name = PART_SheetName, Type = typeof(TextBox))]
[TemplatePart(Name = PART_VerScrollbar, Type = typeof(ScrollBar))]
[TemplatePart(Name = PART_HorScrollbar, Type = typeof(ScrollBar))]
public class AavaloniaSheet : TemplatedControl{
    private const string PART_GridCanvas = "PART_GridCanvas";
    private const string PART_SheetName = "PART_SheetName";
    private const string PART_VerScrollbar = "PART_VerScrollbar";
    private const string PART_HorScrollbar = "PART_HorScrollbar";

    public ISheetData SheetData;
    public int HeadRowID; 

    private GridCanvasX? gridCanvas;
    private TextBox? sheetNameTextBox;
    private ScrollBar? verScrollbar;
    private ScrollBar? horScrollbar;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e){
        base.OnApplyTemplate(e);

        gridCanvas = e.NameScope.Find<GridCanvasX>(PART_GridCanvas);
        sheetNameTextBox = e.NameScope.Find<TextBox>(PART_SheetName);
        verScrollbar = e.NameScope.Find<ScrollBar>(PART_VerScrollbar);
        horScrollbar = e.NameScope.Find<ScrollBar>(PART_HorScrollbar);

        if (gridCanvas == null ||
            verScrollbar == null ||
            horScrollbar == null ||
            sheetNameTextBox == null){
            throw new Exception("gridCanvas == null || verScrollbar == null || horScrollbar == null");
        }

        gridCanvas.SheetData = SheetData;
        gridCanvas.HeadRowID = HeadRowID;
        
        sheetNameTextBox.Text = SheetData.SheetName;

        horScrollbar.Scroll += (sender, args) => { gridCanvas.OnHorizontalScrollBarScroll(args.NewValue); };

        verScrollbar.Scroll += (sender, args) => {
            Console.WriteLine($"OnVerticalScrollBarScroll() {args.NewValue}");
            gridCanvas.OnVerticalScrollBarScroll(args.NewValue);
        };

        this.Loaded += (sender, args) => { gridCanvas.AfterLoad(); };
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e){
        
        this.Focus();
        Console.WriteLine($"OnPointerCaptureLost() {e}");
    }
}