-- G · 私教课包与预约演示数据
-- 覆盖功能点：#12 课包有效期 / #13 排课冲突（用说明时段现场演示）/ #14 确认与消课
--
-- 共享库真实约束（勿再写 '1'/'2' 当作课包状态）：
--   PERSONALPACKAGE.PACKAGE_STATUS IN ('有效', '已用完', '已过期')
--   PTBOOKING.COACH_CONFIRMED / MEMBER_CONFIRMED 原为 IN ('0','1')
--   本脚本会扩成 IN ('0','1','2')，以匹配后端拒绝=2 / 会员取消=2
--
-- 前置：
--   1. 共享库已有 MEMBER、COACH
--   2. 建议已执行 procedures/sp_book_personal_training.sql
--
-- 用法：DBeaver / DataGrip 连接后 Run Script（整份执行）。
-- 可重复执行：固定 91xx～93xx 编号，先清再插。

-- 确保消课字段存在
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

-- 允许教练拒绝 / 会员取消使用 '2'（与 PtBookingAppService 一致）
BEGIN
    EXECUTE IMMEDIATE 'ALTER TABLE PTBOOKING DROP CONSTRAINT SYS_C008479';
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE NOT IN (-2443, -2320) THEN -- 不存在 / 已删除
            RAISE;
        END IF;
END;
/

BEGIN
    EXECUTE IMMEDIATE
        'ALTER TABLE PTBOOKING ADD CONSTRAINT CHK_PT_COACH_CONFIRMED '
        || 'CHECK (COACH_CONFIRMED IN (''0'', ''1'', ''2''))';
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE <> -2264 THEN -- name already used
            RAISE;
        END IF;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'ALTER TABLE PTBOOKING DROP CONSTRAINT SYS_C008480';
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE NOT IN (-2443, -2320) THEN
            RAISE;
        END IF;
END;
/

BEGIN
    EXECUTE IMMEDIATE
        'ALTER TABLE PTBOOKING ADD CONSTRAINT CHK_PT_MEMBER_CONFIRMED '
        || 'CHECK (MEMBER_CONFIRMED IN (''0'', ''1'', ''2''))';
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE <> -2264 THEN
            RAISE;
        END IF;
END;
/

DECLARE
    v_member_a       NUMBER;
    v_member_b       NUMBER;
    v_member_preview NUMBER;
    v_coach_id       NUMBER;
    v_coach_name     VARCHAR2(100);
    v_count          NUMBER;
BEGIN
    -- ---------- 解析会员 A（主演示会员）----------
    BEGIN
        SELECT MEMBER_ID INTO v_member_a FROM MEMBER WHERE MEMBER_ID = 1001;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            BEGIN
                SELECT MEMBER_ID INTO v_member_a FROM MEMBER WHERE MEMBER_ID = 1;
            EXCEPTION
                WHEN NO_DATA_FOUND THEN
                    SELECT MIN(MEMBER_ID) INTO v_member_a FROM MEMBER;
            END;
    END;

    IF v_member_a IS NULL THEN
        RAISE_APPLICATION_ERROR(-20001, '共享库中没有 MEMBER，请先插入会员或执行 SEED_USER_DEMO.sql');
    END IF;

    -- ---------- 解析会员 B ----------
    BEGIN
        SELECT MEMBER_ID INTO v_member_b
        FROM MEMBER
        WHERE MEMBER_ID = 1002
          AND MEMBER_ID <> v_member_a;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            BEGIN
                SELECT MIN(MEMBER_ID) INTO v_member_b
                FROM MEMBER
                WHERE MEMBER_ID <> v_member_a;
            EXCEPTION
                WHEN NO_DATA_FOUND THEN
                    v_member_b := NULL;
            END;
    END;

    -- ---------- 预览回落会员 ID=1 ----------
    BEGIN
        SELECT MEMBER_ID INTO v_member_preview FROM MEMBER WHERE MEMBER_ID = 1;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            v_member_preview := NULL;
    END;

    -- ---------- 解析教练 ----------
    BEGIN
        SELECT COACH_ID, COACH_NAME INTO v_coach_id, v_coach_name
        FROM COACH WHERE COACH_ID = 3001;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            BEGIN
                SELECT COACH_ID, COACH_NAME INTO v_coach_id, v_coach_name
                FROM COACH WHERE COACH_ID = 101;
            EXCEPTION
                WHEN NO_DATA_FOUND THEN
                    SELECT COACH_ID, COACH_NAME INTO v_coach_id, v_coach_name
                    FROM (
                        SELECT COACH_ID, COACH_NAME
                        FROM COACH
                        ORDER BY COACH_ID
                    )
                    WHERE ROWNUM = 1;
            END;
    END;

    IF v_coach_id IS NULL THEN
        RAISE_APPLICATION_ERROR(-20002, '共享库中没有 COACH，请先由 C 建教练或插入测试教练');
    END IF;

    DBMS_OUTPUT.PUT_LINE('G seed members: A=' || v_member_a
        || ' B=' || NVL(TO_CHAR(v_member_b), 'N/A')
        || ' preview=' || NVL(TO_CHAR(v_member_preview), 'N/A'));
    DBMS_OUTPUT.PUT_LINE('G seed coach: ' || v_coach_id || ' (' || v_coach_name || ')');

    DELETE FROM PTBOOKING WHERE PT_BOOKING_ID BETWEEN 9301 AND 9399;
    DELETE FROM PERSONALPACKAGE WHERE PACKAGE_ID BETWEEN 9201 AND 9299;
    DELETE FROM PERSONAL_COURSE WHERE PERSONAL_COURSE_ID BETWEEN 9101 AND 9199;

    INSERT INTO PERSONAL_COURSE (PERSONAL_COURSE_ID, COURSE_NAME, COURSE_DESCRIPTION, COACH_ID)
    VALUES (9101, '力量塑形私教', '针对深蹲、硬拉等复合动作的一对一力量课，适合增肌塑形。', v_coach_id);

    INSERT INTO PERSONAL_COURSE (PERSONAL_COURSE_ID, COURSE_NAME, COURSE_DESCRIPTION, COACH_ID)
    VALUES (9102, '减脂燃脂私教', '有氧结合力量的减脂私教方案，控制配速与心率区间。', v_coach_id);

    INSERT INTO PERSONAL_COURSE (PERSONAL_COURSE_ID, COURSE_NAME, COURSE_DESCRIPTION, COACH_ID)
    VALUES (9103, '体态康复私教', '肩颈腰体态评估与矫正训练，强度较低，适合康复期。', v_coach_id);

    -- 可用课包（PACKAGE_STATUS 必须是：有效 / 已用完 / 已过期）
    INSERT INTO PERSONALPACKAGE (
        PACKAGE_ID, MEMBER_ID, COACH_ID, TOTAL_SESSIONS, REMAINING_SESSIONS,
        EXPIRE_DATE, PACKAGE_STATUS, PERSONAL_COURSE_ID
    ) VALUES (
        9201, v_member_a, v_coach_id, 12, 10,
        ADD_MONTHS(TRUNC(SYSDATE), 12), '有效', 9101
    );

    INSERT INTO PERSONALPACKAGE (
        PACKAGE_ID, MEMBER_ID, COACH_ID, TOTAL_SESSIONS, REMAINING_SESSIONS,
        EXPIRE_DATE, PACKAGE_STATUS, PERSONAL_COURSE_ID
    ) VALUES (
        9202, v_member_a, v_coach_id, 10, 3,
        ADD_MONTHS(TRUNC(SYSDATE), 6), '有效', 9102
    );

    -- #12：已过期
    INSERT INTO PERSONALPACKAGE (
        PACKAGE_ID, MEMBER_ID, COACH_ID, TOTAL_SESSIONS, REMAINING_SESSIONS,
        EXPIRE_DATE, PACKAGE_STATUS, PERSONAL_COURSE_ID
    ) VALUES (
        9203, v_member_a, v_coach_id, 8, 5,
        TRUNC(SYSDATE) - 7, '已过期', 9103
    );

    -- #12：已用完
    INSERT INTO PERSONALPACKAGE (
        PACKAGE_ID, MEMBER_ID, COACH_ID, TOTAL_SESSIONS, REMAINING_SESSIONS,
        EXPIRE_DATE, PACKAGE_STATUS, PERSONAL_COURSE_ID
    ) VALUES (
        9204, v_member_a, v_coach_id, 10, 0,
        ADD_MONTHS(TRUNC(SYSDATE), 3), '已用完', 9101
    );

    -- 再给一张有效课包，方便连续预约演示
    INSERT INTO PERSONALPACKAGE (
        PACKAGE_ID, MEMBER_ID, COACH_ID, TOTAL_SESSIONS, REMAINING_SESSIONS,
        EXPIRE_DATE, PACKAGE_STATUS, PERSONAL_COURSE_ID
    ) VALUES (
        9205, v_member_a, v_coach_id, 6, 6,
        ADD_MONTHS(TRUNC(SYSDATE), 8), '有效', 9103
    );

    IF v_member_b IS NOT NULL THEN
        INSERT INTO PERSONALPACKAGE (
            PACKAGE_ID, MEMBER_ID, COACH_ID, TOTAL_SESSIONS, REMAINING_SESSIONS,
            EXPIRE_DATE, PACKAGE_STATUS, PERSONAL_COURSE_ID
        ) VALUES (
            9206, v_member_b, v_coach_id, 8, 6,
            ADD_MONTHS(TRUNC(SYSDATE), 9), '有效', 9101
        );
    END IF;

    IF v_member_preview IS NOT NULL
       AND v_member_preview <> v_member_a
       AND (v_member_b IS NULL OR v_member_preview <> v_member_b) THEN
        INSERT INTO PERSONALPACKAGE (
            PACKAGE_ID, MEMBER_ID, COACH_ID, TOTAL_SESSIONS, REMAINING_SESSIONS,
            EXPIRE_DATE, PACKAGE_STATUS, PERSONAL_COURSE_ID
        ) VALUES (
            9207, v_member_preview, v_coach_id, 5, 5,
            ADD_MONTHS(TRUNC(SYSDATE), 6), '有效', 9102
        );
    END IF;

    -- 9301：待教练确认
    INSERT INTO PTBOOKING (
        PT_BOOKING_ID, PACKAGE_ID, MEMBER_ID, COACH_ID,
        BOOKING_TIME, SESSION_TIME, COACH_CONFIRMED, MEMBER_CONFIRMED,
        CONSUME_STATUS, CONSUMED_TIME
    ) VALUES (
        9301, 9201, v_member_a, v_coach_id,
        SYSDATE - 1, TRUNC(SYSDATE) + 3 + 10/24, '0', '1',
        '0', NULL
    );

    -- 9302：已确认、未来课（冲突演示锚点）
    INSERT INTO PTBOOKING (
        PT_BOOKING_ID, PACKAGE_ID, MEMBER_ID, COACH_ID,
        BOOKING_TIME, SESSION_TIME, COACH_CONFIRMED, MEMBER_CONFIRMED,
        CONSUME_STATUS, CONSUMED_TIME
    ) VALUES (
        9302, 9201, v_member_a, v_coach_id,
        SYSDATE - 2, TRUNC(SYSDATE) + 5 + 15/24, '1', '1',
        '0', NULL
    );

    -- 9303：已确认、已过上课时间 → 可消课
    INSERT INTO PTBOOKING (
        PT_BOOKING_ID, PACKAGE_ID, MEMBER_ID, COACH_ID,
        BOOKING_TIME, SESSION_TIME, COACH_CONFIRMED, MEMBER_CONFIRMED,
        CONSUME_STATUS, CONSUMED_TIME
    ) VALUES (
        9303, 9202, v_member_a, v_coach_id,
        SYSDATE - 3, SYSDATE - 2/24, '1', '1',
        '0', NULL
    );

    -- 9304：已消课 → 可撤销消课
    INSERT INTO PTBOOKING (
        PT_BOOKING_ID, PACKAGE_ID, MEMBER_ID, COACH_ID,
        BOOKING_TIME, SESSION_TIME, COACH_CONFIRMED, MEMBER_CONFIRMED,
        CONSUME_STATUS, CONSUMED_TIME
    ) VALUES (
        9304, 9202, v_member_a, v_coach_id,
        SYSDATE - 5, SYSDATE - 4, '1', '1',
        '1', SYSDATE - 3
    );

    -- 9305：教练已拒绝（COACH_CONFIRMED='2'）
    INSERT INTO PTBOOKING (
        PT_BOOKING_ID, PACKAGE_ID, MEMBER_ID, COACH_ID,
        BOOKING_TIME, SESSION_TIME, COACH_CONFIRMED, MEMBER_CONFIRMED,
        CONSUME_STATUS, CONSUMED_TIME
    ) VALUES (
        9305, 9201, v_member_a, v_coach_id,
        SYSDATE - 6, TRUNC(SYSDATE) + 7 + 11/24, '2', '1',
        '0', NULL
    );

    -- 9306：会员已取消（MEMBER_CONFIRMED='2'）
    INSERT INTO PTBOOKING (
        PT_BOOKING_ID, PACKAGE_ID, MEMBER_ID, COACH_ID,
        BOOKING_TIME, SESSION_TIME, COACH_CONFIRMED, MEMBER_CONFIRMED,
        CONSUME_STATUS, CONSUMED_TIME
    ) VALUES (
        9306, 9201, v_member_a, v_coach_id,
        SYSDATE - 7, TRUNC(SYSDATE) + 8 + 16/24, '0', '2',
        '0', NULL
    );

    IF v_member_b IS NOT NULL THEN
        INSERT INTO PTBOOKING (
            PT_BOOKING_ID, PACKAGE_ID, MEMBER_ID, COACH_ID,
            BOOKING_TIME, SESSION_TIME, COACH_CONFIRMED, MEMBER_CONFIRMED,
            CONSUME_STATUS, CONSUMED_TIME
        ) VALUES (
            9307, 9206, v_member_b, v_coach_id,
            SYSDATE - 1, TRUNC(SYSDATE) + 4 + 14/24, '0', '1',
            '0', NULL
        );
    END IF;

    COMMIT;

    SELECT COUNT(*) INTO v_count FROM PERSONAL_COURSE WHERE PERSONAL_COURSE_ID BETWEEN 9101 AND 9199;
    DBMS_OUTPUT.PUT_LINE('PERSONAL_COURSE rows: ' || v_count);
    SELECT COUNT(*) INTO v_count FROM PERSONALPACKAGE WHERE PACKAGE_ID BETWEEN 9201 AND 9299;
    DBMS_OUTPUT.PUT_LINE('PERSONALPACKAGE rows: ' || v_count);
    SELECT COUNT(*) INTO v_count FROM PTBOOKING WHERE PT_BOOKING_ID BETWEEN 9301 AND 9399;
    DBMS_OUTPUT.PUT_LINE('PTBOOKING rows: ' || v_count);
    DBMS_OUTPUT.PUT_LINE('G personal-training demo seed completed.');
END;
/

-- 验证：
-- SELECT PACKAGE_ID, PACKAGE_STATUS, REMAINING_SESSIONS, EXPIRE_DATE
-- FROM PERSONALPACKAGE WHERE PACKAGE_ID BETWEEN 9201 AND 9299 ORDER BY 1;
