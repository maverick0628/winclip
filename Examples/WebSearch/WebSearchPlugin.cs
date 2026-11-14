using System;
using System.Diagnostics;
using WinClip.SDK;

namespace WinClip.Examples.WebSearch
{
    /// <summary>
    /// A plugin demonstrating external integration by launching web searches.
    /// Shows how to open URLs in the default browser based on clipboard text.
    /// </summary>
    [Plugin("com.winclip.examples.websearch", "Web Search", "1.0.0",
        Description = "Search the web using various search engines",
        Author = "WinClip Team")]
    public class WebSearchPlugin : PluginBase
    {
        /// <summary>
        /// Determines whether this plugin should be enabled for the current clipboard content.
        /// This plugin only works with text content.
        /// </summary>
        /// <param name="context">The clipboard context containing the current clipboard data.</param>
        /// <returns>True if the clipboard contains text; otherwise, false.</returns>
        public override bool ShouldEnable(IClipboardContext context)
        {
            // Only enable when clipboard contains text
            return context.HasText;
        }

        /// <summary>
        /// Searches Google for the clipboard text.
        /// </summary>
        /// <param name="context">The action context providing access to clipboard and system operations.</param>
        [Action("search-google", "Search Google", Order = 1)]
        public void SearchGoogle(IActionContext context)
        {
            string? text = context.Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(text))
            {
                context.ShowNotification(
                    "Web Search",
                    "No text available to search",
                    NotificationType.Warning);
                return;
            }

            string encodedQuery = Uri.EscapeDataString(text);
            string url = $"https://www.google.com/search?q={encodedQuery}";
            OpenUrl(url, context);
        }

        /// <summary>
        /// Searches Bing for the clipboard text.
        /// </summary>
        /// <param name="context">The action context providing access to clipboard and system operations.</param>
        [Action("search-bing", "Search Bing", Order = 2)]
        public void SearchBing(IActionContext context)
        {
            string? text = context.Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(text))
            {
                context.ShowNotification(
                    "Web Search",
                    "No text available to search",
                    NotificationType.Warning);
                return;
            }

            string encodedQuery = Uri.EscapeDataString(text);
            string url = $"https://www.bing.com/search?q={encodedQuery}";
            OpenUrl(url, context);
        }

        /// <summary>
        /// Searches DuckDuckGo for the clipboard text.
        /// </summary>
        /// <param name="context">The action context providing access to clipboard and system operations.</param>
        [Action("search-duckduckgo", "Search DuckDuckGo", Order = 3)]
        public void SearchDuckDuckGo(IActionContext context)
        {
            string? text = context.Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(text))
            {
                context.ShowNotification(
                    "Web Search",
                    "No text available to search",
                    NotificationType.Warning);
                return;
            }

            string encodedQuery = Uri.EscapeDataString(text);
            string url = $"https://duckduckgo.com/?q={encodedQuery}";
            OpenUrl(url, context);
        }

        /// <summary>
        /// Searches Stack Overflow for the clipboard text.
        /// </summary>
        /// <param name="context">The action context providing access to clipboard and system operations.</param>
        [Action("search-stackoverflow", "Search Stack Overflow", Order = 4)]
        public void SearchStackOverflow(IActionContext context)
        {
            string? text = context.Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(text))
            {
                context.ShowNotification(
                    "Web Search",
                    "No text available to search",
                    NotificationType.Warning);
                return;
            }

            string encodedQuery = Uri.EscapeDataString(text);
            string url = $"https://stackoverflow.com/search?q={encodedQuery}";
            OpenUrl(url, context);
        }

        /// <summary>
        /// Searches GitHub for the clipboard text.
        /// </summary>
        /// <param name="context">The action context providing access to clipboard and system operations.</param>
        [Action("search-github", "Search GitHub", Order = 5)]
        public void SearchGitHub(IActionContext context)
        {
            string? text = context.Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(text))
            {
                context.ShowNotification(
                    "Web Search",
                    "No text available to search",
                    NotificationType.Warning);
                return;
            }

            string encodedQuery = Uri.EscapeDataString(text);
            string url = $"https://github.com/search?q={encodedQuery}&type=repositories";
            OpenUrl(url, context);
        }

        /// <summary>
        /// Searches Wikipedia for the clipboard text.
        /// </summary>
        /// <param name="context">The action context providing access to clipboard and system operations.</param>
        [Action("search-wikipedia", "Search Wikipedia", Order = 6)]
        public void SearchWikipedia(IActionContext context)
        {
            string? text = context.Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(text))
            {
                context.ShowNotification(
                    "Web Search",
                    "No text available to search",
                    NotificationType.Warning);
                return;
            }

            string encodedQuery = Uri.EscapeDataString(text);
            string url = $"https://en.wikipedia.org/wiki/Special:Search?search={encodedQuery}";
            OpenUrl(url, context);
        }

        /// <summary>
        /// Opens a URL in the default browser.
        /// </summary>
        /// <param name="url">The URL to open.</param>
        /// <param name="context">The action context for logging and notifications.</param>
        private void OpenUrl(string url, IActionContext context)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });

                context.Log($"Opened URL: {url}", LogLevel.Info);
                context.ShowNotification(
                    "Web Search",
                    "Search opened in browser",
                    NotificationType.Success);
            }
            catch (Exception ex)
            {
                context.Log($"Failed to open URL: {ex.Message}", LogLevel.Error);
                context.ShowNotification(
                    "Web Search",
                    $"Failed to open browser: {ex.Message}",
                    NotificationType.Error);
            }
        }
    }
}
