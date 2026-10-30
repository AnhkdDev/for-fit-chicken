-- Run in SSMS against KDADEPTRAI\KDADEPTRAI AFTER stopping local ForFit API.
-- Full copy-only backup, unique file, checksum. Does not overwrite or delete data.
USE [master];
SET NOCOUNT ON;
DECLARE @dir nvarchar(4000) = CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath'));
IF @dir IS NULL THROW 50001, 'SQL default backup directory is not configured.', 1;
IF RIGHT(@dir,1) NOT IN ('\','/') SET @dir = @dir + '\';
DECLARE @file nvarchar(4000) = @dir + 'ForFitChicken_Phase1_' + REPLACE(CONVERT(nvarchar(36),NEWID()),'-','') + '.bak';
BACKUP DATABASE [ForFitChicken_Development] TO DISK = @file WITH COPY_ONLY, CHECKSUM, STATS = 10;
RESTORE VERIFYONLY FROM DISK = @file WITH CHECKSUM;
SELECT @file AS BackupFile;
-- VERIFYONLY is not a restore drill. Validate actual restore on target before cutover.
