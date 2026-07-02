USE [SchoolERP];
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'Section')
    ALTER TABLE dbo.Students ADD Section NVARCHAR(10) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'StudentFormBOrCnicNumber')
    ALTER TABLE dbo.Students ADD StudentFormBOrCnicNumber NVARCHAR(50) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'StudentFormBOrCnicPicturePath')
    ALTER TABLE dbo.Students ADD StudentFormBOrCnicPicturePath NVARCHAR(1000) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'StudentFormBOrCnicFrontPicturePath')
    ALTER TABLE dbo.Students ADD StudentFormBOrCnicFrontPicturePath NVARCHAR(1000) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'StudentFormBOrCnicBackPicturePath')
    ALTER TABLE dbo.Students ADD StudentFormBOrCnicBackPicturePath NVARCHAR(1000) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'GuardianCnicNumber')
    ALTER TABLE dbo.Students ADD GuardianCnicNumber NVARCHAR(50) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'GuardianCnicPicturePath')
    ALTER TABLE dbo.Students ADD GuardianCnicPicturePath NVARCHAR(1000) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'GuardianCnicFrontPicturePath')
    ALTER TABLE dbo.Students ADD GuardianCnicFrontPicturePath NVARCHAR(1000) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'GuardianCnicBackPicturePath')
    ALTER TABLE dbo.Students ADD GuardianCnicBackPicturePath NVARCHAR(1000) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'GuardianPhone')
    ALTER TABLE dbo.Students ADD GuardianPhone NVARCHAR(50) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'EmergencyContactNumber')
    ALTER TABLE dbo.Students ADD EmergencyContactNumber NVARCHAR(50) NULL;
GO

IF OBJECT_ID('dbo.SchemaVersions') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE VersionName = '3.0-student-profile-fields')
    INSERT INTO dbo.SchemaVersions(VersionName, Notes)
    VALUES ('3.0-student-profile-fields', 'Adds section, student Form-B/CNIC, guardian CNIC, guardian phone, emergency contact, and front/back picture path fields.');
GO
