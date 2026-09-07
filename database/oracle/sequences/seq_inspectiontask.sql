-- I · 巡检任务主键序列
-- 仅在序列不存在时创建；首次值从当前 INSPECTIONTASK 最大主键之后开始。
DECLARE
    v_sequence_count PLS_INTEGER;
    v_start_with      NUMBER;
BEGIN
    SELECT COUNT(*)
    INTO v_sequence_count
    FROM USER_SEQUENCES
    WHERE SEQUENCE_NAME = 'SEQ_INSPECTIONTASK';

    IF v_sequence_count = 0 THEN
        SELECT NVL(MAX(TASK_ID), 0) + 1
        INTO v_start_with
        FROM INSPECTIONTASK;

        IF v_start_with > 9999999999 THEN
            RAISE_APPLICATION_ERROR(-20004, 'INSPECTIONTASK 主键已超出 NUMBER(10) 范围');
        END IF;

        EXECUTE IMMEDIATE
            'CREATE SEQUENCE SEQ_INSPECTIONTASK ' ||
            'START WITH ' || TO_CHAR(v_start_with, 'FM9999999990') || ' ' ||
            'INCREMENT BY 1 CACHE 20 NOCYCLE';
    END IF;
END;
/

SELECT SEQUENCE_NAME, MIN_VALUE, INCREMENT_BY, CACHE_SIZE, LAST_NUMBER
FROM USER_SEQUENCES
WHERE SEQUENCE_NAME = 'SEQ_INSPECTIONTASK';
