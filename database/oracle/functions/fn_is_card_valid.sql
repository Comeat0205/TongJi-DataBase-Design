-- D · 判断会员卡是否有效（返回 1=有效，0=无效）
CREATE OR REPLACE FUNCTION fn_is_card_valid(p_card_id IN NUMBER)
RETURN NUMBER
IS
    v_status CHAR(1);
BEGIN
    SELECT CARD_STATUS INTO v_status
    FROM MEMBER_BENEFIT_CARD
    WHERE CARD_ID = p_card_id;

    IF v_status = '0' THEN
        RETURN 1;
    END IF;
    RETURN 0;
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        RETURN 0;
END;
/
