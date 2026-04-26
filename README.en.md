# SimplePlanes 2 Part Editor Plugin

[中文](README.md)

This is an in-game part data editor for `SimplePlanes 2`, independent from the localization plugin. It takes inspiration from Overload-style editing, but the implementation is tailored for SP2: the default target is the XML-backed `PartData` / `PartModifierData` data layer instead of the full Unity runtime object graph.

## Scope

- Runs independently from the SimplePlanes 2 localization plugin.
- Uses `BepInEx 5 Mono` and Unity IMGUI.
- Write-back is controlled through `Apply / Reset`, so typing does not immediately mutate game data.
- Prioritizes the data path: `Designer.SelectedPart -> PartScript -> PartData -> Modifiers`.
- Includes Chinese/English UI switching and JSON localization files for other languages.

## Install

Download the latest version from GitHub Releases:

https://github.com/hahaha8459812/simpleplanes2-part-editor-plugin/releases

The release package already includes `BepInEx 5 Mono x64`. You do not need to download BepInEx separately.

After downloading `SimplePlanes2PartEditor-Release.zip`:

1. Close `SimplePlanes 2`.
2. Extract the zip.
3. Place all extracted contents into the game root, the folder containing `SimplePlanes 2.exe`.
4. Start the game and enter the designer.
5. Select a part and press `F8`.

You can also run the installer from the extracted release package root:

```powershell
.\install.ps1
```

Custom game directory:

```powershell
.\install.ps1 -GameDir "D:\SteamLibrary\steamapps\common\SimplePlanes 2"
```

After installation, the game folder should contain:

```text
SimplePlanes 2\
├─ winhttp.dll
├─ doorstop_config.ini
├─ BepInEx\
│  ├─ core\
│  └─ plugins\
│     └─ SimplePlanes2PartEditor\
│        ├─ SimplePlanes2PartEditor.dll
│        ├─ settings.json
│        └─ localization\
└─ SimplePlanes 2.exe
```

## Update Notice

If you update by manually copying the whole mod package into the game root, back up your existing settings first:

```text
BepInEx\plugins\SimplePlanes2PartEditor\settings.json
```

Overwriting the whole folder may reset personal settings such as the floating button position, panel size, language, and hotkey. Using `install.ps1` or `build.ps1 -InstallToGame` is recommended because they preserve an existing `settings.json` when possible.

## Version Index

The repository root `index.json` is a lightweight update index used by the plugin. Keep it limited to the version number and release notes:

```json
{
  "version": "0.3.0",
  "releaseNotes": "Release notes here."
}
```

When publishing a new version, update this file and set its raw URL in the plugin settings:

```json
{
  "updateCheckEnabled": true,
  "updateIndexUrl": "https://api.github.com/repos/hahaha8459812/simpleplanes2-part-editor-plugin/contents/index.json?ref=main"
}
```

The plugin requests this URL once, the first time the panel is opened after each game launch. Network failures are ignored silently and do not affect the editor. A notice is shown only when the remote version is newer than the local plugin version.

## Build

```powershell
cd E:\Code\simpleplanes2-part-editor-plugin
.\build.ps1
```

Build outputs:

```text
artifacts\SimplePlanes2PartEditor.dll
release\SimplePlanes2PartEditor-Release.zip
```

Build and install locally:

```powershell
.\build.ps1 -InstallToGame
```

## Automated Releases

The repository includes a GitHub Actions hosted build. When a `v*` tag is pushed, GitHub Actions will automatically:

- Download public BepInEx and Unity reference dependencies.
- Compile `SimplePlanes2PartEditor.dll`.
- Package `SimplePlanes2PartEditor-Release.zip`.
- Create or update the matching GitHub Release and upload the mod package.

Before publishing a new version, update:

- `PluginVersion` in [src/SimplePlanes2PartEditorPlugin.cs](src/SimplePlanes2PartEditorPlugin.cs).
- `version` and `releaseNotes` in [index.json](index.json).

Then commit and tag:

```powershell
git add .
git commit -m "Release v0.3.1"
git tag -a v0.3.1 -m "Release v0.3.1"
git push
git push origin v0.3.1
```

CI validates that the Git tag, `PluginVersion`, and `index.json.version` match. If they do not match, publishing stops before a wrong package can be released.

CI build script:

```powershell
.\build-ci.ps1
```

## Localization

Runtime localization files live under:

```text
BepInEx\plugins\SimplePlanes2PartEditor\localization\*.json
```

Runtime settings:

```text
BepInEx\plugins\SimplePlanes2PartEditor\settings.json
```

Example:

```json
{
  "language": "zh-CN",
  "toggleWindowHotkey": "F8",
  "updateCheckEnabled": true,
  "updateIndexUrl": "https://api.github.com/repos/hahaha8459812/simpleplanes2-part-editor-plugin/contents/index.json?ref=main",
  "selectionRefreshIntervalSeconds": 0.25,
  "maxMembersPerGroup": 120
}
```

To add another language, copy an existing JSON file and set `language` to the file name without `.json`.

## Technical Direction

SP2's main game code is currently in:

```text
SimplePlanes 2_Data\Managed\Game.dll
```

Confirmed designer entry point:

```text
Assets.Scripts.Design.Designer.Instance
Assets.Scripts.Design.Designer.SelectedPart
```

Phase 1 reads this path through reflection:

```text
Designer.Instance.SelectedPart
  -> PartScript
  -> PartData
  -> PartData.Modifiers
```

The plugin does not strongly reference `Game.dll` in phase 1. If the game changes internal type names, the plugin can fail softly instead of failing to load outright.
