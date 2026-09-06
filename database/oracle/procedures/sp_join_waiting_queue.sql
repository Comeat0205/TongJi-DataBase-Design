CREATE OR REPLACE PROCEDURE sp_join_waiting_queue (
    p_member_id      IN NUMBER,
    p_course_id      IN NUMBER,
    p_queue_id       IN NUMBER,
    p_result         OUT NUMBER,
    p_message        OUT VARCHAR2
)
IS
    v_current_capacity NUMBER;
    v_max_capacity     NUMBER;
    v_count             NUMBER;
    v_booking_status    CHAR(1);
BEGIN
    -- 1. 检查课程并锁定课程记录
    SELECT CURRENT_CAPACITY, MAX_CAPACITY
    INTO v_current_capacity, v_max_capacity
    FROM GROUPCOURSE
    WHERE COURSE_ID = p_course_id
    FOR UPDATE;

    -- 2. 课程还有空位时，不允许进入候补
    IF v_current_capacity < v_max_capacity THEN
        p_result := 0;
        p_message := '课程当前仍有空位，请直接预约';
        RETURN;
    END IF;

    -- 3. 检查会员是否已经有有效预约
    BEGIN
        SELECT BOOKING_STATUS
        INTO v_booking_status
        FROM GROUP_COURSE_BOOKING
        WHERE MEMBER_ID = p_member_id
          AND COURSE_ID = p_course_id;

        IF v_booking_status = '1' THEN
            p_result := 0;
            p_message := '您已经预约该课程';
            RETURN;
        END IF;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            NULL;
    END;

    -- 4. 检查是否已经在候补队列中
    SELECT COUNT(*)
    INTO v_count
    FROM WAITINGQUEUE
    WHERE MEMBER_ID = p_member_id
      AND COURSE_ID = p_course_id
      AND QUEUE_STATUS = '0';

    IF v_count > 0 THEN
        p_result := 0;
        p_message := '您已经在候补队列中';
        RETURN;
    END IF;

    -- 5. 写入候补队列
    INSERT INTO WAITINGQUEUE (
        QUEUE_ID,
        MEMBER_ID,
        COURSE_ID,
        ENQUEUE_TIME,
        QUEUE_STATUS,
        NOTIFIED
    )
    VALUES (
        p_queue_id,
        p_member_id,
        p_course_id,
        SYSDATE,
        '0',
        '0'
    );

    COMMIT;

    p_result := 1;
    p_message := '加入候补队列成功';

EXCEPTION
    WHEN NO_DATA_FOUND THEN
        ROLLBACK;
        p_result := 0;
        p_message := '课程不存在';

    WHEN OTHERS THEN
        ROLLBACK;
        p_result := 0;
        p_message := SQLERRM;
END;
/
