-- Create Database Script
-- This script creates the ProjectReport database

-- Check if database exists, create if not
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'ProjectReport')
BEGIN
    CREATE DATABASE [ProjectReport];
    PRINT 'Database ProjectReport created successfully.';
END
ELSE
BEGIN
    PRINT 'Database ProjectReport already exists.';
END
GO

USE [ProjectReport];
GO

-- Database setup will be continued in the next phase
PRINT 'Database ProjectReport is ready for setup.';
GO

