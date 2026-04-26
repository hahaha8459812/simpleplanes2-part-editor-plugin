# SimplePlanes 2 Part Editor Plugin

[English](README.en.md)

这是一个独立于汉化插件的 `SimplePlanes 2` 游戏内零件数据编辑器项目。它借鉴 Overload 的“在游戏内查看和修改零件底层数据”思路，但实现路线会更贴合 SP2：优先面向会保存进 XML 的 `PartData` / `PartModifierData` 数据层，而不是默认暴露整棵 Unity 运行时对象。

## 项目定位

- 独立运行，不依赖 SimplePlanes 2 本地化插件。
- 基于 `BepInEx 5 Mono` 和 Unity IMGUI。
- 写回通过 `Apply / Reset` 控制，避免输入时立刻修改游戏数据。
- 优先展示数据层：`Designer.SelectedPart -> PartScript -> PartData -> Modifiers`。
- 插件 UI 内置中文/英文切换，并提供 JSON 本地化接口。

## 安装

普通测试者请在 GitHub Releases 下载最新版：

https://github.com/hahaha8459812/simpleplanes2-part-editor-plugin/releases

发布包已经内置 `BepInEx 5 Mono x64`，不需要另外下载 BepInEx。

下载 `SimplePlanes2PartEditor-Release.zip` 后：

1. 关闭 `SimplePlanes 2`。
2. 解压压缩包。
3. 把压缩包里的内容放入 `SimplePlanes 2` 游戏根目录，也就是 `SimplePlanes 2.exe` 所在目录。
4. 启动游戏，进入设计器。
5. 选中一个零件，按 `F8` 打开面板。

如果你只拿到了解压后的 Release 包，也可以在包根目录运行：

```powershell
.\install.ps1
```

如果游戏不在默认路径：

```powershell
.\install.ps1 -GameDir "D:\SteamLibrary\steamapps\common\SimplePlanes 2"
```

安装后游戏目录应该包含：

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

## 更新提醒

如果你是直接把整个 Mod 包复制到游戏根目录来更新，请先备份已有配置文件：

```text
BepInEx\plugins\SimplePlanes2PartEditor\settings.json
```

直接覆盖整个文件夹可能会把按钮位置、面板大小、语言、快捷键等个人设置重置。推荐使用 `install.ps1` 或 `build.ps1 -InstallToGame` 更新，它们会尽量保留已有 `settings.json`。

## 版本索引

仓库根目录的 `index.json` 是给插件检查更新用的轻量索引，只保留版本号和发布更新内容：

```json
{
  "version": "0.3.0",
  "releaseNotes": "Release notes here."
}
```

发布新版本时同步更新这个文件，并把它的 raw 地址填入插件设置：

```json
{
  "updateCheckEnabled": true,
  "updateIndexUrl": "https://api.github.com/repos/hahaha8459812/simpleplanes2-part-editor-plugin/contents/index.json?ref=main"
}
```

插件会在每次启动游戏后第一次打开面板时请求一次该地址。请求失败会静默跳过，不影响编辑器使用；只有远端版本比本地插件版本新时，才会在面板内显示更新提醒。

## 构建

```powershell
cd E:\Code\simpleplanes2-part-editor-plugin
.\build.ps1
```

构建产物：

```text
artifacts\SimplePlanes2PartEditor.dll
release\SimplePlanes2PartEditor-Release.zip
```

直接构建并安装到本机游戏：

```powershell
.\build.ps1 -InstallToGame
```

默认游戏目录：

```text
E:\Game\steam\steamapps\common\SimplePlanes 2
```

## 自动发版

仓库已配置 GitHub Actions 托管编译。推送 `v*` 标签时，云端会自动：

- 下载公开的 BepInEx 和 Unity 引用依赖。
- 编译 `SimplePlanes2PartEditor.dll`。
- 打包 `SimplePlanes2PartEditor-Release.zip`。
- 创建或更新对应 GitHub Release，并上传 Mod 本体。

发新版前请同时更新：

- [src/SimplePlanes2PartEditorPlugin.cs](src/SimplePlanes2PartEditorPlugin.cs) 里的 `PluginVersion`。
- [index.json](index.json) 里的 `version` 和 `releaseNotes`。

然后提交并打标签：

```powershell
git add .
git commit -m "Release v0.3.1"
git tag -a v0.3.1 -m "Release v0.3.1"
git push
git push origin v0.3.1
```

CI 会校验 tag、`PluginVersion`、`index.json.version` 三者一致。不一致时会停止发版，避免发错包。

CI 专用构建脚本：

```powershell
.\build-ci.ps1
```

## 本地化接口

运行时语言文件位于：

```text
BepInEx\plugins\SimplePlanes2PartEditor\localization\*.json
```

设置文件：

```text
BepInEx\plugins\SimplePlanes2PartEditor\settings.json
```

示例：

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

新增语言时复制一个现有 JSON 文件，例如：

```text
localization\en-US.json -> localization\ja-JP.json
```

然后把 `settings.json` 里的 `language` 改为对应文件名：

```json
{
  "language": "ja-JP"
}
```

语言文件是简单键值表：

```json
{
  "window.title": "SP2 零件数据编辑器",
  "button.close": "关闭"
}
```

## 技术路线

SP2 当前核心代码位于：

```text
SimplePlanes 2_Data\Managed\Game.dll
```

已确认的设计器入口：

```text
Assets.Scripts.Design.Designer.Instance
Assets.Scripts.Design.Designer.SelectedPart
```

本项目第一阶段通过反射读取：

```text
Designer.Instance.SelectedPart
  -> PartScript
  -> PartData
  -> PartData.Modifiers
```

第一阶段不强引用 `Game.dll`，这样即使游戏更新导致类型变化，插件也更容易以“提示未检测到设计器”的方式失败，而不是直接加载失败。

## 后续阶段

### 阶段 2：安全编辑

- 支持 `int`、`float`、`bool`、`string`、`enum`、`Vector2/3/4` 和简单列表编辑。
- 输入后点击 `Apply` 再写回，不做逐字符实时写入。
- 写回前尝试调用游戏的 Undo 逻辑。
- 字段旁提供 `Reset`。
- 写回失败时在 UI 显示错误。

### 阶段 3：SP2 化体验

- 按 `PartData` 与 modifier 分组。
- 优先排序带 `DesignerPropertyAttribute` 的属性。
- 显示当前属性是否会进入 XML。
- 支持收藏、最近修改和属性预设。

### 阶段 4：高级模式

- 原始 XML 编辑。
- 危险 runtime 字段单独隔离。
- 对称零件同步修改。
- 导出/导入属性预设。

## 项目原则

- 默认安全，危险功能显式隔离。
- 优先数据层，不优先运行时 Unity 对象。
- 先验证读取链路，再做写回链路。
- UI 文本全部走本地化接口。
- 不和汉化插件共享发版和运行状态。
