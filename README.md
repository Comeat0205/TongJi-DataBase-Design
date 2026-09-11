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
