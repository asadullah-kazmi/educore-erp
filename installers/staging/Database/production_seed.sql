USE [SchoolERP];
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = 'Admin')
    INSERT INTO dbo.Roles(RoleName) VALUES('Admin');
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = 'Accountant')
    INSERT INTO dbo.Roles(RoleName) VALUES('Accountant');
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = 'Receptionist')
    INSERT INTO dbo.Roles(RoleName) VALUES('Receptionist');
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = 'Teacher')
    INSERT INTO dbo.Roles(RoleName) VALUES('Teacher');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = 'admin')
BEGIN
    INSERT INTO dbo.Users(Username, PasswordHash, FullName, IsActive)
    VALUES('admin', HASHBYTES('SHA2_256','admin123'), 'System Administrator', 1);
END
GO

DECLARE @adminId INT = (SELECT UserId FROM dbo.Users WHERE Username = 'admin');
DECLARE @adminRoleId INT = (SELECT RoleId FROM dbo.Roles WHERE RoleName = 'Admin');
IF @adminId IS NOT NULL AND @adminRoleId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.UserRoles WHERE UserId = @adminId AND RoleId = @adminRoleId)
BEGIN
    INSERT INTO dbo.UserRoles(UserId, RoleId) VALUES(@adminId, @adminRoleId);
END
GO
