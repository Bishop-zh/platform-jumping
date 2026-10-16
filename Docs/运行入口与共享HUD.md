# 直接运行与共享 GameplayHUD

已完成运行时绑定，正常使用无需再执行生成工具。

- 测试完整游戏：打开 `Assets/Scenes/MainMenu.unity`，点击 Unity Play，再点击“开始游戏”或从选关进入关卡。
- 测试刚编辑的地图：保存 `Level01_Map`、`Level02_Map` 或 `Level03_Map`，打开对应 `Assets/Scenes/Level01.unity`、`Level02.unity`、`Level03.unity`，直接 Play，立即控制当前关卡角色。
- 没有玩家或关卡系统的普通入口场景：自动创建首页。Unity 的 Prefab Mode 用于编辑，关卡运行验收请打开对应场景。

## UI 独立于地图

唯一的共享界面模板是 `Assets/Prefabs/UI/GameplayHUD.prefab`。你改过的模板没有重建，文件字节校验与改动前一致。三个地图内原先嵌入的 HUD 副本已移除，LevelSession 的 Hud 引用在编辑状态为空属于正常情况。

进入关卡后，LevelSession 从 `Assets/Resources/GameRuntimeAssets.asset` 获取模板，在场景根节点创建 GameplayHUD，绑定当前关卡。切换场景时销毁该实例，不会保存回地图。场景已有独立 WorldHud 时使用该实例，避免重复创建。

电量、水晶比例、时间、死亡数、关卡数字和冲刺冷却动态刷新，数字沿用 SpriteNumberLabel 图集。WorldHud.levelName 是显示关卡名的 Image；levelNames 依次配置三关的名称图片，Bind 会依据 LevelSession.level 替换 sprite，不改变其 RectTransform 尺寸、位置或 preserveAspect。未来替换关卡名图片只需更新这个数组。

Settings/Pause 绑定当前 LevelSession 的设置/暂停处理；多次绑定会先移除本脚本自己的监听，不会清除你在 Inspector 设置的其他事件。未使用的统计栏位允许不赋值，不会导致运行报错。

## 全屏适配

运行时 FullScreenCanvas 以 Composition 的设计尺寸为参考，控件整体等比适配屏幕，保持控件之间的相对位置和尺寸比例；只有装饰背景单独等比铺满，超出屏幕的装饰边缘会裁切。首页、选关背景覆盖全屏，设置/胜利/失败的遮罩覆盖全屏，中央弹窗保持原排版。不同宽高比下，既要控件完整可见又不变形，需要背景和控件分别适配。

GameplayHUD 同样保持你的排版，CanvasScaler 的适配由运行脚本处理，不修改模板文件。游戏相机按屏幕比例扩展可见范围，取消固定 16:9 黑边；不同屏幕会看到更多横向或纵向世界内容。地图边缘需要铺足背景，扩大的可见范围也需检查，不应拿黑边掩盖未搭建区域。

原本相机整数放大的规则已更新为全屏优先；世界资产仍是 32 PPU、Point，但部分屏幕尺寸下显示放大倍率会为小数。碰撞与移动距离没有改变。

## 本轮验证

`Artifacts/Handover/runtime-validation.txt` 包含运行导航、空入口启动、直接测试关卡、唯一根节点 HUD、排版保留、三关名称图片切换、全屏视口，以及 16:9、16:10、超宽和 4:3 的背景覆盖/控件容纳比例检查。多比例检查验证缩放算法，仍建议在 Unity Game View 切换分辨率检查实际素材边缘。

不要运行 WorldBuilder 或 UI Builder 来更新数据，它们会重建素材排版。运行只依赖已有模板和运行脚本。RuntimeSetup 是首次迁移/修复引用的维护工具，正常 Play 不执行它。

