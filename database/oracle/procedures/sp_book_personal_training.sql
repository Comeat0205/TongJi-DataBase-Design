-- G · 私教预约
-- 规则：课包可用、剩余次数足够、会员/教练同一时刻无冲突后创建待教练确认的预约。
-- 提交预约时立即扣减 1 次剩余次数；教练拒绝或会员取消（上课前 24 小时外）时返还。
-- 教练端消课只标记上课完成，不再二次扣次。
BEGIN
    EXECUTE IMMEDIATE
        'ALTER TABLE PTBOOKING ADD (CONSUME_STATUS CHAR(1) DEFAULT ''0'' NOT NULL)';
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE <> -1430 THEN
            RAISE;
        END IF;
END;
/

BEGIN
    EXECUTE IMMEDIATE
        'ALTER TABLE PTBOOKING ADD (CONSUMED_TIME DATE)';
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE <> -1430 THEN
            RAISE;
        END IF;
END;
/

DECLARE
    v_start_with NUMBER;
BEGIN
    SELECT NVL(MAX(PT_BOOKING_ID), 0) + 1
    INTO v_start_with
    FROM PTBOOKING;

    EXECUTE IMMEDIATE
        'CREATE SEQUENCE seq_ptbooking_id START WITH ' || v_start_with ||
        ' INCREMENT BY 1 NOCACHE';
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE <> -955 THEN
            RAISE;
        END IF;
END;
/

CREATE OR REPLACE PROCEDURE sp_book_personal_training (
    p_member_id    IN  NUMBER,
    p_package_id   IN  NUMBER,
    p_session_time IN  DATE,
    p_booking_id   OUT NUMBER,
    p_result       OUT NUMBER,
    p_message      OUT VARCHAR2
)
IS
    v_package_member   NUMBER;
    v_coach_id         NUMBER;
    v_remaining        NUMBER;
    v_expire_date      DATE;
    v_package_status   VARCHAR2(20);
    v_conflict_count   NUMBER;
BEGIN
    p_booking_id := NULL;
    p_result := 0;

    IF p_session_time <= SYSDATE THEN
        p_message := '预约时间必须晚于当前时间';
        RETURN;
    END IF;

    SELECT MEMBER_ID, COACH_ID, REMAINING_SESSIONS, EXPIRE_DATE, PACKAGE_STATUS
    INTO v_package_member, v_coach_id, v_remaining, v_expire_date, v_package_status
    FROM PERSONALPACKAGE
    WHERE PACKAGE_ID = p_package_id
    FOR UPDATE;

    IF v_package_member <> p_member_id THEN
        p_message := '该课包不属于当前会员';
        RETURN;
    END IF;

    IF v_remaining <= 0 THEN
        p_message := '课包没有剩余次数';
        RETURN;
    END IF;

    IF TRUNC(v_expire_date) < TRUNC(SYSDATE) THEN
        p_message := '课包已经过期';
        RETURN;
    END IF;

    IF UPPER(TRIM(v_package_status)) IN
        ('2', 'INACTIVE', 'EXPIRED', 'CANCELLED', '已过期', '已用完', '已取消', '停用')
       OR TRIM(v_package_status) IN ('已过期', '已用完', '已取消', '停用') THEN
        p_message := '课包当前不可用';
        RETURN;
    END IF;

    SELECT COUNT(*)
    INTO v_conflict_count
    FROM PTBOOKING
    WHERE SESSION_TIME = p_session_time
      AND (MEMBER_ID = p_member_id OR COACH_ID = v_coach_id)
      AND MEMBER_CONFIRMED <> '2'
      AND COACH_CONFIRMED <> '2';

    IF v_conflict_count > 0 THEN
        p_message := '该时段会员或教练已有私教预约';
        RETURN;
    END IF;

    SELECT seq_ptbooking_id.NEXTVAL
    INTO p_booking_id
    FROM DUAL;

    INSERT INTO PTBOOKING (
        PT_BOOKING_ID,
        PACKAGE_ID,
        MEMBER_ID,
        COACH_ID,
        BOOKING_TIME,
        SESSION_TIME,
        COACH_CONFIRMED,
        MEMBER_CONFIRMED,
        CONSUME_STATUS,
        CONSUMED_TIME
    )
    VALUES (
        p_booking_id,
        p_package_id,
        p_member_id,
        v_coach_id,
        SYSDATE,
        p_session_time,
        '0',
        '1',
        '0',
        NULL
    );

    -- 提交预约即扣减一次；用完时标记课包状态
    UPDATE PERSONALPACKAGE
    SET REMAINING_SESSIONS = REMAINING_SESSIONS - 1,
        PACKAGE_STATUS = CASE
            WHEN REMAINING_SESSIONS - 1 <= 0 THEN '已用完'
            ELSE PACKAGE_STATUS
        END
    WHERE PACKAGE_ID = p_package_id;

    COMMIT;
    p_result := 1;
    p_message := '预约成功，等待教练确认';
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        ROLLBACK;
        p_result := 0;
        p_message := '未找到指定的私教课包';
    WHEN OTHERS THEN
        ROLLBACK;
        p_result := 0;
        p_message := SQLERRM;
END;
/
