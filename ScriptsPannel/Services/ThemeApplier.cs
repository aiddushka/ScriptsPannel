using System.Windows;
using ScriptsPannel.Models;

namespace ScriptsPannel.Services;

public static class ThemeApplier
{
    /// <summary>Maps legacy <see cref="UiThemeKind.CustomBackground"/> to dark tokens.</summary>
    public static UiThemeKind ToLightDark(UiThemeKind theme) =>
        theme == UiThemeKind.Light ? UiThemeKind.Light : UiThemeKind.Dark;

    public static void ApplyLanguage(string? lang)
    {
        var code = string.IsNullOrWhiteSpace(lang) ? "ru" : lang.Trim().ToLowerInvariant();
        var shortLang = code.StartsWith("en", StringComparison.Ordinal) ? "en" : "ru";
        var app = System.Windows.Application.Current;
        var md = app.Resources.MergedDictionaries;
        for (var i = md.Count - 1; i >= 0; i--)
        {
            var s = md[i].Source?.OriginalString;
            if (s != null && s.Contains("Lang/Strings.", StringComparison.OrdinalIgnoreCase))
                md.RemoveAt(i);
        }
        md.Add(new ResourceDictionary
        {
            Source = new Uri($"/Lang/Strings.{shortLang}.xaml", UriKind.Relative)
        });
    }

    public static void ApplyThemeResources(UiThemeKind theme)
    {
        var app = System.Windows.Application.Current;
        var md = app.Resources.MergedDictionaries;
        for (var i = md.Count - 1; i >= 0; i--)
        {
            var s = md[i].Source?.OriginalString;
            if (s != null && s.Contains("Themes/Theme.", StringComparison.OrdinalIgnoreCase))
                md.RemoveAt(i);
        }
        var tail = ToLightDark(theme) == UiThemeKind.Light ? "Light" : "Dark";
        md.Insert(0, new ResourceDictionary
        {
            Source = new Uri($"/Themes/Theme.{tail}.xaml", UriKind.Relative)
        });
    }
}
