CREATE OR REPLACE PROCEDURE sp_recalculate_transaction_summary (
    p_from_date        IN DATE DEFAULT NULL,
    p_to_date          IN DATE DEFAULT NULL,
    p_total_income     OUT NUMBER,
    p_total_expense    OUT NUMBER,
    p_balance          OUT NUMBER,
    p_transaction_count OUT NUMBER
) AS
BEGIN
    SELECT
        NVL(SUM(CASE WHEN transaction_type = 'Income' THEN amount ELSE 0 END), 0),
        NVL(SUM(CASE WHEN transaction_type = 'Expense' THEN amount ELSE 0 END), 0),
        COUNT(*)
    INTO p_total_income, p_total_expense, p_transaction_count
    FROM transactions
    WHERE (p_from_date IS NULL OR transaction_date >= p_from_date)
      AND (p_to_date IS NULL OR transaction_date < p_to_date + 1);

    p_balance := p_total_income - p_total_expense;

    INSERT INTO transaction_summary (
        period_from, period_to, total_income, total_expense, balance, transaction_count
    ) VALUES (
        p_from_date, p_to_date, p_total_income, p_total_expense, p_balance, p_transaction_count
    );

    COMMIT;
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        RAISE;
END sp_recalculate_transaction_summary;
/

-- Exemple SQL*Plus
-- VARIABLE income NUMBER;
-- VARIABLE expense NUMBER;
-- VARIABLE balance NUMBER;
-- VARIABLE tx_count NUMBER;
-- EXEC sp_recalculate_transaction_summary(TRUNC(SYSDATE, 'MM'), SYSDATE, :income, :expense, :balance, :tx_count);
-- PRINT income expense balance tx_count;

