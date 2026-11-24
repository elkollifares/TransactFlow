-- Appelé par le flux SSIS après le chargement de dbo.StageTransaction.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

MERGE dbo.DimCategory AS target
USING (SELECT DISTINCT Category FROM dbo.StageTransaction) AS source
ON target.CategoryName = source.Category
WHEN NOT MATCHED THEN INSERT (CategoryName) VALUES (source.Category);

MERGE dbo.DimDate AS target
USING (
    SELECT DISTINCT
        CONVERT(INT, FORMAT(TransactionDate, 'yyyyMMdd')) AS DateKey,
        TransactionDate AS FullDate,
        YEAR(TransactionDate) AS CalendarYear,
        MONTH(TransactionDate) AS CalendarMonth,
        DATENAME(MONTH, TransactionDate) AS MonthName,
        DAY(TransactionDate) AS DayOfMonth
    FROM dbo.StageTransaction
) AS source
ON target.DateKey = source.DateKey
WHEN NOT MATCHED THEN
    INSERT (DateKey, FullDate, CalendarYear, CalendarMonth, MonthName, DayOfMonth)
    VALUES (source.DateKey, source.FullDate, source.CalendarYear, source.CalendarMonth, source.MonthName, source.DayOfMonth);

MERGE dbo.FactTransaction AS target
USING (
    SELECT s.*, d.DateKey, c.CategoryKey
    FROM dbo.StageTransaction s
    JOIN dbo.DimDate d ON d.FullDate = s.TransactionDate
    JOIN dbo.DimCategory c ON c.CategoryName = s.Category
) AS source
ON target.SourceTransactionId = source.TransactionId
WHEN MATCHED THEN UPDATE SET
    target.DateKey = source.DateKey,
    target.CategoryKey = source.CategoryKey,
    target.TransactionType = source.TransactionType,
    target.Description = source.Description,
    target.Amount = source.Amount,
    target.LoadedAt = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (SourceTransactionId, DateKey, CategoryKey, TransactionType, Description, Amount)
    VALUES (source.TransactionId, source.DateKey, source.CategoryKey, source.TransactionType, source.Description, source.Amount);

COMMIT TRANSACTION;

