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
- `UIRoot/UIBridges/InteractionPromptToolkitSink`：正式 UI Toolkit 方案挂 `InteractionPromptToolkitSink`，并拖给 `InteractionPromptBridge.Prompt Sink Provider`。旧 `SimpleInteractionPromptSink` 只用于历史测试场景，不再作为新场景配置入口。
- `Pickup_xxx`：拾取物挂 `InventoryPickupInteractable`；测试可用 `PickupInteractable`。
- `NPC_xxx`：对话 NPC 挂 `NiumaDialogueInteractable`。
- `Shop_xxx`：商店入口挂 `ShopInteractable`。
- `CraftStation_xxx`：合成台挂 `CraftStationInteractable`。
- `Puzzle_xxx`：解密入口可挂 `PuzzleStartInteractable` 或自定义 IInteractable。
- 复杂目标建议把 Collider 放子物体，IInteractable 挂父物体，检测层通过父级查找目标。

## 协作边界
Interact 不负责背包入库、商店交易、对话播放等具体业务。拾取物、NPC、商店入口通过自己的 Interactable 组件把交互转发到对应模块。

## 场景挂载与 Inspector 配置
### NiumaInteractionController
建议挂载位置：`PlayerRoot/InteractionRoot`。

用途：统一处理玩家附近/远距离交互检测、焦点仲裁、短按/长按输入和提示输出。

| 字段 | 怎么填 | 可否留空 | 不填会怎样 |
| --- | --- | --- | --- |
| `Input Source Provider` | 拖交互输入脚本 | 不可以 | 没有输入，无法触发交互 |
| `Detector Providers` | 拖范围检测、射线检测等检测器 | 不可以 | 找不到可交互目标 |
| `Prompt Sink Provider` | 正式场景拖 `InteractionPromptToolkitSink`；旧测试场景才可拖 `SimpleInteractionPromptSink` | 可以 | 交互仍可用，但没有提示 UI |
| `Gate Provider` | 剧情/菜单阻塞交互时拖 Gate 脚本 | 可以 | 留空时默认不阻塞交互 |
| `Auto Tick` | 没有统一模块启动器时开启 | 按项目决定 | 外部已 Tick 时再开启会重复检测 |

### SphereInteractionDetector / RaycastInteractionDetector
建议挂载位置：`PlayerRoot/InteractionRoot/Detectors`。

| 字段 | 怎么填 | 可否留空 | 不填会怎样 |
| --- | --- | --- | --- |
| `Origin Transform` | 拖玩家或相机 Transform | 不建议 | 自动使用当前物体，检测位置可能不准 |
| `Layer Mask` | 只勾交互物层 | 不可以 | 默认全层会检测地形/玩家/特效，性能和结果都不稳定 |
| `Radius / Distance` | 按交互距离填写 | 不可以 | 太小检测不到，太大误选远处物体 |
| `View Camera` | 射线检测拖玩家相机 | 射线检测不建议留空 | 为空时射线方向可能不符合视角 |

### 可交互物脚本
建议挂载位置：NPC、拾取物、商店入口、机关物体。

| 字段 | 怎么填 | 可否留空 | 不填会怎样 |
| --- | --- | --- | --- |
| `Interaction Id` | 填稳定 ID，例如 `herb_001`、`npc_blacksmith` | 不建议 | 任务/存档/调试难以追踪 |
| `Display Name` | 填 UI 提示名称 | 可以 | UI 可能显示默认名或空 |
| `Prompt Anchor` | 拖提示锚点 Transform | 可以 | 留空时使用物体自身位置 |
| `Interact Kind` | 按需求选短按/长按 | 不可以 | 配错会导致提示和输入不一致 |
| `Priority` | 常用目标设高一点 | 可以 | 默认优先级可能被其它目标抢焦点 |

### UI Toolkit 交互提示接入

`InteractionPromptToolkitSink` 位于 `NiumaInteract.ToolkitBridge`，负责把交互提示数据转成 NiumaUI Toolkit 的 `InteractionPrompt` ViewData。NiumaUI 只保留纯显示 Binding，不再直接引用 NiumaInteract。

推荐挂载：

```text
CoreScene
└── UIRoot
    ├── UIManager
    │   ├── UIToolkitUIManager
    │   └── UIToolkitViewFactory
    └── UIBridges
        └── InteractionPromptToolkitSink
```

配置步骤：

1. 在 `UIToolkitViewRegistrySO` 中注册 `InteractionPrompt`。
2. `Layer Id` 建议填 `Prompt`。
3. `Binding Provider Id` 填 `InteractionPrompt`。
4. `Input Policy` 选 `None`，交互提示只显示，不阻塞玩家输入。
5. 在 `UIToolkitViewFactory.Binding Provider Behaviours` 中拖入 `InteractionPromptToolkitBindingProvider`。
6. 在 `PlayerRoot/InteractionRoot/PromptBridge` 的 `Prompt Sink Provider` 字段拖入 `InteractionPromptToolkitSink`。

`InteractionPromptToolkitSink` 字段说明：

| 字段 | 怎么填 | 可否留空 | 不填会怎样 |
| --- | --- | --- | --- |
| `UI Manager` | 拖核心场景 `UIRoot/UIManager` 上的 `UIToolkitUIManager` | 可以 | 开启自动查找时会尝试找；找不到则不显示提示 |
| `Auto Find UI Manager` | 测试场景可开，正式场景建议手动绑定后关闭 | 可以 | 关闭且未绑定时不显示提示 |
| `View Id` | 默认 `InteractionPrompt` | 可以 | 空值会回退到 `InteractionPrompt` |
| `Interact Key Label` | 填显示给玩家看的按键名，例如 `E`、`F`、`鼠标左键` | 可以 | 使用默认 `E` |
| `Text Format` | `{0}=按键名，{1}=提示文本，{2}=目标名` | 可以 | 使用默认 `[{0}] {1} {2}` |
| `Close View When Empty` | 建议开启 | 可以 | 关闭后无交互目标时不会主动关闭提示 View |

`InteractionPromptToolkitBindingProvider` 在 NiumaUI 模块中，负责把 ViewData 写入 UXML。常用元素 name：

- `PromptText`：完整提示文本 Label。
- `TargetName`：目标名称 Label，可留空。
- `HoldProgress`：长按进度 ProgressBar，可留空。
- `ProgressFill`：自定义进度填充 VisualElement，可留空。


