# 场景与 UI 编辑说明

## 场景入口

- `Assets/_Game/Scenes/MainMenu.unity`：主菜单，包含开始装车、玩法说明和静态标题。
- `Assets/_Game/Scenes/Warehouse.unity`：完整装车界面，可单独打开并 Play 调试三关。
- Build Settings 中 MainMenu 为第一个启用场景，Warehouse 为第二个。旧 SampleScene 保留但不参与构建，也不再自动生成 UI。

## 如何修改

打开场景，展开 `Canvas/SafeArea`，使用 Scene 的 2D 模式或 Game 预览查看。Game 建议设为 9:16；在 Scene 选中 Canvas 或 SafeArea 按 F 可以定位 UI。

| 修改项 | 对象或组件 |
| --- | --- |
| 主菜单标题、说明、按钮文字 | MainMenu 的 GameTitle、Tagline、StartButton/Text、HelpButton/Text 等 Text 组件 |
| 游戏标题、提示、进度、货物名称、底部按钮 | Warehouse 的 Canvas/SafeArea 下对应命名对象 |
| 字号、字体、颜色、对齐 | 对应 Text 组件 |
| 按钮位置、尺寸和配色 | 对应 Button 对象的 RectTransform 与 Image |
| 车厢位置与最大区域大小 | CargoSurface/BoardArea 的 RectTransform，推荐保持正方形，代表最大 4×4 容量 |
| 待装卡片位置与尺寸 | CargoSurface/CargoSlot_A～E 的 RectTransform |
| 拖回待装区的判定范围 | CargoSurface/TrayArea 的 RectTransform，应覆盖全部待装槽位 |
| 货物、格子和槽位颜色、触摸抬升距离 | CargoSurface 组件的序列化字段 |
| 暂停和结算弹层 | PauseAndResultPanel，编辑时可临时启用；运行时由流程控制 |
| 玩法说明弹层 | MainMenu 的 HelpPanel，编辑时可临时启用 |
| 控件引用 | WarehouseView、WarehouseController、MainMenuController 的 Inspector |
| 音效、轻弹、震动强度与完成延迟 | Assets/_Game/Settings/WarehouseFeedback.asset |
| 完成印章与反馈层 | Canvas/SafeArea/CompletionStamp、PlacementEffects |
| 音效和震动开关布局 | PauseAndResultPanel 下 SoundToggle 与 MotionToggle |

车厢格线、货物和落点预览仍由 CargoSurface 自绘；位置来自场景中的区域与槽位，编辑状态预览第 1 关。单件货物的形状仍由关卡配置决定，不把每个占格拆成独立 Image。

SafeArea 在编辑状态居中展示设计画布；运行时 SafeAreaFrame 仅缩放和移动根画布以适配安全区，不重新创建子对象。不要在 Play 时修改后期待自动保存，停止后修改并保存场景。

## 静态与动态内容

游戏静态标题、按钮文字、层级与布局由场景保存。订单标题、教学提示、进度、选中说明、货物名称和结算文字属于动态内容，运行时仍由关卡流程更新。编辑 Text 的字体、位置、字号会保留；修改其预览文字不等于修改关卡配置或动态文案。

按钮事件通过控制器在初始化时注册，Inspector 的 OnClick 留空是正常的。不需要再次手动绑定，否则可能重复执行。运行时不生成额外 Canvas、按钮或 EventSystem。

返回主菜单会丢弃当前订单中的摆放，重新开始从第 1 关进入；目前没有存档。暂停与结算弹层都有返回主菜单入口，完成状态隐藏无实际作用的取消按钮。

## 字体

两个场景使用项目附带的 Noto Sans CJK SC Regular，字体和许可证位于 `Assets/_Game/Fonts`，不再依赖运行时查找 Windows 字体。来源见 [资源授权记录](AssetLicenses.md)。完整字体约 16 MB，后续可在确认文案后评估裁剪；安卓实际显示仍需真机验收。

## 编辑器工具

可通过 `Warehouse/Open Main Menu Scene` 与 `Warehouse/Open Warehouse Scene` 快速打开场景并定位 Canvas；存在未保存场景修改时工具会停止，避免丢失编辑。

- `Warehouse/Create Initial Authored Scenes` 仅用于首次生成场景；检测到现有场景会拒绝覆盖，不能用它重新生成你修改后的 UI。
- `Warehouse/Run Pointer Smoke Checks`：在 Warehouse 的 Play 模式检查三关，再恢复第 1 关。
- `Warehouse/Run Scene Flow Checks`：在 MainMenu 的 Play 模式检查帮助、开始游戏、三关、暂停和返回主菜单。会重置试玩状态。
- `Warehouse/Apply Bundled Font To Authored Scenes`：将两个场景的 Text 统一设为附带字体；通常不需要再执行，且会替换自定义字体。当前场景有未保存修改时会拒绝操作。
