-- Create the TravelWise database if it doesn't exist
IF NOT EXISTS (SELECT *
FROM sys.databases
WHERE name = 'TravelWise')
BEGIN
    CREATE DATABASE TravelWise;
END
GO

USE TravelWise;
GO

-- Create roles if they don't exist
IF NOT EXISTS (SELECT *
FROM sys.tables
WHERE name = 'AspNetRoles')
BEGIN
    CREATE TABLE AspNetRoles
    (
        Id NVARCHAR(450) NOT NULL PRIMARY KEY,
        Name NVARCHAR(256) NULL,
        NormalizedName NVARCHAR(256) NULL,
        ConcurrencyStamp NVARCHAR(MAX) NULL
    );
END
GO

-- Insert roles if they don't exist
MERGE AspNetRoles AS target
USING (VALUES
    ('90806F23-5144-4AA3-9D65-D73564EAB15E', 'Admin', 'ADMIN', NULL),
    ('b150977e-a5f3-42ba-a068-6c50120b3ea9', 'User', 'USER', NULL),
    ('E0A2435E-6EBE-4B5F-8D4C-10F6848AAB50', 'Agency', 'AGENCY', NULL)
) AS source (Id, Name, NormalizedName, ConcurrencyStamp)
ON target.Id = source.Id
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (source.Id, source.Name, source.NormalizedName, source.ConcurrencyStamp);

-- Create users table if it doesn't exist
IF NOT EXISTS (SELECT *
FROM sys.tables
WHERE name = 'AspNetUsers')
BEGIN
    CREATE TABLE AspNetUsers
    (
        Id NVARCHAR(450) NOT NULL PRIMARY KEY,
        UserName NVARCHAR(256) NULL,
        NormalizedUserName NVARCHAR(256) NULL,
        Email NVARCHAR(256) NULL,
        NormalizedEmail NVARCHAR(256) NULL,
        EmailConfirmed BIT NOT NULL,
        PasswordHash NVARCHAR(MAX) NULL,
        SecurityStamp NVARCHAR(MAX) NULL,
        ConcurrencyStamp NVARCHAR(MAX) NULL,
        PhoneNumber NVARCHAR(MAX) NULL,
        PhoneNumberConfirmed BIT NOT NULL,
        TwoFactorEnabled BIT NOT NULL,
        LockoutEnd DATETIMEOFFSET NULL,
        LockoutEnabled BIT NOT NULL,
        AccessFailedCount INT NOT NULL,
        FirstName NVARCHAR(MAX) NULL,
        LastName NVARCHAR(MAX) NULL,
        RefreshToken NVARCHAR(MAX) NULL,
        RefreshTokenExpiresAt DATETIME2 NULL
    );
END
GO

-- Create user roles table if it doesn't exist
IF NOT EXISTS (SELECT *
FROM sys.tables
WHERE name = 'AspNetUserRoles')
BEGIN
    CREATE TABLE AspNetUserRoles
    (
        UserId NVARCHAR(450) NOT NULL,
        RoleId NVARCHAR(450) NOT NULL,
        CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId),
        CONSTRAINT FK_AspNetUserRoles_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES AspNetRoles (Id) ON DELETE CASCADE,
        CONSTRAINT FK_AspNetUserRoles_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE
    );
END
GO

-- Insert admin user if it doesn't exist
MERGE AspNetUsers AS target
USING (VALUES
    (
        'dbda83f0-629d-429f-b940-443cc6322db9', -- Id
        'Admin', -- UserName
        'ADMIN', -- NormalizedUserName
        'admin@gmail.com', -- Email
        'ADMIN@GMAIL.COM', -- NormalizedEmail
        1, -- EmailConfirmed
        'AQAAAAIAAYagAAAAEKIi4Du1tcQZTGjZmVzj3P44sRLBksz835YzhJGfPod1rbU3Mj5RWnVBSvFRxcYcmA==', -- PasswordHash
        '6FIXTUFPZP5DV4QEEAFMHLBJVAH3EEQ4', -- SecurityStamp
        '48c64ac9-3da6-4b6c-bced-d1b8dbdc66d8', -- ConcurrencyStamp
        NULL, -- PhoneNumber
        0, -- PhoneNumberConfirmed
        0, -- TwoFactorEnabled
        NULL, -- LockoutEnd
        1, -- LockoutEnabled
        0, -- AccessFailedCount
        'Admin', -- FirstName
        'Admin', -- LastName
        NULL, -- RefreshToken
        NULL  -- RefreshTokenExpiresAt
)
) AS source (
    Id, UserName, NormalizedUserName, Email, NormalizedEmail,
    EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
    PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnd,
    LockoutEnabled, AccessFailedCount, FirstName, LastName, RefreshToken, RefreshTokenExpiresAt
)
ON target.Id = source.Id
WHEN NOT MATCHED BY TARGET THEN
    INSERT (
        Id, UserName, NormalizedUserName, Email, NormalizedEmail,
        EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
        PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnd,
        LockoutEnabled, AccessFailedCount, FirstName, LastName, RefreshToken, RefreshTokenExpiresAt
    )
    VALUES (
        source.Id, source.UserName, source.NormalizedUserName, source.Email, source.NormalizedEmail,
        source.EmailConfirmed, source.PasswordHash, source.SecurityStamp, source.ConcurrencyStamp,
        source.PhoneNumber, source.PhoneNumberConfirmed, source.TwoFactorEnabled, source.LockoutEnd,
        source.LockoutEnabled, source.AccessFailedCount, source.FirstName, source.LastName,
        source.RefreshToken, source.RefreshTokenExpiresAt
    );

-- Assign admin role to admin user if not already assigned
INSERT INTO AspNetUserRoles
    (UserId, RoleId)
SELECT
    'dbda83f0-629d-429f-b940-443cc6322db9', -- UserId (Admin user)
    '90806F23-5144-4AA3-9D65-D73564EAB15E'
-- RoleId (Admin role)
WHERE NOT EXISTS (
    SELECT 1
FROM AspNetUserRoles
WHERE UserId = 'dbda83f0-629d-429f-b940-443cc6322db9'
    AND RoleId = '90806F23-5144-4AA3-9D65-D73564EAB15E'
);
