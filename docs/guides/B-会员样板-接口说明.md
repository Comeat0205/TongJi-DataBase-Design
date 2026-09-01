# B · 接口说明

基址：`http://localhost:5078`  
前端开发时由 Vite 将 `/api` 代理到上述地址。  
统一响应：

```json
{
  "code": "SUCCESS",
  "message": "OK",
  "data": {},
  "traceId": "..."
}
```

常见失败：`DOMAIN_ERROR`（400）、`NOT_FOUND`（404）、`INTERNAL_SERVER_ERROR`（500）。

---

## 1. 登录

`POST /api/auth/login`

相对原 main：请求体由 `identifier` + `phoneNumber` 改为 `loginName` + `password`。

### 请求

```json
{
  "loginType": "member",
  "loginName": "demo_member",
  "password": "Demo@123456"
}
```


| 字段        | 说明                              |
| --------- | ------------------------------- |
| loginType | `member` / `employee` / `coach` |
| loginName | USERS.LOGIN_NAME                |
| password  | 明文，后端 BCrypt 校验 PASSWORD_HASH   |


### 成功 200 · data


| 字段          | 说明                                         |
| ----------- | ------------------------------------------ |
| userType    | 与 loginType 对应                             |
| userId      | **业务 ID**（会员/员工/教练主键，不是 USERS.USER_ID）     |
| displayName | 业务表姓名                                      |
| targetPath  | `/member/home`、`/admin/home`、`/coach/home` |


### 逻辑

查 USERS → BCrypt.Verify → STATUS=1 → 按 Tab 用 USER_ID 找 MEMBER/EMPLOYEE/COACH。  
凭证错误统一返回「账号或密码错误」。

### 调用链

`LoginView.handleLogin` → `api/auth.ts` `login()` → `AuthController.Login` → `AuthAppService.LoginAsync`

---

## 2. 查询会员档案

`GET /api/members/{id}`

原 main 已有。成功返回 MemberDto；不存在 404。

前端：`getMemberProfile(id)`，档案页加载时调用。

### 成功 data（MemberDto）

memberId, name, phoneNumber, idCard, memberLevel, gender, birthday, registerDate, status

---

## 3. 会员分页（无前端页面）

`GET /api/members?pageNumber=1&pageSize=10`

仅按 MEMBER_ID 排序分页，无姓名/手机筛选。前端未封装，员工端查询页归 C。

---

## 4. 更新会员档案（新增）

`PUT /api/members/{id}`

### 请求

```json
{
  "name": "演示会员",
  "phoneNumber": "13800000001",
  "gender": "M",
  "birthday": "2000-01-01",
  "idCard": ""
}
```

可改：姓名、手机、性别、生日、身份证。  
不可改：会员编号、等级、注册时间、状态。  
性别入库：`M` / `F`。

前端：`MemberProfileView.saveProfile` → `updateMember`  
后端：`MembersController.Update` → `MemberAppService.UpdateAsync`

---

## 5. 会员注册（新增）

`POST /api/members`  
成功 201。

### 请求

```json
{
  "loginName": "new_member",
  "password": "Demo@123456",
  "name": "新会员",
  "phoneNumber": "13900000002",
  "gender": "M",
  "birthday": "2000-01-01",
  "idCard": null
}
```

必填：loginName、password（至少 6 位）、name。

顺序：INSERT USERS（BCrypt）→ SaveChanges → INSERT MEMBER（写 USER_ID）→ SaveChanges。

前端：`RegisterView.handleRegister` → `registerMember`，成功后再调登录接口。  
后端：`MembersController.Register` → `MemberAppService.RegisterAsync`

---

## 6. 演示账号（测试数据）

脚本：`database/oracle/SEED_USER_DEMO.sql`  
在 DBeaver / SQL Developer 中 **Run Script（F5）** 执行。


| 登录名         | 明文密码（仅注释/文档） | 库内                     | 关联                                      |
| ----------- | ------------ | ---------------------- | --------------------------------------- |
| demo_member | Demo@123456  | PASSWORD_HASH 为 BCrypt | 优先绑定 USER_ID 为空的最小 MEMBER_ID；否则新建「演示会员」 |
| demo_coach  | Demo@123456  | 同上                     | 绑定 USER_ID 为空的最小 COACH_ID               |


约束：MEMBER.USER_ID 唯一（UK_MEMBER_USER_ID）。  
主键：无序列时 MAX+1。  
不要把云库连接密码写进本脚本。

员工演示账号本阶段未插入，需 C 按「先 USERS 再 EMPLOYEE」自建。

调试请求示例见：`backend/Api/Api.http`