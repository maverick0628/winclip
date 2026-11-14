using System;
using WinClip.SDK;

namespace WinClip.Plugin.Template
{
    /// <summary>
    /// PLUGIN_NAME_PLACEHOLDER - PLUGIN_DESCRIPTION_PLACEHOLDER
    /// </summary>
    [Plugin("PLUGIN_ID_PLACEHOLDER", "PLUGIN_NAME_PLACEHOLDER", "PLUGIN_VERSION_PLACEHOLDER",
        Description = "PLUGIN_DESCRIPTION_PLACEHOLDER",
        Author = "PLUGIN_AUTHOR_PLACEHOLDER")]
    public class MyPlugin : PluginBase
    {
        /// <summary>
        /// Determines whether this plugin should be enabled for the current clipboard content.
        /// </summary>
        /// <param name="context">The clipboard context containing the current clipboard data.</param>
        /// <returns>True if the plugin should be enabled; otherwise, false.</returns>
        public override bool ShouldEnable(IClipboardContext context)
        {
            // Enable this plugin when there is text in the clipboard
            return context.HasText;
        }

        /// <summary>
        /// Example action that converts text to uppercase.
        /// </summary>
        /// <param name="context">The action context providing access to clipboard and system operations.</param>
        [Action("to-uppercase", "To Uppercase", Order = 1)]
        public void ToUppercase(IActionContext context)
        {
            var text = context.Clipboard.GetText();
            if (!string.IsNullOrEmpty(text))
            {
                var upperText = text.ToUpper();
                context.SetClipboard(upperText);
                context.PasteText(upperText);
                context.ShowNotification("PLUGIN_NAME_PLACEHOLDER", "Text converted to uppercase", NotificationType.Success);
            }
        }

        /// <summary>
        /// Example action that converts text to lowercase.
        /// </summary>
        /// <param name="context">The action context providing access to clipboard and system operations.</param>
        [Action("to-lowercase", "To Lowercase", Order = 2)]
        public void ToLowercase(IActionContext context)
        {
            var text = context.Clipboard.GetText();
            if (!string.IsNullOrEmpty(text))
            {
                var lowerText = text.ToLower();
                context.SetClipboard(lowerText);
                context.PasteText(lowerText);
                context.ShowNotification("PLUGIN_NAME_PLACEHOLDER", "Text converted to lowercase", NotificationType.Success);
            }
        }

        /// <summary>
        /// Example action that searches the selected text on the web.
        /// </summary>
        /// <param name="context">The action context providing access to clipboard and system operations.</param>
        [Action("web-search", "Search Web", Order = 3, Icon = "Search")]
        public void SearchWeb(IActionContext context)
        {
            var text = context.Clipboard.GetText();
            if (!string.IsNullOrEmpty(text))
            {
                var searchUrl = $"https://www.google.com/search?q={Uri.EscapeDataString(text)}";
                context.OpenUrl(searchUrl);
                context.Log($"Opening web search for: {text}", LogLevel.Info);
            }
        }
    }
}
