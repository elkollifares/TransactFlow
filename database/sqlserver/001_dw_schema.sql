-- TransactFlowDW - modèle en étoile SQL Server 2019+
IF DB_ID('TransactFlowDW') IS NULL CREATE DATABASE TransactFlowDW;
GO
USE TransactFlowDW;
GO

CREATE TABLE dbo.DimDate (
    DateKey INT NOT NULL PRIMARY KEY,
    FullDate DATE NOT NULL UNIQUE,
    CalendarYear SMALLINT NOT NULL,
    CalendarMonth TINYINT NOT NULL,
    MonthName NVARCHAR(20) NOT NULL,
    DayOfMonth TINYINT NOT NULL
);

CREATE TABLE dbo.DimCategory (
    CategoryKey INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(60) NOT NULL UNIQUE,
    IsActive BIT NOT NULL CONSTRAINT DF_DimCategory_IsActive DEFAULT 1
);

CREATE TABLE dbo.FactTransaction (
    TransactionKey BIGINT IDENTITY(1,1) PRIMARY KEY,
    SourceTransactionId UNIQUEIDENTIFIER NOT NULL UNIQUE,
    DateKey INT NOT NULL REFERENCES dbo.DimDate(DateKey),
    CategoryKey INT NOT NULL REFERENCES dbo.DimCategory(CategoryKey),
    TransactionType VARCHAR(10) NOT NULL,
    Description NVARCHAR(120) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    LoadedAt DATETIME2 NOT NULL CONSTRAINT DF_FactTransaction_LoadedAt DEFAULT SYSUTCDATETIME()
);

CREATE TABLE dbo.EtlRun (
    EtlRunId BIGINT IDENTITY(1,1) PRIMARY KEY,
    PipelineName NVARCHAR(100) NOT NULL,
    StartedAt DATETIME2 NOT NULL,
    FinishedAt DATETIME2 NULL,
    RowsRead INT NULL,
    RowsWritten INT NULL,
    RunStatus VARCHAR(20) NOT NULL,
    ErrorMessage NVARCHAR(2000) NULL
);
GO

