# 操作与完成反馈

版本：0.1 · 2026-11-03。适用于当前三关，独立于占格规则。

## 资源位置

| 目录 / 文件 | 内容 |
| --- | --- |
| `Assets/_Game/Audio/SFX/` | 项目自制的六个 WAV 短音效及 SFXManifest.json |
| `Assets/_Game/Settings/WarehouseFeedback.asset` | 音效引用、总音量、吸附时间、弹性和震动强度、结算延迟 |
| `Assets/_Game/Scripts/UI/FeedbackProfile.cs` | 反馈配置类型，只存配置 |
| `Assets/_Game/Scripts/UI/WarehouseFeedback.cs` | 声音、轻震、光圈与完成印章的呈现编排 |
| `Assets/_Game/Scripts/UI/FeedbackOverlay.cs` | UGUI 光圈和小亮片绘制，无输入拦截 |
| `Tools/generate_sfx.py` | 确定性程序音效生成器，无外部依赖和第三方采样 |

声音为风格化电子/木质提示音，不是拟真仓库录音。统一为 44.1 kHz、单声道、16 位 PCM；短音效在 Unity 使用 PCM / Decompress On Load。六段音频合计约 162 KB，生成器保留来源与峰值/RMS记录。

| 文件名 | 时长 | 用途 |
| --- | --- | --- |
| cargo_pickup.wav | 0.11 秒 | 真正开始拖动时的拿起声，单纯点选不播放 |
| cargo_rotate.wav | 0.18 秒 | 成功旋转 |
| cargo_place.wav | 0.22 秒 | 成功放进车厢，低频轻落箱加安静的确认音 |
| cargo_invalid.wav | 0.24 秒 | 非法放置或旋转失败，柔和下行提示 |
| cargo_return.wav | 0.14 秒 | 放回待装区 |
| level_complete.wav | 0.95 秒 | 完成订单的上行短乐句 |

## 行为

- 成功放置：先提交合法位置，再用约 0.18 秒贴合到目标位置并轻弹；落点有扩散光圈和亮片；装车区域轻震约 0.16 秒。
- 非法放置：正式布局保持原状，货物回到原位置并短暂偏红；显示越界、重叠或禁用格的对应提示；不进行屏幕震动。
- 重新拿起：立即结束该货物上一次落地动画，命中仍依据正式位置，不需要等动画播完。
- 最后一件装入：立即锁定通关，不再接收棋盘编辑；保留整车布局。约 0.12 秒后播放完成短乐句、光圈和“装车完成”印章，再经过配置中的 0.85 秒显示结算。
- 失败反馈只在明确的松手或旋转失败时播放，不在拖动经过非法格时反复响。
- 暂停、重置、切关和退出：清理轻弹、亮片、印章和旧音频，终止旧结算协程。

逻辑坐标、碰撞、货物朝向和输入矩形均不随震动改变。震动只偏移 CargoSurface 的渲染顶点，标题与按钮保持稳定；不调用手机振动，也不修改 Camera Transform。

## 修改方法

选择 `WarehouseFeedback.asset` 调整参数或替换剪辑。默认总音量 0.65，放置震幅为 2.5 个设计像素，通关为 5 个设计像素。参数是初始值，音量与手感仍应根据手机试听调整。

Warehouse 场景根对象 `WarehouseFeedback` 保存音频源及配置引用。`Canvas/SafeArea/PlacementEffects` 是落点特效层；`CompletionStamp` 是可编辑的完成印章。无需运行时创建对象。

暂停/结算弹层新增“音效”和“震动 / 弹性”开关。关闭音效会立即停止当前声音；关闭震动/弹性会清除正在播放的落地动画和震动，保留静态高亮与轻量光圈提示。两项选择通过 PlayerPrefs 保存，尚不包含关卡进度存档。

可在 Unity Project 中选中 WAV 使用 Inspector 预听。替换剪辑时保留短促、头尾平滑的声音，避免连续装货时叠成噪声。运行生成器会覆盖这六个程序音效，手工替换资源建议另起文件名并在配置中引用。

## 验证入口

```powershell
python Tools/generate_sfx.py
./Tools/check_csharp.ps1
```

Warehouse Play 模式的 `Warehouse/Run Feedback Checks` 验证静音、播放启动、非法回退、重新抓取、延迟结算、输入矩形不位移和暂停/切关清理，并恢复测试前的音效/震动偏好。它不代替真实扬声器试听、安卓性能与触屏验收。

`Warehouse/Install Operation Feedback` 是一次性场景迁移工具；已有反馈引用时保留现场，不覆盖现有反馈调节，也不重建原有 UI。
