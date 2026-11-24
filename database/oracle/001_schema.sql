-- TransactFlow - schéma transactionnel Oracle 19c+
CREATE TABLE transactions (
    transaction_id RAW(16) DEFAULT SYS_GUID() PRIMARY KEY,
    description VARCHAR2(120 CHAR) NOT NULL,
    amount NUMBER(18, 2) NOT NULL CHECK (amount > 0),
    transaction_type VARCHAR2(10 CHAR) NOT NULL CHECK (transaction_type IN ('Income', 'Expense')),
    category VARCHAR2(60 CHAR) NOT NULL,
    transaction_date DATE NOT NULL,
    note VARCHAR2(500 CHAR),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT SYSTIMESTAMP NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT SYSTIMESTAMP NOT NULL
);

CREATE INDEX ix_transactions_date ON transactions (transaction_date);
CREATE INDEX ix_transactions_type_category ON transactions (transaction_type, category);

CREATE TABLE transaction_summary (
    summary_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    period_from DATE,
    period_to DATE,
    total_income NUMBER(18, 2) NOT NULL,
    total_expense NUMBER(18, 2) NOT NULL,
    balance NUMBER(18, 2) NOT NULL,
    transaction_count NUMBER NOT NULL,
    calculated_at TIMESTAMP WITH TIME ZONE DEFAULT SYSTIMESTAMP NOT NULL
);

