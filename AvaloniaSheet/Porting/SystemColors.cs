using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace AvaloniaSheetControl.Porting;

public static class SystemColors{
    public static Brush ControlDarkBrush = new SolidColorBrush(
        new Color(0XFF, 0xA0, 0xA0, 0xA0));

    public static Brush ControlBrush = new SolidColorBrush(
        new Color(0XFF, 0xF0, 0xF0, 0xF0));

    public static Brush ControlDarkDarkBrush = new SolidColorBrush(
        new Color(0XFF, 0x69, 0x69, 0x69));

    public static Brush PaleBlueColorBrush = new SolidColorBrush(
        new Color(0XFF, 153, 204, 255));
     
    public static IImmutableSolidColorBrush HighlightColor = new ImmutableSolidColorBrush(
        Color.FromArgb(0XFF, 0x00, 0x78, 0xd7));

    public static ImmutableSolidColorBrush WindowColor = new ImmutableSolidColorBrush(
        Color.FromArgb(0XFF, 0xFF, 0xFF, 0xFF));

    public static ImmutableSolidColorBrush WindowTextColor = new ImmutableSolidColorBrush(
        Color.FromArgb(0XFF, 0x00, 0x00, 0x00));

    public static ImmutableSolidColorBrush ControlColor = new ImmutableSolidColorBrush(
        Color.FromArgb(0XFF, 0xF0, 0xF0, 0xF0));

    public static ImmutableSolidColorBrush ControlLightColor = new ImmutableSolidColorBrush(
        Color.FromArgb(0XFF, 0xE3, 0xE3, 0xE3));

    public static ImmutableSolidColorBrush ControlDarkColor = new ImmutableSolidColorBrush(
        Color.FromArgb(0XFF, 0xA0, 0xA0, 0xA0));
}