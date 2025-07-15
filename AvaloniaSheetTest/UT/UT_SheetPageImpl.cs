using NUnit.Framework;
using System;

namespace Orunco.AvaloniaSheetTest.UT;

[TestFixture]
public class UT_SheetPageImpl{
    private SheetDataPageImpl _sheetData;

    [SetUp]
    public void Setup(){
        _sheetData = new SheetDataPageImpl();
    }

    [Test]
    public void SetAndGetCellText_LeftSide(){
        // 测试左侧分块存储
        _sheetData.SetCellText(10, 10, "TestLeft");
        Assert.That(_sheetData.GetCellText(10, 10), Is.EqualTo("TestLeft"));
    }

    [Test]
    public void SetAndGetCellText_RightSide(){
        // 测试右侧字典存储
        _sheetData.SetCellText(10, 200, "TestRight");
        Assert.That(_sheetData.GetCellText(10, 200), Is.EqualTo("TestRight"));
    }

    [Test]
    public void BoundaryConditions_MaxRowMaxCol(){
        // 测试最大行列边界
        int maxRow = SheetDataPageImpl.MaxRowCount - 1;
        int maxCol = SheetDataPageImpl.MaxColCount - 1;

        _sheetData.SetCellText(maxRow, maxCol, "Corner");
        Assert.That(_sheetData.GetCellText(maxRow, maxCol), Is.EqualTo("Corner"));
    }

    [Test]
    public void InvalidCoordinates_ThrowsException(){
        // 测试无效坐标
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _sheetData.GetCellText(-1, 0));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _sheetData.SetCellText(0, SheetDataPageImpl.MaxColCount, "X"));
    }

    [Test]
    public void RowHeightManagement(){
        // 测试行高设置
        uint rowId = 5;
        float newHeight = 100f;

        _sheetData.SetRowHeightInUI(rowId, newHeight);
        Assert.That(_sheetData.GetRowHeightInUI(rowId), Is.EqualTo(newHeight));

        // 测试默认行高
        Assert.That(_sheetData.GetRowHeightInUI(0),
            Is.EqualTo(SheetDataPageImpl.DefaultRowHeight));
    }

    [Test]
    public void ColWidthManagement(){
        // 测试列宽设置
        uint colId = 3;
        float newWidth = 150f;

        _sheetData.SetColWidthInUI(colId, newWidth);
        Assert.That(_sheetData.GetColWidthInUI(colId), Is.EqualTo(newWidth));

        // 测试默认列宽
        Assert.That(_sheetData.GetColWidthInUI(0),
            Is.EqualTo(SheetDataPageImpl.DefaultColWidth));
    }

    [Test]
    public void ValidRowCountCalculation(){
        // 测试有效行数计算
        _sheetData.SetCellText(100, 0, "Data");
        _sheetData.SetCellText(200, 0, "Data");

        Assert.That(_sheetData.GetValidRowCount(), Is.EqualTo(201));
    }

    [Test]
    public void ValidColCountCalculation(){
        // 测试有效列数计算
        _sheetData.SetCellText(0, 100, "Data");
        _sheetData.SetCellText(0, 200, "Data");

        Assert.That(_sheetData.GetValidColCount(), Is.EqualTo(201));
    }
}