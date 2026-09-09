-- 放宽器材状态检查约束，支持：正常/停用/维修（及历史编码）
-- 1=正常, 0=停用, 2=维修；同时兼容中文与触发器写入的「维护中」

ALTER TABLE EQUIPMENT DROP CONSTRAINT EQUIPMENT_CHECK;

ALTER TABLE EQUIPMENT ADD CONSTRAINT EQUIPMENT_CHECK
    CHECK (STATUS IN ('0', '1', '2', '正常', '停用', '维修', '维护中'));

-- 可选：确认约束
SELECT constraint_name, search_condition
FROM user_constraints
WHERE table_name = 'EQUIPMENT' AND constraint_name = 'EQUIPMENT_CHECK';
