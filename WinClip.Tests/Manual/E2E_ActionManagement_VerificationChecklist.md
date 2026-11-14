# End-to-End Verification Checklist: Action Management

**Feature:** Action Management (Task 013)
**Date:** 2026-02-02
**Verification Type:** Manual E2E Testing
**Status:** Ready for Manual Testing

## Overview
This checklist verifies the complete action management workflow from settings configuration to popup display. All implementation subtasks (1-1 through 5-3) have been completed. This document provides step-by-step verification instructions for QA/manual testing.

---

## Prerequisites
- WinClip application built and running
- No previous action configuration (or reset to defaults)
- Test text available in any application for selection

---

## Test Case 1: Open Settings and Navigate to Actions

**Steps:**
1. Launch WinClip application
2. Open Settings window (via system tray icon or hotkey)
3. Click on "Actions" tab

**Expected Results:**
- ✅ Settings window opens successfully
- ✅ Actions tab is visible and accessible
- ✅ ActionManagementView loads with list of all available actions
- ✅ All actions show: Copy, Search, Case Transform, Translate, Email
- ✅ All actions are enabled by default (checkboxes checked)
- ✅ Actions appear in default order

---

## Test Case 2: Disable Copy Action

**Steps:**
1. In Settings > Actions tab
2. Locate "Copy" action in the list
3. Uncheck the checkbox next to "Copy" action
4. Note: Do NOT click Save yet (changes should apply immediately per spec)

**Expected Results:**
- ✅ Copy action checkbox becomes unchecked
- ✅ UI updates to reflect disabled state (possibly grayed out)

---

## Test Case 3: Enable All Other Actions

**Steps:**
1. Verify all other actions are enabled:
   - Search ✓
   - Case Transform ✓
   - Translate ✓
   - Email ✓
2. Check any that are not already checked

**Expected Results:**
- ✅ All actions except Copy are enabled (checkboxes checked)

---

## Test Case 4: Reorder Actions (Search First, Case Transform Second)

**Steps:**
1. Use drag-and-drop or Up/Down buttons to reorder actions
2. Move "Search" action to the top (first position)
3. Move "Case Transform" action to second position
4. Resulting order should be:
   1. Search
   2. Case Transform
   3. (Other actions in any order)

**Expected Results:**
- ✅ Drag-and-drop works smoothly (if implemented via mouse)
- ✅ Up/Down buttons reorder correctly
- ✅ Visual feedback during drag operation
- ✅ Actions reorder as expected
- ✅ Search is now first, Case Transform is second

---

## Test Case 5: Set MaxVisibleActions to 5

**Steps:**
1. Locate "Maximum Visible Actions" field
2. Clear existing value (if any)
3. Enter "5"

**Expected Results:**
- ✅ Field accepts numeric input
- ✅ Value updates to 5

---

## Test Case 6: Save Configuration

**Steps:**
1. Click "Save" or "Apply" button
2. Wait for confirmation (if any)

**Expected Results:**
- ✅ Save operation completes successfully
- ✅ No error messages appear
- ✅ Settings window remains open or closes gracefully
- ✅ Configuration persisted to `AppData/Local/WinClip/settings.json`

---

## Test Case 7: Test Popup Without Copy Action

**Steps:**
1. Close Settings window
2. Open any application (Notepad, browser, etc.)
3. Select some text
4. Trigger WinClip popup (should appear automatically or via hotkey)

**Expected Results:**
- ✅ Popup appears near selected text
- ✅ Copy action is NOT present in popup
- ✅ Only enabled actions appear: Search, Case Transform, Translate, Email
- ✅ No error or missing action indicators

---

## Test Case 8: Verify Action Order in Popup

**Steps:**
1. With popup still open from Test Case 7
2. Examine the order of actions in the popup

**Expected Results:**
- ✅ Search action appears FIRST (top of list)
- ✅ Case Transform appears SECOND
- ✅ Other enabled actions follow in configured order

---

## Test Case 9: Verify MaxVisibleActions Limit

**Steps:**
1. Count the number of actions shown in popup
2. Note: We enabled 4 actions (Copy disabled), so all should be visible
3. Verify "More" indicator logic:
   - If more than 5 actions configured, should show "... (N more in settings)"
   - If 5 or fewer, no indicator should appear

**Expected Results:**
- ✅ Maximum of 5 actions displayed (or fewer if less than 5 enabled)
- ✅ If more actions exist, "... (N more in settings)" message appears
- ✅ If all actions fit, no "More" indicator

---

## Test Case 10: Verify Settings Persistence (Survive Restart)

**Steps:**
1. Close WinClip application completely (exit from system tray)
2. Wait 2-3 seconds
3. Restart WinClip application
4. Open Settings > Actions

**Expected Results:**
- ✅ Copy action is still DISABLED (unchecked)
- ✅ Action order is preserved: Search first, Case Transform second
- ✅ MaxVisibleActions still shows "5"
- ✅ All configuration changes survived restart
- ✅ Settings loaded from `AppData/Local/WinClip/settings.json`

---

## Test Case 11: Configure Web Search Engine (DuckDuckGo)

**Steps:**
1. In Settings > Actions tab
2. Locate "Search" or "Web Search" action
3. Expand the action (click arrow/expander if present)
4. Look for "Search Engine" dropdown or configuration option
5. Select "DuckDuckGo" from the dropdown
6. Click Save/Apply

**Expected Results:**
- ✅ Action expands to show configuration options
- ✅ Search engine dropdown is present
- ✅ DuckDuckGo option is available
- ✅ Selection updates successfully
- ✅ Configuration saved (no errors)

---

## Test Case 12: Test Search Action Uses DuckDuckGo

**Steps:**
1. Close Settings window
2. In any application, select some text (e.g., "quantum physics")
3. Open WinClip popup
4. Click on "Search" action
5. Observe which website opens

**Expected Results:**
- ✅ Search action executes successfully
- ✅ Browser opens with DuckDuckGo search results
- ✅ Search query includes the selected text
- ✅ URL starts with `https://duckduckgo.com/` (not Google or other engines)

---

## Additional Verification Points

### UI/UX Quality
- ✅ All UI elements properly aligned and styled
- ✅ No overlapping text or controls
- ✅ Responsive to window resizing
- ✅ Drag-and-drop provides visual feedback
- ✅ Tooltips appear where appropriate

### Error Handling
- ✅ Invalid MaxVisibleActions values handled gracefully (e.g., negative numbers, text)
- ✅ Corrupted settings.json loads defaults without crashing
- ✅ Missing action metadata handled gracefully

### Performance
- ✅ Settings load quickly (<500ms)
- ✅ Popup appears instantly when text selected
- ✅ No lag when toggling actions or reordering
- ✅ Configuration saves within 1 second

### Integration
- ✅ ActionService properly filters enabled actions
- ✅ SettingsService persists to correct location
- ✅ ActionConfigurationChanged event triggers popup refresh
- ✅ All components communicate correctly

---

## Acceptance Criteria Checklist

From spec.md:
- ✅ Enable/disable individual actions
- ✅ Drag-and-drop reordering of actions
- ✅ Per-action configuration (e.g., which search engine for Search)
- ✅ Limit visible actions (e.g., show only top 5)
- ✅ Changes reflected immediately in popup
- ✅ Settings persist across application restarts

---

## Sign-Off

**Tester Name:** _________________________
**Date:** _________________________
**Status:** ☐ PASS  ☐ FAIL  ☐ PARTIAL

**Issues Found:**
_____________________________________________________________________________
_____________________________________________________________________________
_____________________________________________________________________________

**Additional Notes:**
_____________________________________________________________________________
_____________________________________________________________________________
_____________________________________________________________________________

---

## Automation Notes

This is a **manual verification** task because:
1. Requires visual inspection of UI elements
2. Involves cross-application interaction (selecting text in other apps)
3. Tests user gestures (drag-and-drop)
4. Verifies application restart behavior
5. Checks browser integration (DuckDuckGo search)

Automated E2E tests using UI automation frameworks (e.g., WinAppDriver, Selenium) could be implemented in a future phase, but manual verification is appropriate for initial QA.

---

**Document Status:** Ready for Manual Testing
**Implementation Status:** All code complete (subtasks 1-1 through 5-3)
**Next Step:** Manual QA execution using this checklist
