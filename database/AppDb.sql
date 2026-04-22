CREATE DATABASE AppAnalytics;
GO

USE AppAnalytics;
GO

-- Users
CREATE TABLE Users (
    Id         INT PRIMARY KEY IDENTITY,
    Email      NVARCHAR(256) UNIQUE NOT NULL,
    PasswordHash NVARCHAR(512) NOT NULL,
    Role       NVARCHAR(50) NOT NULL DEFAULT 'user',  -- 'user' | 'admin'
    SqlConnectionString NVARCHAR(1000) NOT NULL,
    SsasConnectionString NVARCHAR(1000) NOT NULL,
    GeminiApiKey NVARCHAR(1000) NULL,
    CreatedAt  DATETIME2 DEFAULT GETUTCDATE()
);

-- Datasets
CREATE TABLE Datasets (
    Id          INT PRIMARY KEY IDENTITY,
    UserId      INT NOT NULL REFERENCES Users(Id),
    FileName    NVARCHAR(512) NOT NULL,
    DbName      NVARCHAR(128) NOT NULL,   -- DW_{UserId}_{DatasetId}
    [RowCount]  INT,
    Status      NVARCHAR(50) DEFAULT 'pending',  -- pending|processing|ready|error
    CreatedAt   DATETIME2 DEFAULT GETUTCDATE()
);

-- AnalysisResults
CREATE TABLE AnalysisResults (
    Id          INT PRIMARY KEY IDENTITY,
    DatasetId   INT NOT NULL REFERENCES Datasets(Id),
    SchemaJson  NVARCHAR(MAX),   -- raw AI response JSON
    InsightsJson NVARCHAR(MAX),  -- array of insights
    SsasCubeJson NVARCHAR(MAX),  -- SSAS cube definition
    CreatedAt   DATETIME2 DEFAULT GETUTCDATE()
);
