USE [master]
GO

-- 1. 데이터베이스가 없으면 생성
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = N'SmartSorterDB')
BEGIN
    CREATE DATABASE [SmartSorterDB]
END
GO

USE [SmartSorterDB]
GO

-- 2. 분류 이력 테이블 (SortingHistory)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'SortingHistory')
BEGIN
    CREATE TABLE [dbo].[SortingHistory](
        [LogID]            [bigint] IDENTITY(1,1) NOT NULL,
        [Timestamp]       [datetime] NOT NULL,
        [Mode]            [varchar](20) NOT NULL,
        [DetectedColor]   [varchar](20) NOT NULL,
        [TargetServoAngle][int] NOT NULL,
        [Message]         [nvarchar](255) NULL,
        CONSTRAINT [PK_SortingHistory] PRIMARY KEY CLUSTERED ([LogID] ASC)
    );
END
GO

-- 3. 시스템 알람 테이블 (SystemAlarms)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'SystemAlarms')
BEGIN
    CREATE TABLE [dbo].[SystemAlarms](
        [AlarmID]   [bigint] IDENTITY(1,1) NOT NULL,
        [Timestamp] [datetime] NULL DEFAULT (GETDATE()),
        [AlarmType] [varchar](50) NOT NULL,
        [Message]   [nvarchar](255) NOT NULL,
        CONSTRAINT [PK_SystemAlarms] PRIMARY KEY CLUSTERED ([AlarmID] ASC)
    );
END
GO

-- 4. 시스템 설정 테이블 (SystemSettings)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'SystemSettings')
BEGIN
    CREATE TABLE [dbo].[SystemSettings](
        [SettingKey]   [varchar](50) NOT NULL,
        [SettingValue] [nvarchar](255) NOT NULL,
        [UpdatedTime]  [datetime] NULL DEFAULT (GETDATE()),
        CONSTRAINT [PK_SystemSettings] PRIMARY KEY CLUSTERED ([SettingKey] ASC)
    );
END
GO

-- 5. 시스템 초기 설정 데이터 삽입 (SystemSettings Initial Data)
MERGE INTO [dbo].[SystemSettings] AS Target
USING (VALUES 
    ('ComPort', 'COM9'),
    ('BaudRate', '9600'),
    ('StationId', '1'),
    ('RedServoAngle', '45'),
    ('GreenServoAngle', '90'),
    ('BlueServoAngle', '135'),
    ('YellowServoAngle', '180'),
    ('CameraSource', '0'),
    ('DetectionIntervalMs', '1000'),
    ('RoiWidth', '120'),
    ('RoiHeight', '120'),
    ('RoiOffsetX', '0'),
    ('RoiOffsetY', '0')
) AS Source ([SettingKey], [SettingValue])
ON (Target.[SettingKey] = Source.[SettingKey])
WHEN NOT MATCHED THEN
    INSERT ([SettingKey], [SettingValue], [UpdatedTime])
    VALUES (Source.[SettingKey], Source.[SettingValue], GETDATE());
GO

-- 6. 조회 성능 향상을 위한 인덱스 생성
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_SortingHistory_Timestamp')
BEGIN
    CREATE INDEX [IX_SortingHistory_Timestamp] ON [dbo].[SortingHistory]([Timestamp]);
END
GO