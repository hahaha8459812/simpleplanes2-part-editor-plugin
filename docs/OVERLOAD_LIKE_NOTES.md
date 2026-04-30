# Dev 应用刷新实验笔记

## 当前阶段约定

- 分支：`dev`。
- 基线：以当前 `main` 代码为基础，优先把普通 `应用` 路径做稳。
- 已删除“重载应用”路径。原因：重载零件会引入连接、碰撞器、运行时对象替换等额外风险；当前测试表明普通应用配合分流刷新更稳定。
- 当前主路径：`PartData` 使用完整刷新，`ModifierData` 使用轻量刷新。
- 首个稳定目标只处理当前选中零件，不做镜像/对称传递，避免把连接、对称、同步问题混在一起。

## 暂不处理

- 暂不自动同步对称/镜像零件。
- 暂不自动保存作品文件。
- 暂不做完整原始 XML 编辑器。

## 实现边界

- 目标是在设计器内写回当前内存对象，并尽可能让游戏运行时状态吃到这份数据。
- 失败时必须给出清晰错误信息，不能静默吞错。
- 刷新逻辑宁可保守，也不要让 `ModifierData` 修改触发 `PartData` Transform 之类高风险副作用。

## 当前刷新分流

- `PartDataFull`：目标对象就是 `PartData` 时使用。刷新内容包括 `RecalculateLoadedMass(true)`、Health 同步到 `PartScript.MaxHealth`、Position/Rotation/PartScale 同步到 `Transform`、刷新 primary/placement collider、重建 editor colliders、刷新 attach point visibility，并标记 Designer 结构变化。
- `ModifierLight`：目标对象是 `ModifierData` 时使用。刷新内容包括 `OnGenericDesignerPropertyChanged(name, value)`、`RecalculateMass(true)`，以及标记 Designer 结构变化。该路径不再同步 PartData 的 Position/Rotation/PartScale，也不重建 PartData colliders。
- 修改记录器新增 `refreshMode` 字段，用于区分每条应用记录走的是 `PartDataFull`、`ModifierLight` 还是 `Unknown`。

## 已验证现象

- 2026-04-29：单个零件在执行早期 `重载应用` 后，连接属性被保留。这个结果说明保留并恢复 `PartConnections` / `AttachPoints` 的思路对单零件基础场景有效，但后续仍决定放弃重载路径，避免引入运行时对象替换风险。
- 2026-04-29：`DesignerPropertyLabel` / `DesignerPropertyButton` 曾在重载路径中触发异常或零件消失。由于重载路径已删除，这两个标签已重新放开，当前只按反射可写性和类型转换能力判断是否可编辑。
- 2026-05-01：曾尝试让悬浮按钮只在设计器界面出现。试过场景名检测和 `Designer.Instance` active 状态检测，但从飞行界面返回设计器时按钮可能不会恢复。当前临时撤销按钮隐藏功能，相关检测代码保留，后续需要重新研究可靠的设计器/飞行界面切换信号。
- 2026-05-01：设计器相机缩放极限来自 `Assets.Scripts.Design.CameraController._maxDistance`。构造函数默认写入 `75`，`EnsureCameraIsInBounds()` 会在桌面端将限制乘以 2 后夹住相机到 `Designer.Position` 的距离。当前通过 `DesignerCameraLimitService` 反射写入 `_maxDistance`，并在设置页提供 `designerCameraMaxDistance`，默认 `500`。远距离缩小后还需要同步放宽 `_defaultFarPlane` 和 `_cameras` 中每个 `Camera.farClipPlane`，否则载具会被相机远裁剪面裁掉。
- 2026-04-29：修改生命值类属性后，当前设计器中的零件可能消失；保存作品并重新加载后零件恢复，且继承修改后的生命值。后续诊断确认问题更可能来自运行时刷新/重建流程不完整，而不是数据写入失败。
- 2026-04-29：运行时诊断确认生命值修改后 PartScript/GameObject/Transform 仍正常，但 PrimaryPartCollider、PrimaryPlacementCollider 和 EditorColliders 可能丢失。代码研究确认 `PartData.CreateGameObject()` 只执行 `PartScript.Initialize()` 和 `CreateAttachPoints()`；`PrimaryPlacementCollider` 在 `PartScript.OnStart()` 中填充，`EditorColliders` 由 `Assembly.CreateEditorCollidersForPartScript(PartScript)` 创建。
- 2026-04-30：`PartData.Health` 直接应用后可以同步到 `PartScript.MaxHealth`；面板刷新后状态一致。
- 2026-04-30：`PartData.PartScale` 默认可为空。当前完整刷新中，有值时同步到 `PartScript.transform.localScale`，清空时恢复 `Vector3.one`。
- 2026-04-30：所有零件的 `PartData` 是同一套结构，重复按零件类型测试 `PartData` 字段意义有限；后续重点转向不同 `ModifierData` 类型。
- 2026-04-30：曾使用临时自动测试工具验证导弹 `ProceduralMissileData` / `MissileData` / `CameraVantageData`：MD 反射写入、轻量刷新、恢复链路整体可用；少数 `requestedValue != afterValue` 更像游戏内部规范化或派生字段行为。该临时测试工具和修改记录器已清理。
- 2026-04-30：火炮 `CannonData._ammoCount` 测试确认：反射写入成功，XML/游戏零件配置页可见变动。此前 ModifierData 应用后火炮会移动到疑似 `0,0,0`，原因倾向于 MD 应用也触发了完整 PartData Transform/Collider 同步。改为刷新分流后，MD 只走 `ModifierLight`，复测火炮不再移动。
