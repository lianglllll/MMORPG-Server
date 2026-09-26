---
name: mmorpg-architecture
description: Navigate the MMORPG distributed server cluster architecture. Documents every server node's responsibility (ControlCenter, LoginGate/LoginGateMgr/Login, GameGate/GameGateMgr/Game, Scene, DBProxy, MasterTimer/SlaveTimer), default ports, startup order, and cross-node message flows. Use when exploring project structure, tracing a request path across servers, planning a new server node, or answering questions about how the cluster works (登录流程, 进入游戏流程, 分布式架构, 服务器职责).
---

# MMORPG 分布式服务器架构导航

## 项目速览

- 修仙题材 MMORPG 学习项目，服务端为 C# / .NET 6，客户端 Unity（不在本仓库）
- **分布式服务器模式**：游戏逻辑分布在多个服务器节点，通过负载均衡动态分配玩家
- 解决方案：`MMORPG/MMO-SERVER.sln`，每个服务器是独立 csproj，公共代码在 `MMORPG/Common`（Summer 框架）
- 通信：Google Protobuf + 自定义帧头（见 mmorpg-protocol-db skill）
- 数据库：MongoDB，业务服务器只能通过 DBProxy 间接访问（Redis 缓存层未开发）
- 服务器间发现：全部通过 ControlCenter 注册 + 事件订阅（EventBitmap 位图机制）

## 服务器节点职责与端口

| 服务器 | 集群端口 | 用户端口 | 职责 |
|---|---|---|---|
| ControlCenter | 10100 | - | 注册中心：服务器信息注册、事件订阅分发、存活监控 |
| DBProxyServer | 10200 | - | MongoDB 代理：替其他服务器读写数据库 |
| GameGateMgrServer | 10300 | - | GameGate/Scene 注册管理；按负载分配 GameGate+Scene 给 Game；响应 Entry 的 GameGateList 请求 |
| GameGateServer | 10401 | 10400 | 与客户端直连的游戏网关：会话管理、密钥交换、消息转发（背包→Game，位置→Scene）、负载上报 |
| GameServer | 10501 | - | 静态数据与游戏逻辑：角色/账户/背包/装备、聊天、任务、组队 |
| LoginGateMgrServer | 10600 | - | LoginGate 注册管理；给 Login 分配 LoginGate；动态调整数量；更新 CDN 存活信息 |
| LoginGateServer | 10701 | 10700 | 与客户端直连的登录网关：密钥交换、请求过滤、转发登录请求到 Login |
| LoginServer | 10800 | - | 登录/注册处理；成功后生成 Session 投递到 Entry（Entry 未开发） |
| SceneServer | 10900（config2: 10901） | - | 动态场景数据：玩家 Transform、移动、战斗、AI、AOI |
| MasterTimerServer | 11000 | - | 时间权威：接受 SlaveTimer 同步请求，向高精度时间源校准 |
| SlaveTimerServer | 见其 config | - | 时间从服务器：定期同步 MasterTimer，其他服务器连它同步时间 |

说明：
- 网关类服务器（LoginGate/GameGate）有**双端口**：`userPort` 对客户端、`serverPort` 对集群内其他服务器
- SceneServer 支持多开：`-config config.yaml` / `-config config2.yaml` 指定配置文件
- MasterTimer/SlaveTimer 未包含在 `Start1.bat` 启动列表中，需手动启动

## 登录链路（客户端 → 游戏世界外）

```
客户端 → CDN(模拟) 取 LoginGate 地址
  → LoginGate: 密钥交换(RSA/AES)，转发登录请求
    → LoginGateMgr: 负载均衡，给 Login 分配 LoginGate 列表
    → Login: 验证账号密码 → 生成 Session → 投递到 Entry（未开发）
    → Entry（未开发）: 向 GameGateMgr 要空闲 GameGateList 返回给客户端
```

关键类：`LoginGateServer/Handle/SecurityHandler.cs`（密钥交换）、`LoginServer/Handle/UserHandler.cs`（登录注册）、`LoginServer/Net/LoginTokenManager.cs`

## 游戏链路（客户端 → 游戏世界内）

```
客户端 → GameGate: 密钥交换，凭 GameToken 建立会话
  → GameGateMgr: 注册 GameGate，负载均衡(GGMMonitor)，向 GameGate 下发命令(ExecuteGGCommand: Start/End)
  → Game: 注册成功后签发 GameToken 给 GameGate，GameGate 据此给用户建立 Session
  → Scene: 通过 Game 的注册响应(RegisterToGResponse.SceneInfoNodes)被 GameGate 发现并连接
```

转发规则（GameGate 按消息类型路由）：
- 背包/聊天/任务/组队等 → **GameServer**
- 移动/战斗/位置同步 → **SceneServer**

关键类：`GameGateServer/Net/ServersMgr.cs`（连接管理+转发）、`GameGateServer/Net/Session.cs`、`GameGateServer/Net/SessionManager.cs`（会话管理）、`GameGateMgrServer/Core/GGMMonitor.cs`（负载均衡核心）

## 数据库与时间同步链路

```
业务服务器 → DBProxyServer(10200) → MongoDB(mongodb://localhost:27017, 库名 mmorpg)
业务服务器 → SlaveTimerServer → MasterTimerServer(11000) → 高精度时间源
```

## 启动顺序（Start1.bat）

必须按序启动，后启动者依赖先启动者注册到 ControlCenter：

1. ControlCenter
2. DBProxyServer
3. LoginGateMgrServer → LoginGateServer → LoginServer
4. GameGateMgrServer → GameGateServer → GameServer
5. SceneServer × 2（config.yaml / config2.yaml）

脚本将 PID 记录到 `pids.txt`，`Stop1.bat` 据此关闭；`Start2.ps1` 为 PowerShell 版本。

## 未开发/占位模块

- **EntryServer**：接受 Login 投递的 Session，向 GameGateMgr 询问 GameGate 列表
- **PortalServer**：版本更新、公告
- **RedisServer**：主从缓存（主写从读），未接入
- **LogServer**：集中日志收集
- **HttpProxyServer**：Web 查看集群状态、发命令

## 快速定位指南

- 「某功能在哪实现」：先对照上表判断属于静态数据（Game）还是场景动态数据（Scene），再到对应 `Handle/` 目录找 Handler
- 「某消息如何跨节点传递」：看发起方 ServersMgr 的 `SendMsgToXxx` 工具方法，再到接收方 ServersMgr/Handler 的 Subscribe
- 「新增服务器节点」：参考 mmorpg-server-dev-patterns skill 的接入流程
- 「协议与数据库」：参考 mmorpg-protocol-db skill
- 「Summer 框架 API」：参考 mmorpg-summer-framework skill
