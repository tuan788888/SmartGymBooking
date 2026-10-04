USE master;
GO

IF DB_ID('SmartGymBookingDB') IS NULL
BEGIN
    CREATE DATABASE SmartGymBookingDB;
END
GO

USE SmartGymBookingDB;
GO