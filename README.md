# TJ-GYM 健身房管理系统

## 项目简介
本项目为数据库课程设计——健身房管理系统。
项目采用前后端分离架构：
- **前端**：Vue 3 + TypeScript + Vite + Pinia
- **后端**：.NET 10 + EF Core (Clean Architecture / DDD 分层架构)

## 技术栈

| 部分   | 技术                                              |
| ------ | ------------------------------------------------- |
| 前端   | Vue 3、TypeScript、Vite、Pinia、Vue Router、Axios |
| 后端   | .NET 10、ASP.NET Core Web API、EF Core            |
| 数据库 | Oracle 18c+（组内共用实例）                       |
| 协作   | Git + GitHub（提交记录作为分工依据）              |

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

具体启动步骤可参考 docs/guides/TJ-GYM 项目构建与运行说明.md

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
├── database/                                    # Oracle 脚本（课程要求入库对象）
│   └── oracle/
│       ├── README.md                            # 脚本目录说明与执行方式
│       ├── views/                               # 视图，文件名 v_*.sql
│       │   └── v_member_booking_summary.sql     #   会员团课预约汇总（J）
│       ├── procedures/                          # 存储过程，文件名 sp_*.sql
│       │   └── sp_book_group_course.sql         #   团课预约（F）
│       ├── functions/                           # 函数，文件名 fn_*.sql
│       │   └── fn_is_card_valid.sql             #   判断会员卡是否有效（D）
│       └── triggers/                            # 触发器，文件名 trg_*.sql
│           └── trg_checkin_update_venue.sql     #   入场后更新场馆人数（E）
│
├── backend/                                     # 后端 .NET 解决方案（Clean Architecture）
│   ├── backend.slnx                             # 解决方案入口
│   │
│   ├── Api/                                     # 接口层（启动项目）
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs                #   POST /api/auth/login（会员/员工/教练）
│   │   │   └── MembersController.cs             #   GET  /api/members/{id}
│   │   ├── Middleware/
│   │   │   └── GlobalExceptionMiddleware.cs     # 全局异常处理
│   │   ├── Properties/
│   │   │   └── launchSettings.json              # 本地端口（HTTP 5078）
│   │   ├── Extensions/                          # API 扩展（预留）
│   │   ├── Mapping/                             # 对象映射（预留）
│   │   ├── Validators/                          # 请求校验（预留）
│   │   ├── Program.cs                           # 应用启动入口
│   │   ├── appsettings.json                     # 运行时配置（含 Oracle 连接）
│   │   ├── appsettings.Development.json
│   │   ├── Api.http                             # 接口调试文件
│   │   └── Api.csproj
│   │
│   ├── Application/                             # 应用层（业务编排 + DTO）
│   │   ├── DTOs/
│   │   │   ├── ApiResponse.cs                   # 统一响应格式
│   │   │   ├── LoginRequestDto.cs               # 登录请求
│   │   │   ├── LoginResultDto.cs                # 登录结果（含 targetPath）
│   │   │   └── MemberDto.cs                     # 会员信息
│   │   ├── Interfaces/
│   │   │   ├── IAuthAppService.cs
│   │   │   └── IMemberAppService.cs
│   │   ├── Services/
│   │   │   ├── AuthAppService.cs                # 三端登录（Member/Employee/Coach）
│   │   │   └── MemberAppService.cs
│   │   ├── Extensions/
│   │   │   └── DependencyInjection.cs           # 注册 Application 服务
│   │   ├── Common/                              # 应用层公共（预留）
│   │   ├── Helpers/                             # 辅助类（预留）
│   │   └── Application.csproj
│   │
│   ├── Domain/                                  # 领域层（实体、规则、仓储接口）
│   │   ├── Entities/                            # 领域实体（≈ Oracle 27 张表）
│   │   │   ├── Member.cs                        #   MEMBER
│   │   │   ├── Member.Domain.cs                 #   Member 业务行为扩展
│   │   │   ├── Coach.cs                         #   COACH
│   │   │   ├── Employee.cs                      #   EMPLOYEE
│   │   │   ├── Venue.cs                         #   VENUE
│   │   │   ├── Equipment.cs                     #   EQUIPMENT
│   │   │   ├── Inspectiontask.cs                #   INSPECTIONTASK
│   │   │   ├── Repairrecord.cs                  #   REPAIRRECORD
│   │   │   ├── Capacitylog.cs                   #   CAPACITYLOG
│   │   │   ├── Cardproduct.cs                   #   CARDPRODUCT
│   │   │   ├── MemberBenefitCard.cs             #   MEMBER_BENEFIT_CARD
│   │   │   ├── CountCardExtension.cs            #   COUNT_CARD_EXTENSION
│   │   │   ├── TimeCardExtension.cs             #   TIME_CARD_EXTENSION
│   │   │   ├── Checkinout.cs                    #   CHECKINOUT
│   │   │   ├── CourseType.cs                    #   COURSE_TYPE
│   │   │   ├── Groupcourse.cs                   #   GROUPCOURSE
│   │   │   ├── GroupCourseBooking.cs            #   GROUP_COURSE_BOOKING
│   │   │   ├── PersonalCourse.cs                #   PERSONAL_COURSE
│   │   │   ├── Personalpackage.cs               #   PERSONALPACKAGE
│   │   │   ├── Ptbooking.cs                     #   PTBOOKING
│   │   │   ├── TimeSlotTemplate.cs              #   TIME_SLOT_TEMPLATE
│   │   │   ├── TimeSlotInstance.cs              #   TIME_SLOT_INSTANCE
│   │   │   ├── CoachSchedule.cs                 #   COACH_SCHEDULE
│   │   │   ├── MemberSchedule.cs                #   MEMBER_SCHEDULE
│   │   │   ├── PaymentOrder.cs                  #   PAYMENT_ORDER
│   │   │   ├── PaymentDetail.cs                 #   PAYMENT_DETAIL
│   │   │   ├── Voucher.cs                       #   VOUCHER
│   │   │   └── PriceList.cs                     #   PRICE_LIST
│   │   ├── Enums/
│   │   │   ├── MemberStatus.cs
│   │   │   ├── Gender.cs
│   │   │   ├── BookingStatus.cs
│   │   │   └── PaymentStatus.cs
│   │   ├── Interfaces/
│   │   │   ├── IRepository.cs                   # 通用仓储接口
│   │   │   ├── IMemberRepository.cs
│   │   │   ├── ICoachRepository.cs              # 教练登录查询
│   │   │   ├── IEmployeeRepository.cs           # 员工登录查询
│   │   │   └── IUnitOfWork.cs
│   │   ├── Common/
│   │   │   ├── Entity.cs
│   │   │   └── IAggregateRoot.cs
│   │   ├── Constants/
│   │   │   └── PagingConstants.cs
│   │   ├── Exceptions/
│   │   │   └── DomainException.cs
│   │   └── Domain.csproj
│   │
│   ├── Infrastructure/                          # 基础设施层（EF Core + Repository）
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs                  # EF Core 上下文（27 表映射）
│   │   │   └── UnitOfWork.cs
│   │   ├── Repositories/
│   │   │   ├── Repository.cs                    # 通用仓储基类
│   │   │   ├── MemberRepository.cs
│   │   │   ├── CoachRepository.cs
│   │   │   └── EmployeeRepository.cs
│   │   ├── Migrations/
│   │   │   ├── 20260708030810_InitialCreate.cs
│   │   │   ├── 20260708030810_InitialCreate.Designer.cs
│   │   │   └── AppDbContextModelSnapshot.cs
│   │   ├── Configuration/                       # EF 配置（预留）
│   │   ├── Extensions/
│   │   │   └── DependencyInjection.cs           # 注册 DbContext、Repository
│   │   └── Infrastructure.csproj
│   │
│   ├── Shared/                                  # 共享层（JWT 等，预留）
│   │   ├── JWT/                                 # JWT 工具（预留）
│   │   ├── Common/                              # 公共类型（预留）
│   │   ├── Helpers/                             # 辅助（预留）
│   │   ├── Utils/                               # 工具（预留）
│   │   └── Shared.csproj
│   │
│   └── tests/                                   # 测试项目（预留）
│
└── frontend/                                    # 前端 Vue 3 + TypeScript + Vite
    ├── public/
    │   └── favicon.ico
    ├── src/
    │   ├── main.ts                              # 应用入口
    │   ├── App.vue                              # 根组件
    │   │
    │   ├── api/                                 # API 封装（按业务模块拆分）
    │   │   ├── http.ts                          #   axios 实例、ApiResponse 解析
    │   │   ├── auth.ts                          #   登录（member/employee/coach）
    │   │   └── members.ts                       #   会员 GET /members/{id}
    │   │
    │   ├── config/
    │   │   └── nav.ts                           # 三端侧栏菜单与 active 规则
    │   │
    │   ├── data/
    │   │   └── home-dashboard-mock.ts           # 三端首页演示数据（联调前）
    │   │
    │   ├── layouts/                             # 三端布局壳
    │   │   ├── MemberLayout.vue                 #   会员端 + 预览
    │   │   ├── AdminLayout.vue                  #   员工端 + 预览
    │   │   ├── CoachLayout.vue                  #   教练端 + 预览
    │   │   └── GuestLayout.vue                  #   访客（预留）
    │   │
    │   ├── router/
    │   │   ├── index.ts                         # 路由入口 + 登录守卫
    │   │   └── routes.ts                        # 三端子路由、占位 meta、组件绑定
    │   │
    │   ├── stores/
    │   │   ├── auth.ts                          # 登录会话（localStorage）
    │   │   └── counter.ts                       # Vue 模板示例（可删）
    │   │
    │   ├── components/
    │   │   ├── AppNav.vue                       # 侧栏导航
    │   │   ├── ui/                              # 通用 UI 块
    │   │   │   ├── PageHeader.vue               #   页头
    │   │   │   ├── PlaceholderPanel.vue         #   「站点骨架 · 占位页」面板
    │   │   │   └── StateCard.vue                #   加载/错误态
    │   │   ├── HelloWorld.vue                   # Vue 脚手架残留（可删）
    │   │   ├── TheWelcome.vue                   # Vue 脚手架残留（可删）
    │   │   ├── WelcomeItem.vue                  # Vue 脚手架残留（可删）
    │   │   ├── icons/                           # 脚手架图标（可删）
    │   │   └── __tests__/                       # 组件测试
    │   │
    │   ├── views/
    │   │   ├── LoginView.vue                    # 登录页（三 Tab + 预览链接）
    │   │   ├── member/
    │   │   │   ├── MemberHomeView.vue           #   会员首页仪表盘（mock）
    │   │   │   └── MemberProfileView.vue        #   档案 GET + 编辑表单占位
    │   │   ├── admin/
    │   │   │   └── AdminHomeView.vue            #   员工工作台仪表盘（mock）
    │   │   ├── coach/
    │   │   │   └── CoachHomeView.vue            #   教练工作台仪表盘（mock）
    │   │   ├── shared/
    │   │   │   └── ModulePlaceholderView.vue    #   各模块未实现前的通用占位页
    │   │   ├── HomeView.vue                     # Vue 脚手架残留（可删）
    │   │   └── AboutView.vue                    # Vue 脚手架残留（可删）
    │   │
    │   ├── lib/                                 # 工具库（预留）
    │   └── assets/
    │       ├── main.css                         # 全局样式入口
    │       ├── base.css
    │       ├── tj-theme.css                     # TJ-GYM 主题变量
    │       └── logo.svg
    │
    ├── index.html                               # HTML 入口
    ├── vite.config.ts                           # Vite（含 /api 代理到 5078）
    ├── vitest.config.ts
    ├── package.json
    ├── package-lock.json
    ├── tsconfig.json
    ├── tsconfig.app.json
    ├── tsconfig.node.json
    ├── tsconfig.vitest.json
    ├── eslint.config.ts
    ├── env.d.ts
    └── README.md
```


