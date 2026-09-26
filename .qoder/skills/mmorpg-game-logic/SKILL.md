---
name: mmorpg-game-logic
description: Develop game business logic for the MMORPG servers. Documents GameServer systems (character model, backpack/inventory, chat channels, task system with strategy-pattern conditions and rewards), SceneServer systems (AOI cross-linked list, combat skills/buffs/attributes, state-machine monster AI, scene component managers), and how GameGate routes messages between Game and Scene. Use when modifying or adding game features like items, tasks, chat, combat, buffs, monster AI, AOI, or movement logic (背包, 任务, 战斗, Buff, 怪物AI, AOI, 聊天, 技能).
---

# MMORPG 游戏业务逻辑开发

游戏逻辑分两处：**GameServer**（与场景无关的静态数据）、**SceneServer**（场景内动态数据）。改动前先确认功能归属，改动跨服务器功能时两端 Handler 都要看。

## GameServer（`MMORPG/GameServer/`）

### 目录与职责

```
core/
├── GameMonitor.cs                  # 服务器内实例监控（场景/网关上下线处理）
├── Model/
│   ├── GameCharacter.cs            # 角色实体：会话绑定、CurSceneId、SendToGate 转发
│   ├── GameCharacterManager.cs     # 角色管理器：按 cid/sceneId 索引角色
│   └── BaseItem/                   # 物品模型：GameItem 基类 + GameConsumable/GameEquipment/GameMaterialItem
├── Backpack/
│   ├── InventoryManager.cs         # 背包逻辑（507 行，核心）
│   └── EquipmentManager.cs         # 装备栏逻辑
├── Chat/ChatManager.cs             # 频道消息管理
└── Task/
    ├── GameTask.cs                 # 任务实体（580 行）
    ├── GameTaskManager.cs          # 任务生命周期管理（333 行）
    ├── Condition/                  # 任务条件：IConditionChecker 接口 + TaskConditionParser
    │   └── Impl/                   # 实现：杀怪/收集物品/等级/到达位置/对话NPC/施放技能/进入游戏/任务完成
    └── Reward/                     # 任务奖励：IRewardHandler 接口 + TaskRewardParser
        └── Impl/                   # 实现：物品奖励/技能奖励/连招技能奖励
Handle/                             # 注意：目录名拼写为 Hanle/
├── ChatHandler.cs                  # 聊天（场景频道/世界频道已实现，私聊/系统/组队/公会未实现）
├── EnterGameWorldHanlder.cs        # 进入游戏世界（验证 GameToken、创建角色、分配场景）
├── GameItemHandler.cs              # 背包物品操作
├── GameServerHandler.cs            # 服务器互联（网关注册、心跳）
└── TaskHandler.cs                  # 任务请求处理
```

### 关键机制

- **角色-网关转发**：`GameCharacter.SendToGate(msg)` 通过 ServersMgr 把消息发回 GameGate（按 `SessionId`），GameGate 再下发给客户端
- **聊天频道**：`Scene` 频道按 `CurSceneId` 过滤 `GetPartGameCharacterBySceneId`，`World` 频道遍历全部角色；发送前服务端补全 `FromChrName` 减少带宽
- **任务系统策略模式**：
  - 条件：实现 `IConditionChecker` 接口，在 `TaskConditionParser` 注册（按条件类型字符串映射），新增条件类放 `Condition/Impl/`
  - 奖励：实现 `IRewardHandler` 接口，在 `TaskRewardParser` 注册，新增奖励放 `Reward/Impl/`
- **GameToken**：GameServer 对 GameGate 签发的凭据（`Net/GameToken.cs` + `GameTokenManager`），进入游戏世界时校验

## SceneServer（`MMORPG/SceneServer/`）

### 目录与职责

```
Core/
├── AOIMap/
│   ├── Linked/                     # 十字链表 AOI（当前使用）：AoiLinkedList/AoiZone/AoiNode/AoiEntity + SkipList 池化
│   └── NineSquareGrid/             # 旧版九宫格 AOI（已弃用）
├── Combat/
│   ├── AI/                         # 怪物 AI（状态机）
│   │   ├── BaseMonsterAI.cs        # AI 基类
│   │   └── Boss/Impl/              # Boss 状态：巡逻/追逐/攻击/受伤/逃跑/死亡/返回出生点
│   │   └── WoodenDummy/Impl/       # 木桩 AI 状态：空闲/受伤/死亡
│   ├── Attrubute/                  # 属性系统：AttributeManager + AttrubuteData
│   ├── Buffs/                      # Buff：BuffBase + BuffManager + BuffScanner（扫描自定义 Buff 类）
│   ├── Skill/                      # 技能：Skill/SkillManager/SkillScanner + SkillImpl 自定义技能
│   │   └── AreaEntitiesFinder.cs   # 范围检测：扇形/圆形/矩形
│   ├── FightManager.cs             # 战斗结算
│   ├── Missile.cs                  # 飞行物
│   └── SCObject.cs                 # 技能释放对象
├── Model/
│   ├── SceneEntity.cs              # 场景实体基类
│   ├── Actor/                      # SceneActor 基类 + SceneCharacter/SceneMonster/SceneNpc
│   ├── Item/SceneItem.cs           # 场景掉落物
│   └── Interactivo/SceneInteractivo.cs
├── Scene/
│   ├── SceneManager.cs             # 场景管理（326 行）
│   └── Component/                  # 组件式管理器：Character/Monster/Entity/Item/Spawn 管理器
└── Task/GameTaskConditionChecker.cs # 场景侧任务条件检测（如杀怪进度上报）
Handle/
├── CombatHandler.cs                # 战斗请求
├── SceneHandler.cs                 # 移动/位置同步/AOI 进出（232 行）
├── EnterGameWorldHanlder.cs        # 进入场景
└── SceneServerHandler.cs           # 服务器互联
```

### 关键机制

- **AOI（十字链表）**：`AoiLinkedList` 按 X/Y 轴双向链表 + 跳跃表（`SkipList` 池化）维护实体位置关系，进入/离开视野触发兴趣管理；旧九宫格 `NineSquareGrid` 已弃用
- **怪物 AI 状态机**：`StateMachine<T>` 驱动（Summer 框架工具类），状态类命名 `XxxMonsterAIState_<状态>`，通过切换状态实现巡逻→追逐→攻击→死亡→返回流程
- **技能/Buff 双轨制**：Excel 配置基础效果 + `SkillImpl/`、`BuffImplement/` 下自定义类扩展，`SkillScanner`/`BuffScanner` 反射扫描加载自定义类
- **范围检测**：`AreaEntitiesFinder` 支持扇形/圆形/矩形技能判定
- **场景组件化**：SceneManager 下挂 Component 管理器（角色/怪物/物品/刷怪点），实体增删走对应 Component

## GameGate 消息路由（`MMORPG/GameGateServer/`）

网关的 `Handle/` 下每个 Handler 与业务服务器对应，职责是**转发**而非业务实现：

- `GameItemHandler`/`TaskHandler`/`ChatHandler` → 转发到 **GameServer**
- `SceneHandler` → 转发到 **SceneServer**（按 `sceneId` 选连接）
- `EnterGameWorldHanlder` → 进入游戏世界流程（验证 GameToken）
- `SecurityHandler` → 密钥交换
- 转发用 `ServersMgr.SendToGameServer(msg)` / `SendToSceneServer(sceneId, msg)`；回包按 `SessionId` 找 `Session` 下发客户端
- `Session` 支持玩家离线时消息缓存（`msgBuffer`），重连后补发

## 会话模型速查

| 服务器 | 会话类 | 说明 |
|---|---|---|
| LoginServer | `Session` + `SessionManager` | 登录会话，LoginToken 对应网关连接 |
| LoginGateServer | `LoginGateToken` + `LoginGateTokenManager` | 网关侧登录凭据 |
| GameGateServer | `Session` + `SessionManager` | 游戏会话（用户端），`m_uId`/`m_cId` 绑定账户与角色 |
| GameServer | `GameToken` + `GameTokenManager` | 网关连接凭据（服务器端） |

## 扩展指南

- **新物品类型**：继承 `GameItem` 放 `GameServer/core/Model/BaseItem/Sub/`，注意 InventoryManager 的物品构造分支
- **新任务条件/奖励**：实现对应接口 + 在 Parser 注册（见上文策略模式）
- **新怪物 AI 状态**：新建 `XxxMonsterAIState_<状态>` 类，在 AI 类中接入状态切换
- **新自定义技能/Buff**：在 `SkillImpl/`/`BuffImplement/` 新增类，确认 Scanner 能扫描到（命名与配置一致）
- **协议定义**：新增消息前先看 mmorpg-protocol-db skill
- 未实现功能现状：聊天私聊/组队/公会频道、组队系统均为 `Log.Warning("没实现")` 占位，实现时保持该约定
