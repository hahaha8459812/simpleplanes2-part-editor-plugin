# SimplePlanes 2 Part Editor Plugin

[English](README.en.md)

> **安装与管理提示**
> 本插件推荐使用 [SimplePlanes 2 Mod Manager](https://github.com/hahaha8459812/simpleplanes2-mod-manager) 进行统一安装、更新、启用、禁用和卸载。`v0.4.1` 起发布包不再内置 BepInEx，请先通过插件管理器安装 BepInEx，或自行安装 BepInEx 5 Mono x64。

这是一个独立于汉化插件的 `SimplePlanes 2` 游戏内零件数据编辑器项目。它借鉴 Overload 的"在游戏内查看和修改零件底层数据"思路，但实现路线更贴合 SP2：优先面向会保存进 XML 的 `PartData` / `PartModifierData` 数据层，而不是默认暴露整棵 Unity 运行时对象。

## 当前功能

### 基础编辑

- `F8` 或悬浮按钮打开/关闭编辑器面板（可在设置中关闭快捷键，悬浮按钮位置可锁定）。
- 在设计器内读取当前选中的零件。
- 展示当前零件名称、ID、PartType 和 PartDataType。
- 按 PartData 和各 PartModifierData 分组展示所有可反射读取的简单成员。
- 支持搜索属性名、类型、值和特性名。
- 支持复制当前零件 `GenerateXml()` 生成的 XML 到剪贴板。

### 属性说明

- 每个属性名旁有 `?` 悬浮提示，显示该属性的作用说明。
- PartData 的 41 个属性全部有中英双语说明。
- 枚举类型属性（如 `DragType`、`PartCollisionResponse`、`LoadContext`）注明了所有可取值。
- 说明跟随面板语言切换自动变化。
- 其他类型（ModifierData 等）的说明通过代码特性自动提取，未来可逐步补充。

### 写回与应用

- 输入后点击 `Apply` 写回内存对象，不做逐字符实时写入。
- PartData 修改走完整刷新（含 Transform、Collider、Mass 同步）。
- ModifierData 修改走轻量刷新（含 `OnGenericDesignerPropertyChanged` 和 `RecalculateMass`）。
- 写回失败时在面板显示错误信息。
- JFuselage 零件有专用形状参数面板，直接写入 SectionA / SectionB 截面数据。

### 自定义 XML 属性

- 支持在面板内添加/删除自定义 XML 属性到当前数据组。
- 自定义属性会出现在 `GenerateXml()` 输出中。
- 删除自定义属性即时生效。

### 设计器增强

- 可调整设计器相机最大距离（默认 500，最高 5000），方便观察大型载具。
- 自动同步相机远裁剪面，避免远距离缩小后零件不渲染。

### 面板设置（可保存）

- 字体大小（12-32）
- 面板宽度与高度
- 背景不透明度（0.65-1）
- 悬浮按钮大小（32-120）与位置锁定
- 是否显示类型列、访问列、完整类型名、运行期缓存字段
- 选中零件自动刷新间隔（0.1-5 秒）

### 更新提醒

- 打开面板时检查远端 `index.json`，发现新版本后面板标题栏显示提醒。
- 检查失败静默跳过，不影响编辑器使用。

### 本地化

- 面板内置中文/英文切换。
- 可通过 `localization/*.json` 添加其他语言。
- 属性说明和 UI 文本全部走本地化键。

## 安装

推荐使用 `SimplePlanes 2 Mod Manager` 安装：

https://github.com/hahaha8459812/simpleplanes2-mod-manager

在管理器里选择游戏目录后，可以输入本仓库地址或 `index.json` 地址安装：

```text
https://github.com/hahaha8459812/simpleplanes2-part-editor-plugin
https://raw.githubusercontent.com/hahaha8459812/simpleplanes2-part-editor-plugin/main/index.json
```

也可以在 GitHub Releases 下载最新版：

https://github.com/hahaha8459812/simpleplanes2-part-editor-plugin/releases

从 `v0.4.1` 开始，发布包不再内置 `BepInEx`。请先通过 Mod Manager 安装 BepInEx，或手动安装 `BepInEx 5 Mono x64` 后再安装本插件。

下载 `SimplePlanes2PartEditor-Plugin.zip` 后，手动安装方式是把压缩包内的 `BepInEx` 文件夹合并到游戏根目录，也就是 `SimplePlanes 2.exe` 所在目录。

安装后游戏目录应该包含：

```text
SimplePlanes 2\
├─ winhttp.dll                    ← 来自 BepInEx，不在本插件发布包内
├─ doorstop_config.ini            ← 来自 BepInEx，不在本插件发布包内
├─ BepInEx\
│  ├─ core\                       ← 来自 BepInEx，不在本插件发布包内
│  └─ plugins\
│     └─ SimplePlanes2PartEditor\
│        ├─ SimplePlanes2PartEditor.dll
│        ├─ settings.json
│        └─ localization\
│           ├─ zh-CN.json
│           └─ en-US.json
└─ SimplePlanes 2.exe
```

## 更新提醒

如果你是直接把整个 Mod 包复制到游戏根目录来更新，请先备份已有配置文件：

```text
BepInEx\plugins\SimplePlanes2PartEditor\settings.json
```

直接覆盖整个文件夹可能会把按钮位置、面板大小、语言、快捷键等个人设置重置。推荐使用 SimplePlanes 2 Mod Manager 更新；它会跳过包根目录的元数据文件，并按插件目录安装。

## 版本索引

仓库根目录的 `index.json` 同时服务于插件面板更新提醒和 SimplePlanes 2 Mod Manager 远程安装。发布新版本时必须同步更新 `version`、`fileName`、`downloadUrl`、`entryDll` 等字段：

```json
{
  "id": "SimplePlanes2PartEditor",
  "name": "SimplePlanes 2 Part Editor",
  "version": "0.4.1",
  "fileName": "SimplePlanes2PartEditor-Plugin.zip",
  "downloadUrl": "https://github.com/hahaha8459812/simpleplanes2-part-editor-plugin/releases/download/v0.4.1/SimplePlanes2PartEditor-Plugin.zip",
  "entryDll": "BepInEx/plugins/SimplePlanes2PartEditor/SimplePlanes2PartEditor.dll",
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
release\SimplePlanes2PartEditor-Plugin.zip
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
- 打包符合 Mod Manager 规范的 `SimplePlanes2PartEditor-Plugin.zip`，包内不包含 BepInEx。
- 创建或更新对应 GitHub Release，并上传 Mod 本体。

发新版前请同时更新：

- [src/SimplePlanes2PartEditorPlugin.cs](src/SimplePlanes2PartEditorPlugin.cs) 里的 `PluginVersion`。
- [index.json](index.json) 里的 `version` 和 `releaseNotes`。

然后提交并打标签：

```powershell
git add .
git commit -m "Release v0.3.6"
git tag -a v0.3.6 -m "Release v0.3.6"
git push
git push origin v0.3.6
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

语言文件是简单键值表。属性说明的键以 `desc.` 开头，例如 `desc.partData.health` 对应 PartData 的 `Health` 属性说明。自定义类型说明需要在 `InspectableMemberDescriptionProvider` 中注册映射。

## 属性说明系统

编辑器为每个属性提供悬浮提示（`?`），说明来源有三层优先级：

1. **代码特性**：如果属性标注了 `[Description]`、`[Tooltip]` 或 `[tooltip]` 等字符串特性，直接使用其内容。
2. **自定义映射**：`InspectableMemberDescriptionProvider` 中注册的键值对，键为 `类型名.属性名` 或 `*.属性名`（通配所有类型），值以 `@` 开头的是本地化键。
3. **无说明**：以上两层都没有时，不显示 `?` 图标。

当前 PartData 的全部 41 个属性已注册说明映射，中英双语完整覆盖。ModifierData 和 JFuselage 专用属性的说明将逐步补充。

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

本项目通过反射读取：

```text
Designer.Instance.SelectedPart
  -> PartScript
  -> PartData
  -> PartData.Modifiers
```

不强引用 `Game.dll`，这样即使游戏更新导致类型变化，插件也更容易以"提示未检测到设计器"的方式失败，而不是直接加载失败。

## 写回与应用机制

写回链路：

```text
用户在面板输入新值
  -> ValueConverter.TryConvert 校验与类型转换
  -> InspectableMember.TryApply 通过反射写入
  -> PartRuntimeRefreshService 根据目标对象类型分流刷新
  -> PartData 目标：完整刷新（Transform、Collider、Mass 等）
  -> ModifierData 目标：轻量刷新（OnGenericDesignerPropertyChanged + RecalculateMass）
  -> 面板刷新显示新值
```

当前暂不处理：

- 对称/镜像零件自动同步。
- 自动保存作品文件。
- DesignerPropertyLabel / DesignerPropertyButton 标记为只读（已实现为不可编辑字段）。
- 完整原始 XML 编辑器。

## 项目原则

- 默认安全，危险功能显式隔离。
- 优先数据层，不优先运行时 Unity 对象。
- 先验证读取链路，再做写回链路。
- UI 文本全部走本地化接口。
- 不和汉化插件共享发版和运行状态。
