# Native UI polish — Windows desktop checks

Automated checks cover settings rendering/search in light and dark resource dictionaries,
history copy failure/retry, mixed-DPI positioning arithmetic and bounded/stale editability reads.
They do not prove final pixels, actual per-monitor window placement or perceived latency.

## Visual and interaction checks

- Select Light, Dark and System. Open settings, a selection popup, history and an AI status window.
  Confirm system typography, flat surfaces, readable help text, a visible history search caret,
  clear hover/selection and keyboard focus. Check Windows high contrast separately.
- Resize settings to its minimum size. Search `sound`, `studio endpoint`, an action name and an
  unmatched phrase. Clear the search. Confirm all sections return, values survive filtering,
  no horizontal clipping occurs and the persistent section label follows scrolling.
- Trigger double-click and drag selection. Every action should appear together, without sliding,
  scaling or stagger. Disable Windows animation effects and repeat: entrance should be immediate.
- At 100%, 150% and 200% display scaling, test both windows on each monitor, including a monitor
  left of or above the primary. Test near all four work-area edges and with a non-bottom taskbar.
- Hold the clipboard open from another tool. Selecting history must show a failure without
  closing the picker/flyout or pasting old content. Release the lock and retry the same entry.
- During a slow editability read, type or click elsewhere. No obsolete popup, copy or paste
  should follow. A hung provider must not freeze the UI or accumulate worker threads.

## Performance evaluation

Before this change, popup entrance took 250 ms; button entry started at 60 + index × 40 ms
and ran for 180–200 ms. Flyout entries also staggered. Now controls are present together and
the surface fades for at most 50 ms (zero when Windows disables client-area animations).

Editability previously called cross-process UI Automation synchronously on the WPF dispatcher.
Now a dedicated MTA thread reads it with a 100 ms caller budget and at most one in-flight read.
A blocked provider can retain that one background thread until it returns; subsequent reads
return unknown immediately. This prevents thread growth but means editability-dependent controls
remain hidden while the provider is stuck. No process-isolation/recovery mechanism was added.

On Windows, collect at least 30 gestures per target (Notepad, Chrome, Word and Terminal) and
record median/p95 from `Selection UI preparation (...) took ... ms` debug log entries. Compare
on the same machine and settings. This includes capture and editability preparation, not display
scanout or every early-return path. Use a screen recording to evaluate time to visible controls.
Check responsiveness while an accessibility provider is busy, then after it recovers.

Remaining performance risks outside this pass: the shared ClipboardService uses synchronous
retry sleeps under contention, SafeReadClipboard has a synchronous retry path, and history
search rebuilds its row collection per keystroke. These were not benchmarked or changed here.
Do not interpret the configured animation/read budgets as a measured overall speedup.
