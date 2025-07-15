namespace AvaloniaSheetControl;

public interface ISheetData{
    string? SheetName{ get; set; }
    string? GetCellText(int cellRowId, int cellColId);
    int GetValidRowCount();
    double GetRowHeightInUI(uint u);
    void SetRowHeightInUI(uint rowId, float newHeight);
    int GetValidColCount();
    double GetColWidthInUI(uint u);
    void SetColWidthInUI(uint colId, float newWidth);
}