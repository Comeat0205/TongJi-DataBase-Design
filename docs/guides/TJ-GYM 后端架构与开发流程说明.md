# TJ-GYM 后端架构与开发流程说明

本文档用于说明 TJ-GYM 当前后端项目的整体架构、各目录职责、代码分层规则，以及后续新增业务时应遵循的开发流程。

当前后端位于：`backend/`

```text
backend/
  Api/
  Application/
  Domain/
  Infrastructure/
  Shared/
  backend.slnx
```

## 1. 后端整体架构

当前后端采用接近 DDD 分层思想的四层结构：

```text
Api  →  Application  →  Domain
                 ↓
           Infrastructure  →  Oracle / EF Core
```

各层的核心职责如下：

| 层 | 项目 | 核心职责 |
| --- | --- | --- |
| 接口层 | `Api` | 接收 HTTP 请求、返回 HTTP 响应、注册中间件和依赖注入 |
| 应用层 | `Application` | 编排业务用例、定义前后端传输 DTO、定义应用服务接口和实现 |
| 领域层 | `Domain` | 存放领域实体、领域规则、枚举、常量、领域异常、仓储接口 |
| 基础设施层 | `Infrastructure` | 实现数据库访问、EF Core DbContext、Repository、UnitOfWork、数据库迁移 |
| 共享层 | `Shared` | 预留公共基础能力，当前暂未承载核心业务逻辑 |

### 1.1 依赖方向

当前项目依赖关系大致为：

```text
Api
 ├─ references Application
 └─ references Infrastructure

Application
 ├─ references Domain
 └─ references Shared

Infrastructure
 ├─ references Domain
 └─ references Shared

Domain
 └─ 不依赖其他业务项目
```

这体现了一个重要原则：

- `Domain` 是核心，不应该依赖 `Api`、`Application`、`Infrastructure`。
- `Application` 调用领域对象和仓储接口，但不直接操作 EF Core 或 Oracle。
- `Infrastructure` 实现 `Domain` 中定义的仓储接口，负责真正访问数据库。
- `Api` 只负责 HTTP 入口，不直接写业务规则，也不直接访问数据库。

## 2. 各项目和目录职责

## 2.1 Api 项目

路径：`backend/Api/`

`Api` 是整个后端的启动项目，也是 HTTP 请求进入系统的入口。

当前主要目录：

```text
Api/
  Controllers/
  Middleware/
  Properties/
  Program.cs
  appsettings.json
  appsettings.Development.json
  Api.csproj
```

### 2.1.1 Program.cs

`Program.cs` 是 ASP.NET Core 应用启动入口，主要负责：

1. 创建 WebApplicationBuilder。
2. 注册 Application 层服务。
3. 注册 Infrastructure 层服务和数据库连接。
4. 注册 Controller。
5. 注册 OpenAPI。
6. 注册全局异常处理中间件。
7. 映射 Controller 路由。
8. 启动 Web 应用。

当前关键逻辑包括：

- `builder.Services.AddApplication()`：注册应用层服务。
- `builder.Services.AddInfrastructure(builder.Configuration)`：注册基础设施层服务，例如 `AppDbContext`、Repository、UnitOfWork。
- `builder.Services.AddControllers()`：启用控制器。
- `builder.Services.AddOpenApi()`：启用接口文档能力。
- `app.UseMiddleware<GlobalExceptionMiddleware>()`：启用统一异常处理。
- `app.MapControllers()`：映射 Controller 路由。

### 2.1.2 Controllers 目录

路径：`backend/Api/Controllers/`

Controller 只负责 HTTP 层相关工作，例如：

- 定义接口路由。
- 接收请求参数。
- 调用 Application 层应用服务。
- 将应用服务结果包装为统一响应。
- 返回合适的 HTTP 状态码。

Controller 不应该：

- 直接访问 `AppDbContext`。
- 直接调用 EF Core。
- 直接写复杂业务规则。
- 直接拼接数据库查询。
- 直接处理领域规则。

当前已有 Controller：

```text
Controllers/
  AuthController.cs
  MembersController.cs
  WeatherForecastController.cs
```

其中：

- `AuthController`：登录相关接口入口。
- `MembersController`：会员查询相关接口入口。
- `WeatherForecastController`：模板示例控制器，后续可以删除或忽略。

### 2.1.3 Middleware 目录

路径：`backend/Api/Middleware/`

当前包含：

```text
GlobalExceptionMiddleware.cs
```

`GlobalExceptionMiddleware` 用于全局异常处理。它的作用是：

1. 捕获后续请求处理中抛出的异常。
2. 根据异常类型映射 HTTP 状态码。
3. 使用统一响应结构 `ApiResponse<object>` 返回错误信息。
4. 避免 Controller 中到处写重复的 try-catch。

当前异常映射规则大致为：

| 异常类型 | HTTP 状态码 | 错误码 |
| --- | --- | --- |
| `DomainException` | 400 | `DOMAIN_ERROR` |
| `KeyNotFoundException` | 404 | `NOT_FOUND` |
| 其他异常 | 500 | `INTERNAL_SERVER_ERROR` |

### 2.1.4 appsettings.json

路径：`backend/Api/appsettings.json`

`appsettings.json` 存放运行时配置，例如数据库连接字符串。

虽然数据库访问实现位于 `Infrastructure`，但是连接字符串仍然放在 `Api` 项目中，这是因为：

- `Api` 是启动项目，负责提供运行环境配置。
- `Infrastructure` 负责读取配置并装配数据库访问能力。
- 配置属于宿主环境，不属于领域规则。

也就是说：

```text
Api 提供配置 → Infrastructure 读取配置 → EF Core 连接 Oracle
```

## 2.2 Application 项目

路径：`backend/Application/`

`Application` 是应用层，负责组织一个具体业务用例的执行流程。

当前目录：

```text
Application/
  DTOs/
  Extensions/
  Interfaces/
  Services/
  Application.csproj
```

应用层的核心原则：

- Controller 不直接处理业务流程，而是调用 Application Service。
- Application Service 负责串联仓储、领域实体、领域规则、DTO 转换。
- Application 层不直接依赖 EF Core，不直接访问 Oracle。
- 前后端传输对象统一放在 `Application/DTOs`。

### 2.2.1 DTOs 目录

路径：`backend/Application/DTOs/`

DTO 是 Data Transfer Object，即数据传输对象。

它用于隔离：

- 前端请求和后端内部实体。
- 后端响应和数据库实体。
- HTTP 接口模型和领域模型。

当前已有 DTO：

```text
DTOs/
  ApiResponse.cs
  LoginRequestDto.cs
  LoginResultDto.cs
  MemberDto.cs
```

#### ApiResponse.cs

`ApiResponse<T>` 是统一接口响应结构。

当前统一响应格式包含：

| 字段 | 作用 |
| --- | --- |
| `Code` | 响应码，例如 `SUCCESS`、`NOT_FOUND`、`DOMAIN_ERROR` |
| `Message` | 响应消息 |
| `Data` | 实际返回数据 |
| `TraceId` | 请求追踪 ID，便于排查问题 |

成功响应通常使用：

```csharp
ApiResponse<T>.Success(data, traceId)
```

失败响应通常使用：

```csharp
ApiResponse<T>.Failure(code, message, traceId)
```

目前项目已经统一约定：

- 所有前后端传输对象放在 `Application/DTOs`。
- `Api` 层不再单独放 DTO 或 Response 模型。

#### LoginRequestDto.cs

登录请求 DTO，主要用于接收前端登录表单。

当前登录设计预留了通用登录能力，关键字段包括：

- `LoginType`：登录类型，例如 `member`，后续可扩展为 `coach`、`admin`。
- `Identifier`：用户名或用户 ID。
- `PhoneNumber`：手机号。

#### LoginResultDto.cs

登录结果 DTO，用于返回登录成功后的关键信息。

通常包括：

- 用户类型。
- 用户 ID。
- 展示名称。
- 登录成功后的跳转路径。

#### MemberDto.cs

会员展示 DTO，用于将会员信息返回给前端。

它避免直接把 `Domain.Entities.Member` 暴露给接口层，从而避免：

- 导航属性泄露。
- 数据库字段细节泄露。
- 未来实体变化直接影响前端接口。

### 2.2.2 Interfaces 目录

路径：`backend/Application/Interfaces/`

该目录存放应用服务接口。

当前已有：

```text
Interfaces/
  IAuthAppService.cs
  IMemberAppService.cs
```

应用服务接口的作用：

- 定义 Application 层对外提供的用例能力。
- 让 Controller 依赖接口，而不是依赖具体实现。
- 便于后续测试、替换实现和保持分层清晰。

命名规范：

```text
I + 业务名 + AppService
```

例如：

```text
IAuthAppService
IMemberAppService
ICoachAppService
IPaymentAppService
```

### 2.2.3 Services 目录

路径：`backend/Application/Services/`

该目录存放应用服务实现。

当前已有：

```text
Services/
  AuthAppService.cs
  MemberAppService.cs
```

应用服务负责：

1. 校验请求中的必要业务参数。
2. 调用 Domain 层仓储接口获取实体。
3. 调用领域实体方法执行业务规则。
4. 组织业务流程。
5. 将领域实体转换为 DTO。
6. 向 Controller 返回 DTO。

应用服务不应该：

- 直接写 SQL。
- 直接依赖 `AppDbContext`。
- 直接处理 HTTP 状态码。
- 返回 `IActionResult`。
- 直接读取 `HttpContext`。

### 2.2.4 Extensions 目录

路径：`backend/Application/Extensions/`

当前包含：

```text
DependencyInjection.cs
```

该文件用于集中注册 Application 层服务。

例如当前已注册：

```csharp
services.AddScoped<IAuthAppService, AuthAppService>();
services.AddScoped<IMemberAppService, MemberAppService>();
```

后续每新增一个应用服务，都应该在这里注册。

例如新增教练业务时，应补充：

```csharp
services.AddScoped<ICoachAppService, CoachAppService>();
```

## 2.3 Domain 项目

路径：`backend/Domain/`

`Domain` 是整个后端最核心的一层，存放业务概念和业务规则。

当前目录：

```text
Domain/
  Common/
  Constants/
  Entities/
  Enums/
  Exceptions/
  Interfaces/
  Domain.csproj
```

Domain 层应尽量保持纯净，不关心：

- HTTP。
- Controller。
- EF Core 的具体实现。
- Oracle 连接字符串。
- 前端页面。

### 2.3.1 Entities 目录

路径：`backend/Domain/Entities/`

该目录存放领域实体。

当前实体主要根据数据库表生成，例如：

```text
Member.cs
Coach.cs
Employee.cs
Venue.cs
Equipment.cs
PaymentOrder.cs
PaymentDetail.cs
Voucher.cs
...
```

实体代表系统中的核心业务对象。

例如：

- `Member`：会员。
- `Coach`：教练。
- `Venue`：场馆。
- `Equipment`：器材。
- `PaymentOrder`：支付订单。
- `Voucher`：券。

#### partial 实体扩展

当前 `Member` 使用了 partial class 扩展：

```text
Member.cs
Member.Domain.cs
```

其中：

- `Member.cs`：保留基础属性，主要对应数据库字段。
- `Member.Domain.cs`：放会员相关领域行为和规则。

这样做的好处是：

1. 数据结构和领域行为分开，代码更清晰。
2. 后续如果重新生成实体，尽量减少覆盖领域行为的风险。
3. 可以逐步把业务规则沉淀到实体方法中。

`Member.Domain.cs` 中已有的领域行为包括：

- 获取会员状态。
- 设置会员状态。
- 激活会员。
- 冻结会员。
- 注销会员。
- 获取性别枚举。
- 设置性别。
- 判断是否有效会员。

### 2.3.2 Common 目录

路径：`backend/Domain/Common/`

当前包含：

```text
Entity.cs
IAggregateRoot.cs
```

该目录用于存放领域模型的通用基础类型。

常见用途包括：

- 定义实体基类。
- 定义聚合根标记接口。
- 后续扩展领域事件、审计字段等基础能力。

当前项目还处于初期，很多实体仍然是直接由数据库表生成的普通实体。后续如果要进一步强化领域模型，可以逐步让核心实体继承通用实体基类或实现聚合根接口。

### 2.3.3 Constants 目录

路径：`backend/Domain/Constants/`

当前包含：

```text
PagingConstants.cs
```

该目录存放领域或应用中反复使用的常量。

例如分页常量：

- 默认页码。
- 默认每页数量。
- 最大每页数量。

这样做可以避免分页数字散落在 Controller、Service、Repository 中。

### 2.3.4 Enums 目录

路径：`backend/Domain/Enums/`

当前包含：

```text
BookingStatus.cs
Gender.cs
MemberStatus.cs
PaymentStatus.cs
```

枚举用于表达固定范围的业务状态。

例如：

- `MemberStatus`：会员状态。
- `Gender`：性别。
- `BookingStatus`：预约状态。
- `PaymentStatus`：支付状态。

枚举的价值是：

- 让业务含义更清晰。
- 减少字符串魔法值。
- 让状态判断更安全。

由于当前 Oracle 表中部分状态仍然以字符串或数字形式存储，所以可以在实体扩展方法中进行转换。例如 `Member.Domain.cs` 中把数据库中的状态字符串转换为 `MemberStatus` 枚举。

### 2.3.5 Exceptions 目录

路径：`backend/Domain/Exceptions/`

当前包含：

```text
DomainException.cs
```

`DomainException` 用于表达业务规则异常。

例如：

- 当前会员状态不可登录。
- 已注销会员不能冻结。
- 登录信息不匹配。

应用服务或领域实体可以抛出 `DomainException`，然后由 `Api` 层的全局异常中间件统一转换为 HTTP 400 响应。

这样可以避免每个 Controller 手动处理业务异常。

### 2.3.6 Interfaces 目录

路径：`backend/Domain/Interfaces/`

当前包含：

```text
IRepository.cs
IMemberRepository.cs
IUnitOfWork.cs
```

该目录存放领域层定义的抽象接口。

#### IRepository.cs

通用仓储接口，定义基础数据操作：

- 根据 ID 查询。
- 新增实体。
- 更新实体。
- 删除实体。

#### IMemberRepository.cs

会员仓储接口，定义会员相关的查询能力。

例如：

- 根据手机号查询会员。
- 根据姓名和手机号查询会员。
- 判断身份证是否已存在。
- 分页查询会员。

#### IUnitOfWork.cs

工作单元接口，用于统一提交数据库变更。

当一个业务流程中涉及多个仓储操作时，可以通过 UnitOfWork 统一调用 `SaveChangesAsync`。

## 2.4 Infrastructure 项目

路径：`backend/Infrastructure/`

`Infrastructure` 是基础设施层，负责和外部技术细节打交道。

当前目录：

```text
Infrastructure/
  Data/
  Extensions/
  Migrations/
  Repositories/
  Infrastructure.csproj
```

当前基础设施层主要处理：

- EF Core。
- Oracle 数据库连接。
- DbContext。
- Repository 实现。
- UnitOfWork 实现。
- Migration 迁移文件。

### 2.4.1 Data 目录

路径：`backend/Infrastructure/Data/`

当前包含：

```text
AppDbContext.cs
UnitOfWork.cs
```

#### AppDbContext.cs

`AppDbContext` 是 EF Core 的数据库上下文。

它负责：

- 定义数据库表对应的 `DbSet`。
- 配置实体和表之间的映射关系。
- 配置字段类型、长度、主键、外键、默认值、注释等。
- 作为 EF Core 访问 Oracle 的核心入口。

虽然实体位于 `Domain/Entities`，但实体如何映射到数据库表，是 Infrastructure 层的职责。

#### UnitOfWork.cs

`UnitOfWork` 是 `IUnitOfWork` 的实现。

它内部依赖 `AppDbContext`，通过调用：

```csharp
_context.SaveChangesAsync(cancellationToken)
```

统一提交数据库变更。

### 2.4.2 Repositories 目录

路径：`backend/Infrastructure/Repositories/`

当前包含：

```text
Repository.cs
MemberRepository.cs
```

#### Repository.cs

通用仓储实现，对应 `Domain.Interfaces.IRepository<TEntity, TId>`。

提供基础操作：

- `GetByIdAsync`
- `AddAsync`
- `Update`
- `Remove`

#### MemberRepository.cs

会员仓储实现，对应 `Domain.Interfaces.IMemberRepository`。

它负责实现会员相关数据库查询，例如：

- 根据手机号查会员。
- 根据姓名和手机号查会员。
- 判断身份证是否存在。
- 分页查询会员。

Repository 可以使用 EF Core，但只应存在于 Infrastructure 层。

Application 层应该依赖 `IMemberRepository` 接口，而不是直接依赖 `MemberRepository` 实现。

### 2.4.3 Migrations 目录

路径：`backend/Infrastructure/Migrations/`

该目录存放 EF Core Migration 文件。

Migration 用于记录数据库结构变更。

在 Code First 模式下，推荐流程是：

1. 修改领域实体或数据库映射。
2. 生成 Migration。
3. 检查 Migration 内容是否符合预期。
4. 应用 Migration 到数据库。

当前项目虽然曾从已有 Oracle 表反向生成过实体和映射，但后续开发倾向于 Code First。因此迁移文件仍然是后续维护数据库结构的重要依据。

### 2.4.4 Extensions 目录

路径：`backend/Infrastructure/Extensions/`

当前包含：

```text
DependencyInjection.cs
```

该文件集中注册 Infrastructure 层服务。

当前主要做了：

- 从配置读取 `DefaultConnection` 数据库连接字符串。
- 注册 `AppDbContext` 并启用 Oracle Provider。
- 注册 `IMemberRepository` 到 `MemberRepository`。
- 注册 `IUnitOfWork` 到 `UnitOfWork`。

后续新增仓储实现时，也应该在这里注册。

例如：

```csharp
services.AddScoped<ICoachRepository, CoachRepository>();
```

## 2.5 Shared 项目

路径：`backend/Shared/`

`Shared` 是共享层，目前暂未承载核心业务逻辑。

后续可以用于放置跨层通用但不属于具体业务的基础能力，例如：

- 通用工具类型。
- 通用扩展方法。
- 通用结果类型。
- 通用时间处理。

但需要注意：不要把业务规则随意放到 `Shared`。业务规则应优先放在 `Domain` 或 `Application`。

## 3. 当前已形成的业务调用链

以“会员登录”为例，当前完整调用链大致为：

```text
frontend/src/views/LoginView.vue
  ↓
frontend/src/api/auth.ts
  ↓
frontend/src/api/http.ts
  ↓
POST /api/auth/login
  ↓
Api.Controllers.AuthController
  ↓
Application.Interfaces.IAuthAppService
  ↓
Application.Services.AuthAppService
  ↓
Domain.Interfaces.IMemberRepository
  ↓
Infrastructure.Repositories.MemberRepository
  ↓
Infrastructure.Data.AppDbContext
  ↓
Oracle 数据库
```

这个调用链体现了当前前后端协作的基本范式：

1. 前端页面只处理交互、表单和页面状态。
2. 前端页面不直接写 axios 或 fetch 请求，而是调用 `frontend/src/api` 下的业务接口函数。
3. `frontend/src/api/http.ts` 统一处理 HTTP 请求、后端统一响应结构和错误消息。
4. 后端 Controller 只调用 Application Service。
5. Application Service 编排用例。
6. Application Service 通过 Domain 中的仓储接口访问数据。
7. Infrastructure 实现仓储接口，真正访问数据库。
8. 领域规则尽量沉淀在 Domain 实体、枚举、异常中。

### 3.1 前端 API Client 层

当前前端已经增加独立的 API Client 层：

```text
frontend/src/api/
  http.ts
  auth.ts
  members.ts
```

各文件职责如下：

| 文件 | 职责 |
| --- | --- |
| `http.ts` | 通用 HTTP 封装，统一 baseURL、请求方法、`ApiResponse<T>` 解析和 `ApiError` 错误处理 |
| `auth.ts` | 登录相关接口封装，例如 `login` |
| `members.ts` | 会员相关接口封装，例如 `getMemberProfile` |

前端页面应调用业务接口函数，例如：

```ts
const result = await login(request)
```

而不是在页面中直接写：

```ts
axios.post('/api/auth/login', request)
```

这样可以保证页面层更干净，也方便后续统一处理 token、请求头、错误提示和接口地址。

## 4. 后端开发规范

## 4.1 目录放置规范

后续新增业务时，优先遵循当前横向目录规范，不要在 `Application` 下按业务再新建 `Member`、`Auth` 这种模块目录。

推荐结构：

```text
Application/
  DTOs/
    XxxDto.cs
    CreateXxxRequestDto.cs
    UpdateXxxRequestDto.cs
  Interfaces/
    IXxxAppService.cs
  Services/
    XxxAppService.cs

Domain/
  Entities/
    Xxx.cs
    Xxx.Domain.cs
  Enums/
    XxxStatus.cs
  Exceptions/
    DomainException.cs
  Interfaces/
    IXxxRepository.cs

Infrastructure/
  Repositories/
    XxxRepository.cs
  Data/
    AppDbContext.cs

Api/
  Controllers/
    XxxController.cs
```

## 4.2 命名规范

详情见

```text
docs/guides/文件命名规范.md
```


## 4.3 各层禁止事项

### Api 层不要做的事

- 不直接写业务规则。
- 不直接调用 `AppDbContext`。
- 不直接调用 EF Core。
- 不直接拼接数据库查询。
- 不直接返回领域实体。

### Application 层不要做的事

- 不依赖 `Infrastructure`。
- 不直接使用 `AppDbContext`。
- 不处理 HTTP 细节。
- 不返回 `IActionResult`。
- 不读取 `HttpContext`。

### Domain 层不要做的事

- 不引用 ASP.NET Core。
- 不引用 EF Core 具体实现。
- 不读取配置文件。
- 不连接数据库。
- 不关心前端页面。

### Infrastructure 层不要做的事

- 不写 Controller。
- 不决定 HTTP 状态码。
- 不承载复杂业务编排。
- 不把 EF Core 细节泄露给 Application 层。

## 5. 后端新增业务开发流程

下面以新增一个“教练查询业务”为例，说明后续开发一个后端业务的标准流程。

## 5.1 第一步：确认业务需求和接口形态

先明确：

1. 前端需要什么页面或功能？
2. 需要哪些接口？
3. 请求参数是什么？
4. 返回数据是什么？
5. 涉及哪些领域实体？
6. 是否需要新增数据库字段或表？

例如教练查询：

```text
GET /api/coaches/{id}
GET /api/coaches?pageNumber=1&pageSize=10
```

## 5.2 第二步：检查或补充 Domain 实体

如果实体已经存在，例如 `Coach.cs` 已存在，则先检查字段是否满足需求。

如果需要补充领域行为，不建议直接把所有业务逻辑写在 Application Service 中，可以新增 partial 文件：

```text
Domain/Entities/Coach.Domain.cs
```

用于放置教练相关的领域行为。

例如：

```csharp
public partial class Coach
{
    public bool IsActive()
    {
        return Status == "1";
    }
}
```

## 5.3 第三步：补充枚举、常量和领域异常

如果业务存在固定状态，应优先考虑枚举。

例如：

```text
Domain/Enums/CoachStatus.cs
```

如果业务存在通用常量，应放入：

```text
Domain/Constants/
```

如果违反业务规则，应抛出：

```csharp
throw new DomainException("具体业务错误信息");
```

## 5.4 第四步：定义仓储接口

在 `Domain/Interfaces/` 下定义仓储接口。

例如：

```text
Domain/Interfaces/ICoachRepository.cs
```

示例：

```csharp
public interface ICoachRepository : IRepository<Coach, int>
{
    Task<IReadOnlyList<Coach>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
```

注意：仓储接口放在 Domain，因为 Application 需要依赖抽象，而不是依赖 Infrastructure 的具体实现。

## 5.5 第五步：实现仓储

在 `Infrastructure/Repositories/` 下实现仓储。

例如：

```text
Infrastructure/Repositories/CoachRepository.cs
```

仓储实现可以使用：

- `AppDbContext`
- `DbSet`
- `AsNoTracking`
- `Where`
- `OrderBy`
- `Skip`
- `Take`
- `ToListAsync`

查询接口建议默认使用 `AsNoTracking()`，减少 EF Core 变更跟踪开销。

## 5.6 第六步：注册仓储依赖注入

在 `Infrastructure/Extensions/DependencyInjection.cs` 中注册：

```csharp
services.AddScoped<ICoachRepository, CoachRepository>();
```

否则 Application Service 无法通过构造函数注入拿到仓储实现。

## 5.7 第七步：定义 DTO

在 `Application/DTOs/` 下定义 DTO。

例如：

```text
Application/DTOs/CoachDto.cs
Application/DTOs/CreateCoachRequestDto.cs
Application/DTOs/UpdateCoachRequestDto.cs
```

DTO 应该只包含前端需要的字段，不要直接复用领域实体。

## 5.8 第八步：定义应用服务接口

在 `Application/Interfaces/` 下定义应用服务接口。

例如：

```text
Application/Interfaces/ICoachAppService.cs
```

示例：

```csharp
public interface ICoachAppService
{
    Task<CoachDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CoachDto>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
```

## 5.9 第九步：实现应用服务

在 `Application/Services/` 下实现应用服务。

例如：

```text
Application/Services/CoachAppService.cs
```

应用服务主要负责：

1. 接收 DTO 或简单参数。
2. 调用仓储接口获取实体。
3. 执行业务规则。
4. 将实体转换为 DTO。
5. 返回 DTO 给 Controller。

## 5.10 第十步：注册应用服务依赖注入

在 `Application/Extensions/DependencyInjection.cs` 中注册：

```csharp
services.AddScoped<ICoachAppService, CoachAppService>();
```

否则 Controller 无法通过构造函数注入应用服务。

## 5.11 第十一步：编写 Controller

在 `Api/Controllers/` 下新增 Controller。

例如：

```text
Api/Controllers/CoachesController.cs
```

Controller 中只做 HTTP 层工作：

- 标注路由。
- 接收参数。
- 调用应用服务。
- 包装 `ApiResponse<T>`。
- 返回 HTTP 状态码。

示例结构：

```csharp
[ApiController]
[Route("api/[controller]")]
public class CoachesController : ControllerBase
{
    private readonly ICoachAppService _coachAppService;

    public CoachesController(ICoachAppService coachAppService)
    {
        _coachAppService = coachAppService;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<CoachDto>>> GetById(int id, CancellationToken cancellationToken)
    {
        var coach = await _coachAppService.GetByIdAsync(id, cancellationToken);
        if (coach is null)
        {
            return NotFound(ApiResponse<object>.Failure("NOT_FOUND", $"未找到编号为 {id} 的教练。", HttpContext.TraceIdentifier));
        }

        return Ok(ApiResponse<CoachDto>.Success(coach, HttpContext.TraceIdentifier));
    }
}
```

## 5.12 第十二步：处理数据库结构变更

如果只是查询已有表，一般不需要新增 Migration。

如果需要新增表、字段、索引或关系，则需要：

1. 修改实体或 `AppDbContext` 映射。
2. 生成 Migration。
3. 检查 Migration 内容。
4. 应用到数据库。

常见命令示例：

```powershell
dotnet ef migrations add AddCoachFeature --project backend/Infrastructure --startup-project backend/Api
```

```powershell
dotnet ef database update --project backend/Infrastructure --startup-project backend/Api
```

实际执行前应确认 Oracle 连接配置正确，并确认迁移内容不会误改已有数据表。

## 5.13 第十三步：构建和联调

后端代码修改完成后，应至少执行一次构建：

```powershell
dotnet build backend/backend.slnx
```

如果 API 项目正在运行导致 DLL 被锁定，可以先停止运行中的 API，或者使用临时输出目录进行构建验证。

接口联调时重点检查：

1. Controller 路由是否正确。
2. 请求参数是否能绑定。
3. Application Service 是否被正确注入。
4. Repository 是否被正确注入。
5. 数据库查询是否符合预期。
6. 返回结构是否统一为 `ApiResponse<T>`。
7. 异常是否被 `GlobalExceptionMiddleware` 正确处理。

## 6. 当前会员登录业务说明

当前已经实现了一个基础会员登录业务。

### 6.1 登录入口

接口：

```text
POST /api/auth/login
```

Controller：

```text
Api/Controllers/AuthController.cs
```

应用服务：

```text
Application/Services/AuthAppService.cs
```

请求 DTO：

```text
Application/DTOs/LoginRequestDto.cs
```

返回 DTO：

```text
Application/DTOs/LoginResultDto.cs
```

### 6.2 登录逻辑

当前支持：

- 会员 ID + 手机号登录。
- 会员姓名 + 手机号登录。

当前 `LoginType` 只支持：

```text
member
```

这样设计是为了后续扩展：

- 会员登录：跳转会员档案页。
- 教练登录：跳转教练工作台或教练档案页。
- 管理员登录：跳转管理后台。

登录成功后，后端返回 `TargetPath`，前端根据该路径跳转。

## 7. 当前会员查询业务说明

当前已有基础会员查询接口。

Controller：

```text
Api/Controllers/MembersController.cs
```

应用服务：

```text
Application/Services/MemberAppService.cs
```

仓储接口：

```text
Domain/Interfaces/IMemberRepository.cs
```

仓储实现：

```text
Infrastructure/Repositories/MemberRepository.cs
```

已支持：

```text
GET /api/members/{id}
GET /api/members?pageNumber=1&pageSize=10
```

分页参数修正规则放在 Application 层，分页常量放在 Domain 层的 `PagingConstants`。

## 8. Code First 开发注意事项

当前项目曾经基于 Oracle 已有表进行过实体和 DbContext 生成，但后续开发倾向于 Code First。

在 Code First 模式下应注意：

1. 实体变化要谨慎，因为会影响数据库结构。
2. 修改 `AppDbContext` 映射后，应检查生成的 Migration。
3. 不要未经确认直接对生产或共享 Oracle 数据库执行破坏性迁移。
4. 对已有表做变更前，应先确认表结构、数据和课程设计要求。
5. Migration 文件提交前应人工阅读，确认没有误删表、误删列或错误修改字段类型。

## 9. 推荐的后端开发顺序总结

后续每开发一个新业务，推荐按以下顺序：

```text
1. 明确接口需求和业务规则
2. 检查或补充 Domain 实体
3. 补充 Domain 枚举、常量、异常或实体行为
4. 在 Domain/Interfaces 定义仓储接口
5. 在 Infrastructure/Repositories 实现仓储
6. 在 Infrastructure/Extensions 注册仓储
7. 在 Application/DTOs 定义请求和响应 DTO
8. 在 Application/Interfaces 定义应用服务接口
9. 在 Application/Services 实现应用服务
10. 在 Application/Extensions 注册应用服务
11. 在 Api/Controllers 编写接口入口
12. 如涉及结构变更，生成并检查 Migration
13. 构建后端项目
14. 启动 API，与前端联调
```

## 10. 当前结构下最重要的约定

请后续开发尽量保持以下约定：

1. `Api` 只做 HTTP 入口。
2. `Application` 负责编排用例和 DTO。
3. `Domain` 存放实体、规则、枚举、常量、异常、仓储接口。
4. `Infrastructure` 实现数据库访问。
5. 所有 DTO 统一放在 `Application/DTOs`。
6. 不在 `Api` 下重复创建 DTO 或 Response 模型。
7. 不在 `Application` 下按业务新建 `Auth`、`Member` 等模块目录，继续使用当前横向目录结构。
8. Controller 不直接访问数据库。
9. Application Service 不直接依赖 EF Core。
10. Repository 不决定 HTTP 响应。
11. 业务错误优先抛出 `DomainException`，由全局异常中间件统一处理。
12. 接口返回统一使用 `ApiResponse<T>`。
13. 新增服务和仓储后，必须记得在对应的 `DependencyInjection.cs` 中注册。

## 11. 常见问题排查

### 11.1 Controller 提示服务无法注入

常见原因：

- 应用服务没有在 `Application/Extensions/DependencyInjection.cs` 中注册。
- 仓储没有在 `Infrastructure/Extensions/DependencyInjection.cs` 中注册。
- 接口和实现命名空间引用错误。

### 11.2 接口返回 500

排查顺序：

1. 查看后端控制台异常日志。
2. 检查数据库连接字符串。
3. 检查 Repository 查询是否报错。
4. 检查实体映射是否和 Oracle 表结构一致。
5. 检查是否有未处理的空值或字段类型不匹配。

### 11.3 业务校验失败返回 400

如果抛出的是 `DomainException`，全局异常中间件会返回 400。

这通常表示请求不满足业务规则，例如：

- 登录信息不匹配。
- 当前会员状态不可登录。
- 已注销会员不能执行某些操作。

### 11.4 数据库字段和实体不一致

排查：

1. 检查 `Domain/Entities` 中实体属性。
2. 检查 `Infrastructure/Data/AppDbContext.cs` 中映射配置。
3. 检查 Oracle 实际表结构。
4. 如果采用 Code First，检查 Migration 是否正确。

## 12. 后续可优化方向

当前后端已经具备基础业务开发范式，但后续可以继续优化：

1. 删除模板生成的 `WeatherForecastController` 和 `WeatherForecast`。
2. 补充分页响应结构，例如 `PagedResult<T>`。
3. 增加请求参数验证机制。
4. 增加登录后的身份认证和授权机制。
5. 统一日志格式。
6. 完善 OpenAPI 文档。
7. 处理依赖包安全警告。
8. 为核心应用服务补充单元测试。

这些优化不影响当前基础业务继续开发，可以在后续迭代中逐步完成。
