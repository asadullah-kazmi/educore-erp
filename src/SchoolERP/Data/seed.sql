-- Seed data for SchoolERP
USE [SchoolERP];
GO

-- Roles
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = 'Admin')
    INSERT INTO dbo.Roles(RoleName) VALUES('Admin');
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = 'Accountant')
    INSERT INTO dbo.Roles(RoleName) VALUES('Accountant');
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = 'Receptionist')
    INSERT INTO dbo.Roles(RoleName) VALUES('Receptionist');
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = 'Teacher')
    INSERT INTO dbo.Roles(RoleName) VALUES('Teacher');
GO

-- Admin user (password: admin123 hashed using SHA2_256)
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = 'admin')
BEGIN
    INSERT INTO dbo.Users(Username, PasswordHash, FullName)
    VALUES('admin', HASHBYTES('SHA2_256','admin123'), 'System Administrator');
END
GO

-- Map admin user to Admin role
DECLARE @adminId INT = (SELECT UserId FROM dbo.Users WHERE Username = 'admin');
DECLARE @adminRole INT = (SELECT RoleId FROM dbo.Roles WHERE RoleName = 'Admin');
IF @adminId IS NOT NULL AND @adminRole IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.UserRoles WHERE UserId = @adminId AND RoleId = @adminRole)
    INSERT INTO dbo.UserRoles(UserId, RoleId) VALUES(@adminId, @adminRole);
GO

-- Sample students
IF NOT EXISTS (SELECT 1 FROM dbo.Students WHERE RegistrationNo = 'REG-001')
    INSERT INTO dbo.Students(RegistrationNo, Name, FatherName, DOB, Class, Phone, AdmissionDate)
    VALUES('REG-001','Ayesha Khan','Muhammad Khan','2010-05-12','5A','03001234567','2016-03-01');
IF NOT EXISTS (SELECT 1 FROM dbo.Students WHERE RegistrationNo = 'REG-002')
    INSERT INTO dbo.Students(RegistrationNo, Name, FatherName, DOB, Class, Phone, AdmissionDate)
    VALUES('REG-002','Bilal Ahmed','Ahmed Raza','2009-08-20','6B','03007654321','2015-02-15');
GO

-- Sample teachers
IF NOT EXISTS (SELECT 1 FROM dbo.Teachers WHERE Name = 'Mr. Ali')
    INSERT INTO dbo.Teachers(Name, Designation, Salary) VALUES('Mr. Ali','Math Teacher',45000.00);
IF NOT EXISTS (SELECT 1 FROM dbo.Teachers WHERE Name = 'Ms. Sara')
    INSERT INTO dbo.Teachers(Name, Designation, Salary) VALUES('Ms. Sara','English Teacher',42000.00);
GO

-- Sample fees (due and paid)
DECLARE @student1 INT = (SELECT StudentID FROM dbo.Students WHERE RegistrationNo = 'REG-001');
IF @student1 IS NOT NULL
BEGIN
    INSERT INTO dbo.Fees(StudentID, Month, Amount, Status) VALUES(@student1,'2026-05',150.00,'Due');
    INSERT INTO dbo.Fees(StudentID, Month, Amount, Status, PaymentDate) VALUES(@student1,'2026-04',150.00,'Paid','2026-04-05');
END
GO

-- Sample expenses
INSERT INTO dbo.Expenses(Category, Amount, [Date], Notes) VALUES('Stationery',1200.00,'2026-05-01','Monthly stationery purchase');
INSERT INTO dbo.Expenses(Category, Amount, [Date], Notes) VALUES('Maintenance',3500.00,'2026-04-20','Repair of AC unit');
GO

-- Sample attendance entries
DECLARE @teacher1 INT = (SELECT TeacherID FROM dbo.Teachers WHERE Name = 'Mr. Ali');
IF @teacher1 IS NOT NULL
    INSERT INTO dbo.Attendance(TeacherID, [Date], InTime, Status) VALUES(@teacher1,'2026-05-20','2026-05-20 08:05:00','Present');
GO

-- Mark seed applied in schema versions (optional entry)
INSERT INTO dbo.SchemaVersions(VersionName, Notes) VALUES ('1.0-seed', 'Inserted initial seed data');
GO
