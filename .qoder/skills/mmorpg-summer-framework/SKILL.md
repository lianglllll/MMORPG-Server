---
name: mmorpg-summer-framework
description: Develop and debug the Summer base framework in MMORPG/Common. Covers the network stack (ConnManager, Connection, NetClient, TcpServer, LengthFieldDecoder, MessageRouter, ProtoHelper, TypeAttributeStore), the Scheduler main loop, security encryption utilities, and tool classes (Singleton, IdGenerator, ObjectPool, StateMachine, Event, DataStream). Use when modifying or reading code under MMORPG/Common/Summer, working with network message handling, packet framing, thread-safe message dispatch, or asking how the net layer works (网络层, 消息路由, 粘包拆包, Scheduler).
---

# MMORPG Summer 基础框架开发

框架代码位于 `MMORPG/Common/Summer/`，所有服务器项目共享。修改框架代码会影响全部服务器，需谨慎并编译整个解决方案验证。

## 模块地图

| 目录 | 内容 |
|---|---|
| `Core/` | `Scheduler`（主循环）、`MyTime`、`Mathf`、`Vector2/3/3Int`（Unity 风格数学库） |
| `Net/` | 网络核心：连接管理、消息收发、路由分发、协议编解码 |
| `Security/` | `EncryptionManager`、`AesEncryption`、`RsaEncryption`、`PasswordHasher` |
| `Server/` | `BaseServersMgr`（服务器管理器基类）、`ServerSelector`（选择策略） |
| `Time/` | `HighPrecisionClock`、`TimeSyncClient`（时间同步客户端） |
| `Tools/` | `Singleton`、`IdGenerator`、`ObjectPool`、`Event`、`DataStream`、`DataConvert`、`Varint`、`StateMachine` |
| `StaticData/` | `ErrorCode`、`StaticDataLoader` |
| `CommonMgr.cs` | 全局初始化：Serilog 日志配置（控制台彩色主题 + 文件 `logs\server-log.txt`） |

注意：`Net/New/` 与 `Net/Old/` 是重构过渡期的代码（`UserMessageHandlerMap`、`LengthFieldDecoder_old`），新代码不要引用。`Tools/LogDemo.cs` 为示例文件。

## 网络层架构

```
Socket
  └─ LengthFieldDecoder      // 粘包拆包：按长度字段切帧，异步接收
      └─ Connection          // 一条连接的封装：收发消息、断开回调
          ├─ TcpServer       // 被动连接（监听端口）
          ├─ NetClient       // 主动连接（连其他服务器）
          └─ ConnManager     // 统一管理所有连接（监听+主动），单例
              └─ MessageRouter // 多线程消息分发（ThreadPool 工作线程 + 并发队列）
```

### ConnManager（单例，全局唯一）

- `Init(workerCount, heartBeatSendInterval, heartBeatCheckInterval, heartBeatTimeOut, ...)`：初始化
  - 可同时监听「用户端口」（`userIp/userPort`，客户端连接）与「集群端口」（`ip/port`，服务器连接）
  - 网关类服务器（LoginGate/GameGate）通过 `isUserServer=true` 开启用户端口监听，逻辑服务器为 false
- `Start()`：启动集群端口监听（服务器就绪标志）；`UserStart()`/`UserEnd()`：控制用户端口开关（GameGate 收到 Start/End 命令时切换）
- `ConnctToServer(ip, port, onConnected, onFailed, onDisconnected)`：主动连接其他服务器，返回 `NetClient`，支持断线重连回调
- `CloseOutgoingServerConnection` / `CloseUserConnection`：主动断开

### Connection

- 每条连接一个 `LengthFieldDecoder`，`Init` 后立即开始异步接收
- 收到数据 → `ProtoHelper.Instance.ByteArrayParse2IMessage` 解析 → `MessageRouter.Instance.AddMessage(conn, msg)`
- `Send(IMessage)`：序列化（4 字节长度 + 2 字节协议号 + protobuf 数据）后 `BeginSend`，内部有 `lock` 保证多线程发送安全
- 断开时通过 `DisconnectedHandler` 委托向上通知
- 继承 `TypeAttributeStore`：可挂任意类型属性，常用 `conn.Set<int>(serverId)` / `conn.Get<int>()` / `conn.Get<Session>()`

### MessageRouter（单例，多线程消息分发）

- `Start(threadCount)`：启动线程池工作线程（上限 10），消息进 `ConcurrentQueue`，`AutoResetEvent` 唤醒
- `Subscribe<T>(handler)` / `UnSubscribe<T>(handler)`：按消息类型（`typeof(T).FullName`）维护委托链，多个订阅者会依次调用
- 处理函数签名：`void Handler(Connection conn, T message)`
- **线程安全注意**：消息在多线程并行处理，Handler 内共享数据必须自行加锁；连接上的消息顺序不保证
- 异常会被捕获并打印日志，不会中断工作线程

### ProtoHelper（单例，协议编解码）

- `Register<T>(int id)`：注册协议号 ↔ 类型映射（双向字典），**未注册的消息无法解析/发送**
- 帧格式（大端）：
  ```
  [4字节 总长度][2字节 协议号][protobuf payload]
  ```
  长度字段 = `message.CalculateSize() + 2`；协议号用 `BinaryPrimitives.ReadUInt16BigEndian` 读取
- `IMessageParse2ByteArray` 用 `DataStream.Allocate()` 池化缓冲，无需手动释放（using）

### 其他 Net 类

- `NetClient`：主动连接封装，自带重连计数（`isEnd` 表示重连次数耗尽）；`ServerId` 字段可挂业务数据
- `TcpServer`：监听 accept，回调传入 Connection
- `LengthFieldDecoder`：支持自定义长度字段偏移/大小（默认 offset 0、size 4），含最大包长限制（64KB）

## Scheduler 主循环

- `Scheduler.Instance.Start(updateHz)`：在 Program.Init 中调用，创建独立线程以固定频率 tick（默认 50Hz）
- `Scheduler.UnixTime`：静态属性，返回 tick 驱动的逻辑时间（浮点秒），业务代码统一用它而非 `DateTime`/`Stopwatch`
- `AddTask(callback, intervalSeconds)`：注册周期任务
- 服务器内部逻辑（实体更新、场景更新等）应挂在 Scheduler 周期任务上，不要自建循环线程

## 安全模块

- `EncryptionManager`：每条 Connection 自带，负责密钥协商状态（RSA 交换 AES 密钥，后续 AES 加密敏感消息）
- `PasswordHasher`：密码哈希（登录注册用）
- 网关的 `SecurityHandler` 使用这些类完成与客户端的密钥交换流程

## 工具类速查

| 类 | 用途 |
|---|---|
| `Singleton<T>` | 单例基类，`Instance` 访问，`Init/UnInit` 虚方法 |
| `IdGenerator` | 整型 ID 分配与回收（`GetId`/`ReturnId`），ControlCenter 用其分配 serverId |
| `ObjectPool<T>` | 对象池 |
| `Event<T1,T2>` | 类型安全事件委托 |
| `DataStream` | 字节流读写（池化），配合 ProtoHelper 序列化 |
| `Varint` | Varint 编解码 |
| `StateMachine<T>` / `StateBase<T>` | 通用状态机（Scene 怪物 AI 的基础） |
| `StaticDataLoader` | JSON 静态数据加载 |

## 修改框架的注意事项

1. 改完必须编译整个解决方案（框架被 17 个项目引用）
2. 网络层改动先在单个服务器联调验证帧格式兼容性（服务端与客户端共用此帧格式，客户端在 Unity 仓库）
3. 不要动 `Net/Old/` 与 `Net/New/`，它们是迁移过渡代码
4. 协议相关开发见 mmorpg-protocol-db skill；服务器接入模式见 mmorpg-server-dev-patterns skill
