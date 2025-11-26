-- Таблица для хранения файлов теории и практики
-- Используйте этот SQL скрипт для создания таблицы в базе данных

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LessonAttachments]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[LessonAttachments](
        [AttachmentId] [int] IDENTITY(1,1) NOT NULL,
        [LessonId] [int] NOT NULL,
        [FileName] [nvarchar](255) NOT NULL,
        [FilePath] [nvarchar](max) NULL,
        [FileType] [nvarchar](50) NULL,
        [FileSize] [nvarchar](50) NULL,
        [UploadDate] [datetime] NOT NULL DEFAULT GETDATE(),
        [IsActive] [bit] NOT NULL DEFAULT 1,
        CONSTRAINT [PK_LessonAttachments] PRIMARY KEY CLUSTERED ([AttachmentId] ASC)
    )
    
    -- Создаем индекс для быстрого поиска по LessonId
    CREATE INDEX [IX_LessonAttachments_LessonId] ON [dbo].[LessonAttachments] ([LessonId])
    
    -- Создаем индекс для фильтрации активных вложений
    CREATE INDEX [IX_LessonAttachments_IsActive] ON [dbo].[LessonAttachments] ([IsActive])
    
    PRINT 'Таблица LessonAttachments успешно создана'
END
ELSE
BEGIN
    -- Проверяем и обновляем структуру таблицы если нужно
    -- Убеждаемся, что FilePath может хранить большие данные
    IF EXISTS (
        SELECT 1 
        FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_NAME = 'LessonAttachments' 
          AND COLUMN_NAME = 'FilePath'
          AND (CHARACTER_MAXIMUM_LENGTH IS NULL OR CHARACTER_MAXIMUM_LENGTH <> -1)
    )
    BEGIN
        ALTER TABLE [dbo].[LessonAttachments] ALTER COLUMN [FilePath] [nvarchar](max) NULL
        PRINT 'Колонка FilePath обновлена до NVARCHAR(MAX)'
    END
    
    PRINT 'Таблица LessonAttachments уже существует'
END

