-- Initial schema for SchoolERP (SQL Server)
-- Run this script on the SQL Server instance where you want the database created.

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'SchoolERP')
BEGIN
    CREATE DATABASE [SchoolERP];
END
GO

USE [SchoolERP];
GO

-- Schema versioning table
IF OBJECT_ID('dbo.SchemaVersions') IS NULL
BEGIN
    CREATE TABLE dbo.SchemaVersions (
        VersionId INT IDENTITY(1,1) PRIMARY KEY,
        VersionName NVARCHAR(50) NOT NULL,
        AppliedOn DATETIME NOT NULL DEFAULT(GETDATE()),
        Notes NVARCHAR(4000) NULL
    );
END
GO

-- Roles and users
IF OBJECT_ID('dbo.Roles') IS NULL
BEGIN
    CREATE TABLE dbo.Roles (
        RoleId INT IDENTITY(1,1) PRIMARY KEY,
        RoleName NVARCHAR(100) NOT NULL UNIQUE
    );
END
GO

IF OBJECT_ID('dbo.Users') IS NULL
BEGIN
    CREATE TABLE dbo.Users (
        UserId INT IDENTITY(1,1) PRIMARY KEY,
        Username NVARCHAR(150) NOT NULL UNIQUE,
        PasswordHash VARBINARY(64) NOT NULL,
        FullName NVARCHAR(250) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME NOT NULL DEFAULT(GETDATE())
    );
END
GO

IF OBJECT_ID('dbo.UserRoles') IS NULL
BEGIN
    CREATE TABLE dbo.UserRoles (
        UserRoleId INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL,
        RoleId INT NOT NULL,
        CONSTRAINT FK_UserRoles_Users FOREIGN KEY(UserId) REFERENCES dbo.Users(UserId) ON DELETE CASCADE,
        CONSTRAINT FK_UserRoles_Roles FOREIGN KEY(RoleId) REFERENCES dbo.Roles(RoleId) ON DELETE CASCADE
    );
END
GO

-- Classes
IF OBJECT_ID('dbo.Classes') IS NULL
BEGIN
    CREATE TABLE dbo.Classes (
        ClassID INT IDENTITY(1,1) PRIMARY KEY,
        ClassName NVARCHAR(100) NOT NULL UNIQUE
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Classes WHERE ClassName = 'Nursery') INSERT INTO dbo.Classes(ClassName) VALUES('Nursery');
IF NOT EXISTS (SELECT 1 FROM dbo.Classes WHERE ClassName = 'Prep') INSERT INTO dbo.Classes(ClassName) VALUES('Prep');
IF NOT EXISTS (SELECT 1 FROM dbo.Classes WHERE ClassName = 'One') INSERT INTO dbo.Classes(ClassName) VALUES('One');
IF NOT EXISTS (SELECT 1 FROM dbo.Classes WHERE ClassName = 'Two') INSERT INTO dbo.Classes(ClassName) VALUES('Two');
IF NOT EXISTS (SELECT 1 FROM dbo.Classes WHERE ClassName = 'Three') INSERT INTO dbo.Classes(ClassName) VALUES('Three');
IF NOT EXISTS (SELECT 1 FROM dbo.Classes WHERE ClassName = 'Four') INSERT INTO dbo.Classes(ClassName) VALUES('Four');
IF NOT EXISTS (SELECT 1 FROM dbo.Classes WHERE ClassName = 'Five') INSERT INTO dbo.Classes(ClassName) VALUES('Five');
IF NOT EXISTS (SELECT 1 FROM dbo.Classes WHERE ClassName = 'Six') INSERT INTO dbo.Classes(ClassName) VALUES('Six');
IF NOT EXISTS (SELECT 1 FROM dbo.Classes WHERE ClassName = 'Seven') INSERT INTO dbo.Classes(ClassName) VALUES('Seven');
IF NOT EXISTS (SELECT 1 FROM dbo.Classes WHERE ClassName = 'Eight') INSERT INTO dbo.Classes(ClassName) VALUES('Eight');
IF NOT EXISTS (SELECT 1 FROM dbo.Classes WHERE ClassName = 'Nine') INSERT INTO dbo.Classes(ClassName) VALUES('Nine');
IF NOT EXISTS (SELECT 1 FROM dbo.Classes WHERE ClassName = 'Ten') INSERT INTO dbo.Classes(ClassName) VALUES('Ten');
GO

-- Students
IF OBJECT_ID('dbo.Students') IS NULL
BEGIN
    CREATE TABLE dbo.Students (
        StudentID INT IDENTITY(1,1) PRIMARY KEY,
        RegistrationNo NVARCHAR(50) NOT NULL UNIQUE,
        Name NVARCHAR(250) NOT NULL,
        FatherName NVARCHAR(250) NULL,
        DOB DATE NULL,
        ClassID INT NULL,
        Class NVARCHAR(50) NULL,
        MonthlyFee DECIMAL(18,2) NOT NULL DEFAULT 0,
        Address NVARCHAR(1000) NULL,
        Phone NVARCHAR(50) NULL,
        StudentFormBOrCnicNumber NVARCHAR(50) NULL,
        StudentFormBOrCnicPicturePath NVARCHAR(1000) NULL,
        StudentFormBOrCnicFrontPicturePath NVARCHAR(1000) NULL,
        StudentFormBOrCnicFrontPictureData VARBINARY(MAX) NULL,
        StudentFormBOrCnicFrontPictureFileName NVARCHAR(260) NULL,
        StudentFormBOrCnicBackPicturePath NVARCHAR(1000) NULL,
        StudentFormBOrCnicBackPictureData VARBINARY(MAX) NULL,
        StudentFormBOrCnicBackPictureFileName NVARCHAR(260) NULL,
        GuardianCnicNumber NVARCHAR(50) NULL,
        GuardianCnicPicturePath NVARCHAR(1000) NULL,
        GuardianCnicFrontPicturePath NVARCHAR(1000) NULL,
        GuardianCnicFrontPictureData VARBINARY(MAX) NULL,
        GuardianCnicFrontPictureFileName NVARCHAR(260) NULL,
        GuardianCnicBackPicturePath NVARCHAR(1000) NULL,
        GuardianCnicBackPictureData VARBINARY(MAX) NULL,
        GuardianCnicBackPictureFileName NVARCHAR(260) NULL,
        GuardianPhone NVARCHAR(50) NULL,
        EmergencyContactNumber NVARCHAR(50) NULL,
        AdmissionDate DATE NULL,
        CONSTRAINT FK_Students_Classes FOREIGN KEY(ClassID) REFERENCES dbo.Classes(ClassID) ON DELETE SET NULL
    );
    CREATE INDEX IX_Students_Name ON dbo.Students(Name);
END
GO

-- Teachers
IF OBJECT_ID('dbo.Teachers') IS NULL
BEGIN
    CREATE TABLE dbo.Teachers (
        TeacherID INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(250) NOT NULL,
        Age INT NULL,
        Experience NVARCHAR(100) NULL,
        DOB DATE NULL,
        ContactNumber NVARCHAR(50) NULL,
        DateOfJoining DATE NULL,
        StaffType NVARCHAR(100) NULL DEFAULT('Teacher'),
        Designation NVARCHAR(150) NULL,
        Salary DECIMAL(18,2) NULL,
        Address NVARCHAR(1000) NULL,
        CnicNumber NVARCHAR(50) NULL,
        CnicFrontImagePath NVARCHAR(1000) NULL,
        CnicFrontImageData VARBINARY(MAX) NULL,
        CnicFrontImageFileName NVARCHAR(260) NULL,
        CnicBackImagePath NVARCHAR(1000) NULL,
        CnicBackImageData VARBINARY(MAX) NULL,
        CnicBackImageFileName NVARCHAR(260) NULL,
        EducationalDocumentsPath NVARCHAR(2000) NULL,
        EducationalDocumentsData VARBINARY(MAX) NULL,
        EducationalDocumentsFileName NVARCHAR(260) NULL,
        CertificatesPath NVARCHAR(2000) NULL,
        CertificatesData VARBINARY(MAX) NULL,
        CertificatesFileName NVARCHAR(260) NULL,
        FingerprintID INT NULL
    );
    CREATE INDEX IX_Teachers_Name ON dbo.Teachers(Name);
END
GO

-- Attendance (supports teacher or student attendance records)
IF OBJECT_ID('dbo.Attendance') IS NULL
BEGIN
    CREATE TABLE dbo.Attendance (
        AttendanceID INT IDENTITY(1,1) PRIMARY KEY,
        StudentID INT NULL,
        TeacherID INT NULL,
        [Date] DATE NOT NULL,
        InTime DATETIME NULL,
        Status NVARCHAR(50) NULL,
        CONSTRAINT FK_Attendance_Students FOREIGN KEY(StudentID) REFERENCES dbo.Students(StudentID) ON DELETE SET NULL,
        CONSTRAINT FK_Attendance_Teachers FOREIGN KEY(TeacherID) REFERENCES dbo.Teachers(TeacherID) ON DELETE SET NULL
    );
    CREATE INDEX IX_Attendance_Date ON dbo.Attendance([Date]);
END
GO

-- Fees
IF OBJECT_ID('dbo.Fees') IS NULL
BEGIN
    CREATE TABLE dbo.Fees (
        FeeID INT IDENTITY(1,1) PRIMARY KEY,
        StudentID INT NOT NULL,
        Month NVARCHAR(20) NOT NULL,
        FeeType NVARCHAR(100) NULL,
        Amount DECIMAL(18,2) NOT NULL,
        PaidAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        Status NVARCHAR(50) NOT NULL DEFAULT('Due'),
        PaymentDate DATETIME NULL,
        CONSTRAINT FK_Fees_Students FOREIGN KEY(StudentID) REFERENCES dbo.Students(StudentID) ON DELETE CASCADE
    );
    CREATE INDEX IX_Fees_Student_Month ON dbo.Fees(StudentID, Month);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Fees') AND name = 'FeeType')
    ALTER TABLE dbo.Fees ADD FeeType NVARCHAR(100) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'MonthlyFee')
    ALTER TABLE dbo.Students ADD MonthlyFee DECIMAL(18,2) NOT NULL CONSTRAINT DF_Students_MonthlyFee DEFAULT 0;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'ClassID')
    ALTER TABLE dbo.Students ADD ClassID INT NULL CONSTRAINT FK_Students_Classes FOREIGN KEY REFERENCES dbo.Classes(ClassID) ON DELETE SET NULL;
GO

-- Fee Receipts (one permanent record per submitted payment)
IF OBJECT_ID('dbo.FeeReceipts') IS NULL
BEGIN
    CREATE TABLE dbo.FeeReceipts (
        ReceiptID INT IDENTITY(1,1) PRIMARY KEY,
        ReceiptNumber NVARCHAR(40) NOT NULL,
        StudentID INT NOT NULL,
        PaymentDate DATETIME NOT NULL,
        AmountPaid DECIMAL(18,2) NOT NULL,
        BalanceAfter DECIMAL(18,2) NOT NULL DEFAULT 0,
        Details NVARCHAR(1000) NULL,
        CreatedOn DATETIME NOT NULL DEFAULT(GETDATE()),
        CONSTRAINT FK_FeeReceipts_Students FOREIGN KEY(StudentID) REFERENCES dbo.Students(StudentID) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX UX_FeeReceipts_ReceiptNumber ON dbo.FeeReceipts(ReceiptNumber);
    CREATE INDEX IX_FeeReceipts_Student_PaymentDate ON dbo.FeeReceipts(StudentID, PaymentDate DESC);
END
GO

-- Family Fee Receipts (one receipt covering payments for multiple siblings)
IF OBJECT_ID('dbo.FamilyFeeReceipts') IS NULL
BEGIN
    CREATE TABLE dbo.FamilyFeeReceipts (
        FamilyReceiptID INT IDENTITY(1,1) PRIMARY KEY,
        ReceiptNumber NVARCHAR(40) NOT NULL,
        GuardianCnic NVARCHAR(50) NOT NULL,
        PaymentDate DATETIME NOT NULL,
        TotalPaid DECIMAL(18,2) NOT NULL,
        TotalBalanceAfter DECIMAL(18,2) NOT NULL,
        CreatedOn DATETIME NOT NULL DEFAULT(GETDATE())
    );
    CREATE UNIQUE INDEX UX_FamilyFeeReceipts_Number ON dbo.FamilyFeeReceipts(ReceiptNumber);
    CREATE INDEX IX_FamilyFeeReceipts_Cnic ON dbo.FamilyFeeReceipts(GuardianCnic, PaymentDate DESC);
END
GO

IF OBJECT_ID('dbo.FamilyFeeReceiptItems') IS NULL
BEGIN
    CREATE TABLE dbo.FamilyFeeReceiptItems (
        FamilyReceiptItemID INT IDENTITY(1,1) PRIMARY KEY,
        FamilyReceiptID INT NOT NULL,
        StudentID INT NOT NULL,
        AmountPaid DECIMAL(18,2) NOT NULL,
        BalanceAfter DECIMAL(18,2) NOT NULL,
        CONSTRAINT FK_FamilyReceiptItems_Receipt FOREIGN KEY(FamilyReceiptID) REFERENCES dbo.FamilyFeeReceipts(FamilyReceiptID) ON DELETE CASCADE,
        CONSTRAINT FK_FamilyReceiptItems_Student FOREIGN KEY(StudentID) REFERENCES dbo.Students(StudentID)
    );
END
GO

-- Exam Slips
IF OBJECT_ID('dbo.ExamSlips') IS NULL
BEGIN
    CREATE TABLE dbo.ExamSlips (
        ExamSlipID INT IDENTITY(1,1) PRIMARY KEY,
        StudentID INT NOT NULL,
        TermName NVARCHAR(100) NOT NULL,
        FeeMonth NVARCHAR(20) NOT NULL,
        ExamNumber NVARCHAR(20) NOT NULL,
        GeneratedOn DATETIME NOT NULL DEFAULT(GETDATE()),
        CONSTRAINT FK_ExamSlips_Students FOREIGN KEY(StudentID) REFERENCES dbo.Students(StudentID) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX UX_ExamSlips_Student_Term_Month ON dbo.ExamSlips(StudentID, TermName, FeeMonth);
    CREATE UNIQUE INDEX UX_ExamSlips_Term_Month_Number ON dbo.ExamSlips(TermName, FeeMonth, ExamNumber);
    CREATE INDEX IX_ExamSlips_GeneratedOn ON dbo.ExamSlips(GeneratedOn);
END
GO

-- Expenses
IF OBJECT_ID('dbo.Expenses') IS NULL
BEGIN
    CREATE TABLE dbo.Expenses (
        ExpenseID INT IDENTITY(1,1) PRIMARY KEY,
        Category NVARCHAR(200) NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        [Date] DATE NOT NULL,
        Notes NVARCHAR(2000) NULL
    );
END
GO

-- SalaryPayments (tracks salary disbursements)
IF OBJECT_ID('dbo.SalaryPayments') IS NULL
BEGIN
    CREATE TABLE dbo.SalaryPayments (
        SalaryPaymentID INT IDENTITY(1,1) PRIMARY KEY,
        TeacherID INT NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        PaymentDate DATE NOT NULL,
        Notes NVARCHAR(1000) NULL,
        CONSTRAINT FK_SalaryPayments_Teachers FOREIGN KEY(TeacherID) REFERENCES dbo.Teachers(TeacherID) ON DELETE CASCADE
    );
END
GO

-- Mark this schema application
INSERT INTO dbo.SchemaVersions(VersionName, Notes) VALUES ('1.0-initial', 'Initial schema applied');
GO
