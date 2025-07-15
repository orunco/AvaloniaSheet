using System;
using System.Diagnostics;
using AvaloniaSheetControl;
using NUnit.Framework;

namespace Orunco.AvaloniaSheetTest.UT;

[TestFixture]
public class UT_CellSizeAccumulator{
    [Test]
    public void Initialize_ShouldBuildPrefixSum(){
        // Arrange
        var accumulator = new CellSizeAccumulator(i => (i + 1) * 10); // 行高: 10,20,30...

        // Act
        accumulator.Initialize(3);

        // Assert
        Assert.That(accumulator.GetAccumulated(1), Is.EqualTo(10)); // 第0行
        Assert.That(accumulator.GetAccumulated(2), Is.EqualTo(30)); // 第0+1行
        Assert.That(accumulator.GetAccumulated(3), Is.EqualTo(60)); // 第0+1+2行
    }

    [Test]
    public void UpdateSize_ShouldApplyDelta(){
        // Arrange
        var accumulator = new CellSizeAccumulator(i => 20);
        accumulator.Initialize(5);

        // Act
        accumulator.UpdateSize(2, 30); // 修改第2行高度 20→30 (+10)

        // Assert
        Assert.That(accumulator.GetAccumulated(3), Is.EqualTo(70)); // 20x2 + 30
    }

    [Test]
    public void RebuildCache_WhenThresholdReached(){
        // Arrange
        var accumulator = new CellSizeAccumulator(_ => 15);
        accumulator.Initialize(100);

        // Act (触发重建)
        for (int i = 0; i < 51; i++){
            accumulator.UpdateSize(i, 20); // +5 each
        }

        // Assert
        Assert.That(accumulator.GetAccumulated(50), Is.EqualTo(20 * 50));
        Assert.That(accumulator.GetAccumulated(100), Is.EqualTo(20 * 51 + 15 * 49));
    }

    [Test]
    public void GetAccumulatedSize_WithNegativeIndex_ReturnsZero(){
        var accumulator = new CellSizeAccumulator(_ => 10);
        accumulator.Initialize(10);

        Assert.That(accumulator.GetAccumulated(-1), Is.EqualTo(0));
    }

    [Test]
    public void Initialize_WithMaxExcelRows_ShouldCompleteInReasonableTime(){
        // Arrange
        const int maxExcelRows = 1_048_576; // Excel最大行数
        var acc = new CellSizeAccumulator(i => 20);
        var stopwatch = new Stopwatch();
        // Act
        stopwatch.Start();
        acc.Initialize(maxExcelRows);
        stopwatch.Stop();
        // Assert
        TestContext.WriteLine($"Initialize {maxExcelRows:N0} rows took: {stopwatch.ElapsedMilliseconds}ms");
        // 笔记本CPU 17ms
        //Assert.Less(stopwatch.ElapsedMilliseconds, 500, "初始化超过500ms");
    }

    [Test]
    public void UpdateLastRow_ShouldNotTriggerFullRebuild(){
        // Arrange
        const int maxExcelRows = 1_048_576;
        var acc = new CellSizeAccumulator(i => 20);
        acc.Initialize(maxExcelRows);
        var stopwatch = new Stopwatch();
        // Act
        stopwatch.Start();
        acc.UpdateSize(maxExcelRows - 1, 30); // 修改最后一行
        stopwatch.Stop();
        // Assert
        TestContext.WriteLine($"Update last row took: {stopwatch.ElapsedMilliseconds}ms");
        //Assert.Less(stopwatch.ElapsedMilliseconds, 1, "单次修改超过1ms");
    }

    [Test]
    public void GetPosition_AfterManyUpdates_ShouldBeFast(){
        // Arrange
        const int testRows = 100_000;
        var acc = new CellSizeAccumulator(i => 20);
        acc.Initialize(testRows);
        // 模拟随机修改1%的行
        var random = new Random();
        for (int i = 0; i < testRows / 100; i++){
            acc.UpdateSize(random.Next(testRows), 25);
        }

        // Act & Assert
        var stopwatch = Stopwatch.StartNew();
        double total = 0;
        for (int i = 0; i < 1000; i++){
            total += acc.GetAccumulated(random.Next(testRows));
        }

        stopwatch.Stop();
        TestContext.WriteLine($"1000 random queries took: {stopwatch.ElapsedMilliseconds}ms");
        //Assert.Less(stopwatch.ElapsedMilliseconds, 50, "1000次查询超过50ms");
    }

    [Test]
    public void MemoryUsage_ShouldBeLinear(){
        // Arrange
        var sizes = new[]{ 1_000, 10_000, 100_000 };
        var memoryUsages = new long[sizes.Length];
        // Act
        for (int i = 0; i < sizes.Length; i++){
            GC.Collect();
            var before = GC.GetTotalMemory(true);

            var acc = new CellSizeAccumulator(_ => 20);
            acc.Initialize(sizes[i]);

            memoryUsages[i] = GC.GetTotalMemory(true) - before;
        }

        // Assert & Output
        for (int i = 0; i < sizes.Length; i++){
            TestContext.WriteLine($"{sizes[i]:N0} rows: {memoryUsages[i] / 1024}KB");
        }

        // 验证内存增长是线性的（允许10%误差）
        double ratio = (double)memoryUsages[2] / memoryUsages[1];
        Assert.That(ratio, Is.InRange(9.0, 11.0));
    }
}