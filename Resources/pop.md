# WinClip pop sound

`pop.wav` was synthesized for WinClip on 2026-11-16 from the oscillator and envelope equations in `scripts/generate-pop.mjs`. No recording, sample library or audio from the previous asset was used. The generator and resulting asset are distributed under the repository's MIT licence.

Run `node scripts/generate-pop.mjs` from the repository root to recreate it. Node is only needed to regenerate the asset, not to build or run WinClip.

The sound is an 85 ms downward sine sweep with a short high-frequency transient, a soft attack and a tapered tail. Format: 44,100 Hz, mono, signed 16-bit PCM WAV. Peak amplitude is limited to approximately -6 dBFS. The embedded resource name and playback code are unchanged.

This replaces the asset with undocumented provenance in future builds. It does not change previously published installers or remove the old asset from Git history.
