-- I · 报修状态与器材状态联动
-- 只要同一器材仍有未完成报修（当前为“待处理”或“维修中”），器材保持“维护中”；
-- 该器材所有报修均为“已完成”时，器材恢复“正常”。
CREATE OR REPLACE TRIGGER TRG_REPAIR_UPDATE_EQUIPMENT
FOR INSERT OR DELETE OR UPDATE OF STATUS, EQUIP_ID ON REPAIRRECORD
COMPOUND TRIGGER
    TYPE t_equip_id_map IS TABLE OF NUMBER INDEX BY VARCHAR2(40);
    g_equip_ids t_equip_id_map;

    PROCEDURE remember_equip_id(p_equip_id IN NUMBER)
    IS
    BEGIN
        IF p_equip_id IS NOT NULL THEN
            g_equip_ids(TO_CHAR(p_equip_id)) := p_equip_id;
        END IF;
    END remember_equip_id;

    AFTER EACH ROW IS
    BEGIN
        IF INSERTING OR UPDATING THEN
            remember_equip_id(:NEW.EQUIP_ID);
        END IF;

        IF DELETING OR UPDATING THEN
            remember_equip_id(:OLD.EQUIP_ID);
        END IF;
    END AFTER EACH ROW;

    AFTER STATEMENT IS
        v_key      VARCHAR2(40);
        v_equip_id NUMBER;
    BEGIN
        v_key := g_equip_ids.FIRST;

        WHILE v_key IS NOT NULL LOOP
            v_equip_id := g_equip_ids(v_key);

            UPDATE EQUIPMENT e
            SET STATUS = CASE
                WHEN EXISTS (
                    SELECT 1
                    FROM REPAIRRECORD r
                    WHERE r.EQUIP_ID = v_equip_id
                      AND NVL(r.STATUS, '待处理') <> '已完成'
                ) THEN '维护中'
                ELSE '正常'
            END
            WHERE e.EQUIP_ID = v_equip_id;

            v_key := g_equip_ids.NEXT(v_key);
        END LOOP;
    END AFTER STATEMENT;
END TRG_REPAIR_UPDATE_EQUIPMENT;
/

SELECT TRIGGER_NAME, TABLE_NAME, STATUS
FROM USER_TRIGGERS
WHERE TRIGGER_NAME = 'TRG_REPAIR_UPDATE_EQUIPMENT';

SELECT NAME, TYPE, LINE, POSITION, TEXT
FROM USER_ERRORS
WHERE NAME = 'TRG_REPAIR_UPDATE_EQUIPMENT'
ORDER BY SEQUENCE;
