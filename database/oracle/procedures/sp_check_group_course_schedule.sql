CREATE OR REPLACE PROCEDURE sp_check_group_course_schedule (
    p_course_id    IN NUMBER,
    p_coach_id     IN NUMBER,
    p_course_date  IN DATE,
    p_start_time   IN TIMESTAMP,
    p_end_time     IN TIMESTAMP,
    p_result       OUT NUMBER,
    p_message      OUT VARCHAR2
)
IS
    v_course_name GROUPCOURSE.COURSE_NAME%TYPE;
BEGIN
    p_result := 0;
    p_message := '排课无冲突';

    IF p_start_time >= p_end_time THEN
        p_result := 1;
        p_message := '开始时间必须早于结束时间';
        RETURN;
    END IF;

    SELECT gc.course_name
      INTO v_course_name
      FROM groupcourse gc
      JOIN time_slot_instance tsi
        ON tsi.time_slot_id = gc.time_slot_id
     WHERE gc.coach_id = p_coach_id
       AND gc.course_id <> p_course_id
       AND TRUNC(tsi.course_date) = TRUNC(p_course_date)
       AND p_start_time < tsi.end_time
       AND p_end_time > tsi.start_time
       AND ROWNUM = 1;

    p_result := 1;
    p_message := '教练时间冲突：' || v_course_name;

EXCEPTION
    WHEN NO_DATA_FOUND THEN
        p_result := 0;
        p_message := '排课无冲突';

    WHEN OTHERS THEN
        p_result := 1;
        p_message := '排课冲突检测失败：' || SQLERRM;
END;
/
