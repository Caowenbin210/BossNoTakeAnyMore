# 老板，真塞不下了！

Unity 2022.3.45f1c1 · Android 竖屏整理游戏 · 前 3 关灰盒原型。

## 运行

1. 用对应版本 Unity 打开项目，等待导入和编译。
2. 打开 `Assets/_Game/Scenes/MainMenu.unity`，按 Play，再点击“开始装车”。
3. Game 视图推荐选择 9:16 比例并放大。横屏窗口会居中显示竖屏内容。
4. 将待装区货物拖到车厢；点击货物后按“旋转”顺时针转向。非法松手回到原位；已装货物可重放或拖回待装区。
5. 全部装入后点击下一单，第 3 关后可以从头重玩。支持暂停、确认重置和失焦暂停。

界面和 EventSystem 已保存在场景中，编辑状态即可查看与修改。`Warehouse.unity` 可以单独打开调试。布局、字体、区域与控件引用的修改方法见 [场景编辑说明](Docs/SceneEditing.md)。旧 SampleScene 不再作为入口。

## 配置与验证

关卡设计源：`Docs/Data/PrototypeLevels.json`。修改后运行：

```powershell
python Tools/verify_level_design.py
python Tools/sync_level_data.py
./Tools/check_csharp.ps1
```

最后一条使用本机 Unity 自带 Mono/Roslyn 运行纯 C# 规则检查并编译运行时代码；其他 Unity 安装位置可以传入 `-EditorRoot`。输出位于被忽略的 `Temp/PrototypeChecks`。它不代替 Unity 场景和真机测试。

Warehouse 场景的 Play 模式下菜单 `Warehouse → Run Pointer Smoke Checks` 可自动检查三关指针处理、旋转按钮和完成判定，之后恢复第 1 关。该检查会重置当前试玩进度，不代替真实触屏验证。

## Android

通过 File → Build Settings 确认 `MainMenu` 为第一个启用场景、`Warehouse` 为第二个，选择 Android 并切换平台。使用 Unity Hub 安装的 SDK/NDK/JDK；在 Player Settings 配置自己的包名，并设置默认方向 Portrait。开发测试使用 Development Build，输出到 `Builds/Android`。运行时也会请求竖屏。当前没有代替用户确定正式包名、签名或发行主体。

项目已附带 Noto Sans CJK SC Regular 和 OFL 许可证，两个场景直接引用字体资产。Android 字体显示与安全区仍须真机验证。资源来源见 [授权记录](Docs/AssetLicenses.md)。

## 当前边界

目前有三关、占位色块、程序短音效、落地轻弹/光圈、装车区域轻震和完成印章。暂停界面可关闭音效与震动；偏好本地保存。资源位置和调节方法见 [反馈说明](Docs/FeedbackDesign.md)。未接入广告、关卡进度存档、工资经济、正式美术与平台服务；应用重启从第 1 关开始。APK 打包和真机测试结果见 `Docs/TestChecklist.md`，不得把桌面验证视为安卓验收。

设计、操作与开发标准在 `Docs`。实现说明见 `Docs/TechnicalDesign.md`。
