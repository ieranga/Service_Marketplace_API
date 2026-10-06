BEGIN TRANSACTION;

-- 1. Drop constraints & indexes referencing ReceiverProfiles
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_ReceiverJobs_ReceiverProfiles_ReceiverProfileId')
BEGIN
    ALTER TABLE ReceiverJobs DROP CONSTRAINT FK_ReceiverJobs_ReceiverProfiles_ReceiverProfileId;
END

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ReceiverJobs_ReceiverProfileId')
BEGIN
    DROP INDEX IX_ReceiverJobs_ReceiverProfileId ON ReceiverJobs;
END

-- 2. Drop ReceiverProfiles table and its FK
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_ReceiverProfiles_Users_UserId')
BEGIN
    ALTER TABLE ReceiverProfiles DROP CONSTRAINT FK_ReceiverProfiles_Users_UserId;
END

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ReceiverProfiles')
BEGIN
    DROP TABLE ReceiverProfiles;
END

-- 3. Modify ReceiverJobs: replace ReceiverProfileId with UserId referencing Users(Id)
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ReceiverJobs') AND name = 'ReceiverProfileId')
BEGIN
    ALTER TABLE ReceiverJobs DROP COLUMN ReceiverProfileId;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ReceiverJobs') AND name = 'UserId')
BEGIN
    ALTER TABLE ReceiverJobs ADD UserId uniqueidentifier NOT NULL;
END

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_UserJobs_Users_UserId')
BEGIN
    ALTER TABLE ReceiverJobs ADD CONSTRAINT FK_UserJobs_Users_UserId FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE;
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserJobs_UserId' AND object_id = OBJECT_ID('ReceiverJobs'))
BEGIN
    CREATE INDEX IX_UserJobs_UserId ON ReceiverJobs(UserId);
END

-- 4. Rename tables as requested:
-- ProviderProfiles -> ProviderServiceProfiles
-- ReceiverJobAreas -> UserJobAreas
-- ReceiverJobs -> UserJobs
-- ReceiverJobTags -> UserJobTags
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ProviderProfiles')
BEGIN
    EXEC sp_rename 'ProviderProfiles', 'ProviderServiceProfiles';
END

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ReceiverJobAreas')
BEGIN
    EXEC sp_rename 'ReceiverJobAreas', 'UserJobAreas';
END

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ReceiverJobs')
BEGIN
    EXEC sp_rename 'ReceiverJobs', 'UserJobs';
END

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ReceiverJobTags')
BEGIN
    EXEC sp_rename 'ReceiverJobTags', 'UserJobTags';
END

-- 5. Rename FK columns in UserJobAreas and UserJobTags:
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('UserJobAreas') AND name = 'ReceiverJobId')
BEGIN
    EXEC sp_rename 'UserJobAreas.ReceiverJobId', 'UserJobId', 'COLUMN';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('UserJobTags') AND name = 'ReceiverJobId')
BEGIN
    EXEC sp_rename 'UserJobTags.ReceiverJobId', 'UserJobId', 'COLUMN';
END

-- 6. Rename FK column in ProviderServices and ProviderServiceAreas to ProviderServiceProfileId
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ProviderServices') AND name = 'ProviderProfileId')
BEGIN
    EXEC sp_rename 'ProviderServices.ProviderProfileId', 'ProviderServiceProfileId', 'COLUMN';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ProviderServiceAreas') AND name = 'ProviderProfileId')
BEGIN
    EXEC sp_rename 'ProviderServiceAreas.ProviderProfileId', 'ProviderServiceProfileId', 'COLUMN';
END

COMMIT TRANSACTION;
PRINT 'Migration completed successfully!';
