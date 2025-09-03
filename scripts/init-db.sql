-- Database initialization script for Better Call Saul
-- This script creates the database and sets up the application user

USE master;
GO

-- Create database if it doesn't exist
IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = 'BetterCallSaul')
BEGIN
    CREATE DATABASE BetterCallSaul;
    PRINT 'Database BetterCallSaul created successfully.';
END
ELSE
BEGIN
    PRINT 'Database BetterCallSaul already exists.';
END
GO

-- Use the database
USE BetterCallSaul;
GO

-- Create application login if it doesn't exist
IF NOT EXISTS(SELECT * FROM sys.server_principals WHERE name = 'bettercallsaul')
BEGIN
    CREATE LOGIN bettercallsaul WITH PASSWORD = 'StrongPassword123!';
    PRINT 'Login bettercallsaul created successfully.';
END
ELSE
BEGIN
    PRINT 'Login bettercallsaul already exists.';
END
GO

-- Create database user for the application
IF NOT EXISTS(SELECT * FROM sys.database_principals WHERE name = 'bettercallsaul')
BEGIN
    CREATE USER bettercallsaul FOR LOGIN bettercallsaul;
    PRINT 'User bettercallsaul created successfully.';
END
ELSE
BEGIN
    PRINT 'User bettercallsaul already exists.';
END
GO

-- Grant necessary permissions to the application user
ALTER ROLE db_datareader ADD MEMBER bettercallsaul;
ALTER ROLE db_datawriter ADD MEMBER bettercallsaul;
ALTER ROLE db_ddladmin ADD MEMBER bettercallsaul;

PRINT 'Permissions granted to bettercallsaul user.';
GO

-- Create basic tables structure (migrations will handle schema changes)
PRINT 'Database initialization completed successfully.';
GO