# 原型技术实现方案

版本 0.1 · 2026-11-03

## 模块

- `Core/GridPosition`：整数坐标值。
- `Core/CargoState`：独立货物实例，缓存四种归一化朝向，外部只读。
- `Core/BoardState`：唯一权威布局、占格判定、原子放置、旋转和重置。当前格子规模很小，按货物占格遍历，避免引入第二份容易失配的占格缓存；判定暂保留为该类内无副作用的 `Check` 方法。
- `Gameplay/LevelController`：显式 Playing/Paused/Completed 状态、选中、拖动事务与一次性完成事件，不依赖 Unity。
- `Data/LevelCatalog`：读取生成后的资源 JSON，转换为独立运行状态，并使用实际 C# 放置规则检查参考解。
- `UI/CargoSurface`：自绘 UGUI 格子、货物与候选预览，处理指针命中、坐标转换和输入转发。
- `UI/PrototypeView`：绑定场景中的按钮和文本，管理暂停/重置/结算状态显示及可取消的结算展示协程；不在运行时创建 UI。
- `UI/FeedbackProfile`：音效与反馈参数资产；`WarehouseFeedback`：组合 AudioSource、货物动画和反馈层，不修改棋盘模型；`FeedbackOverlay`：光圈和亮片绘制。
- `Bootstrap/PrototypeBootstrap`：Warehouse 场景中的入口组件，通过 Inspector 引用视图，连接关卡数据与控制器。

Core 和 Gameplay 不反向引用 UI。UI 通过控制器执行动作，通过 Changed 刷新。纯数据 DTO 的公开字段仅服务 JsonUtility 序列化，不作为可变业务状态共享。

场景迁移补充：MainMenu 与 Warehouse 均保存 Canvas、EventSystem、文字、按钮及弹层。MainMenuController 负责主菜单操作和场景切换；SafeAreaFrame 负责运行时根画布适配；CargoSurface 读取场景 RectTransform，并在编辑状态提供第 1 关预览。详细层级见 [场景编辑说明](SceneEditing.md)。

## 数据

设计源保持在 Docs/Data。Python 同步工具将字典和坐标数组转为 JsonUtility 可解析的字段，提交生成后的 Resources JSON，用户打开项目不需要先安装 Python。原型明确选择 JSON 配置，暂不制作 ScriptableObject 编辑器，避免维护两套手工数据；扩关时可增加资产导入工具。

## 拖动事务

拖动期间不修改正式布局，验证移动货物时排除它自己的原占格，UI 隐藏它的正式图形。单指所有权与控制器禁用其他修改，确保原位置不会被其他操作占用。合法松手一次提交；取消、非法松手、暂停直接丢弃预览，原状态天然保留。

此方案替代操作文档中“临时释放占格再回滚”的内部实现，玩家行为相同；不复制位置、朝向和占格快照。完成只检查成功操作后的正式布局，拖动期间不检查完成。

## 界面线框

720×1280 设计坐标，自下向上：按钮 30～90；选中说明约 103；待装货物两行约 175～523；货物名称分别在槽位下方；进度约 540；提示约 590；棋盘起点 Y=650，每格 108；订单与标题在 1090 以上。

Canvas 使用屏幕覆盖模式，内容按安全区等比缩放并居中。弹层为模态遮罩，阻断底层输入。所有货物开局可见；初版没有滚动待装区。

## 尚待完善

已加入程序短音效、吸附轻弹、仅渲染的震动和完成反馈，见 [反馈说明](FeedbackDesign.md)。正式美术、分阶段教学、关卡进度存档和广告仍未实现；仅音效/震动偏好已本地保存。当前预览主要使用颜色与描边，后续应加强有效/无效符号差异。UI 指针取消与移动端系统行为仍需真机检查。
