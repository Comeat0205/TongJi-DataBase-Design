-- E · 23:00 自动签退存储过程
-- 将所有未退场记录标记为自动签退（CHECK_OUT_MODE = '1'），
-- 重置对应场馆容量为 0。
-- 容量日志（CAPACITYLOG）改由应用每 10 分钟采样写入，本过程不再插入日志。
CREATE OR REPLACE PROCEDURE sp_auto_checkout(
    p_result  OUT NUMBER,
    p_message OUT VARCHAR2
) AS
    v_count       NUMBER := 0;
    v_now         DATE := CAST(CURRENT_TIMESTAMP AS DATE);

    CURSOR c_open IS
        SELECT CHECK_IN_OUT_ID, VENUE_ID
        FROM CHECKINOUT
        WHERE CHECK_OUT_TIME IS NULL
        FOR UPDATE;
BEGIN
    FOR r IN c_open LOOP
        -- 自动签退
        UPDATE CHECKINOUT
        SET CHECK_OUT_TIME = v_now,
            CHECK_OUT_MODE = '1'
        WHERE CURRENT OF c_open;

        v_count := v_count + 1;
    END LOOP;

    -- 重置所有场馆容量为 0（签退后场馆应清空）
    UPDATE VENUE
    SET CURRENT_CAPACITY = 0
    WHERE CURRENT_CAPACITY > 0;

    COMMIT;
    p_result  := 1;
    p_message := '自动签退完成，共处理 ' || v_count || ' 条记录。';
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        p_result  := 0;
        p_message := '自动签退失败：' || SQLERRM;
END;
/
