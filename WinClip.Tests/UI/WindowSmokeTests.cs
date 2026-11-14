using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Moq;
using WinClip.Actions;
using WinClip.Core.Services;
using WinClip.Services;
using WinClip.UI;

namespace WinClip.Tests.UI;

[CollectionDefinition("WpfWindows", DisableParallelization = true)]
public class WpfWindowsCollection { }

[Collection("WpfWindows")]
public class WindowSmokeTests
{
    [Fact]
    public async Task SettingsRendersBothThemesAndSearchNeverIndexesUserInputs()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            Application? app = null;
            try
            {
                app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                app.Resources = (ResourceDictionary)XamlReader.Parse("""
                    <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                        xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml">
                        <ResourceDictionary.MergedDictionaries>
                            <ui:ThemesDictionary Theme="Light"/>
                            <ui:ControlsDictionary/>
                            <ResourceDictionary Source="/WinClip;component/UI/Styles/ModernTheme.xaml"/>
                        </ResourceDictionary.MergedDictionaries>
                    </ResourceDictionary>
                    """);
                var action = new Mock<IAction>();
                action.SetupGet(x => x.Name).Returns("Hash SHA256");
                var registry = new Mock<IActionRegistry>();
                registry.Setup(x => x.GetAllActions()).Returns(new[] { action.Object });
                var window = new SettingsWindow(new SettingsManager(), registry.Object);
                try
                {
                    var search = (TextBox)window.FindName("SettingsSearch");
                    var sections = (StackPanel)window.FindName("SettingsSections");
                    var key = (TextBox)window.FindName("AIApiKeyInput");
                    key.Text = "unique-input-must-not-be-indexed";
                    search.Text = key.Text;
                    Assert.Empty(sections.Children.OfType<FrameworkElement>().Where(x => x.Visibility == Visibility.Visible));
                    search.Text = "SHA256";
                    var matched = Assert.Single(sections.Children.OfType<FrameworkElement>().Where(x => x.Visibility == Visibility.Visible));
                    Assert.Equal("Enabled Actions", matched.Tag);
                    search.Text = "";
                    Assert.Equal(7, sections.Children.Count);
                    Assert.All(sections.Children.OfType<FrameworkElement>(), x => Assert.Equal(Visibility.Visible, x.Visibility));

                    RenderContent(window);
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary
                    {
                        Source = new Uri("/WinClip;component/UI/Styles/ModernTheme.Dark.xaml", UriKind.Relative)
                    });
                    RenderContent(window);
                    Assert.Equal(Color.FromRgb(0x94, 0xA3, 0xB8), ((SolidColorBrush)window.FindResource("TextMutedBrush")).Color);
                }
                finally { window.Close(); }
                CheckHistoryCopyFailure();
                completion.SetResult();
            }
            catch (Exception ex) { completion.SetException(ex); }
            finally
            {
                app?.Shutdown();
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(30));
    }

    private static void CheckHistoryCopyFailure()
    {
        var history = new ClipboardHistory();
        history.Add("selected clip");
        var window = new PopupWindow(PopupThemeKind.Light)
        {
            ClipboardHistorySource = history,
            CopyHistoryText = _ => false,
        };
        var border = new Border();
        var flyout = new System.Windows.Controls.Primitives.Popup { Child = border, IsOpen = true };
        try
        {
            window.ShowClipboardHistoryDrilldown(flyout, border, new DispatcherTimer());
            var panel = (StackPanel)border.Child;
            var entry = Assert.Single(panel.Children.OfType<Button>());
            entry.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.True(flyout.IsOpen);
            Assert.Contains(panel.Children.OfType<TextBlock>(), text =>
                text.Visibility == Visibility.Visible && text.Text.Contains("Clipboard busy"));
            window.CopyHistoryText = _ => true;
            entry.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.False(flyout.IsOpen);
        }
        finally
        {
            flyout.IsOpen = false;
            window.DismissImmediately();
        }
    }

    private static void RenderContent(Window window)
    {
        var content = (FrameworkElement)window.Content;
        content.Measure(new Size(500, 600));
        content.Arrange(new Rect(0, 0, 500, 600));
        content.UpdateLayout();
        var bitmap = new RenderTargetBitmap(500, 600, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(content);
        Assert.True(content.ActualWidth > 0);
    }
}
