---
name: mmorpg-skills-index
description: Route to the correct MMORPG project knowledge skill before starting any development task in this repository. Maps task types (architecture, Summer framework, server dev patterns, game logic, protocols/database, git commits) to six specialized skills and defines load order for cross-domain tasks. Use when beginning work on this MMORPG codebase to determine which knowledge skill to load, when unsure which specialized skill applies, or when a task spans multiple domains (任务路由, 项目知识索引, 该看哪个skill, 跨模块任务).
---

# MMORPG 项目 Skill 索引（任务路由入口）

本项目 `.qoder/skills/` 下共 7 个 skill（含本索引）。接到开发任务后先查本表确定要加载的专项 skill，再动手。

## 路由表

| 任务类型 | 加载 skill | 典型任务 |
|---|---|---|
| 架构/数据流/节点职责 | mmorpg-architecture | 登录流程怎么走、某功能在哪台服务器、新增服务器节点规划、端口配置 |
| Summer 框架 | mmorpg-summer-framework | 改网络层、消息路由、Scheduler、安全模块、工具类 |
| 服务器编码模式 | mmorpg-server-dev-patterns | 新增 Handler、新增服务器、启动流程、编码规范、config.yaml |
| 游戏业务逻辑 | mmorpg-game-logic | 背包/任务/聊天/战斗/Buff/怪物AI/AOI/技能 |
| 协议与数据库 | mmorpg-protocol-db | 新增 proto、协议号、DBProxy/MongoDB、序列化排查 |
| 提交代码 | mmorpg-git-commit | 写 commit message、提交改动 |

## 路由规则

1. **单域任务**：按上表直接加载对应 skill
2. **跨域任务**：按依赖顺序依次加载，标准顺序：
   - 先 `mmorpg-architecture` 定位功能归属（哪台服务器、哪个目录）
   - 再 `mmorpg-server-dev-patterns` 确定实现模式（Handler/启动流程）
   - 最后按需 `mmorpg-protocol-db`（涉及新协议）或 `mmorpg-game-logic`（业务细节）
3. **拿不准归属**：加载 `mmorpg-architecture`，读其「快速定位指南」节
4. **触碰 `MMORPG/Common/Summer/` 框架代码**：必读 `mmorpg-summer-framework` 的「修改框架的注意事项」

## 典型任务组合示例

- 「新增背包物品类型」：`mmorpg-game-logic`（物品模型 + InventoryManager 分支）→ `mmorpg-protocol-db`（Backpack.proto）
- 「新增服务器节点」：`mmorpg-architecture`（职责/端口/链路）→ `mmorpg-server-dev-patterns`（接入 8 步）→ 涉及新协议时加 `mmorpg-protocol-db`
- 「修消息解析失败 bug」：`mmorpg-protocol-db`（帧格式/Register 排查）→ `mmorpg-summer-framework`（ProtoHelper/MessageRouter 机制）
- 「实现聊天私聊频道」：`mmorpg-game-logic`（ChatHandler 现状与占位约定）→ `mmorpg-protocol-db`（Chat.proto）→ `mmorpg-server-dev-patterns`（Handler 模板）
- 「新怪物 AI 状态」：`mmorpg-game-logic`（状态机 + AI 目录）→ `mmorpg-summer-framework`（StateMachine 工具类）

## 注意

- 本索引是**路由入口，不含具体知识**；每个专项 skill 的 description 已带精准触发词，单域任务会被直接匹配激活，此时不必先读本索引
- 专项 skill 文件位于 `.qoder/skills/<name>/SKILL.md`，用 Skill 工具加载；需要对照原文时也可直接 Read 该文件
- 索引与 6 个专项 skill 内容冲突时，以专项 skill 为准（本索引只是导航）
