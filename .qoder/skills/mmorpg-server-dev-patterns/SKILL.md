---
name: mmorpg-server-dev-patterns
description: Follow the standard patterns for building MMORPG server nodes: the unified startup flow (Config.Init, CommonMgr, Scheduler, ServersMgr), the phased ControlCenter registration pattern (Phase0-3), Handler registration idiom (ProtoHelper.Register + MessageRouter.Subscribe), config.yaml conventions, and coding conventions (naming, goto End error handling, Serilog logging, Singleton). Use when adding a new server node, adding a message Handler to an existing server, modifying a server's startup or networking code, or writing any new server-side C# code in this project (新增服务器, 写Handler, 接入ControlCenter, 启动流程).
---

# MMORPG 服务器开发模式与规范

所有服务器遵循同一套骨架。写新代码前先读同类服务器现有实现（如 GameServer/LoginServer），保持一致。

## 标准目录结构（每个服务器项目）

```
<ServerName>/
├── Program.cs          # 入口：Init() + Shell()，逐条初始化
├── config.yaml         # 本服务器配置（SceneServer 支持多份 config2.yaml）
├── Net/
│   ├── ServersMgr.cs   # 网络与集群连接核心：ConnManager 初始化、向 CC 注册、与其他服务器互联
│   └── <Token>.cs      # 可选：对端连接凭据（GameToken/LoginToken）
├── Handle/             # 消息处理器（有些项目拼写为 Hanle/，如 GameServer）
│   └── XxxHandler.cs   # 每个业务域一个 Handler 单例
├── Core/               # 业务逻辑（Manager、Model、数据）
└── Utils/
    ├── Config.cs       # YamlDotNet 读取 config.yaml
    └── StaticDataManager.cs  # 可选：静态数据
```

## 统一启动流程（Program.cs）

每个服务器的 `Main` 都是：

```csharp
public static void Main(string[] args)
{
    Init();
    Shell();   // 可选：命令行交互，ControlCenter 默认注释掉
}
private static bool Init()
{
    CommonMgr.Instance.Init();          // 1. Serilog 日志（必须在最前）
    Config.Init();                      // 2. 读 config.yaml
    Scheduler.Instance.Start(Config.Server.updateHz);  // 3. 主循环（50Hz）
    ServersMgr.Instance.Init();         // 4. 网络初始化（触发阶段流程）
    return true;
}
```

## ServersMgr 阶段启动模式（接入集群的标准流程）

所有服务器按**回调驱动的阶段（Phase）**顺序启动，不要改写成同步顺序代码：

```
Phase0  _ConnectToCC()                         连接 ControlCenter
        ↓ _CCConnectedCallback 发送 ServerInfoRegisterRequest
注册响应  _RegisterServerInfo2ControlCenterResponse（ResultCode==0 时）
Phase1  _ExecutePhase1(ClusterEventNodes)      处理注册前错过的集群事件（补连依赖服务器，如 DBProxy/GameGateMgr）
Phase2  _ExecutePhase2()                       连接依赖服务器完成后检查
Phase3  _ExecutePhase3() → ConnManager.Instance.Start()   开始集群端口监听（就绪标志）
```

关键约定：
- 构造 `ServerInfoNode` 时填 `ServerType`、`Ip`、`Port`（网关还要填 `UserPort` 相关字段）、`ServerId` 初始 0（由 CC 分配）
- `EventBitmap` 用位图声明本服务器关心的 `ClusterEventType`（如 Game 订阅 `DbproxyEnter`，GameGate 订阅 `GamegatemgrEnter`），`SetEventBitmap()` 里按位或
- `IsFirstConn` 标志区分首次连接与重连：重连成功时跳过 Phase1 重复处理
- 注册成功后 `m_curSin.ServerId = message.ServerId` 并保存
- 依赖服务器的连接方法命名：`_ConnectToXxx` + `_XxxConnectedCallback`（连接成功即发注册请求）+ `_XxxConnectedFailedCallback`（记录「重连还没写」的地方需补全）

## Handler 编写模式（Handle/ 目录）

每个业务域一个 `Singleton` Handler，`Init()` 里完成两件事：**协议注册** + **消息订阅**：

```csharp
public class ChatHandler : Singleton<ChatHandler>
{
    public override void Init()
    {
        // 协议注册：协议号来自 proto 中定义的 XXXProtocl 枚举
        ProtoHelper.Instance.Register<SendChatMessageRequest>((int)ChatProtocl.SendChatMessageReq);
        ProtoHelper.Instance.Register<ChatMessageResponse>((int)ChatProtocl.ChatMessageResp);
        // 消息订阅
        MessageRouter.Instance.Subscribe<SendChatMessageRequest>(_HandleSendChatMessageRequest);
    }

    private void _HandleSendChatMessageRequest(Connection conn, SendChatMessageRequest message)
    {
        // 处理逻辑...
    }
}
```

规则：
- **收发的每个消息类型都必须先 Register，否则序列化/解析失败**
- Handler 方法签名统一 `void _HandleXxxRequest(Connection conn, XxxRequest message)`
- 错误处理沿用项目惯例：`goto End` 跳到函数末尾统一返回（见 ChatHandler 现有写法）
- 服务器互联注册消息（如 `ServerInfoRegisterRequest`）的注册与订阅写在 `ServersMgr.Init()`，业务消息写在各自 Handler 的 `Init()`
- Handler 的 `Init()` 由 `ServersMgr.Init()` 统一调用（如 `ChatHandler.Instance.Init()`），不要遗漏

## config.yaml 约定

```yaml
server:
  ip: 127.0.0.1
  serverPort: 10501        # 集群端口；网关类服务器额外有 userPort
  workerCount: 1           # MessageRouter 线程数
  updateHz: 50             # Scheduler 频率
  heartBeatTimeOut: 30000  # 毫秒
  heartBeatCheckInterval: 15
  heartBeatSendInterval: 10
cc:
  ip: 127.0.0.1
  port: 10100              # ControlCenter 地址
# 按需追加段：database(DBProxy地址) / mongodb(DBProxy专用连接串)
```

- 用 `Config.cs`（YamlDotNet）加载，每个服务器自持一份（字段略有差异）
- 修改端口后注意各服务器 config.yaml 之间的一致性（cc 段、database 段指向要正确）

## 编码约定

- **命名**：私有字段 `m_` 前缀（`m_curSin`）、私有方法 `_` 前缀（`_HandleXxx`）、属性 PascalCase；中文注释
- **单例**：管理器/Handler 全部继承 `Singleton<T>`，通过 `Instance` 访问
- **日志**：用 Serilog 静态类 `Log.Information/Warning/Error`，不用 `Console.WriteLine`；关键节点（连接成功、注册成功、收到命令）必须打日志，风格与现有代码一致（英文消息 + 中文错误提示混用）
- **错误处理**：早期代码用 `goto End` 模式（label 放函数末尾）；不要引入异常抛出的新风格
- **线程模型**：MessageRouter 多线程处理消息，业务数据容器需考虑线程安全（项目现状：部分用 Concurrent 集合，部分依赖锁）
- **配置读取**：不得硬编码 IP/端口，从 Config 读
- **注释的初始化代码**：Program.cs 中被注释的单体架构时代码（如 GameServer 的 DbManager/UserService）不要恢复使用，那是迁移遗留

## 新服务器接入步骤（以新增 XxxServer 为例）

1. 复制结构最接近的现有服务器目录（如纯逻辑服务器抄 LoginServer，网关抄 GameGateServer）
2. 改 csproj（ProjectReference 指向 `..\Common\Common.csproj`）、命名空间、Program 里的 ASCII banner
3. 配置 `config.yaml`（新端口，避开 10100-11000 已占用段）
4. 在 `Common/Summer/Proto/` 下为它新建或复用 proto 模块（见 mmorpg-protocol-db skill）
5. `ServersMgr.Init()` 里：定义 `m_curSin`（ServerType、EventBitmap）、`ConnManager.Init`、注册订阅互联协议、`_ExecutePhase0()`
6. ControlCenter 侧：`ControlCenter/Net/ServersMgr.cs` 的 `m_serversByType` 初始化、注册 switch、断开 switch、事件分发位图处理中加入新类型
7. `Start1.bat` 按依赖顺序插入启动项
8. 编译整个解决方案，按 Start1.bat 顺序启动验证注册日志
