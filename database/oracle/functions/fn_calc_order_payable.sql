-- H · 计算订单应付金额（明细合计 - 优惠券折扣，不低于 0）
CREATE OR REPLACE FUNCTION fn_calc_order_payable(p_order_id IN NUMBER)
RETURN NUMBER
IS
    v_subtotal NUMBER(10, 2) := 0;
    v_discount NUMBER(10, 2) := 0;
    v_order_exists NUMBER := 0;
BEGIN
    SELECT COUNT(*) INTO v_order_exists
    FROM PAYMENT_ORDER
    WHERE ORDER_ID = p_order_id;

    IF v_order_exists = 0 THEN
        RETURN 0;
    END IF;

    SELECT NVL(SUM(SUBTOTAL_AMOUNT), 0)
    INTO v_subtotal
    FROM PAYMENT_DETAIL
    WHERE ORDER_ID = p_order_id;

    -- 若无明细，回退使用订单 TOTAL_AMOUNT
    IF v_subtotal = 0 THEN
        SELECT NVL(TOTAL_AMOUNT, 0)
        INTO v_subtotal
        FROM PAYMENT_ORDER
        WHERE ORDER_ID = p_order_id;
    END IF;

    BEGIN
        SELECT NVL(v.DISCOUNT_VALUE, 0)
        INTO v_discount
        FROM PAYMENT_ORDER o
        LEFT JOIN VOUCHER v ON o.VOUCHER_ID = v.VOUCHER_ID
        WHERE o.ORDER_ID = p_order_id;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            v_discount := 0;
    END;

    RETURN GREATEST(v_subtotal - v_discount, 0);
EXCEPTION
    WHEN OTHERS THEN
        RETURN 0;
END;
/
