
# **Avalonia Sheet**

**A lightweight, Excel-like grid control for Avalonia UI with virtualized rendering and customizable data sources** ✨

![Avalonia Sheet](./preview.gif)

---

## **Features**

- ⭐️ **Excel-like Grid Interface** – Spreadsheet UI with cells, rows, and columns
- ⭐️ **High Performance Rendering** – Optimized for large datasets with virtualized rendering (Supports **1,048,576 × 16,384** cells)
- ⭐️ **Memory-Efficient Design** – Leverages C#'s memory-efficient data structures for massive datasets (1M+ rows demo)
- ⭐️ **Customizable Data Source** – Implement `ISheetData` interface for your data backend
- **Row/Column Resizing** – Interactive resizing of rows and columns with mouse
- **Smooth Scrolling** – Vertical and horizontal scrolling with scrollbar synchronization
- **Cell Selection** – Visual highlighting of selected cells
- **In-place Editing** – Double-click cells to edit content
- **Header Customization** – Customizable row and column headers
- **Pixel-perfect Layout** – Precise measurement and alignment of all elements
- **Theming Support** – Styled using Avalonia's Fluent theme system
- **Two Data Implementations Included**:
  - `SheetDataArrayImpl` – Simple array-based storage
  - `SheetDataPageImpl` – Advanced paged memory model for large datasets
- **Extensible Architecture** – Easy to add new features via inheritance or plugin interfaces

## **Getting Started**

```bash
git clone https://github.com/orunco/AvaloniaSheet.git
cd AvaloniaSheet
Open the project in your preferred IDE
Run `AvaloniaSheetTest`
```

## **Requirements**

- .NET 6+
- Avalonia 11+
- Zero additional dependencies - Only requires Avalonia, no other external libraries

### **Why Avalonia Sheet?**

When evaluating UI frameworks for our project, we needed a high-performance spreadsheet control capable of handling extreme data scales (**1,048,576 × 16,384 cells**). While existing solutions like Avalonia's DataGrid provide general-purpose functionality, we required a specialized component optimized for memory efficiency and smooth rendering.

**Key Motivations:**

1. **Performance at Scale**
   Web-based solutions struggle with datasets of this magnitude, whereas C# enables fine-grained memory control. Our implementation demonstrates that Avalonia can efficiently render massive grids when optimized correctly.

2. **Engineering Passion**
   Building a high-performance grid from scratch was both a challenge and an opportunity to push Avalonia's capabilities further.

3. **Inspiration from Pioneers**
   We drew inspiration from early innovations like the [WCMFC Library](https://www.willcoxson.net/wcmfclib.htm), Oh, 1996, adapting their concepts for modern Avalonia applications.

## **License**

**MIT** © Pete Zhang
