# UI QA Checklist

Purpose: final visual and interaction validation after the theme-system modernization work.

Scope:
- `UI/SettingsWindow.xaml`
- `UI/PopupWindow.xaml`
- `src/WinClip.UI/Views/SettingsWindow.xaml`
- `src/WinClip.UI/Views/PopupWindow.xaml`
- `src/WinClip.UI/Views/PluginManagerWindow.xaml`
- `src/WinClip.UI/Views/PermissionRequestDialog.xaml`

## 1) Environment Matrix

- Windows 11 in Light mode
- Windows 11 in Dark mode
- Windows 10 in Light mode
- Windows 10 in Dark mode
- Display scale: 100%, 125%, 150%

## 2) Screenshot Set (Required)

Capture one screenshot per scenario:

- Settings window, General tab
- Settings window, Actions tab
- Settings window, About tab
- Popup in "Copied" state
- Popup in "Selection" state with actions visible
- Plugin manager window (list + detail panel)
- Permission request dialog (with low, medium, high permissions visible)

Naming convention:
- `ui-{window}-{state}-{theme}-{scale}.png`
- Example: `ui-settings-general-dark-125.png`

## 3) Visual Consistency Checks

- Shared controls use the same corner radius family and spacing rhythm.
- Primary buttons use accent gradient and remain readable in both themes.
- Secondary buttons remain visually distinct from primary in both themes.
- Cards/surfaces use consistent border and elevation treatment.
- Header/title typography hierarchy is consistent across windows.
- No hardcoded legacy colors appear visually in `src/WinClip.UI/Views`.

## 4) Contrast and Accessibility Checks

- Body text remains clearly readable against all surfaces in light and dark.
- Muted/help text remains readable (not too low contrast).
- Badge text (status/risk) remains readable against badge backgrounds.
- Link text is distinguishable from non-link text.
- Keyboard focus indicator is visible for:
  - text inputs
  - primary/secondary buttons
  - list selections
  - dialog actions

## 5) Interaction Checks

- Hover states are visible and consistent for primary and secondary buttons.
- Pressed/active states are visible and do not "jump" layout.
- Disabled state looks disabled and prevents interaction.
- List row hover and selected states are visually clear and consistent.
- Popup action hover/selection behavior remains smooth and predictable.
- Theme changes at runtime apply updated colors without restart artifacts.

## 6) Functional Regression Checks

- Settings links still open correctly and invalid URLs still show warnings.
- Plugin manager enable/disable/reload/uninstall buttons still behave correctly.
- Permission dialog allow/deny and website-link behavior still works.
- Popup action execution feedback still appears (success/failure/timeout text).

## 7) Sign-off Criteria

Ship-ready when all are true:

- All required screenshots captured for at least one light and one dark run.
- No visual defects rated High.
- No contrast/accessibility blockers.
- No interaction regressions in core flows.
- No functional regressions found in UI-adjacent behavior.

## 8) Defect Template

Use this format for each issue:

- ID: `UI-###`
- Severity: `High` | `Medium` | `Low`
- Theme/Scale: `light|dark`, `100|125|150`
- Area: window + state
- Expected:
- Actual:
- Screenshot:
- Repro steps:

