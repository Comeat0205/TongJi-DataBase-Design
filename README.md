# TJ-GYM 健身房管理系统

## 项目简介
本项目为数据库课程设计——健身房管理系统。
项目采用前后端分离架构：
- **前端**：Vue 3 + TypeScript + Vite + Pinia
- **后端**：.NET 10 + EF Core (Clean Architecture / DDD 分层架构)

## 目录结构
- `frontend/`: 前端 Vue 项目
- `backend/`: 后端 .NET 解决方案
- `docs/`: 数据库设计文档（ER图、数据字典、SQL脚本等）

## 快速启动

### 前端
```bash
cd frontend
npm install
npm run dev
```

### 后端
1. 确保安装了 .NET 10 SDK。
2. 配置 `backend/Api/appsettings.json` 中的数据库连接字符串。
3. 运行后端：
```bash
cd backend/Api
dotnet run
```
