using System;
using AvaloniaSheetControl;
using GridCanvasX = AvaloniaSheetControl.GridCanvasX;

namespace Orunco.AvaloniaSheetTest;

public class SheetDataArrayImpl : ISheetData{
    private readonly string?[,] _cells;
    private readonly float[] _rowHeights;
    private readonly float[] _colWidths;
    private string _sheetName = "Sheet-array";

    public SheetDataArrayImpl(int rowCount, int colCount){
        _cells = new string[rowCount, colCount];
        _rowHeights = new float[rowCount];
        _colWidths = new float[colCount];

        // 初始化默认行高和列宽
        for (int i = 0; i < rowCount; i++){
            _rowHeights[i] = (float)GridCanvasX.DefaultRowHeightInExcel *
                             (float)GridCanvasX.RowHeightInExcelToUIFactor;
        }

        for (int i = 0; i < colCount; i++){
            _colWidths[i] = (float)GridCanvasX.DefaultColWidthInExcel *
                            (float)GridCanvasX.ColWidthInExcelToUIFactor;
        }
    }

    public string SheetName{
        get => _sheetName;
        set => _sheetName = value ?? throw new ArgumentNullException(nameof(value));
    }

    public int GetValidRowCount() => _cells.GetLength(0);
    public int GetValidColCount() => _cells.GetLength(1);

    public double GetRowHeightInUI(uint rowId) =>
        rowId < _rowHeights.Length ? _rowHeights[rowId] : 0;

    public void SetRowHeightInUI(uint rowId, float height){
        if (rowId >= _rowHeights.Length)
            throw new ArgumentOutOfRangeException(nameof(rowId));

        _rowHeights[rowId] = height;
    }

    public double GetColWidthInUI(uint colId) =>
        colId < _colWidths.Length ? _colWidths[colId] : 0;

    public void SetColWidthInUI(uint colId, float width){
        if (colId >= _colWidths.Length)
            throw new ArgumentOutOfRangeException(nameof(colId));

        _colWidths[colId] = width;
    }

    public string? GetCellText(int rowId, int colId){
        if (rowId < 0 || rowId >= _cells.GetLength(0) ||
            colId < 0 || colId >= _cells.GetLength(1))
            return string.Empty;

        return _cells[rowId, colId];
    }

    public void SetCellText(int rowId, int colId, string? text){
        if (rowId < 0 || rowId >= _cells.GetLength(0) ||
            colId < 0 || colId >= _cells.GetLength(1))
            return;

        _cells[rowId, colId] = text;
    }
}