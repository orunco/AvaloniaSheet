using System;
using System.Collections.Generic;
using System.Linq;

namespace AvaloniaSheetControl;

/*
为什么要实现这个？因为单元格的高度和宽度因为布局，有累加性，原来的实现：
        // 计算到指定行顶部的累计高度
        for (int i = 0; i < rowId; i++){
            position += SheetData.GetRowHeightInUI((uint)i);
        }
而这种计算因为实时渲染，无时无刻存在，很不友好，因此空间换时间：
单元格尺寸累计计算器（统一处理行高/列宽）
特性：
1. 初始化构建前缀和数组（O(n)）
2. 修改时记录差值（O(1)）
3. 查询时动态合并差值（O(k), k=未合并的修改数）
4. 修改积累到阈值后自动重建缓存（O(n)）

初始化时一次性完成计算，笔记本CPU大概17ms(1048576)，一帧的时间
单次修改接口：单词微调只需要简单记录，如果累积到一定次数，就重算
查询位置

| 场景                | 传统方案          | 本方案               |
|---------------------|-------------------|----------------------|
| **初始化**          | 不预处理          | O(n)构建前缀和       |
| **单次行高修改**    | 全量重算(O(n))    | 记录差值(O(1))       |
| **频繁位置计算**    | 每次O(n)累加      | 大部分情况O(k)(k=修改行数) |
| **内存占用**        | 无额外开销        | 额外O(n)+O(k)空间    |

核心优势：将行高变化的成本从每次修改时的O(n)分摊到多次查询时的O(k)
 */

public sealed class CellSizeAccumulator{
    private readonly Func<int, double> _getSizeFunc; // 获取原始尺寸的方法
    private double[] _prefixSum; // 前缀和数组（初始累计值）
    private readonly Dictionary<int, double> _sizeDeltas = new(); // 未合并的尺寸修改
    private bool _requiresRebuild; // 是否需要重建缓存
    private const int RebuildThreshold = 50; // 触发重建的修改次数阈值

    public CellSizeAccumulator(Func<int, double> getSizeFunc){
        _getSizeFunc = getSizeFunc ?? throw new ArgumentNullException(nameof(getSizeFunc));
    }

    public void Initialize(int totalCount){
        _prefixSum = new double[totalCount];
        double sum = 0;
        for (int i = 0; i < totalCount; i++){
            sum += _getSizeFunc(i);
            _prefixSum[i] = sum;
        }

        _sizeDeltas.Clear();
        _requiresRebuild = false;
    }

    public void UpdateSize(int index, double newSize){
        double delta = newSize - _getSizeFunc(index);
        _sizeDeltas[index] = _sizeDeltas.TryGetValue(index, out var existing) ? existing + delta : delta;
        _requiresRebuild = _sizeDeltas.Count >= RebuildThreshold;
    }

    public double GetAccumulated(int endIndex){
        if (endIndex < 0) return 0;
        if (_requiresRebuild) RebuildCache();

        double baseSum = endIndex == 0 ? 0 : _prefixSum[endIndex - 1];

        foreach (var kv in _sizeDeltas.Where(x => x.Key < endIndex)){
            baseSum += kv.Value;
        }

        return baseSum;
    }

    private void RebuildCache(){
        foreach (var (index, delta) in _sizeDeltas.OrderBy(x => x.Key)){
            for (int i = index; i < _prefixSum.Length; i++){
                _prefixSum[i] += delta;
            }
        }

        _sizeDeltas.Clear();
        _requiresRebuild = false;
    }
}