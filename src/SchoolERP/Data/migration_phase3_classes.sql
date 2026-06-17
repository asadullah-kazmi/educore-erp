USE [SchoolERP];
GO

-- Classes table (referenced by Students but never created in schema.sql)
IF OBJECT_ID('dbo.Classes') IS NULL
BEGIN
    CREATE TABLE dbo.Classes (
        ClassID   INT IDENTITY(1,1) PRIMARY KEY,
        ClassName NVARCHAR(100) NOT NULL
    );
END
GO

-- Add MonthlyFee and ClassID to Students if missing
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'MonthlyFee')
    ALTER TABLE dbo.Students ADD MonthlyFee DECIMAL(18,2) NOT NULL DEFAULT 0;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'ClassID')
    ALTER TABLE dbo.Students ADD ClassID INT NULL
        CONSTRAINT FK_Students_Classes FOREIGN KEY REFERENCES dbo.Classes(ClassID) ON DELETE SET NULL;
GO

-- Seed some classes
IF NOT EXISTS (SELECT 1 FROM dbo.Classes WHERE ClassName = 'Class 1')
BEGIN
    INSERT INTO dbo.Classes(ClassName) VALUES
        ('Class 1'),('Class 2'),('Class 3'),('Class 4'),('Class 5'),
        ('Class 6'),('Class 7'),('Class 8'),('Class 9'),('Class 10');
END
GO
