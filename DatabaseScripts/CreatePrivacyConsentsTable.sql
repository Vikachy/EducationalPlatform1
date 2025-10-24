-- Создание таблицы для согласий на обработку персональных данных
-- Этот скрипт нужно выполнить в SQL Server Management Studio

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='PrivacyConsents' AND xtype='U')
BEGIN
    CREATE TABLE PrivacyConsents (
        ConsentId INT PRIMARY KEY IDENTITY(1,1),
        UserId INT NOT NULL,
        ConsentText NVARCHAR(MAX) NOT NULL,
        Version NVARCHAR(10) NOT NULL DEFAULT '1.0',
        ConsentDate DATETIME2 DEFAULT GETDATE(),
        IPAddress NVARCHAR(50),
        FOREIGN KEY (UserId) REFERENCES Users(UserId)
    );
    
    PRINT 'Таблица PrivacyConsents создана успешно';
END
ELSE
BEGIN
    PRINT 'Таблица PrivacyConsents уже существует';
END

-- Создание индекса для быстрого поиска по UserId
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_PrivacyConsents_UserId')
BEGIN
    CREATE INDEX IX_PrivacyConsents_UserId ON PrivacyConsents(UserId);
    PRINT 'Индекс IX_PrivacyConsents_UserId создан';
END

