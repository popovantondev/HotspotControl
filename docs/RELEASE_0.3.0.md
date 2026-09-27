# Hotspot Control 0.3.0

The Windows 11 Mobile Hotspot app now has an approved compact interface in German, Russian and English. The first launch asks for a language; Settings can change it for the next launch. Main, devices, settings, messages and tray use the same visual design. This release also improves single-instance restoration, operation serialization, timeout feedback and migration of 0.2.0 preferences.

The release ZIP is self-contained for Windows 11 24H2+ x64. Extract all files and start `HotspotControl.App.exe`; no installer or administrator elevation is requested. The `.sha256` file contains the archive checksum.

Automated checks and WPF demo renders use fictional data and do not toggle the real hotspot. Practical verification has been performed only on the owner's computer. Real start/stop, a network-name or password change, login startup, and other hardware/policy combinations should be treated as unverified for this version unless separately recorded. Windows policy or hardware may prevent operations. A timeout does not guarantee Windows stopped the request.

Source viewing and binary use are governed by [RIGHTS.md](../RIGHTS.md). The bundled .NET components retain their own notices in [THIRD_PARTY_NOTICES.md](../THIRD_PARTY_NOTICES.md). Public screenshots are rendered from demonstration data.
