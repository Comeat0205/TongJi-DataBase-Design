-- D · 判断会员卡是否有效（返回 1=有效，0=无效）
-- CARD_STATUS 以 DDL 为准：'1'=有效（默认），'0'/'2'=无效
-- 执行后验证：SELECT fn_is_card_valid(<card_id>) FROM dual;
CREATE OR REPLACE FUNCTION fn_is_card_valid(p_card_id IN NUMBER)
RETURN NUMBER
IS
    v_status CHAR(1);
BEGIN
    SELECT CARD_STATUS INTO v_status
    FROM MEMBER_BENEFIT_CARD
    WHERE CARD_ID = p_card_id;

    -- DDL 默认 '1' 表示有效卡
    IF v_status = '1' THEN
        RETURN 1;
    END IF;
    RETURN 0;
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        RETURN 0;
END;
/
