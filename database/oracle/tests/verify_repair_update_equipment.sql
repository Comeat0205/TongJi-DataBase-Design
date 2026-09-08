-- I · 报修联动触发器验证
-- 注意：本脚本会临时插入、更新测试数据，必须在独立 SQLcl 会话中运行。
-- 测试结束会回滚表数据，但 Oracle 序列号不会回退，因此允许出现编号间隔。
SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE NONE

DECLARE
    c_test_equip_id CONSTANT NUMBER := -900000001;
    v_first_record_id  NUMBER;
    v_second_record_id NUMBER;
    v_existing_count   NUMBER;
    v_status           EQUIPMENT.STATUS%TYPE;

    PROCEDURE assert_equipment_status(p_expected_status IN VARCHAR2)
    IS
    BEGIN
        SELECT STATUS
        INTO v_status
        FROM EQUIPMENT
        WHERE EQUIP_ID = c_test_equip_id;

        IF v_status <> p_expected_status THEN
            RAISE_APPLICATION_ERROR(
                -20002,
                '器材状态应为“' || p_expected_status || '”，实际为“' || v_status || '”'
            );
        END IF;
    END assert_equipment_status;
BEGIN
    SAVEPOINT before_repair_trigger_test;

    SELECT COUNT(*)
    INTO v_existing_count
    FROM EQUIPMENT
    WHERE EQUIP_ID = c_test_equip_id;

    IF v_existing_count > 0 THEN
        RAISE_APPLICATION_ERROR(-20003, '测试器材编号已被占用，请先更换 c_test_equip_id');
    END IF;

    INSERT INTO EQUIPMENT (EQUIP_ID, EQUIP_NAME, STATUS)
    VALUES (c_test_equip_id, 'I-报修联动测试器材', '正常');

    SELECT SEQ_REPAIRRECORD.NEXTVAL INTO v_first_record_id FROM DUAL;
    INSERT INTO REPAIRRECORD (RECORD_ID, EQUIP_ID, STATUS, DESCRIPTION)
    VALUES (v_first_record_id, c_test_equip_id, '待处理', '第一条测试报修');
    assert_equipment_status('维护中');

    SELECT SEQ_REPAIRRECORD.NEXTVAL INTO v_second_record_id FROM DUAL;
    INSERT INTO REPAIRRECORD (RECORD_ID, EQUIP_ID, STATUS, DESCRIPTION)
    VALUES (v_second_record_id, c_test_equip_id, '维修中', '第二条测试报修');

    UPDATE REPAIRRECORD
    SET STATUS = '已完成'
    WHERE RECORD_ID = v_first_record_id;
    assert_equipment_status('维护中');

    UPDATE REPAIRRECORD
    SET STATUS = '已完成'
    WHERE RECORD_ID = v_second_record_id;
    assert_equipment_status('正常');

    ROLLBACK TO before_repair_trigger_test;
    DBMS_OUTPUT.PUT_LINE('PASS: 报修联动状态矩阵验证通过，测试表数据已回滚。');
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK TO before_repair_trigger_test;
        RAISE;
END;
/
