# NiumaInteract

## 模块定位
NiumaInteract 是通用交互模块，负责检测可交互目标、输入快照、候选黑板、仲裁评分、短按/长按裁决、提示 UI 数据和交互触发。

## 框架设计思路
- 输入层只输出 Pressed / Released / IsHolding / HoldTime，不计算目标相关进度。
- 检测层只输出几何事实，如距离、朝向、命中来源，不决定最终目标。
- 仲裁器负责候选评分、焦点选择、Gate 查询、短按/长按触发和冷却。
- IInteractable 只表达目标能力和执行入口，不耦合 UI、任务或背包。

## 核心流程
1. InputSource 每帧采集交互输入。
2. Detector 收集范围或射线候选。
3. Blackboard 保存候选、焦点、输入快照和最近结果。
4. Arbiter 按距离、朝向、优先级、锁定目标选择 CurrentTarget。
5. UI 根据 CurrentTarget 与 HoldTime 显示提示和长按进度。
6. 输入满足条件后调用目标 Interact。

## 模块用法
- 可交互物体优先挂现有交互脚本：拾取物挂 `InventoryPickupInteractable`，测试拾取物挂 `PickupInteractable`，对话 NPC 挂 `NiumaDialogueInteractable`，商店入口挂 `ShopInteractable`，合成台挂 `CraftStationInteractable`。只有这些组件覆盖不了需求时，再由程序新增自定义交互脚本。
- 目标必须提供稳定 Transform/Anchor 供检测与提示定位。
- 长按进度由 UI 用 HoldTime / CurrentTarget.LongPressDuration 计算。

## 场景使用方法
推荐放置方式：`PlayerRoot/InteractionRoot` 承载玩家交互功能集，目标物体各自挂交互脚本。

- `PlayerRoot/InteractionRoot`：挂 `NiumaInteractionController`，绑定输入源、DetectorGroup、PromptBridge、GateProvider。
- `PlayerRoot/InteractionRoot/Detectors`：挂 `InteractionDetectorGroup`，子物体或同物体挂 `SphereInteractionDetector`、`RaycastInteractionDetector`。
- `PlayerRoot/InteractionRoot/PromptBridge`：挂 `InteractionPromptBridge`，把黑板焦点转成 UI 提示数据。
- `UIRoot/InteractionPrompt`：挂 `SimpleInteractionPromptSink` 或团队自定义提示 Receiver。
- `Pickup_xxx`：拾取物挂 `InventoryPickupInteractable`；测试可用 `PickupInteractable`。
- `NPC_xxx`：对话 NPC 挂 `NiumaDialogueInteractable`。
- `Shop_xxx`：商店入口挂 `ShopInteractable`。
- `CraftStation_xxx`：合成台挂 `CraftStationInteractable`。
- `Puzzle_xxx`：解密入口可挂 `PuzzleStartInteractable` 或自定义 IInteractable。
- 复杂目标建议把 Collider 放子物体，IInteractable 挂父物体，检测层通过父级查找目标。

## 协作边界
Interact 不负责背包入库、商店交易、对话播放等具体业务。拾取物、NPC、商店入口通过自己的 Interactable 组件把交互转发到对应模块。


