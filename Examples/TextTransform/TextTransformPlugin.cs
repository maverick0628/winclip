using System;
using System.Globalization;
using System.Linq;
using WinClip.SDK;

namespace WinClip.Examples.TextTransform
{
    /// <summary>
    /// A plugin demonstrating multiple text transformation actions.
    /// Shows how to work with clipboard text and provide multiple related actions.
    /// </summary>
    [Plugin("com.winclip.examples.texttransform", "Text Transform", "1.0.0",
        Description = "Transform text with various formatting options",
        Author = "WinClip Team")]
    public class TextTransformPlugin : PluginBase
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
        /// Converts text to UPPERCASE.
        /// </summary>
        /// <param name="context">The action context providing access to clipboard and system operations.</param>
        [Action("uppercase", "UPPERCASE", Order = 1)]
        public void ToUpperCase(IActionContext context)
        {
            string? text = context.Clipboard.GetText();
            if (string.IsNullOrEmpty(text))
            {
                context.ShowNotification(
                    "Text Transform",
                    "No text available to transform",
                    NotificationType.Warning);
                return;
            }

            string transformed = text.ToUpperInvariant();
            context.PasteText(transformed);
            context.Log($"Transformed text to uppercase: {text.Length} characters", LogLevel.Info);
        }

        /// <summary>
        /// Converts text to lowercase.
        /// </summary>
        /// <param name="context">The action context providing access to clipboard and system operations.</param>
        [Action("lowercase", "lowercase", Order = 2)]
        public void ToLowerCase(IActionContext context)
        {
            string? text = context.Clipboard.GetText();
            if (string.IsNullOrEmpty(text))
            {
                context.ShowNotification(
                    "Text Transform",
                    "No text available to transform",
                    NotificationType.Warning);
                return;
            }

            string transformed = text.ToLowerInvariant();
            context.PasteText(transformed);
            context.Log($"Transformed text to lowercase: {text.Length} characters", LogLevel.Info);
        }

        /// <summary>
        /// Converts text to Title Case.
        /// </summary>
        /// <param name="context">The action context providing access to clipboard and system operations.</param>
        [Action("titlecase", "Title Case", Order = 3)]
        public void ToTitleCase(IActionContext context)
        {
            string? text = context.Clipboard.GetText();
            if (string.IsNullOrEmpty(text))
            {
                context.ShowNotification(
                    "Text Transform",
                    "No text available to transform",
                    NotificationType.Warning);
                return;
            }

            TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
            string transformed = textInfo.ToTitleCase(text.ToLowerInvariant());
            context.PasteText(transformed);
            context.Log($"Transformed text to title case: {text.Length} characters", LogLevel.Info);
        }

        /// <summary>
        /// Reverses the text character by character.
        /// </summary>
        /// <param name="context">The action context providing access to clipboard and system operations.</param>
        [Action("reverse", "Reverse", Order = 4)]
        public void ReverseText(IActionContext context)
        {
            string? text = context.Clipboard.GetText();
            if (string.IsNullOrEmpty(text))
            {
                context.ShowNotification(
                    "Text Transform",
                    "No text available to transform",
                    NotificationType.Warning);
                return;
            }

            char[] charArray = text.ToCharArray();
            Array.Reverse(charArray);
            string transformed = new string(charArray);
            context.PasteText(transformed);
            context.Log($"Reversed text: {text.Length} characters", LogLevel.Info);
        }

        /// <summary>
        /// Removes all whitespace from the text.
        /// </summary>
        /// <param name="context">The action context providing access to clipboard and system operations.</param>
        [Action("remove-whitespace", "Remove Whitespace", Order = 5)]
        public void RemoveWhitespace(IActionContext context)
        {
            string? text = context.Clipboard.GetText();
            if (string.IsNullOrEmpty(text))
            {
                context.ShowNotification(
                    "Text Transform",
                    "No text available to transform",
                    NotificationType.Warning);
                return;
            }

            string transformed = new string(text.Where(c => !char.IsWhiteSpace(c)).ToArray());
            context.PasteText(transformed);
            context.Log($"Removed whitespace: {text.Length} -> {transformed.Length} characters", LogLevel.Info);
        }

        /// <summary>
        /// Trims leading and trailing whitespace from the text.
        /// </summary>
        /// <param name="context">The action context providing access to clipboard and system operations.</param>
        [Action("trim", "Trim", Order = 6)]
        public void TrimText(IActionContext context)
        {
            string? text = context.Clipboard.GetText();
            if (string.IsNullOrEmpty(text))
            {
                context.ShowNotification(
                    "Text Transform",
                    "No text available to transform",
                    NotificationType.Warning);
                return;
            }

            string transformed = text.Trim();
            context.PasteText(transformed);
            context.Log($"Trimmed text: {text.Length} -> {transformed.Length} characters", LogLevel.Info);
        }
    }
}
