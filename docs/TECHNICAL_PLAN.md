# SP2 Part Editor 技术规划

## 设计结论

本项目做 SP2 专用的数据层编辑器，不直接照搬 Overload 的全量内存对象编辑器路线。

优先编辑对象：

- PartData
- PartModifierData

暂不默认编辑对象：

- PartScript
- PartModifierScript
- Transform
- GameObject
- Collider
- Rigidbody
- 其他 Unity runtime/cache/event 对象

原因：PartData.GenerateXml() 与 PartModifierData.GenerateStateXml() 更接近游戏真实保存路径，风险低、可验证、可长期维护。

## 已确认入口

核心程序集：

    SimplePlanes 2_Data\Managed\Game.dll

设计器入口：

    Assets.Scripts.Design.Designer.Instance
    Assets.Scripts.Design.Designer.SelectedPart

选中零件链路：

    Designer.Instance.SelectedPart
      -> PartScript.Part
      -> PartData.Modifiers

## 当前工程状态

### 已完成

- 只读反射浏览所有 PartData / ModifierData 可显示成员。
- 属性说明系统：41 个 PartData 属性有中英双语悬浮说明，枚举值可取范围已标注。
- IMGUI 面板：搜索、分组、可折叠专用面板、大文本编辑器。
- 自定义 XML 属性添加/删除。
- 写回与分流刷新：PartData 走完整刷新，ModifierData 走轻量刷新。
- JFuselage 专用形状参数面板。
- 设计器相机缩放解除（可调最大距离，同步远裁剪面）。
- 悬浮按钮（可拖动、可锁定位置、可调大小）。
- 快捷键开关面板（F8 可在设置中禁用）。
- 面板设置持久化（字体、大小、透明度、刷新间隔等）。
- 中英双语切换，JSON 本地化接口可扩展其他语言。
- 远端版本更新提醒。
- PartData 全属性实时修改（含 PartScale 非等比缩放）。
- DesignerPropertyLabel / DesignerPropertyButton 标记为只读。

### 进行中

- ModifierData 专用属性说明逐步补充。
- JFuselage 专用面板属性说明标注。
- 悬浮按钮在设计器外自动隐藏（当前暂时禁用，等待更可靠的界面状态检测）。

### 暂不处理

- 暂不自动同步对称/镜像零件。
- 暂不自动保存作品文件。
- 暂不做完整原始 XML 编辑器。

## 写回与应用机制

写回链路：

    用户在面板输入新值
      -> ValueConverter.TryConvert 校验与类型转换
      -> InspectableMember.TryApply 通过反射写入
      -> PartRuntimeRefreshService 根据目标对象类型分流刷新
      -> PartData 目标：完整刷新（Transform、Collider、Mass 等）
      -> ModifierData 目标：轻量刷新（OnGenericDesignerPropertyChanged + RecalculateMass）
      -> 面板刷新显示新值

刷新分流细节：

- PartDataFull：RecalculateLoadedMass(true)、Health 同步到 PartScript.MaxHealth、Position/Rotation/PartScale 同步到 Transform、刷新 primary/placement collider、重建 editor colliders、刷新 attach point visibility、标记 Designer 结构变化。
- ModifierLight：OnGenericDesignerPropertyChanged(name, value)、RecalculateMass(true)、标记 Designer 结构变化。不再同步 PartData 的 Transform 或 Collider。

## 属性说明系统

编辑器为每个属性提供悬浮提示（?），说明来源有三层优先级：

1. 代码特性：如果属性标注了 [Description]、[Tooltip] 或 [tooltip] 等字符串特性，直接使用其内容。
2. 自定义映射：InspectableMemberDescriptionProvider 中注册的键值对，键为 TypeName.MemberName 或 *.MemberName（通配所有类型），值以 @ 开头的是本地化键。
3. 无说明：以上两层都没有时，不显示 ? 图标。

当前 PartData 的全部 41 个属性已注册说明映射，中英双语完整覆盖。

枚举类型说明包含可取值：

- DragType: Default / Standard / OccludeOnly / None
- PartCollisionResponse: Default / None / DisconnectOnly
- LoadContext: Default / Menu / Designer / Flight / Studio

## 本地化接口

运行时语言文件：

    content/localization/zh-CN.json
    content/localization/en-US.json

运行时目录：

    BepInEx/plugins/SimplePlanes2PartEditor/localization/*.json

其他语言只需要新增同结构 JSON 文件，并修改 settings.json 的 language。

## 发版形态

Release 包结构：

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

插件不依赖汉化项目，可以单独安装、运行和卸载。

## Dev 应用刷新实验笔记

详见 docs/OVERLOAD_LIKE_NOTES.md。
