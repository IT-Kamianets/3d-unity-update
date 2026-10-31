# 3d-unity-update

Self-update for Unity games that are released on GitHub: when you publish a new release, players get it by themselves.

### Logic

`AutoUpdater` is a `MonoBehaviour` you give the game's repository (`WebArtWork/<game>.temnorid.com`).

- `Check()` reads `releases/latest` from the GitHub API and compares its tag with `Application.version` (`SemVer.IsNewer`). Drafts and pre-releases are ignored. No network is normal: the state becomes `Failed` with a message and the game goes on.
- `Install()` (only when the state is `Available`):
  - **Windows:** downloads the release asset ending with `-Setup.exe` to the temporary folder and runs it with `/SILENT /SUPPRESSMSGBOXES /NOCANCEL /CLOSEAPPLICATIONS`, then quits. The installer has a fixed `AppId`, so it replaces the game in place, and its `[Run]` entry starts the game again.
  - **Android:** opens `StoreUrl` (the Play page) or, without one, the `.apk` of the release.
  - **iOS and others:** opens `StoreUrl` or the release page.
- `State`, `Progress`, `Latest` and the `Changed` event let a screen show "Updating to 0.4.0…" while it downloads.

The game version is the Unity `bundleVersion`; the release tag is `v<bundleVersion>`.

### Status

v0.1: `AutoUpdater`, `ReleaseInfo`, `SemVer`, edit-mode tests for version compare and release parsing. Windows download and silent run, Android and iOS open a page.

### Roadmap

- In-app APK install on Android (a `FileProvider` and the install-packages permission).
- Checksum check of the downloaded installer.
