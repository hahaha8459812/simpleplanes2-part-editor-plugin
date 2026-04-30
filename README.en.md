# SimplePlanes 2 Part Editor Plugin

[中文](README.md)

This is an in-game part data editor for SimplePlanes 2, independent from the localization plugin. It takes inspiration from Overload-style editing, but the implementation is tailored for SP2: the default target is the XML-backed PartData / PartModifierData data layer instead of the full Unity runtime object graph.

## Current Features

### Basic Editing

- Press F8 or click the floating button to toggle the editor panel (hotkey can be disabled in settings, button position can be locked).
- Reads the currently selected part inside the designer.
- Shows part name, ID, PartType, and PartDataType.
- Groups all reflected members by PartData and each PartModifierData.
- Search by property name, type, value, or attribute name.
- Copy the current part GenerateXml() output to clipboard.

### Property Descriptions

- Each property name has a ? badge that shows a tooltip with usage description.
- All 41 PartData properties have Chinese and English descriptions.
- Enum-type properties (e.g. DragType, PartCollisionResponse, LoadContext) list all valid values.
- Descriptions automatically follow the panel language setting.
- Other types (ModifierData, etc.) extract descriptions from code attributes, with more to be added over time.

### Write-back and Apply

- Click Apply to write modified values back to the in-memory object (no per-keystroke live write).
- PartData changes go through a full refresh (Transform, Collider, Mass sync included).
- ModifierData changes go through a lightweight refresh (OnGenericDesignerPropertyChanged + RecalculateMass).
- Apply failures are shown as error messages in the panel.
- JFuselage parts have a dedicated shape parameter panel that writes directly to SectionA / SectionB data.

### Custom XML Attributes

- Add or remove custom XML attributes on the current data group from within the panel.
- Custom attributes appear in GenerateXml() output.
- Removing a custom attribute takes effect immediately.

### Designer Enhancements

- Adjustable designer camera max distance (default 500, max 5000) for viewing large crafts.
- Automatically syncs camera far clip plane to avoid parts disappearing at far zoom.

### Panel Settings (persistent)

- Font size (12-32)
- Panel width and height
- Background opacity (0.65-1)
- Floating button size (32-120) and position lock
- Show/hide type column, access column, full type names, runtime cache fields
- Auto-refresh interval for selected part (0.1-5 seconds)

### Update Checker

- Checks remote index.json when the panel is first opened and shows a notice when a newer version is available.
- Network failures are silent and do not affect the editor.

### Localization

- Built-in Chinese/English UI switching.
- Add other languages via localization/*.json files.
- Property descriptions and UI text all go through localization keys.

## Install

Download the latest version from GitHub Releases:

https://github.com/hahaha8459812/simpleplanes2-part-editor-plugin/releases

The release package already includes BepInEx 5 Mono x64. You do not need to download BepInEx separately.

After downloading SimplePlanes2PartEditor-Release.zip:

1. Close SimplePlanes 2.
2. Extract the zip.
3. Place all extracted contents into the game root, the folder containing SimplePlanes 2.exe.
4. Start the game and enter the designer.
5. Select a part and press F8 or click the floating button.

You can also run the installer from the extracted release package:

    .\install.ps1

Custom game directory:

    .\install.ps1 -GameDir "D:\SteamLibrary\steamapps\common\SimplePlanes 2"

Manual install: if you already have BepInEx 5 Mono x64 set up, copy these files into the matching directories:

    BepInEx\plugins\SimplePlanes2PartEditor\SimplePlanes2PartEditor.dll
    BepInEx\plugins\SimplePlanes2PartEditor\settings.json
    BepInEx\plugins\SimplePlanes2PartEditor\localization\zh-CN.json
    BepInEx\plugins\SimplePlanes2PartEditor\localization\en-US.json

## Update Notice

If you update by manually copying the whole mod package into the game root, back up your existing settings first:

    BepInEx\plugins\SimplePlanes2PartEditor\settings.json

Overwriting the whole folder may reset personal settings such as the floating button position, panel size, language, and hotkey. Using install.ps1 or build.ps1 -InstallToGame is recommended because they preserve an existing settings.json when possible.

## Version Index

The repository root index.json is a lightweight update index used by the plugin:

    {"version": "0.3.5", "releaseNotes": "Release notes here."}

When publishing a new version, update this file and set its raw URL in the plugin settings:

    {"updateCheckEnabled": true, "updateIndexUrl": "https://api.github.com/repos/hahaha8459812/simpleplanes2-part-editor-plugin/contents/index.json?ref=main"}

The plugin requests this URL once the first time the panel is opened after each game launch. Network failures are silent; a notice appears only when the remote version is newer.

## Build

    cd E:\Code\simpleplanes2-part-editor-plugin
    .\build.ps1

Build outputs:

    artifacts\SimplePlanes2PartEditor.dll
    release\SimplePlanes2PartEditor-Release.zip

Build and install locally:

    .\build.ps1 -InstallToGame

## Automated Releases

The repository includes a GitHub Actions hosted build. When a v* tag is pushed, GitHub Actions will automatically:

- Download public BepInEx and Unity reference dependencies.
- Compile SimplePlanes2PartEditor.dll.
- Package SimplePlanes2PartEditor-Release.zip.
- Create or update the matching GitHub Release and upload the mod package.

Before publishing a new version, update:

- PluginVersion in src/SimplePlanes2PartEditorPlugin.cs.
- version and releaseNotes in index.json.

Then commit and tag:

    git add .
    git commit -m "Release v0.3.6"
    git tag -a v0.3.6 -m "Release v0.3.6"
    git push
    git push origin v0.3.6

CI validates that the Git tag, PluginVersion, and index.json.version match. If they do not match, publishing stops.

CI build script:

    .\build-ci.ps1

## Localization

Runtime localization files:

    BepInEx\plugins\SimplePlanes2PartEditor\localization\*.json

Runtime settings:

    BepInEx\plugins\SimplePlanes2PartEditor\settings.json

To add another language, copy an existing JSON file and set language to the file name without .json.

Language files are simple key-value maps. Property description keys start with desc., for example desc.partData.health maps to the PartData Health property description. Custom type descriptions require registering a mapping in InspectableMemberDescriptionProvider.

## Property Description System

The editor provides hover tooltips (?) for each property. Description sources have three priority levels:

1. Code attributes: if a property has Description, Tooltip, or tooltip string attributes, those are used directly.
2. Custom mappings: entries registered in InspectableMemberDescriptionProvider, using TypeName.MemberName or *.MemberName (wildcard for all types) as keys, with @-prefixed values resolved through the localization system.
3. No description: if neither source provides a description, the ? badge is hidden.

All 41 PartData properties currently have description mappings, fully covered in Chinese and English. ModifierData and JFuselage-specific property descriptions will be added incrementally.

## Technical Direction

SP2 core game code is in:

    SimplePlanes 2_Data\Managed\Game.dll

Confirmed designer entry point:

    Assets.Scripts.Design.Designer.Instance
    Assets.Scripts.Design.Designer.SelectedPart

The plugin reads through reflection:

    Designer.Instance.SelectedPart
      -> PartScript
      -> PartData
      -> PartData.Modifiers

No strong reference to Game.dll. If the game changes internal type names, the plugin can fail softly instead of failing to load.

## Write-back and Apply Mechanism

Write-back pipeline:

    User enters new value in panel
      -> ValueConverter.TryConvert validates and type-converts
      -> InspectableMember.TryApply writes via reflection
      -> PartRuntimeRefreshService dispatches refresh based on target type
      -> PartData target: full refresh (Transform, Collider, Mass, etc.)
      -> ModifierData target: lightweight refresh (OnGenericDesignerPropertyChanged + RecalculateMass)
      -> Panel refreshes to show new value

Not yet implemented:

- Symmetry/mirror part auto-sync.
- Auto-save craft file.
- DesignerPropertyLabel / DesignerPropertyButton already handled as non-editable.
- Full raw XML editor.

## Project Principles

- Safe defaults, dangerous features explicitly opt-in.
- Data layer first, runtime Unity objects second.
- Read path validated before write path.
- All UI text goes through localization.
- Independent from the localization plugin for release and runtime.
