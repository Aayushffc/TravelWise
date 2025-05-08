-- Set QUOTED_IDENTIFIER ON to fix MERGE statement issues
SET QUOTED_IDENTIFIER ON;
GO

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
        'Admin Admin', -- FullName
        NULL, -- RefreshToken
        NULL  -- RefreshTokenExpiresAt
    )
) AS source (
    Id, UserName, NormalizedUserName, Email, NormalizedEmail,
    EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
    PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnd,
    LockoutEnabled, AccessFailedCount, FirstName, LastName, FullName,
    RefreshToken, RefreshTokenExpiresAt
)
ON target.Id = source.Id
WHEN NOT MATCHED BY TARGET THEN
    INSERT (
        Id, UserName, NormalizedUserName, Email, NormalizedEmail,
        EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
        PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnd,
        LockoutEnabled, AccessFailedCount, FirstName, LastName, FullName,
        RefreshToken, RefreshTokenExpiresAt
    )
    VALUES (
        source.Id, source.UserName, source.NormalizedUserName, source.Email, source.NormalizedEmail,
        source.EmailConfirmed, source.PasswordHash, source.SecurityStamp, source.ConcurrencyStamp,
        source.PhoneNumber, source.PhoneNumberConfirmed, source.TwoFactorEnabled, source.LockoutEnd,
        source.LockoutEnabled, source.AccessFailedCount, source.FirstName, source.LastName, source.FullName,
        source.RefreshToken, source.RefreshTokenExpiresAt
    );
GO

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
GO