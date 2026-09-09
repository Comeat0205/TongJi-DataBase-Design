-- I · 报修状态与器材状态联动
-- 未完成报修（待处理/维修中）→ 器材 STATUS = '0'（维修）
-- 全部报修已完成 → 器材 STATUS = '1'（正常）
-- 与应用层编码一致，避免写入中文导致 ORA-01722 / CHECK 失败。
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
                ) THEN '0'
                ELSE '1'
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
