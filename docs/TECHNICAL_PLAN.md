# SP2 Part Editor 技术规划

## 设计结论

本项目不直接照搬 Overload 的“全量内存对象编辑器”路线，而是做 SP2 专用的数据层编辑器。

优先编辑对象：

```text
PartData
PartModifierData
```

暂不默认编辑对象：

```text
PartScript
PartModifierScript
Transform
GameObject
Collider
Rigidbody
其他 Unity runtime/cache/event 对象
```

原因：`PartData.GenerateXml()` 与 `PartModifierData.GenerateStateXml()` 更接近游戏真实保存路径，风险低、可验证、可长期维护。

## 已确认入口

核心程序集：

```text
SimplePlanes 2_Data\Managed\Game.dll
```

设计器入口：

```text
Assets.Scripts.Design.Designer.Instance
Assets.Scripts.Design.Designer.SelectedPart
```

选中零件链路：

```text
Designer.Instance.SelectedPart
  -> PartScript.Part
  -> PartData.Modifiers
```

## 当前工程阶段

### Phase 1：只读验证

目标：验证插件和数据链路。

已落地模块：

- `SimplePlanes2PartEditorPlugin`：BepInEx 插件入口、快捷键、生命周期。
- `DesignerSelectionService`：反射读取设计器当前选中零件。
- `ReflectionMemberScanner`：扫描可展示的简单成员。
- `ImguiPartEditorWindow`：IMGUI 面板。
- `LocalizationProvider`：JSON 本地化加载。
- `PluginSettings`：运行时设置。

当前只读展示，不写回游戏对象。

## Phase 2：安全写回

写回入口只允许来自 UI 的显式 Apply，不允许输入框逐字符写回。

支持类型优先级：

1. `bool`
2. `int` / `long`
3. `float` / `double`
4. `string`
5. `enum`
6. `Vector2` / `Vector3` / `Vector4`
7. 简单数组和 `List<int>` / `List<float>` / `List<string>`

写回流程：

```text
读取原值
校验输入
尝试创建 UndoStep
SetValue 写回
尝试触发通用刷新
刷新快照
失败则显示错误并保留原值
```

Undo 候选入口：

```text
Designer.CreateUndoStepForSelectedPart(propertyName)
```

通用刷新候选：

```text
PartData.RecalculateLoadedMass(...)
PartModifierData.OnGenericDesignerPropertyChanging(...)
PartModifierData.OnGenericDesignerPropertyChanged(...)
PartModifierData.OnGenericDesignerPropertiesUpdate(...)
```

## Phase 3：SP2 原生属性体验

利用游戏已有属性标记排序和辅助显示：

```text
DesignerPropertyAttribute
DesignerPropertySliderAttribute
DesignerPropertyTextInputAttribute
DesignerPropertyToggleButtonAttribute
DesignerPropertyVectorAttribute
DesignerPropertySpinnerAttribute
DesignerPropertyColorAttribute
```

目标：

- 原生设计器属性优先显示。
- XML 相关属性其次。
- 危险 runtime 字段放入独立高级区。
- 增加搜索、收藏、最近修改、复制 XML。

## Phase 4：高级模式

高级能力必须显式开启：

- 原始 XML 编辑。
- 危险 runtime 字段编辑。
- 对称零件同步。
- 属性预设导入/导出。

## 本地化接口

UI 文本全部使用键值表：

```text
content/localization/zh-CN.json
content/localization/en-US.json
```

运行时目录：

```text
BepInEx/plugins/SimplePlanes2PartEditor/localization/*.json
```

其他语言只需要新增同结构 JSON 文件，并修改 `settings.json` 的 `language`。

## 发版形态

Release 包结构：

```text
SimplePlanes2PartEditor-Release.zip
  BepInEx/
    plugins/
      SimplePlanes2PartEditor/
        SimplePlanes2PartEditor.dll
        settings.json
        localization/
          zh-CN.json
          en-US.json
  install.ps1
  README.md
  README.en.md
```

插件不依赖汉化项目，可以单独安装、运行和卸载。
