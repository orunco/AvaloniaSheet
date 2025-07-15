using Avalonia.Controls;

namespace Orunco.AvaloniaSheetTest;

public partial class MainWindow : Window{
    public MainWindow(){
        InitializeComponent();

        // InitializeWithArrayImpl();
        InitializeWithPageImpl();
    }

    private void InitializeWithArrayImpl(){
        // 初始化Sheet数据
        var sheet = new SheetDataArrayImpl(100, 26);

        // 填充测试数据
        for (int row = 0; row < 50; row++){
            for (int col = 0; col < 10; col++){
                sheet.SetCellText(row, col, $"R{row + 1}C{col + 1}");
            }
        }

        // 设置标题行
        for (int col = 0; col < 10; col++){
            sheet.SetCellText(0, col, $"标题{col + 1}");
        }

        // 设置长文本
        sheet.SetCellText(5, 5,
            "数组实现的长文本测试\n" +
            "当文本内容超过单元格宽度时，应该自动换行显示\n" +
            "这是第二行文本，继续测试换行效果\n" +
            "第三行文本，看看效果如何。");
        sheet.SetRowHeightInUI(5, 150);
        sheet.SetColWidthInUI(5, 150);

        // 设置控件属性
        myAavaloniaSheet.HeadRowID = 0;
        myAavaloniaSheet.SheetData = sheet;
    }

    private void InitializeWithPageImpl(){
        var sheet = new SheetDataPageImpl();
        var colmax = 50;

        // 填充测试数据
        for (int row = 0; row < 5000; row++){
            for (int col = 0; col < colmax; col++){
                sheet.SetCellText(row, col, $"R{row + 1}C{col + 1}");
            }
        }

        // 设置标题行
        myAavaloniaSheet.HeadRowID = 0;
        for (int col = 0; col < colmax; col++){
            sheet.SetCellText(0, col, $"标题{col + 1}");
        }

        // 设置长文本
        sheet.SetCellText(5, 5,
            "分页实现的长文本测试\n" +
            "这种实现适合处理Excel级别的海量数据\n" +
            "使用分块存储优化内存使用\n" +
            "右侧区域使用字典存储稀疏数据");
        sheet.SetRowHeightInUI(5, 150);
        sheet.SetColWidthInUI(5, 150);

        // 设置特殊位置
        sheet.SetCellText(
            SheetDataPageImpl.MaxRowCount - 1, colmax,
            "hi-1048576");

        // 挂载
        myAavaloniaSheet.SheetData = sheet;
    }
}