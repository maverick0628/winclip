# Design System — WinClip

## Product Context
- **What this is:** A Windows system tray clipboard utility with 52 gesture-driven text actions, AI-powered transforms, and theme-aware popups
- **Who it's for:** Power users, developers, writers — anyone who does heavy text manipulation on Windows
- **Space/industry:** Desktop productivity tools / clipboard managers (peers: Raycast, Paste, Ditto, Alfred, ClipClip)
- **Project type:** Desktop WPF app + marketing website

## Aesthetic Direction
- **Direction:** Industrial/Utilitarian — function-first with deliberate warmth
- **Decoration level:** Intentional — subtle depth via layered surfaces and edge glow, perceived depth through translucency (WPF renders this natively)
- **Mood:** A premium power tool, not a SaaS dashboard. WinClip is a workshop instrument — precise, fast, and quietly beautiful. The first clipboard utility that makes Windows power users feel like someone designed *for them*.
- **Reference sites:** raycast.com, pasteapp.io, alfredapp.com

## Typography
- **Display/Hero:** Satoshi (900/700/500) — geometric but warm, distinctive at large sizes without being loud. For popup headers, marketing headlines, hero text.
- **Body:** General Sans (400/500/600) — slightly condensed proportions fit more content in the popup without cramming. Clean at 12-14px. For action labels, settings text, marketing body copy.
- **UI/Labels:** General Sans (same as body)
- **Data/Tables:** Geist Mono (tabular-nums) or JetBrains Mono — modern, clean 0/O/l/1 disambiguation
- **Code:** JetBrains Mono — industry standard for code display
- **WPF runtime:** Segoe UI Variable Text, falling back to Segoe UI. Satoshi/General Sans are marketing fonts only; their files are not bundled in this repository.
- **Loading:** Fontshare CDN for Satoshi + General Sans (marketing site), Google Fonts for JetBrains Mono
- **Scale:**
  - Hero: 48px / 3rem (weight 900, tracking -0.03em)
  - H1: 28px / 1.75rem (weight 700)
  - H2: 22px / 1.375rem (weight 700)
  - H3: 18px / 1.125rem (weight 600)
  - Body: 15px / 0.9375rem (weight 400)
  - Small: 13px / 0.8125rem (weight 400-500)
  - Caption: 11px / 0.6875rem (weight 400)
  - Mono: 14px / 0.875rem (labels), 11px / 0.6875rem (shortcuts)

## Color
- **Approach:** Restrained — one signature accent + neutral slate palette. Color is rare and meaningful.
- **Primary accent:** `#2DD4BF` (teal) — distinctive from the sea of blue/purple productivity tools, sharp on both dark and light backgrounds. Used for: active states, selected action highlight, primary buttons, links, the "you are here" signal.
- **Primary hover:** `#14B8A6` — slightly deeper teal for hover/press states
- **Accent glow:** `rgba(45, 212, 191, 0.12-0.15)` — subtle background glow for active items
- **Secondary:** `#94A3B8` (slate 400) — metadata, timestamps, secondary information

### Neutrals — Light Mode
| Token | Hex | Usage |
|-------|-----|-------|
| Background 0 | `#F8FAFC` | Page/window background |
| Background 1 | `#FFFFFF` | Surface (cards, popups) |
| Background 2 | `#F1F5F9` | Elevated/soft surfaces, hover states |
| Border | `#E2E8F0` | Borders, separators |
| Text Primary | `#0F172A` | Headings, labels |
| Text Secondary | `#475569` | Body text, descriptions |
| Text Muted | `#94A3B8` | Placeholders, disabled, shortcuts |

### Neutrals — Dark Mode
| Token | Hex | Usage |
|-------|-----|-------|
| Background 0 | `#0B1220` | Window background (deepest) |
| Background 1 | `#0F172A` | Surface (cards, popups) |
| Background 2 | `#1E293B` | Elevated surfaces, hover states |
| Border | `#334155` | Borders, separators |
| Text Primary | `#E2E8F0` | Headings, labels |
| Text Secondary | `#94A3B8` | Body text, descriptions |
| Text Muted | `#64748B` | Placeholders, disabled, shortcuts |

### Semantic Colors
| Role | Light | Dark |
|------|-------|------|
| Success | `#22C55E` | `#4ADE80` |
| Warning | `#F59E0B` | `#FBBF24` |
| Error | `#EF4444` | `#F87171` |
| Info | `#38BDF8` | `#7DD3FC` |

### Dark Mode Strategy
Reduce accent saturation by 10-20% in dark mode. Use `#5EEAD4` as dark-mode accent alternative if `#2DD4BF` feels too bright on dark surfaces. Surface layers step up in lightness: 0B → 0F → 1E → 33.

## Spacing
- **Base unit:** 4px
- **Density:** Comfortable — precise but breathable, not cramped like Ditto, not airy like a SaaS dashboard
- **Scale:** 2xs(2px) xs(4px) sm(8px) md(16px) lg(24px) xl(32px) 2xl(48px) 3xl(64px)

## Layout
- **Approach:** Grid-disciplined — the popup is a precision instrument with tight, predictable alignment
- **Popup width:** Content-sized compact toolbar, capped to the active monitor work area
- **Settings:** Single-scroll with sticky section headers and searchable filter field (no tabs)
- **Max content width:** 1080px (marketing site)
- **Border radius:**
  - sm: 4px (shortcuts, badges)
  - md: 8px (buttons, inputs, alerts)
  - lg: 12px (cards, popup window, settings window)
  - full: 9999px (pills, toggles)

## Motion
- **Approach:** Minimal-functional — the popup appears instantly (never delay the user). Subtle fade for state transitions only.
- **Easing:** enter(ease-out) exit(ease-in) move(ease-in-out)
- **Duration:** micro(50-100ms) short(150-250ms) medium(250-400ms)
- **Rules:**
  - Popup show/hide: instant or 50ms fade (never slide/bounce/spring)
  - Hover states: 150ms transition
  - Theme switch: 300ms crossfade
  - No choreography, no staggered animations, no spring physics

## Shadow System
- **Surface:** `0 1px 3px rgba(15,23,42,0.06), 0 0 0 1px rgba(15,23,42,0.04)` — cards, popup
- **Elevated:** `0 4px 16px rgba(15,23,42,0.08), 0 0 0 1px rgba(15,23,42,0.04)` — floating popup, dropdowns
- **Dark mode:** Increase shadow opacity by ~2x, use `rgba(0,0,0,...)` base

## Decisions Log
| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-03-23 | Initial design system created | Created from competitive research (Raycast, Paste, Alfred, Ditto, ClipClip) |
| 2026-03-23 | Teal accent (#2DD4BF) over indigo/purple | Every productivity tool defaults to indigo/blue. Teal stands out in the Windows ecosystem without being abrasive |
| 2026-03-23 | Satoshi + General Sans for marketing | Distinctive typography signals intentional design. Fontshare CDN (free). WPF app keeps Segoe UI Variable as runtime font |
| 2026-03-23 | Single-scroll settings over tabs | More discoverable, more modern, respects user intelligence. Searchable with sticky headers |
| 2026-03-23 | Restrained color approach | One signature accent + neutrals. Color is rare and meaningful — fits the "precision tool" aesthetic |

## 2026-11-13 — Refined native Windows theme

The maintainer asked for a more modern theme alongside the UI polish. Keep the teal/slate identity,
but use flat surfaces, hairline borders, 12px cards, 20px card padding and 16px section gaps.
Settings uses native Mica chrome with a flat content background, persistent search and a sticky
current-section label over one continuous page. Search matches all query words across section
labels and action names; it never indexes API keys or other user-entered values.

Use dark teal (#0F766E) for light-mode links and primary controls, white text on those controls,
and mint (#5EEAD4) with dark text in dark mode. Secondary helper copy must remain legible:
light muted text is #64748B and dark muted text is #94A3B8. History selection combines a tinted
surface with a teal outline; hover, caret and keyboard focus follow the chosen theme.

Popup and flyout actions appear together. The only entrance motion is a 50ms fade, skipped
when Windows disables client-area animation. Do not scale or translate a target under the pointer.
