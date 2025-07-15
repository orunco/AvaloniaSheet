using System;

namespace AvaloniaSheetControl;

public static class ReferenceHelper{
    // columnID从0开始计数，workbook从1开始计数，我们采用输入0->A 
    public static string ToColumnLetter(int columnID){
        int readableID = columnID + 1;
        // 用不了stringbuilder，是逆向生成的，代价也不大
        string columnName = string.Empty;

        while (readableID > 0){
            int modulo = (readableID - 1) % 26;
            columnName = Convert.ToChar('A' + modulo) + columnName;
            readableID = (readableID - modulo) / 26;
        }

        return columnName;
    }
}