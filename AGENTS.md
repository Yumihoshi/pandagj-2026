# AGENTS.md

Godot 4.7.2 (mono) + C# 的 48 小时 Game Jam 项目：2D 时间循环（loop）叙事游戏。

## 策划案（唯一需求来源）

- 飞书 wiki：<https://my.feishu.cn/wiki/IjAAwgxwviZZHrkELxVcACSjnEg> —— 用户会持续更新，动手实现前先拉最新：
  `lark-cli docs +fetch --doc "https://my.feishu.cn/wiki/IjAAwgxwviZZHrkELxVcACSjnEg" --as user --doc-format markdown`
- 「头脑风暴 / 参考游戏 / 美术参考」三块是画板，正文只返回 `<whiteboard token=...>`；要看内容用 lark-whiteboard skill 导出：
  `lark-cli whiteboard +export --whiteboard-token <token> --output-type raw --output ./wb.json`（`--output` 只接受 cwd 相对路径）
- 已定设定：JK 主角、现代日本校园；定时炸弹末日 + 幕后黑手；平视 2D，A/D 左右走、F 交互，部分交互切到第一人称鼠标点击；到点触发大事件后重置循环，NPC 在各时间点的行为固定。
- 定位：点击式步行模拟器 + 知识锁推进（靠每轮累积的线索拼出真相）。**不做平台跳跃**。
- 参考：命运石之门、梅祖拉的假面、十三机兵防卫圈、星际拓荒、人狼村之谜；美术走十三机兵的 JK 与 galgame 对话框。

## 工具链

- 引擎：`D:\Downloads\Compressed\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe`（项目 features=4.7；C# 目标框架 net8.0，本机 .NET SDK 10 + 运行时 8 构建正常）。同目录 `_console.exe` 便于在终端看输出。
- 校验命令（仓库根目录执行）：
  - 导入资源：`& $godot --headless --path . --import`
  - 编译 C#：`& $godot --headless --path . --build-solutions --quit`（或 `dotnet build`）
  - 无头跑主场景并立刻退出：`& $godot --headless --path . --quit-after 120`
- `*.csproj` / `*.sln` 由 Godot 首次创建 C# 脚本时生成；在生成之前 `dotnet build` 没有可构建项目。
- 主场景要在 项目设置 → 运行 → 主场景 指定，否则无头运行直接报 `no main scene defined`。
- godot MCP 已配在 `opencode.json`（`mcp.servers.godot`，源码构建于 `%USERPROFILE%\.local\share\godot-mcp`，`GODOT_PATH` 指向上面的 exe）。优先用它做反馈闭环：`launch_editor`、`run_project` + `get_debug_output`、`stop_project`；项目相关工具要传绝对 `projectPath`。场景用 `create_scene` / `add_node` / `save_scene` 生成，手写 `.tscn` 容易破坏资源引用。

## 开发约定（48h jam）

- 先做能玩的最小闭环，再谈表现。**不要过度设计**：不引入 DI / 事件总线 / 通用框架 / 单元测试框架，不做为「以后扩展」预留的分层。
- 只实现用户当轮要求，不顺手重构、不主动扩需求。
- 脚本一律 C#：`.cs` 文件名与类名一致，`public partial class X : Node`。不要写 GDScript。
- 空引用、资源缺失要显式报错，禁止用默认值或空实现静默兜底。
- git 只读：禁止 commit / branch / checkout / push（用户明确要求）。
- 这是 2D 项目，但 `project.godot` 仍是 Forward Plus + d3d12、拉伸 `canvas_items/expand`；没有明确需要不要改渲染设置。
