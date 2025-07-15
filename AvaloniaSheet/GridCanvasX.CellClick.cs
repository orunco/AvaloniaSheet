using AvaloniaSheetControl.Porting;

namespace AvaloniaSheetControl;

public partial class GridCanvasX{
    private void UpdateHighlightRange(){
        if (clickCellRowID == -1 ||
            clickCellColID == -1){
            singleCellSelectionBorder.IsVisible = false;
            return;
        }

        singleCellSelectionBorder.IsVisible = true;
        singleCellSelectionBorder.Width = SheetData.GetColWidthInUI((uint)clickCellColID);
        singleCellSelectionBorder.Height = SheetData.GetRowHeightInUI((uint)clickCellRowID);

        // 调整位置以考虑像素级滚动
        double topPosition = CalculateRowPositionY(clickCellRowID);

        // 设置边框位置，考虑像素级滚动
        CanvasX.SetTop(singleCellSelectionBorder, topPosition);
        CanvasX.SetLeft(singleCellSelectionBorder, singleCellSelectionBorderLeft);

        log.Debug($"Updating highlight at row={clickCellRowID}, position={topPosition}");
    }

    // private bool isInEditMode = false;
    private void UpdateEditorTextBox(){
        if (clickCellRowID == -1 ||
            clickCellColID == -1){
            editorTextBox.IsVisible = false;
            return;
        }

        editorTextBox.IsVisible = true;
        editorTextBox.ZIndex = 1;
        editorTextBox.Width = SheetData.GetColWidthInUI((uint)clickCellColID);
        editorTextBox.Height = SheetData.GetRowHeightInUI((uint)clickCellRowID);
        editorTextBox.Text = SheetData.GetCellText(
            clickCellRowID,
            clickCellColID);

        // 调整位置以考虑像素级滚动
        double topPosition = CalculateRowPositionY(clickCellRowID);

        // 设置文本框位置，考虑像素级滚动
        CanvasX.SetTop(editorTextBox, topPosition);
        CanvasX.SetLeft(editorTextBox, singleCellSelectionBorderLeft);

        // isInEditMode = true;
    }

    private void UpdateClearHighlightRange(){
        clickCellRowID = -1;
        clickCellColID = -1;
        singleCellSelectionBorder.IsVisible = false;
        editorTextBox.IsVisible = false;
        editorTextBox.Text = "";
        this.Focus();
    }
}