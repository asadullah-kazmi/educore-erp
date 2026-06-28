USE [SchoolERP];
GO

-- 1. MonthlyFee on Students
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Students') AND name='MonthlyFee')
  ALTER TABLE dbo.Students ADD MonthlyFee DECIMAL(18,2) NOT NULL DEFAULT 0;
GO

-- 2. FeeType on Fees
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Fees') AND name='FeeType')
  ALTER TABLE dbo.Fees ADD FeeType NVARCHAR(100) NULL;
GO

-- 2b. PaidAmount on Fees for partial payments
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Fees') AND name='PaidAmount')
BEGIN
  ALTER TABLE dbo.Fees ADD PaidAmount DECIMAL(18,2) NOT NULL DEFAULT 0;
  EXEC('UPDATE dbo.Fees SET PaidAmount = Amount WHERE Status = ''Paid''');
END
GO

-- 3. Classes table
IF OBJECT_ID('dbo.Classes') IS NULL
BEGIN
  CREATE TABLE dbo.Classes (
    ClassID INT IDENTITY(1,1) PRIMARY KEY,
    ClassName NVARCHAR(100) NOT NULL UNIQUE
  );
  INSERT INTO dbo.Classes(ClassName) VALUES
    ('Nursery'),('Prep'),('One'),('Two'),('Three'),('Four'),
    ('Five'),('Six'),('Seven'),('Eight'),('Nine'),('Ten');
END
GO

-- 3b. Staff profile fields
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Teachers') AND name='Age')
  ALTER TABLE dbo.Teachers ADD Age INT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Teachers') AND name='Experience')
  ALTER TABLE dbo.Teachers ADD Experience NVARCHAR(100) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Teachers') AND name='DOB')
  ALTER TABLE dbo.Teachers ADD DOB DATE NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Teachers') AND name='ContactNumber')
  ALTER TABLE dbo.Teachers ADD ContactNumber NVARCHAR(50) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Teachers') AND name='DateOfJoining')
  ALTER TABLE dbo.Teachers ADD DateOfJoining DATE NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Teachers') AND name='Address')
  ALTER TABLE dbo.Teachers ADD Address NVARCHAR(1000) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Teachers') AND name='CnicNumber')
  ALTER TABLE dbo.Teachers ADD CnicNumber NVARCHAR(50) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Teachers') AND name='CnicFrontImagePath')
  ALTER TABLE dbo.Teachers ADD CnicFrontImagePath NVARCHAR(1000) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Teachers') AND name='CnicBackImagePath')
  ALTER TABLE dbo.Teachers ADD CnicBackImagePath NVARCHAR(1000) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Teachers') AND name='EducationalDocumentsPath')
  ALTER TABLE dbo.Teachers ADD EducationalDocumentsPath NVARCHAR(2000) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Teachers') AND name='CertificatesPath')
  ALTER TABLE dbo.Teachers ADD CertificatesPath NVARCHAR(2000) NULL;
GO

-- 4. ClassID FK on Students
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Students') AND name='ClassID')
  ALTER TABLE dbo.Students ADD ClassID INT NULL CONSTRAINT FK_Students_Classes FOREIGN KEY REFERENCES dbo.Classes(ClassID);
GO

-- 5. Version row
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE VersionName = '2.0-finance-patch')
  INSERT INTO dbo.SchemaVersions(VersionName, Notes) VALUES ('2.0-finance-patch', 'Finance module schema additions');
GO
