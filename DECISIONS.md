# Decisions

Newest first.

## 2026-11-16 — Original synthesized pop sound

Replace the sound with an original 85 ms PCM pop generated from documented oscillator equations, without using the former asset as input. Keep the resource path and playback implementation unchanged; this addresses provenance for future builds, not old releases or history. No new installer is released by this change.

The managed-worktree request returned only after work had begun in a dedicated Git worktree. Archive was refused because the app treats the new checkout as primary, so it remains clean and unused; the dedicated worktree preserves this task.

## 2026-11-14 — WinClip is free and MIT-licensed

WinClip became free and open source under the MIT licence. The 30-day trial, the licence-file verifier and the activation dialog are removed, along with the Gumroad and Keygen code and the BouncyCastle dependency. Old `settings.json` files still load because unknown JSON properties are ignored. The bundled Satoshi and General Sans fonts are removed because their licences do not allow redistribution; the app uses Segoe UI. The repository was republished without its earlier history.
