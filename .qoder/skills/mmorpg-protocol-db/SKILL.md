---
name: mmorpg-protocol-db
description: Define Protobuf protocols and database access for the MMORPG servers. Documents the proto module layout (ProtoSource committed, ProtoClass generated and gitignored), protocol id enum conventions (XXXProtocl), message naming (Request/Response), the register-and-subscribe usage flow, the wire frame format, and the DBProxy MongoDB layer (Operations + Handler pattern, BsonDocument mapping). Use when adding or modifying proto definitions, registering new messages, changing MongoDB storage, or debugging protocol parse failures (新增协议, proto, 协议号, DBProxy, MongoDB, 序列化).
---

# MMORPG 协议与数据库开发

## Proto 目录结构（`MMORPG/Common/Summer/Proto/`）

```
Proto/<模块>/
├── ProtoSource/        # *.proto 源文件（git 跟踪，手工编辑这里）
│   └── Xxx.proto
└── ProtoClass/         # 生成的 C# 类（.gitignore 忽略，不提交，由工具生成）
```

现有模块及命名空间（package 均为 `HS.Protobuf.<模块>`）：

| 模块 | proto 文件 | 协议号枚举示例 |
|---|---|---|
| Common | Common.proto | SERVER_TYPE、ClusterEventType、ErrorCode 等公共定义 |
| ControlCenter | ControlCenter.proto | ControlCenterProtocl |
| Login | Login.proto / LoginGate.proto / LoginGateMgr.proto | LoginProtocl / LoginGateMgrProtocl |
| Game | Game.proto / GameGate.proto / GameGateMgr.proto / Backpack.proto / GameTask.proto | GameProtocl / GameGateMgrProtocl |
| Scene | Scene.proto / SceneEntity.proto | SceneProtocl |
| Combat | Skill.proto / Buff.proto | 战斗相关 |
| Chat | Chat.proto | ChatProtocl |
| DBProxy | DBUser.proto / DBCharacter.proto / DBInventory .proto / DBTask.proto / DBWorld.proto | 数据库消息（DB 前缀类，如 DBUserNode） |
| TimeSync | MasterTime.proto | 时间同步 |

## 协议定义规范

proto3 语法，字段/枚举用下划线命名，生成的 C# 自动转 PascalCase。每个模块 proto 顶部定义一个协议号枚举：

```protobuf
syntax = "proto3";
package HS.Protobuf.Chat;

enum ChatProtocl{
	CHAT_PROTOCL_NONE				= 0;
	CHAT_PROTOCL_SEND_CHAT_MESSAGE_REQ		= 34001;
	CHAT_PROTOCL_CHAT_MESSAGE_RESP		= 34002;
}

message SendChatMessageRequest{ ... }
message ChatMessageResponse{ ... }
```

约定：
- 协议号枚举名：`<模块>Protocl`（注意是 Protocl 不是 Protocol，沿用现有拼写）
- 枚举成员命名：`<模块大写>_PROTOCL_<消息名大写>_REQ/RESP`，各模块有独立号段（Chat 从 34001 起，新模块沿用现有分段规则，避免与其他模块冲突）
- 消息命名：请求 `XxxRequest`、响应 `XxxResponse`；数据库消息 `DB` 前缀（如 `DBUserNode`）
- **proto 修改后必须重新生成 C# 类到 ProtoClass 目录**（生成工具在 Tools 仓库，README 指明 Tools 含 proto 工具）；ProtoClass 不入库，新拉取的仓库需要本地生成
- 网关转发型消息（如 `ChatMessageResponse` 带 `sessionId` 字段）会跨节点传递，字段名以 `sessionId` 指代目标客户端会话，这是服务端→网关→客户端的回包惯例

## 协议使用流程

每个收发消息的类型都要走「注册 + 订阅」两步，见 mmorpg-server-dev-patterns skill 的 Handler 模板：

```csharp
// 1. 协议注册（按 XXXProtocl 枚举）
ProtoHelper.Instance.Register<SendChatMessageRequest>((int)ChatProtocl.SendChatMessageReq);
// 2. 消息订阅（按类型分发）
MessageRouter.Instance.Subscribe<SendChatMessageRequest>(_HandleSendChatMessageRequest);
```

- 协议号在编译期不存在运行时校验，改 proto 后忘记重新生成或忘记 Register 会得到「未找到对应的协议id/类型」运行时错误（ProtoHelper 会打印 Error 日志）
- 排查解析失败：先看两端是否都 Register 了同一协议号；再看 ProtoClass 是否为最新生成

## 线格式（服务端与客户端共用，改动需同步 Unity 客户端）

```
[4字节 总长度(含协议号2字节)][2字节 协议号(大端)][protobuf payload]
```

实现：`ProtoHelper.IMessageParse2ByteArray` / `ByteArrayParse2IMessage`。改动帧格式属于破坏性变更，必须同步客户端仓库。

## 数据库链路与 DBProxy

```
业务服务器 → DBProxyServer(10200) → MongoDB (mongodb://localhost:27017 / 库名 mmorpg)
```

DBProxy 结构（`MMORPG/DBProxyServer/`）：

```
Core/
├── Conn/MongoDBConnection.cs   # 连接管理（config.yaml 的 mongodb 段）
├── UserOperations.cs           # 用户集合操作（单例）
├── CharacterOperations.cs      # 角色集合操作
├── InventoryOperations.cs      # 背包集合操作
├── TaskOperations.cs           # 任务集合操作
└── WorldOperations.cs          # 世界集合操作
Handle/
├── DBProxyServerHandler.cs     # 服务器互联
├── UserHandler.cs              # 用户相关请求 → UserOperations
├── CharacterHandler.cs         # 角色相关请求
└── WorldHandler.cs             # 世界相关请求
```

### Operations 模式约定

- 每个集合一个 `Singleton` Operations 类，`Init(MongoDBConnection)` 时拿 `IMongoCollection<BsonDocument>`
- 方法命名 `XxxAsync`（返回 `Task<bool>`），内部 `try/catch` 打日志
- 输入是 protobuf 消息（`DBUserNode` 等），手动映射为 `BsonDocument` 再写入（字段名沿用 proto 字段的 camelCase，如 `userName`、`worldCharacters`）
- Handler（请求入口）→ Operations（数据库执行），不要在 Handler 里直接碰 MongoDriver

### 添加新数据库操作的步骤

1. 在 `Proto/DBProxy/ProtoSource/` 相应 proto 增加消息定义，重新生成
2. 对应 Operations 类加 `XxxAsync` 方法（BsonDocument 映射 + InsertOne/Find/Update 等）
3. 对应 Handler 注册 + 订阅新协议（见模板），调用 Operations
4. 调用方（如 GameServer）在 `ServersMgr.SendMsgToDBProxy(msg)` 发送请求，订阅响应处理

## 相关提示

- 错误码：`Common/Summer/StaticData/ErrorCode.cs`（ErrorCode.GetCode("OK")）
- 静态数据 JSON 加载：`StaticDataLoader`（配置数据不走 MongoDB）
- 网关会话/转发、业务归属见 mmorpg-architecture 与 mmorpg-game-logic skill
