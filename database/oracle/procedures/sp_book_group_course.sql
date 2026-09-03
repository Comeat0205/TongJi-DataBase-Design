-- F · 团课预约（查容量 → 插入预约 → 更新名额）
CREATE OR REPLACE PROCEDURE sp_book_group_course (
    p_member_id   IN  NUMBER,
    p_course_id   IN  NUMBER,
    p_booking_id  IN  NUMBER,
    p_result      OUT NUMBER,
    p_message     OUT VARCHAR2
)
IS
    v_current NUMBER;
    v_max     NUMBER;
BEGIN
    SELECT CURRENT_CAPACITY, MAX_CAPACITY
    INTO v_current, v_max
    FROM GROUPCOURSE
    WHERE COURSE_ID = p_course_id
    FOR UPDATE;

    IF v_current >= v_max THEN
        p_result := 0;
        p_message := '课程已满';
        RETURN;
    END IF;

    INSERT INTO GROUP_COURSE_BOOKING (BOOKING_ID, MEMBER_ID, COURSE_ID, BOOKING_TIME, BOOKING_STATUS)
    VALUES (p_booking_id, p_member_id, p_course_id, SYSDATE, '1');

    UPDATE GROUPCOURSE
    SET CURRENT_CAPACITY = v_current + 1
    WHERE COURSE_ID = p_course_id;

    COMMIT;
    p_result := 1;
    p_message := '预约成功';
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        p_result := 0;
        p_message := SQLERRM;
END;

