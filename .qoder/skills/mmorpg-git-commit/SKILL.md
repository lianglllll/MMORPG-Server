---
name: mmorpg-git-commit
description: Generate git commit messages following the MMORPG project's lightweight Conventional Commits convention: type(scope) prefix with Chinese descriptions, scopes mapped to server modules (cc, dbproxy, login, game, scene, timer, summer, proto). Use when the user asks to write a commit message, commit changes, or generate a commit based on git diffs in this repository (提交, commit message, 写commit, 生成提交信息).
---

# MMORPG 项目 Git Commit 生成

个人项目的轻量化 Conventional Commits 变体。分析 git diff 后按本格式生成 commit message。

## 格式

```
<type>(<scope>): <中文简述>
```

- 一行搞定，默认不写 body/footer；除非 diff 中存在需要解释"为什么"的非显然改动，才追加空行 + 简短 body
- 描述用中文（与项目注释语言一致），动词开头，简洁描述"做了什么"
- 单次 commit 只包含一类改动；若 diff 混杂多种改动，提示用户拆分或选主导类型

## type 选择

| type | 用途 | 判定线索 |
|---|---|---|
| `feat` | 新功能 | 新增协议、Handler、Manager、条件/奖励类、AI 状态、Buff/技能 |
| `fix` | 修 bug | 逻辑错误、空引用、协议号不匹配、数据校验 |
| `refactor` | 重构 | 单体→分布式迁移、目录调整、不改变行为的代码重写 |
| `perf` | 性能优化 | AOI、消息分发、序列化、锁/线程模型相关优化 |
| `docs` | 文档 | README、注释补充、skill 文件 |
| `chore` | 杂项 | proto 重新生成、config.yaml、启动脚本、csproj 依赖 |
| `test` | 测试 | 测试代码 |

## scope 选择（按改动文件路径）

| scope | 对应路径 |
|---|---|
| `cc` | `MMORPG/ControlCenter/` |
| `dbproxy` | `MMORPG/DBProxyServer/` |
| `login` | `MMORPG/LoginServer/`、`LoginGateServer/`、`LoginGateMgrServer/` |
| `game` | `MMORPG/GameServer/`、`GameGateServer/`、`GameGateMgrServer/` |
| `scene` | `MMORPG/SceneServer/` |
| `timer` | `MMORPG/MasterTimerServer/`、`SlaveTimerServer/` |
| `summer` | `MMORPG/Common/Summer/`（框架改动，谨慎标注） |
| `proto` | `MMORPG/Common/Summer/Proto/ProtoSource/` |

规则：
- 改动涉及多个服务器时，选主导模块，或用 `login`/`game` 这类链路级 scope；跨域大改动省略 scope
- 拿不准就省略 scope：`feat: xxx`

## 生成流程

1. 运行 `git diff --stat` 与 `git diff` 查看改动内容
2. 按文件路径映射 scope（跨目录时取主导模块）
3. 按改动性质选 type（判定线索见上表）
4. 用中文写一句动词开头的简述，控制在 50 字内

## 示例

**输入**：在 SceneServer 的 `Core/Combat/AI/Boss/Impl/` 新增 `BossMonsterAIState_Flee.cs` 并在 AI 类接入状态切换
**输出**：
```
feat(scene): Boss AI 新增逃跑状态
```

**输入**：GameServer 的 `InventoryManager.cs` 修复物品堆叠数量校验逻辑
**输出**：
```
fix(game): 修复背包物品堆叠数量校验错误
```

**输入**：LoginServer 的登录流程从单体架构迁移到分布式（通过 ControlCenter 注册）
**输出**：
```
refactor(login): 登录流程迁移到分布式架构
```

**输入**：`Common/Summer/Proto/Chat/ProtoSource/Chat.proto` 修改后重新生成 ProtoClass
**输出**：
```
chore(proto): 重新生成 Chat 协议类
```

**输入**：更新 README 架构图与服务器说明
**输出**：
```
docs: 更新服务器架构图
```

## 注意

- 只生成 message 文本，**不主动执行 git commit**；除非用户明确要求提交
- 严格遵守 Git 安全协议：不改 git config、不跳过 hooks、不做破坏性操作
- 生成前检查改动是否包含本 skill 约定外的文件（如 pids.txt、bin/、obj/），这些不应被提交，提醒用户确认 .gitignore
