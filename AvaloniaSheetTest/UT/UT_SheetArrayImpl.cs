using NUnit.Framework;

namespace Orunco.AvaloniaSheetTest.UT;

[TestFixture]
public class UT_SheetArrayImpl{
    private const int TestRows = 100;
    private const int TestCols = 26;
    private SheetDataArrayImpl _sheetData;

    [SetUp]
    public void Setup(){
        _sheetData = new SheetDataArrayImpl(TestRows, TestCols);
    }

    [Test]
    public void SetAndGetCellText(){
        _sheetData.SetCellText(5, 5, "TestValue");
        Assert.That(_sheetData.GetCellText(5, 5), Is.EqualTo("TestValue"));
    }

    [Test]
    public void OutOfBounds_ReturnsEmpty(){
        Assert.That(_sheetData.GetCellText(TestRows, 0), Is.EqualTo(string.Empty));
        Assert.That(_sheetData.GetCellText(0, TestCols), Is.EqualTo(string.Empty));
    }

    [Test]
    public void OutOfBounds_Set_NoEffect(){
        _sheetData.SetCellText(TestRows, 0, "ShouldNotSet");
        Assert.That(_sheetData.GetCellText(TestRows, 0), Is.EqualTo(string.Empty));
    }

    [Test]
    public void RowHeightManagement(){
        uint rowId = 5;
        float newHeight = 100f;

        _sheetData.SetRowHeightInUI(rowId, newHeight);
        Assert.That(_sheetData.GetRowHeightInUI(rowId), Is.EqualTo(newHeight));

        // 测试边界行高
        Assert.That(_sheetData.GetRowHeightInUI((uint)TestRows), Is.EqualTo(0));
    }

    [Test]
    public void ColWidthManagement(){
        uint colId = 3;
        float newWidth = 150f;

        _sheetData.SetColWidthInUI(colId, newWidth);
        Assert.That(_sheetData.GetColWidthInUI(colId), Is.EqualTo(newWidth));

        // 测试边界列宽
        Assert.That(_sheetData.GetColWidthInUI((uint)TestCols), Is.EqualTo(0));
    }

    [Test]
    public void ValidDimensions(){
        Assert.That(_sheetData.GetValidRowCount(), Is.EqualTo(TestRows));
        Assert.That(_sheetData.GetValidColCount(), Is.EqualTo(TestCols));
    }

    [Test]
    public void DefaultHeightsAndWidths(){
        for (int i = 0; i < TestRows; i++){
            Assert.That(_sheetData.GetRowHeightInUI((uint)i),
                Is.GreaterThan(0));
        }

        for (int i = 0; i < TestCols; i++){
            Assert.That(_sheetData.GetColWidthInUI((uint)i),
                Is.GreaterThan(0));
        }
    }
}