# Architecture

`HotspotControl.Core` contains language-independent contracts, input validation, operation serialization and retry rules. `HotspotControl.Windows` adapts the Windows Mobile Hotspot API and projects its results into those contracts. `HotspotControl.Localization` maps result codes and UI keys to German, Russian and English. `HotspotControl.Presentation` implements the shared WPF views. `HotspotControl.App` binds them to Windows networking, preferences, single-instance activation and the tray.

`HotspotControl.Preview` uses the same views with an in-memory demonstration service. It neither references the production Windows/App projects nor accesses actual network settings or user preferences. `HotspotControl.Probe` reads diagnostic state without changing it.

Windows operations are serialized. A timeout ends the UI wait but may leave an underlying Windows operation running; its late result is not reported as a new success. A fresh status read establishes the next state. Manual commands cancel pending automatic-start retries. A per-user instance coordinator restores an existing window when the executable is started again.

Preferences preserve the 0.2.0 auto-enable flag, add a language and reject damaged or future schemas safely. Network passwords are passed to Windows when saved and are not stored in the app preference file. The Windows-start shortcut is per user.

`Run-Checks.ps1` builds and runs behavior checks without changing the network. Preview checks and render exports use fictional data. Integration tests that change the real hotspot require an explicitly chosen test window. Historical numbered documents record earlier 0.2.0 design and verification, not current 0.3.0 claims.
