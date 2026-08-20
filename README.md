# TJ-GYM 健身房管理系统

## 项目简介
本项目为数据库课程设计——健身房管理系统。
项目采用前后端分离架构：
- **前端**：Vue 3 + TypeScript + Vite + Pinia
- **后端**：.NET 10 + EF Core (Clean Architecture / DDD 分层架构)

## 目录结构

```text
TJ-GYM/
├── README.md                         # 项目简介与快速入口
├── .gitignore                        # Git 忽略规则（bin/、obj/、node_modules/ 等）
│
├── docs/                             # 项目文档
│   ├── guides/                       # 构建、运行与开发说明
│   │   ├── TJ-GYM 项目构建与运行说明.md     # 构建、运行、联调、协作规范
│   │   └── TJ-GYM 后端架构与开发流程说明.md # 后端分层规则与开发流程
│   └── design/                       # 课程设计原文
│       ├── 健身房会员与课程预约管理系统_数据库设计文档.doc      # 数据库设计
│       └── 健身房会员与课程预约管理系统_系统需求分析文档.docx   # 需求分析
│
├── database/                         # Oracle 脚本（课程要求入库对象）
│   └── oracle/
│       ├── README.md                 # 脚本目录说明与执行方式
│       ├── views/                    # 视图，文件名 v_*.sql
│       │   └── v_member_booking_summary.sql  #   会员团课预约汇总
│       ├── procedures/               # 存储过程，文件名 sp_*.sql
│       │   └── sp_book_group_course.sql      #   团课预约
│       ├── functions/                # 函数，文件名 fn_*.sql
│       │   └── fn_is_card_valid.sql          #   判断会员卡是否有效
│       └── triggers/                 # 触发器，文件名 trg_*.sql
│           └── trg_checkin_update_venue.sql  #   入场后更新场馆人数
│
├── backend/                          # 后端 .NET 解决方案（Clean Architecture / DDD）
│   ├── backend.slnx                  # 解决方案入口
│   │
│   ├── Api/                          # 接口层（启动项目）
│   │   ├── Controllers/              # HTTP 控制器
│   │   │   ├── AuthController.cs     #   POST /api/auth/login
│   │   │   └── MembersController.cs  #   GET  /api/members/{id}
│   │   ├── Middleware/
│   │   │   └── GlobalExceptionMiddleware.cs  # 全局异常处理
│   │   ├── Properties/
│   │   │   └── launchSettings.json   # 本地端口（HTTP 5078）
│   │   ├── Program.cs                # 应用启动入口
│   │   ├── appsettings.json          # 运行时配置（含数据库连接）
│   │   ├── appsettings.Development.json
│   │   ├── Api.http                  # 接口调试文件
│   │   └── Api.csproj
│   │
│   ├── Application/                  # 应用层（业务编排 + DTO）
│   │   ├── DTOs/
│   │   │   ├── ApiResponse.cs        # 统一响应格式
│   │   │   ├── LoginRequestDto.cs    # 登录请求
│   │   │   ├── LoginResultDto.cs     # 登录结果
│   │   │   └── MemberDto.cs          # 会员信息
│   │   ├── Interfaces/
│   │   │   ├── IAuthAppService.cs
│   │   │   └── IMemberAppService.cs
│   │   ├── Services/
│   │   │   ├── AuthAppService.cs
│   │   │   └── MemberAppService.cs
│   │   ├── Extensions/
│   │   │   └── DependencyInjection.cs  # 注册 Application 服务
│   │   └── Application.csproj
│   │
│   ├── Domain/                       # 领域层（实体、规则、仓储接口）
│   │   ├── Entities/                 # 领域实体（≈ Oracle 表；DbContext 映射 27 张）
│   │   │   ├── Member.cs             #   MEMBER
│   │   │   ├── Member.Domain.cs      #   Member 业务行为扩展
│   │   │   ├── Coach.cs              #   COACH
│   │   │   ├── Venue.cs              #   VENUE
│   │   │   ├── Capacitylog.cs        #   CAPACITYLOG
│   │   │   ├── Cardproduct.cs        #   CARDPRODUCT
│   │   │   ├── MemberBenefitCard.cs  #   MEMBER_BENEFIT_CARD
│   │   │   ├── CountCardExtension.cs #   COUNT_CARD_EXTENSION
│   │   │   ├── TimeCardExtension.cs  #   TIME_CARD_EXTENSION
│   │   │   ├── Checkinout.cs         #   CHECKINOUT
│   │   │   ├── CourseType.cs         #   COURSE_TYPE
│   │   │   ├── Groupcourse.cs        #   GROUPCOURSE
│   │   │   ├── GroupCourseBooking.cs #   GROUP_COURSE_BOOKING
│   │   │   ├── PersonalCourse.cs     #   PERSONAL_COURSE
│   │   │   ├── Personalpackage.cs    #   PERSONALPACKAGE
│   │   │   ├── Ptbooking.cs          #   PTBOOKING
│   │   │   ├── TimeSlotTemplate.cs   #   TIME_SLOT_TEMPLATE
│   │   │   ├── TimeSlotInstance.cs   #   TIME_SLOT_INSTANCE
│   │   │   ├── CoachSchedule.cs      #   COACH_SCHEDULE
│   │   │   ├── MemberSchedule.cs     #   MEMBER_SCHEDULE
│   │   │   ├── PaymentOrder.cs       #   PAYMENT_ORDER
│   │   │   ├── PaymentDetail.cs      #   PAYMENT_DETAIL
│   │   │   └── PriceList.cs          #   PRICE_LIST
│   │   ├── Enums/
│   │   │   ├── MemberStatus.cs
│   │   │   ├── Gender.cs
│   │   │   ├── BookingStatus.cs
│   │   │   └── PaymentStatus.cs
│   │   ├── Interfaces/
│   │   │   ├── IRepository.cs        # 通用仓储接口
│   │   │   ├── IMemberRepository.cs  # 会员仓储接口
│   │   │   └── IUnitOfWork.cs        # 工作单元
│   │   ├── Common/
│   │   │   ├── Entity.cs
│   │   │   └── IAggregateRoot.cs
│   │   ├── Constants/
│   │   │   └── PagingConstants.cs
│   │   ├── Exceptions/
│   │   │   └── DomainException.cs
│   │   └── Domain.csproj
│   │
│   ├── Infrastructure/               # 基础设施层（数据库访问）
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs       # EF Core 上下文（表映射）
│   │   │   └── UnitOfWork.cs         # 统一提交事务
│   │   ├── Repositories/
│   │   │   ├── Repository.cs         # 通用仓储基类
│   │   │   └── MemberRepository.cs   # 会员仓储实现
│   │   ├── Migrations/               # EF Core 迁移文件
│   │   │   ├── 20260708030810_InitialCreate.cs
│   │   │   ├── 20260708030810_InitialCreate.Designer.cs
│   │   │   └── AppDbContextModelSnapshot.cs
│   │   ├── Extensions/
│   │   │   └── DependencyInjection.cs  # 注册 DbContext、Repository
│   │   └── Infrastructure.csproj
│   │
│   └── Shared/                       # 共享层（预留，当前无业务代码）
│       └── Shared.csproj
│
└── frontend/                         # 前端 Vue 项目
    ├── public/
    │   └── favicon.ico
    ├── src/
    │   ├── main.ts                   # 应用入口
    │   ├── App.vue                   # 根组件
    │   ├── api/                      # API 请求封装（按业务模块拆分）
    │   │   ├── http.ts               #   axios 实例、ApiResponse 解析
    │   │   ├── auth.ts               #   登录接口
    │   │   └── members.ts            #   会员接口
    │   ├── views/                    # 页面
    │   │   ├── LoginView.vue         #   登录页
    │   │   ├── MemberProfileView.vue #   会员档案页
    │   │   ├── HomeView.vue          #   模板页（可后续删除）
    │   │   └── AboutView.vue         #   模板页（可后续删除）
    │   ├── router/
    │   │   └── index.ts              # 路由配置与登录守卫
    │   ├── stores/
    │   │   ├── auth.ts               # 登录会话状态
    │   │   └── counter.ts            # 模板示例（可后续删除）
    │   ├── components/               # 可复用组件
    │   │   ├── HelloWorld.vue
    │   │   ├── TheWelcome.vue
    │   │   ├── WelcomeItem.vue
    │   │   ├── icons/
    │   │   └── __tests__/
    │   └── assets/                   # 样式与静态资源
    │       ├── main.css
    │       ├── base.css
    │       └── logo.svg
    ├── index.html                    # HTML 入口
    ├── vite.config.ts                # Vite 配置（含 /api 代理）
    ├── vitest.config.ts              # 测试配置
    ├── package.json                  # 依赖与脚本
    ├── package-lock.json
    ├── tsconfig.json                 # TypeScript 配置
    ├── tsconfig.app.json
    ├── tsconfig.node.json
    ├── tsconfig.vitest.json
    ├── eslint.config.ts              # ESLint 配置
    ├── env.d.ts
    └── README.md
```

## 快速启动

### 前端
```bash
cd frontend
npm install
npm run dev
```

### 后端
1. 确保安装了 .NET 10 SDK。
2. 配置 `backend/Api/appsettings.json` 中的数据库连接字符串(上传的文件中已经配置好，若想在自己创建的数据库中调试可以修改部分内容)。
3. 运行后端：
```bash
cd backend/Api
dotnet run
```
