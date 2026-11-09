# 今天也准时爆炸

**Panda Jam 2026 参赛作品 · 八岐大蛇**

> 千原鸣子从课桌上睡醒以后度过了一个平静的下午，就在她即将踏出校门时，轰———
>
> 巨大的爆炸声与蘑菇云打破了放学的热闹，回过神来时鸣子回到了刚从课桌上睡醒时的教室，时针奇迹般的重新指向下午一点，鸣子可以在放学到来前阻止这场危机吗？

[![itch.io](https://img.shields.io/badge/itch.io-Download-fa5c5c?style=for-the-badge)](https://yumihoshi.itch.io/today-booom)
[![GitHub](https://img.shields.io/badge/GitHub-pandagj--2026-181717?style=for-the-badge&logo=github&logoColor=white)](https://github.com/Yumihoshi/pandagj-2026)
[![Godot](https://img.shields.io/badge/Godot-4.7.2%20mono-478cbf?style=for-the-badge&logo=godotengine&logoColor=white)](https://godotengine.org/)
[![C#](https://img.shields.io/badge/C%23-.NET%208-512bd4?style=for-the-badge&logo=csharp&logoColor=white)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green?style=for-the-badge)](LICENSE)

![今天也准时爆炸](docs/itch-cover.png)

## 游戏简介

《今天也准时爆炸》是一款以现代日本校园为舞台的 **2D 时间循环叙事游戏**：视觉小说式的对话、点击式步行模拟器的探索，加上「用信息解锁路线」的知识锁推进。

下午一点，鸣子在教室醒来；五点半，炸弹准时引爆。爆炸把她送回 13:00，只有她保留着上一轮的记忆。玩家需要在一次次循环中探索校园、与 NPC 交谈、收集线索与「重要的事」，在 17:30 前拼出真相，阻止这场危机。

- **类型**：时间循环 / 叙事冒险 / 视觉小说 / 点击式步行模拟器
- **平台**：Windows（x86_64）
- **语言**：简体中文
- **状态**：原型 / Jam 版本
- **操作**：键鼠（A/D 移动、F 交互、鼠标推进对话、F11 全屏）

## 特色

- **17:30 的倒计时**：游戏内时间从 13:00 走到 17:30。开门换房间、在地图间旅行都会消耗时间；到点后炸弹引爆，循环重置。
- **知识锁推进**：线索、「重要的事」与手机记事本跨循环保留。记住时间、地点和人物，才能解锁新的地点、对话与路线。
- **NPC 日程**：小优香、山田老师、新井次郎在不同时间出现在不同地点；随着真相推进，他们的状态也会变化。
- **校园探索**：教室、走廊、隔壁班、化学实验室、操场、操场后、器材准备室、地下空间、行政楼（入口 / 办公室）。
- **手机系统**：查看「重要的事」、记笔记，或者玩手机消磨时间。
- **多周目与真结局**：反复循环、拼出真相，才能抵达真正的结局。
- **没有平台跳跃**：左右走、按 F 交互，把注意力留给对话与推理。

## 操作

| 操作 | 按键 |
| --- | --- |
| 左右移动 | `A` / `D` |
| 交互 | `F` |
| 推进对话 / 选择选项 | 鼠标左键 |
| 跳过打字 | `Esc` |
| 关闭手机界面 | `Esc` |
| 全屏 / 窗口切换 | `F11` |

> 对话推进只使用鼠标左键；空格和回车不会推进下一句。

## 下载 / 游玩

- **itch.io（推荐）**：<https://yumihoshi.itch.io/today-booom>
- 当前包体：`TodayBooom-v1.0-fix.zip`（Windows，约 161 MB），解压后运行 `TodayBooom.exe`。
- 也可以在 itch 页面查看截图、更新与评论。

## 制作名单

**八岐大蛇**

| 分工 | 成员 |
| --- | --- |
| 程序 | 绘星（Yumihoshi） |
| 策划 | 六花 |
| 美术 | 小鱼 |
| AI 全栈 | 毛毛雨 |

### 使用素材 / 工具

- **引擎**：[Godot Engine 4.7.2 (.NET)](https://godotengine.org/) — MIT
- **对话系统**：[Dialogue Manager 4.2.0](https://github.com/nathanhoad/godot_dialogue_manager) by Nathan Hoad — MIT
- **UI 音效**：[Kenney UI Audio](https://kenney.nl/assets/ui-audio) — CC0
- **BGM 素材**：[甘茶の音楽工房](https://amachamusic.chagasi.com/) — 《静止的宇宙》《开开心心啦啦啦》《梦》《浅浅的月光》《暑假大冒险》《夏天的雾》《雨的恋人们》
- **封面插图**：八岐大蛇

## AI 使用说明

本作在开发过程中使用 AI 辅助（代码与部分图形），与 itch.io 页面上的 AI Disclosure 一致。

## 开发

- **引擎**：Godot 4.7.2（.NET / C#，`net8.0`）
- **主场景**：`Scenes/Root/MainMenu.tscn`
- **运行**：用 Godot 打开 `project.godot`，按 F5
- **编译**：`dotnet build --nologo`
- **导入资源**：`godot --headless --path . --import`
- **导出**：`export_presets.cfg` 已配置 Windows Desktop

### 目录结构

| 路径 | 内容 |
| --- | --- |
| `Scenes/` | 主菜单、游戏主场景、地点、地图、UI |
| `Scripts/` | C# 游戏逻辑（Core / Map / Location / Player / Npc / Dialogue / Phone / Audio / UI） |
| `Dialogue/` | Dialogue Manager 剧本 |
| `Resources/` | 地图配置与「重要的事」注册表 |
| `ArtRes/` | 角色、背景、UI 美术 |
| `Audio/` | BGM 与音效 |
| `docs/` | 设计与实现文档 |

## 文档

- [飞书策划案](https://my.feishu.cn/wiki/IjAAwgxwviZZHrkELxVcACSjnEg)
- [地图 / 场景系统设计](docs/superpowers/specs/2026-11-06-map-scene-system-design.md)
- [时间系统设计](docs/superpowers/specs/2026-11-07-time-system-design.md)
- [正式主流程设计](docs/superpowers/specs/2026-11-07-formal-main-flow-design.md)
- [项目开发约定](AGENTS.md)

## 许可

- 代码采用 **MIT** 许可，见 [LICENSE](LICENSE)（Copyright © 2026 绘星tsuki）。
- 美术、音乐、剧本等创作素材版权归八岐大蛇 / 各自作者所有，未经许可请勿单独复用。
- 第三方素材遵循各自许可，见「使用素材 / 工具」。

---

[![猫猫计数器](https://starry-trace-sky-moe-counter.vercel.app/get/@pandagj-2026?theme=rule34)](https://github.com/Yumihoshi/pandagj-2026)
