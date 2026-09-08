# TJ-GYM 项目构建与运行说明

本文档面向团队成员，说明如何在本地拉取、构建、运行和验证 TJ-GYM 项目。

项目包含两个主要部分：

```text
TJ-GYM/
  backend/    后端 ASP.NET Core Web API
  frontend/   前端 Vue + Vite 项目
  docs/       项目说明文档
```

## 1. 环境准备

## 1.1 必需环境

团队成员本地需要安装：

| 工具 | 用途 | 当前项目使用情况 |
| --- | --- | --- |
| Git | 拉取和提交代码 | 必需 |
| .NET SDK | 构建和运行后端 | 后端项目目标框架为 `net10.0` |
| Node.js | 构建和运行前端 | `package.json` 要求 `^22.18.0 || >=24.12.0` |
| npm | 安装前端依赖、运行脚本 | 随 Node.js 安装 |
| Oracle 数据库访问环境 | 后端连接数据库 | 当前使用 Oracle EF Core Provider |

建议每位成员先确认版本：

```powershell
git --version
```

```powershell
dotnet --version
```

```powershell
node -v
```

```powershell
npm -v
```

## 1.2 推荐开发工具

推荐使用：

- Visual Studio：适合查看和运行后端解决方案。
- VS Code / Trae / WebStorm：适合前端开发。
- Oracle SQL Developer / DataGrip：适合查看 Oracle 数据库。

## 2. 获取项目代码

首次参与开发时，在本地选择一个目录，然后执行：

```powershell
git clone <项目仓库地址>
```

进入项目根目录：

```powershell
cd TJ-GYM
```

项目根目录应能看到：

```text
backend/
frontend/
docs/
README.md
```

如果是已经拉取过项目，更新代码时执行：

```powershell
git pull
```

## 3. 后端项目说明

后端路径：

```text
backend/
```

后端解决方案文件：

```text
backend/backend.slnx
```

后端启动项目：

```text
backend/Api/Api.csproj
```

当前后端主要分层：

```text
backend/Api              HTTP 入口层
backend/Application      应用服务和 DTO
backend/Domain           领域实体、枚举、规则、接口
backend/Infrastructure   EF Core、Oracle、Repository、Migration
backend/Shared           共享基础层
```

更详细的后端架构说明见：

```text
docs/guides/TJ-GYM 后端架构与开发流程说明.md
```

## 4. 后端配置

## 4.1 数据库连接配置

后端数据库连接字符串位于：

```text
backend/Api/appsettings.json
```

当前配置项为：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  }
}
```

注意：

1. `Api` 是启动项目，所以运行环境配置放在 `Api/appsettings.json`。
2. `Infrastructure` 会读取这个连接字符串并注册 `AppDbContext`。
3. 团队成员运行前应确认本机能访问配置中的 Oracle 数据库地址和端口。
4. 如果连接字符串包含密码，不建议随意公开或发到聊天群中。

## 4.2 后端运行端口

后端启动配置位于：

```text
backend/Api/Properties/launchSettings.json
```

当前 HTTP 地址为：

```text
http://localhost:5078
```

当前 HTTPS 地址为：

```text
https://localhost:7113
```

前端开发服务器已经代理 `/api` 到：

```text
http://localhost:5078
```

因此本地联调时，建议后端使用 HTTP 端口 `5078` 运行。

## 5. 构建后端

在项目根目录执行：

```powershell
dotnet build backend/backend.slnx
```

或者进入 `backend` 目录后执行：

```powershell
dotnet build backend.slnx
```

如果构建成功，说明后端代码可以正常编译。

## 6. 运行后端

推荐在项目根目录执行：

```powershell
dotnet run --project backend/Api/Api.csproj --launch-profile http
```

运行成功后，后端监听：

```text
http://localhost:5078
```

如果需要停止后端，在运行终端中按：

```text
Ctrl + C
```

## 7. 后端常见问题

## 7.1 数据库连接失败

可能原因：

1. Oracle 服务不可访问。
2. 本机网络无法连接数据库服务器。
3. 连接字符串账号或密码错误。
4. 数据库服务名或端口错误。

排查建议：

1. 确认数据库服务器地址和端口能访问。
2. 使用数据库工具测试账号密码。
3. 检查 `backend/Api/appsettings.json` 中的 `DefaultConnection`。

## 7.3 Microsoft.OpenApi 安全警告

当前构建可能出现 `Microsoft.OpenApi 2.0.0` 的安全警告。

这属于依赖包安全提示，不代表业务代码无法运行。后续可以通过升级相关包版本处理。

## 8. 前端项目说明

前端路径：

```text
frontend/
```

当前前端技术栈：

| 技术 | 用途 |
| --- | --- |
| Vue | 页面和组件 |
| Vite | 前端开发服务器和构建工具 |
| TypeScript | 类型检查 |
| Pinia | 状态管理 |
| Vue Router | 前端路由 |
| Axios | HTTP 请求 |

当前前端 API Client 层位于：

```text
frontend/src/api/
  http.ts
  auth.ts
  members.ts
```

说明：

- 页面不直接写 axios 请求。
- 页面调用 `src/api` 下的业务接口函数。
- `http.ts` 统一解析后端的 `ApiResponse<T>`。
- 业务接口文件按模块拆分，例如 `auth.ts`、`members.ts`。

## 9. 安装前端依赖

首次运行前端前，进入前端目录：

```powershell
cd frontend
```

安装依赖：

```powershell
npm install
```

如果团队中已经提交了 `package-lock.json`，建议统一使用 `npm install`，避免不同成员依赖版本差异过大。

## 10. 运行前端开发服务器

进入前端目录：

```powershell
cd frontend
```

运行：

```powershell
npm run dev
```

Vite 会输出本地访问地址，通常类似：

```text
http://localhost:5173
```

浏览器打开该地址即可访问前端页面。

## 11. 前后端联调运行顺序

本项目本地联调建议开两个终端。

### 终端一：运行后端

在项目根目录执行：

```powershell
dotnet run --project backend/Api/Api.csproj --launch-profile http
```

确认后端运行在：

```text
http://localhost:5078
```

### 终端二：运行前端

进入前端目录：

```powershell
cd frontend
```

运行：

```powershell
npm run dev
```

打开 Vite 输出的地址，例如：

```text
http://localhost:5173
```

当前前端代理配置在：

```text
frontend/vite.config.ts
```

其中：

```ts
proxy: {
  '/api': {
    target: 'http://localhost:5078',
    changeOrigin: true,
  },
}
```

因此前端请求：

```text
/api/auth/login
```

会被代理到后端：

```text
http://localhost:5078/api/auth/login
```

## 12. 构建前端

进入前端目录：

```powershell
cd frontend
```

执行：

```powershell
npm run build
```

该命令会同时执行：

1. TypeScript 类型检查。
2. Vite 生产构建。

构建成功后，前端产物通常位于：

```text
frontend/dist/
```

## 13. 预览前端构建产物

如果需要预览生产构建结果，先执行：

```powershell
npm run build
```

再执行：

```powershell
npm run preview
```

然后打开终端输出的预览地址。

## 14. 推荐的日常开发流程

团队成员日常开发建议按以下顺序：

```text
1. git pull 获取最新代码
2. 确认后端数据库连接可用
3. dotnet build backend/backend.slnx 验证后端可构建
4. cd frontend
5. npm install 安装或更新前端依赖
6. npm run build 验证前端可构建
7. 启动后端
8. 启动前端
9. 开始开发和联调
10. 提交前再次构建相关部分
```

## 15. 提交代码前检查

提交代码前建议至少检查：

### 后端

```powershell
dotnet build backend/backend.slnx
```

### 前端

```powershell
cd frontend
npm run build
```

如果只改了前端，也至少执行前端构建。

如果只改了后端，也至少执行后端构建。

如果改动涉及前后端接口联调，建议两个都构建。

## 16. 团队协作注意事项

1. 不要提交本地临时构建目录，例如 `bin/`、`obj/`、`dist/`、`.build-check/`。
2. 不要随意修改公共数据库连接信息。
3. 修改后端接口时，要同步更新前端 API Client 层。
4. 修改后端 DTO 时，要检查前端 TypeScript 类型是否需要同步。
5. 新增前端接口时，优先放到 `frontend/src/api/`，不要直接在页面中写 axios。
6. 新增后端业务时，遵循 `docs/backend-development-guide.md` 中的分层规则。
7. 如果修改数据库结构，必须先确认 Migration 内容，再执行数据库更新。
8. 提交前尽量保证前后端构建通过。

## 17. 常用命令汇总

### 根目录执行

```powershell
dotnet build backend/backend.slnx
```

```powershell
dotnet run --project backend/Api/Api.csproj --launch-profile http
```

### frontend 目录执行

```powershell
npm install
```

```powershell
npm run dev
```

```powershell
npm run build
```

```powershell
npm run preview
```

## 18. 新成员快速启动清单

新成员第一次运行项目时，可以按这个清单操作：

```text
1. 安装 Git、.NET SDK、Node.js
2. git clone 项目仓库
3. 进入 TJ-GYM 根目录
4. dotnet build backend/backend.slnx
5. cd frontend
6. npm install
7. npm run build
8. 回到项目根目录
9. 启动后端：dotnet run --project backend/Api/Api.csproj --launch-profile http
10. 新开终端进入 frontend
11. 启动前端：npm run dev
12. 浏览器访问 Vite 输出的地址
```
