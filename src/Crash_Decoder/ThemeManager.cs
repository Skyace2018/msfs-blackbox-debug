// MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
using Microsoft.Win32;
using System.Windows.Media;

namespace MsfsInputBlackbox.Decoder;

internal static class ThemeManager
{
    private sealed record Palette(string App, string Sidebar, string Surface, string Raised, string Input, string Text, string Muted, string Accent, string Border, string Button);
    private static readonly Palette Dark = new("#0B1017", "#101821", "#121A24", "#182332", "#0F1721", "#F1F6FC", "#B5C5D7", "#52A8FF", "#31506F", "#18324C");
    private static readonly Palette Light = new("#F5F8FC", "#E8F0F8", "#FFFFFF", "#F1F6FB", "#FFFFFF", "#142233", "#496173", "#006DBA", "#A9BED2", "#DCEAF7");

    public static bool IsDark { get; private set; }

    public static void Apply(string? mode)
    {
        IsDark = !string.Equals(mode, "Light", StringComparison.OrdinalIgnoreCase)
            && (string.Equals(mode, "Dark", StringComparison.OrdinalIgnoreCase) || !SystemPrefersLight());
        var palette = IsDark ? Dark : Light;
        Set("AppBackground", palette.App); Set("SidebarBackground", palette.Sidebar); Set("Surface", palette.Surface);
        Set("SurfaceRaised", palette.Raised); Set("InputBackground", palette.Input); Set("Text", palette.Text);
        Set("Muted", palette.Muted); Set("Accent", palette.Accent); Set("Border", palette.Border); Set("ButtonBackground", palette.Button);
    }

    private static bool SystemPrefersLight()
    {
        try { return (Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 0) as int? ?? 0) != 0; }
        catch { return false; }
    }

    private static void Set(string resourceName, string color)
    {
        System.Windows.Application.Current.Resources[resourceName] = new SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));
    }
}

