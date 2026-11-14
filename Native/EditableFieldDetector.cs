using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;

namespace WinClip.Native;

/// <summary>
/// Detects whether the currently focused control is an editable text field.
/// Uses an inverted approach: defaults to editable (show Cut/Paste), and only
/// returns false when we can POSITIVELY confirm the control is read-only.
/// This ensures Cut/Paste buttons always appear unless we're certain they shouldn't.
/// </summary>
internal static class EditableFieldDetector
{
    private static readonly BoundedEditabilityReader Reader = new();
    internal static readonly TimeSpan ReadBudget = TimeSpan.FromMilliseconds(100);

    internal static async Task<EditabilityResult> IsFocusedControlEditableAsync(int screenX, int screenY)
    {
        var target = NativeMethods.GetForegroundWindow();
        var input = ReadLastInput();
        var result = await Reader.ReadAsync(() => IsFocusedControlEditable(screenX, screenY), ReadBudget);
        return EditabilityResult.FromRead(result,
            target != IntPtr.Zero && target == NativeMethods.GetForegroundWindow() && input == ReadLastInput());
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo
    {
        public uint Size;
        public uint Tick;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetLastInputInfo(ref LastInputInfo info);

    private static uint? ReadLastInput()
    {
        var info = new LastInputInfo { Size = (uint)Marshal.SizeOf<LastInputInfo>() };
        return GetLastInputInfo(ref info) ? info.Tick : null;
    }

    /// <summary>
    /// Returns true (editable) by default. Only returns false when we can
    /// positively confirm the control is read-only (e.g. static text labels,
    /// read-only Document controls, web page body text).
    /// </summary>
    internal static bool IsFocusedControlEditable(int screenX, int screenY)
    {
        try
        {
            // ── Fast path: Win32 caret check (~0ms) ──
            // If a caret is present, it's definitely editable — return immediately
            if (HasWin32Caret())
                return true;

            // ── Check for known read-only scenarios via UI Automation ──
            // Only returns false when POSITIVELY confirmed read-only
            return !IsConfirmedReadOnly(screenX, screenY);
        }
        catch
        {
            // On any error, default to showing Cut/Paste (better to show
            // than to hide — clicking Cut on non-editable text is harmless)
            return true;
        }
    }

    /// <summary>
    /// Strict editable check using only the caret signal. Use this when
    /// false positives are worse than false negatives (e.g. hiding paste
    /// on drag-select outside text boxes).
    /// </summary>
    internal static bool HasCaret() => HasWin32Caret();

    /// <summary>
    /// Quick check: does the foreground thread have a system caret?
    /// Caret = definitely editable (Notepad, terminal, Win32 Edit, etc.)
    /// </summary>
    private static bool HasWin32Caret()
    {
        var fgHwnd = NativeMethods.GetForegroundWindow();
        if (fgHwnd == IntPtr.Zero) return false;

        uint threadId = NativeMethods.GetWindowThreadProcessId(fgHwnd, out _);

        var gui = new NativeMethods.GUITHREADINFO();
        gui.cbSize = Marshal.SizeOf<NativeMethods.GUITHREADINFO>();

        if (NativeMethods.GetGUIThreadInfo(threadId, ref gui))
        {
            if (gui.hwndCaret != IntPtr.Zero)
                return true;
            if ((gui.flags & NativeMethods.GUI_CARETBLINKING) != 0)
                return true;
        }

        return false;
    }

    /// <summary>
    /// UI Automation check: returns true ONLY when we can positively confirm
    /// the element at the cursor is read-only. If detection fails or is ambiguous,
    /// returns false (meaning "not confirmed read-only" → show Cut/Paste).
    /// </summary>
    private static bool IsConfirmedReadOnly(int screenX, int screenY)
    {
        AutomationElement? element;
        try
        {
            var point = new System.Windows.Point(screenX, screenY);
            element = AutomationElement.FromPoint(point);
        }
        catch
        {
            // Can't get automation element — not confirmed read-only
            return false;
        }

        if (element == null) return false;

        ControlType controlType;
        try
        {
            controlType = element.Current.ControlType;
        }
        catch
        {
            return false;
        }

        // ── Known editable types: never read-only ──
        if (controlType == ControlType.Edit)
            return false; // Editable
        if (controlType == ControlType.Spinner)
            return false; // Editable

        // ── Static text labels: confirmed read-only ──
        if (controlType == ControlType.Text)
            return true;

        // ── Document controls: check IsReadOnly explicitly ──
        // (web page body = read-only, contenteditable div = writable)
        if (controlType == ControlType.Document)
        {
            try
            {
                if (element.TryGetCurrentPattern(ValuePattern.Pattern, out var docPattern))
                {
                    var vp = (ValuePattern)docPattern;
                    return vp.Current.IsReadOnly;
                }
            }
            catch { /* Can't determine — not confirmed read-only */ }
            // No ValuePattern on Document = likely web page body (read-only)
            return true;
        }

        // ── For all other control types: check ValuePattern if available ──
        try
        {
            if (element.TryGetCurrentPattern(ValuePattern.Pattern, out var valPattern))
            {
                var vp = (ValuePattern)valPattern;
                if (vp.Current.IsReadOnly)
                    return true;  // Explicitly marked read-only
                return false;     // Has writable ValuePattern → editable
            }
        }
        catch { /* Can't determine — not confirmed read-only */ }

        // ── No ValuePattern and not a known type ──
        // Default: not confirmed read-only → show Cut/Paste
        return false;
    }
}

internal readonly record struct EditabilityResult(bool IsEditable, bool IsCurrent)
{
    internal static EditabilityResult FromRead(bool? value, bool isCurrent) =>
        new(isCurrent && value == true, isCurrent);
}
