/* =========================================================
   RaceDay – Database Schema (SQL Server / SSMS)
   Matches the ERD in RaceDay_ERD.png exactly.
   ========================================================= */

IF DB_ID('RaceDayDB') IS NULL
BEGIN
    CREATE DATABASE RaceDayDB;
END
GO

USE RaceDayDB;
GO

/* Drop in FK-safe order if re-running the script */
IF OBJECT_ID('dbo.Results', 'U') IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID('dbo.Enrolments', 'U') IS NOT NULL DROP TABLE dbo.Enrolments;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Events', 'U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID('dbo.Participants', 'U') IS NOT NULL DROP TABLE dbo.Participants;
IF OBJECT_ID('dbo.Organisers', 'U') IS NOT NULL DROP TABLE dbo.Organisers;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
GO

/* =========================================================
   1. Users – base account for both Organisers and Participants
   ========================================================= */
CREATE TABLE dbo.Users (
    UserId          INT             IDENTITY(1,1)   NOT NULL,
    FullName        NVARCHAR(150)   NOT NULL,
    Email           NVARCHAR(150)   NOT NULL,
    PasswordHash    NVARCHAR(255)   NOT NULL,
    Role            NVARCHAR(20)    NOT NULL DEFAULT ('Participant'),
    CreatedAt       DATETIME2       NOT NULL DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_Users PRIMARY KEY (UserId),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT CK_Users_Role CHECK (Role IN ('Organiser', 'Participant'))
);
GO

/* =========================================================
   2. Organisers – one-to-one extension of Users
   ========================================================= */
CREATE TABLE dbo.Organisers (
    OrganiserId       INT             IDENTITY(1,1)   NOT NULL,
    UserId            INT             NOT NULL,
    OrganisationName  NVARCHAR(150)   NULL,
    ContactNumber     NVARCHAR(20)    NULL,
    CONSTRAINT PK_Organisers PRIMARY KEY (OrganiserId),
    CONSTRAINT UQ_Organisers_UserId UNIQUE (UserId),
    CONSTRAINT FK_Organisers_Users FOREIGN KEY (UserId)
        REFERENCES dbo.Users (UserId) ON DELETE CASCADE
);
GO

/* =========================================================
   3. Participants – one-to-one extension of Users
   ========================================================= */
CREATE TABLE dbo.Participants (
    ParticipantId       INT             IDENTITY(1,1)   NOT NULL,
    UserId              INT             NOT NULL,
    DateOfBirth         DATE            NULL,
    EmergencyContact    NVARCHAR(20)    NULL,
    CONSTRAINT PK_Participants PRIMARY KEY (ParticipantId),
    CONSTRAINT UQ_Participants_UserId UNIQUE (UserId),
    CONSTRAINT FK_Participants_Users FOREIGN KEY (UserId)
        REFERENCES dbo.Users (UserId) ON DELETE CASCADE
);
GO

/* =========================================================
   4. Events – created and owned by an Organiser
   ========================================================= */
CREATE TABLE dbo.Events (
    EventId         INT             IDENTITY(1,1)   NOT NULL,
    OrganiserId     INT             NOT NULL,
    EventName       NVARCHAR(150)   NOT NULL,
    Description     NVARCHAR(1000)  NULL,
    EventDate       DATE            NOT NULL,
    StartTime       TIME            NULL,
    Location        NVARCHAR(200)   NOT NULL,
    RouteMapUrl     NVARCHAR(255)   NULL,
    CreatedAt       DATETIME2       NOT NULL DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_Events PRIMARY KEY (EventId),
    CONSTRAINT FK_Events_Organisers FOREIGN KEY (OrganiserId)
        REFERENCES dbo.Organisers (OrganiserId) ON DELETE CASCADE
);
GO

/* =========================================================
   5. Categories – distance/price bands within an Event
   ========================================================= */
CREATE TABLE dbo.Categories (
    CategoryId      INT             IDENTITY(1,1)   NOT NULL,
    EventId         INT             NOT NULL,
    CategoryName    NVARCHAR(100)   NOT NULL,
    DistanceKm      DECIMAL(5,2)    NOT NULL,
    EntryFee        DECIMAL(8,2)    NOT NULL DEFAULT (0),
    MaxParticipants INT             NULL,
    CONSTRAINT PK_Categories PRIMARY KEY (CategoryId),
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventId)
        REFERENCES dbo.Events (EventId) ON DELETE CASCADE
);
GO

/* =========================================================
   6. Enrolments – junction resolving Participants <-M:M-> Categories
   ========================================================= */
CREATE TABLE dbo.Enrolments (
    EnrolmentId     INT             IDENTITY(1,1)   NOT NULL,
    ParticipantId   INT             NOT NULL,
    CategoryId      INT             NOT NULL,
    EnrolmentDate   DATETIME2       NOT NULL DEFAULT (SYSDATETIME()),
    Status          NVARCHAR(20)    NOT NULL DEFAULT ('Confirmed'),
    CONSTRAINT PK_Enrolments PRIMARY KEY (EnrolmentId),
    CONSTRAINT UQ_Enrolments_Participant_Category UNIQUE (ParticipantId, CategoryId),
    CONSTRAINT FK_Enrolments_Participants FOREIGN KEY (ParticipantId)
        REFERENCES dbo.Participants (ParticipantId) ON DELETE NO ACTION,
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryId)
        REFERENCES dbo.Categories (CategoryId) ON DELETE CASCADE,
    CONSTRAINT CK_Enrolments_Status CHECK (Status IN ('Confirmed', 'Cancelled'))
);
GO

/* =========================================================
   7. Results – one-to-one with Enrolments
   ========================================================= */
CREATE TABLE dbo.Results (
    ResultId            INT             IDENTITY(1,1)   NOT NULL,
    EnrolmentId         INT             NOT NULL,
    FinishTime          TIME            NULL,
    PositionOverall     INT             NULL,
    PositionCategory    INT             NULL,
    Status              NVARCHAR(20)    NOT NULL DEFAULT ('Finished'),
    CONSTRAINT PK_Results PRIMARY KEY (ResultId),
    CONSTRAINT UQ_Results_EnrolmentId UNIQUE (EnrolmentId),
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentId)
        REFERENCES dbo.Enrolments (EnrolmentId) ON DELETE CASCADE,
    CONSTRAINT CK_Results_Status CHECK (Status IN ('Finished', 'DNF', 'DNS'))
);
GO

/* =========================================================
   SEED DATA – realistic sample data for RaceDay
   (2 Organisers, 2 Participants, 3 Events, categories per
   event, and sample enrolments/results)
   ========================================================= */

-- Users (2 Organisers + 2 Participants)
INSERT INTO dbo.Users (FullName, Email, PasswordHash, Role) VALUES
('Thabo Nkosi',    'thabo.nkosi@raceday.co.za',    'HASHED_PASSWORD_1', 'Organiser'),
('Lerato Dube',     'lerato.dube@raceday.co.za',    'HASHED_PASSWORD_2', 'Organiser'),
('Sipho Mahlangu',  'sipho.mahlangu@example.com',   'HASHED_PASSWORD_3', 'Participant'),
('Naledi Khumalo',  'naledi.khumalo@example.com',   'HASHED_PASSWORD_4', 'Participant');
GO

-- Organisers (linked to the two Organiser users)
INSERT INTO dbo.Organisers (UserId, OrganisationName, ContactNumber)
SELECT UserId, 'Johannesburg Road Running Club', '011-555-0101' FROM dbo.Users WHERE Email = 'thabo.nkosi@raceday.co.za';

INSERT INTO dbo.Organisers (UserId, OrganisationName, ContactNumber)
SELECT UserId, 'Cape Cycling Events', '021-555-0102' FROM dbo.Users WHERE Email = 'lerato.dube@raceday.co.za';
GO

-- Participants (linked to the two Participant users)
INSERT INTO dbo.Participants (UserId, DateOfBirth, EmergencyContact)
SELECT UserId, '1995-03-14', '082-555-0111' FROM dbo.Users WHERE Email = 'sipho.mahlangu@example.com';

INSERT INTO dbo.Participants (UserId, DateOfBirth, EmergencyContact)
SELECT UserId, '1998-11-02', '083-555-0112' FROM dbo.Users WHERE Email = 'naledi.khumalo@example.com';
GO

-- Events (3 events, owned by the two Organisers)
INSERT INTO dbo.Events (OrganiserId, EventName, Description, EventDate, StartTime, Location, RouteMapUrl)
SELECT OrganiserId, 'Johannesburg City Marathon', 'An annual road marathon through the streets of Johannesburg.', '2026-11-08', '06:00', 'Johannesburg, Gauteng', 'https://raceday.co.za/routes/jhb-marathon'
FROM dbo.Organisers WHERE OrganisationName = 'Johannesburg Road Running Club';

INSERT INTO dbo.Events (OrganiserId, EventName, Description, EventDate, StartTime, Location, RouteMapUrl)
SELECT OrganiserId, 'Soweto Fun Run', 'A community fun run supporting local charities.', '2026-10-18', '07:00', 'Soweto, Gauteng', 'https://raceday.co.za/routes/soweto-fun-run'
FROM dbo.Organisers WHERE OrganisationName = 'Johannesburg Road Running Club';

INSERT INTO dbo.Events (OrganiserId, EventName, Description, EventDate, StartTime, Location, RouteMapUrl)
SELECT OrganiserId, 'Cape Town Cycle Challenge', 'A scenic road cycling event around the Cape Peninsula.', '2027-03-07', '06:30', 'Cape Town, Western Cape', 'https://raceday.co.za/routes/ct-cycle-challenge'
FROM dbo.Organisers WHERE OrganisationName = 'Cape Cycling Events';
GO

-- Categories (per event)
INSERT INTO dbo.Categories (EventId, CategoryName, DistanceKm, EntryFee, MaxParticipants)
SELECT EventId, '42.2km Full Marathon', 42.2, 350.00, 3000 FROM dbo.Events WHERE EventName = 'Johannesburg City Marathon';
INSERT INTO dbo.Categories (EventId, CategoryName, DistanceKm, EntryFee, MaxParticipants)
SELECT EventId, '21.1km Half Marathon', 21.1, 250.00, 4000 FROM dbo.Events WHERE EventName = 'Johannesburg City Marathon';

INSERT INTO dbo.Categories (EventId, CategoryName, DistanceKm, EntryFee, MaxParticipants)
SELECT EventId, '10km Fun Run', 10.0, 100.00, 1000 FROM dbo.Events WHERE EventName = 'Soweto Fun Run';
INSERT INTO dbo.Categories (EventId, CategoryName, DistanceKm, EntryFee, MaxParticipants)
SELECT EventId, '5km Walk', 5.0, 60.00, 800 FROM dbo.Events WHERE EventName = 'Soweto Fun Run';

INSERT INTO dbo.Categories (EventId, CategoryName, DistanceKm, EntryFee, MaxParticipants)
SELECT EventId, '109km Full Cycle', 109.0, 500.00, 2000 FROM dbo.Events WHERE EventName = 'Cape Town Cycle Challenge';
GO

-- Enrolments (sample participant sign-ups)
INSERT INTO dbo.Enrolments (ParticipantId, CategoryId, Status)
SELECT p.ParticipantId, c.CategoryId, 'Confirmed'
FROM dbo.Participants p
JOIN dbo.Users u ON u.UserId = p.UserId
JOIN dbo.Categories c ON c.CategoryName = '21.1km Half Marathon'
WHERE u.Email = 'sipho.mahlangu@example.com';

INSERT INTO dbo.Enrolments (ParticipantId, CategoryId, Status)
SELECT p.ParticipantId, c.CategoryId, 'Confirmed'
FROM dbo.Participants p
JOIN dbo.Users u ON u.UserId = p.UserId
JOIN dbo.Categories c ON c.CategoryName = '10km Fun Run'
WHERE u.Email = 'naledi.khumalo@example.com';

INSERT INTO dbo.Enrolments (ParticipantId, CategoryId, Status)
SELECT p.ParticipantId, c.CategoryId, 'Confirmed'
FROM dbo.Participants p
JOIN dbo.Users u ON u.UserId = p.UserId
JOIN dbo.Categories c ON c.CategoryName = '109km Full Cycle'
WHERE u.Email = 'naledi.khumalo@example.com';
GO

-- Results (sample captured result for a completed enrolment)
INSERT INTO dbo.Results (EnrolmentId, FinishTime, PositionOverall, PositionCategory, Status)
SELECT e.EnrolmentId, '01:45:32', 214, 48, 'Finished'
FROM dbo.Enrolments e
JOIN dbo.Participants p ON p.ParticipantId = e.ParticipantId
JOIN dbo.Users u ON u.UserId = p.UserId
JOIN dbo.Categories c ON c.CategoryId = e.CategoryId
WHERE u.Email = 'sipho.mahlangu@example.com' AND c.CategoryName = '21.1km Half Marathon';
GO
