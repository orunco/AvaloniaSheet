using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AvaloniaSheetControl;
using GridCanvasX = AvaloniaSheetControl.GridCanvasX;

namespace Orunco.AvaloniaSheetTest;

public class SheetDataPageImpl : ISheetData{
    // 分页存储常量
    public const int MaxRowCount = 1048576;
    public const int MaxColCount = 16384;
    private const int ColumnLeftSideSize = 128;
    private const int BlockHeight = 2048;
    private const int BlockWidth = 32;

    // 核心存储结构
    private readonly string?[,][,] _leftSide =
        new string[MaxRowCount / BlockHeight, ColumnLeftSideSize / BlockWidth][,];

    private readonly Dictionary<ulong, string?> _rightSide = new();
    private readonly BitArray _rawRowIsEmptyInfo = new(MaxRowCount);
    private readonly BitArray _rawColIsEmptyInfo = new(MaxColCount);

    // 行高列宽存储
    private readonly Dictionary<uint, float> _rowHeights = new();
    private readonly Dictionary<uint, float> _colWidths = new();
    private string _sheetName = "Sheet-Page";

    // 行高列宽默认值
    public static readonly double DefaultRowHeight =
        GridCanvasX.DefaultRowHeightInExcel *
        GridCanvasX.RowHeightInExcelToUIFactor;

    public static readonly double DefaultColWidth =
        GridCanvasX.DefaultColWidthInExcel *
        GridCanvasX.ColWidthInExcelToUIFactor;

    public string SheetName{
        get => _sheetName;
        set => _sheetName = value ?? throw new ArgumentNullException(nameof(value));
    }

    // 组合行号列号为唯一键
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Combine14(uint rowID, ushort colID){
        return (ulong)rowID << 14 | colID;
    }

    // 获取单元格文本
    public string? GetCellText(int rowID, int colID){
        ArgumentOutOfRangeException.ThrowIfLessThan(rowID, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(colID, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(rowID, MaxRowCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(colID, MaxColCount);

        if (colID < ColumnLeftSideSize){
            var blockrowID = rowID / BlockHeight;
            var blockcolID = colID / BlockWidth;

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            return _leftSide[blockrowID, blockcolID] == null
                ? string.Empty
                : _leftSide[blockrowID, blockcolID][rowID % BlockHeight, colID % BlockWidth];
        }

        return _rightSide.GetValueOrDefault(
            Combine14((uint)rowID, (ushort)colID));
    }

    // 设置单元格文本
    public void SetCellText(int rowID, int colID, string text){
        ArgumentOutOfRangeException.ThrowIfLessThan(rowID, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(colID, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(rowID, MaxRowCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(colID, MaxColCount);

        // 左侧分块存储
        if (colID < ColumnLeftSideSize){
            var blockrowID = rowID / BlockHeight;
            var blockcolID = colID / BlockWidth;

            // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
            _leftSide[blockrowID, blockcolID] ??= new string[BlockHeight, BlockWidth];

            var blockInnerrowID = rowID % BlockHeight;
            var blockInnercolID = colID % BlockWidth;

            _leftSide[blockrowID, blockcolID][blockInnerrowID, blockInnercolID] = text;
            _rawRowIsEmptyInfo.Set(rowID, true);
            _rawColIsEmptyInfo.Set(colID, true);
        }
        // 右侧字典存储
        else{
            if (_rightSide.TryAdd(Combine14((uint)rowID, (ushort)colID), text)){
                _rawRowIsEmptyInfo.Set(rowID, true);
                _rawColIsEmptyInfo.Set(colID, true);
            }
            else{
                throw new NotSupportedException();
            }
        }

        // 标记行列非空
        _rawRowIsEmptyInfo.Set(rowID, true);
        _rawColIsEmptyInfo.Set(colID, true);
    }

    // 获取有效行数
    public int GetValidRowCount(){
        for (int i = MaxRowCount - 1; i >= 0; i--)
            if (_rawRowIsEmptyInfo[i])
                return i + 1;
        return 0;
    }

    // 获取有效列数
    public int GetValidColCount(){
        for (int i = MaxColCount - 1; i >= 0; i--)
            if (_rawColIsEmptyInfo[i])
                return i + 1;
        return 0;
    }

    // 行高管理
    public double GetRowHeightInUI(uint rowID) =>
        _rowHeights.TryGetValue(rowID, out var height) ? height : DefaultRowHeight;

    public void SetRowHeightInUI(uint rowID, float height) =>
        _rowHeights[rowID] = height;

    // 列宽管理
    public double GetColWidthInUI(uint colID) =>
        _colWidths.TryGetValue(colID, out var width) ? width : DefaultColWidth;

    public void SetColWidthInUI(uint colID, float width) =>
        _colWidths[colID] = width;
    
}