using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace WinClip.UI.Styles;

/// <summary>
/// Drives WinClip's own light/dark resource dictionaries based on the user's
/// <see cref="PopupThemeKind"/> setting (System / Light / Dark). Pre-v2.1 this
/// only followed Windows mode, which broke when the user picked Light explicitly
/// while running in dark Windows — Settings would still render dark.
/// </summary>
public static class AppThemeManager
{
    private static readonly Uri DarkThemeUri = new("UI/Styles/ModernTheme.Dark.xaml", UriKind.Relative);
    private static Application? _app;
    private static ResourceDictionary? _darkDictionary;
    private static PopupThemeKind _currentKind = PopupThemeKind.System;

    /// <summary>
    /// One-time setup. Subscribes to Windows preference changes so that when the
    /// user has chosen System mode, switching the Windows theme repaints WinClip.
    /// </summary>
    public static void Initialize(Application app)
    {
        if (_app != null) return;
        _app = app;
        SystemEvents.UserPreferenceChanged += (_, args) =>
        {
            if (args.Category is not (UserPreferenceCategory.General or UserPreferenceCategory.Color))
                return;
            // Only re-apply automatically when the user picked System; otherwise
            // their explicit choice should win even if Windows mode flips.
            if (_currentKind == PopupThemeKind.System)
                app.Dispatcher.BeginInvoke(() => Apply(_currentKind));
        };
    }

    /// <summary>
    /// Applies the resource dictionary that matches <paramref name="kind"/>.
    /// System resolves via <see cref="ThemeDetector.IsDarkMode"/>; Light forces
    /// the light palette; Dark (and any legacy enum value) forces dark.
    /// </summary>
    public static void Apply(PopupThemeKind kind)
    {
        _currentKind = kind;
        if (_app == null) return;

        var isDark = kind switch
        {
            PopupThemeKind.Light => false,
            PopupThemeKind.System => ThemeDetector.IsDarkMode(),
            _ => true, // Dark + all legacy named themes
        };

        var merged = _app.Resources.MergedDictionaries;
        if (isDark)
        {
            _darkDictionary ??= new ResourceDictionary { Source = DarkThemeUri };
            if (!merged.Any(d => d.Source == DarkThemeUri))
                merged.Add(_darkDictionary);
        }
        else
        {
            var existing = merged.FirstOrDefault(d => d.Source == DarkThemeUri);
            if (existing != null) merged.Remove(existing);
        }
    }
}
