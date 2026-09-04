# Oracle 脚本目录

课程要求视图、存储过程、函数、触发器在 **Oracle 库中创建**，脚本保存在此目录并提交 Git。

```text
database/oracle/
├── views/          ← 视图，文件名 v_*.sql
├── procedures/     ← 存储过程，文件名 sp_*.sql
├── functions/      ← 函数，文件名 fn_*.sql
├── triggers/       ← 触发器，文件名 trg_*.sql
└── seed_*.sql      ← 各模块演示数据（可重复执行）
```

## 怎么执行

1. 用 SQL Developer 或 DBeaver 连接组内共用 Oracle。
2. 打开对应 `.sql` 文件。
3. 使用 **Run Script（F5）** 执行整份脚本（函数、过程、触发器末尾需要 `/`）。
4. 在 SQL 窗口里验证（见各脚本上方注释或 `docs/初步分工.md` 第三节）。

## 已有示例（可改可删，按实际表结构核对）

| 脚本 | 类型 | 建议负责人 |
| --- | --- | --- |
| `views/v_member_booking_summary.sql` | 视图 | J |
| `functions/fn_is_card_valid.sql` | 函数 | D |
| `triggers/trg_checkin_update_venue.sql` | 触发器 | E |
| `procedures/sp_book_group_course.sql` | 存储过程 | F |
| `procedures/sp_book_personal_training.sql` | 存储过程 | G |
| `seed_personal_training_demo.sql` | 演示数据 | G |

C、G、H、I 在对应子目录 **新建** 自己的 `.sql`，不要覆盖他人文件。演示数据用固定编号区间，只清自己的 seed ID。

**G 课包状态：** 共享库检查约束只允许 `有效` / `已用完` / `已过期`（不要写 `'1'` / `'2'`）。

## 执行后怎么确认

```sql
SELECT view_name FROM user_views ORDER BY view_name;

SELECT object_name, object_type
FROM user_objects
WHERE object_type IN ('PROCEDURE', 'FUNCTION')
ORDER BY object_name;

SELECT trigger_name, table_name, status FROM user_triggers ORDER BY trigger_name;
```
