# 已有验证记录

这些文件从本地 `Artifacts/` 中的开发验证输出复制而来，作为 README 技术说明的补充证据。本次整理仓库没有重新执行 Unity 验证；结果仅代表记录生成时的分项检查，不代表当前版本的三关连续通关或完整性能验收。

| 文件 | 覆盖内容 | 原始位置 |
| --- | --- | --- |
| [ground-probe.txt](ground-probe.txt) | 角色缩放、碰撞箱和落地探测 | `Artifacts/World/ground-probe-validation.txt` |
| [level-layout.txt](level-layout.txt) | 三关布局与运行绑定 | `Artifacts/World/layout-validation.txt` |
| [level-runtime.txt](level-runtime.txt) | 地图运行时分项检查 | `Artifacts/World/layout-runtime-validation.txt` |
| [physics.txt](physics.txt) | 跳跃、移动平台等物理行为 | `Artifacts/World/physics-validation.txt` |
| [runtime-ui.txt](runtime-ui.txt) | 入口、导航、共享 HUD 和界面适配 | `Artifacts/Handover/runtime-validation.txt` |
| [interactions.txt](interactions.txt) | 箱子、压板、交互提示和能量 | `Artifacts/Handover/interaction-validation.txt` |
| [background.txt](background.txt) | 纵向背景与视口覆盖 | `Artifacts/UIBackdrop/backdrop-validation.txt` |
| [audio.txt](audio.txt) | 音频、音量与设置 | `Artifacts/Audio/runtime-validation.txt` |

对应验证代码位于 `Assets/Editor/World/`、`Assets/Editor/UI/` 等目录。运行前请查看各脚本入口；生成类工具可能修改场景或预制体，不应为试玩游戏而批量运行。
