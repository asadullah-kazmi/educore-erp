-- Migration 3.0-attendance-v2: Add OutTime, DeviceSerial, Source columns and unique index
USE [SchoolERP];
GO

-- Step 1: Add OutTime column
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Attendance') AND name='OutTime') 
BEGIN
    ALTER TABLE dbo.Attendance ADD OutTime DATETIME NULL;
END
GO

-- Step 2: Add DeviceSerial column
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Attendance') AND name='DeviceSerial') 
BEGIN
    ALTER TABLE dbo.Attendance ADD DeviceSerial NVARCHAR(100) NULL;
END
GO

-- Step 3: Add Source column
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Attendance') AND name='Source') 
BEGIN
    ALTER TABLE dbo.Attendance ADD Source NVARCHAR(50) NOT NULL DEFAULT 'Manual';
END
GO

-- Step 4: Add unique index UX_Attendance_Teacher_Date
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_Attendance_Teacher_Date' AND object_id=OBJECT_ID('dbo.Attendance')) 
BEGIN
    CREATE UNIQUE INDEX UX_Attendance_Teacher_Date ON dbo.Attendance(TeacherID, [Date]) WHERE TeacherID IS NOT NULL;
END
GO

-- Step 5: Insert schema version
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE VersionName = '3.0-attendance-v2') 
BEGIN
    INSERT INTO dbo.SchemaVersions (VersionName, Notes) 
    VALUES ('3.0-attendance-v2', 'Added OutTime, DeviceSerial, Source columns and unique index for teacher attendance');
END
GO
