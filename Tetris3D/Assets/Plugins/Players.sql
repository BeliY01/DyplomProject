IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' and xtype='U')
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nickname NVARCHAR(255) UNIQUE NOT NULL,
    Progress NVARCHAR(255),
    Points INT DEFAULT 0
);
