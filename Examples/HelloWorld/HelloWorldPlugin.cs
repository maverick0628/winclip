using System;
using WinClip.SDK;

namespace WinClip.Examples.HelloWorld
{
    /// <summary>
    /// The simplest possible WinClip plugin - demonstrates basic structure with a single action.
    /// This is a great starting point for learning plugin development.
    /// </summary>
    [Plugin("com.winclip.examples.helloworld", "Hello World", "1.0.0",
        Description = "A simple example plugin that greets the user",
        Author = "WinClip Team")]
    public class HelloWorldPlugin : PluginBase
    {
        /// <summary>
        /// Determines whether this plugin should be enabled for the current clipboard content.
        /// This example is always enabled, regardless of clipboard content.
        /// </summary>
        /// <param name="context">The clipboard context containing the current clipboard data.</param>
        /// <returns>True if the plugin should be enabled; otherwise, false.</returns>
        public override bool ShouldEnable(IClipboardContext context)
        {
            // This simple plugin is always enabled
            return true;
        }

        /// <summary>
        /// A simple action that greets the user with a notification.
        /// </summary>
        /// <param name="context">The action context providing access to clipboard and system operations.</param>
        [Action("say-hello", "Say Hello", Order = 1)]
        public void SayHello(IActionContext context)
        {
            context.ShowNotification(
                "Hello World",
                "Hello from WinClip! 👋",
                NotificationType.Success);

            context.Log("Hello action executed", LogLevel.Info);
        }
    }
}
