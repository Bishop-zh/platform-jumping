# 游戏 HUD 与暂停界面

直接在 Project 的 `Assets/Prefabs/UI` 文件夹双击以下预制体：

- `GameplayHUD.prefab`：游戏进行中的血条/能量、收集计数、关卡名、时间、设置与暂停按钮等。修改 `Composition` 下的控件并保存即可，三个关卡共用该预制体。运行时数据仍由 WorldHud 更新，不会重建你的排版。
- `GameplayPauseModal.prefab`：游戏内暂停、胜利、失败弹窗。暂停内容在 `Composition/SettingsContent`；`Composition/PauseTitle` 是 TMP 的“暂停”。暂停底部依次为恢复默认、返回游戏、退出主菜单。
- `SharedModal.prefab`：游戏外设置弹窗，保留原来的设置标题和两个按钮。

GameplayHUD 从 `Prefabs/World` 移至 `Prefabs/UI`，保留原 GUID，因此引用继续有效。不要另建同名副本；运行资源库 `Assets/Resources/GameRuntimeAssets.asset` 已绑定 HUD 和独立的 GameplayPauseModal。

## 屏幕适配与手动排版

用户确认先前看不到 UI 是窗口放大导致，已撤回上一轮根据子控件边界自动缩小和重新居中的改动，恢复原来的屏幕适配方式。

双击预制体进入 Prefab Mode，在停止运行的状态下编辑并保存。也可以使用菜单 `Rustbound > UI > Edit Gameplay HUD` 和 `Edit Gameplay Pause Window`。

控件已经保存在预制体中，运行时只实例化预制体、绑定按钮事件和更新数字。修改数字整体位置请移动 BatteryCount、Fragments、Time 等父容器；其内部 Glyph 是动态数字字形，会随数据更新。

暂停的三按钮和 TMP 标题不会在打开时重新排版。滑入动画以 Composition 保存的位置为基准，不再强制归零。前两关/末关胜利按钮数量变化时的居中逻辑继续保留。

## 暂停逻辑

Esc、HUD 设置和暂停按钮均打开游戏内暂停弹窗。返回按钮恢复游戏；退出主菜单加载 MainMenu、恢复 Time.timeScale 为 1，不自动打开选关。游戏外设置仍使用 SharedModal。

中文标题及退出文字使用 TextMeshProUGUI，米白配色。字体资源位于 `Assets/Art/UI/Fonts/PauseChinese.asset`，为当前文字生成静态字形。新增其他汉字时需扩充字体图集。

本次预制体排版保留与暂停退出测试见 `Artifacts/PauseHud/editing-validation.txt`。该目录早先的边界适配截图和报告属于已经撤回的版本。

`PauseHudRevision.Run` 是资源迁移工具，会重新生成暂停预制体；手动修改预制体后不要重复运行。
