School ERP professional installer

Requirements on the development/build computer:
- Inno Setup 6
- .NET SDK capable of building .NET Framework 4.8
- Official prerequisite installers listed in prerequisites\README.txt

Build the single installer from PowerShell:

  .\Build-Installer.ps1

Output:

  output\SchoolERP-Setup-1.1.1.exe

The generated setup runs as administrator, checks and installs .NET Framework
4.8 and SQL Server Express, installs School ERP, configures the SCHOOLERP SQL
instance and database, creates shortcuts, and optionally launches the program.

Initial login:
  Username: admin
  Password: admin123

Change the default password before production handover.
