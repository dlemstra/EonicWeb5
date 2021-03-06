/*
   16 August 201320:47:38
   User: 
   Server: eonicds01
   Database: ew_cobra
   Application: 
*/

/* To prevent any potential data loss issues, you should review this script in detail before running it outside the context of the database designer.*/
BEGIN TRANSACTION
SET QUOTED_IDENTIFIER ON
SET ARITHABORT ON
SET NUMERIC_ROUNDABORT OFF
SET CONCAT_NULL_YIELDS_NULL ON
SET ANSI_NULLS ON
SET ANSI_PADDING ON
SET ANSI_WARNINGS ON
COMMIT
BEGIN TRANSACTION
GO
CREATE TABLE dbo.Tmp_tblLookup
	(
	nLkpID int NOT NULL IDENTITY (1, 1),
	cLkpKey nvarchar(255) NOT NULL,
	cLkpValue nvarchar(255) NULL,
	cLkpCategory nvarchar(255) NOT NULL,
	nLkpParent int NULL,
	nDisplayOrder int NULL,
	nAuditId int NULL
	)  ON [PRIMARY]
GO
ALTER TABLE dbo.Tmp_tblLookup SET (LOCK_ESCALATION = TABLE)
GO
SET IDENTITY_INSERT dbo.Tmp_tblLookup ON
GO
IF EXISTS(SELECT * FROM dbo.tblLookup)
	 EXEC('INSERT INTO dbo.Tmp_tblLookup (nLkpID, cLkpKey, cLkpValue, cLkpCategory, nLkpParent, nAuditId)
		SELECT nLkpID, cLkpKey, cLkpValue, cLkpCategory, nLkpParent, nAuditId FROM dbo.tblLookup WITH (HOLDLOCK TABLOCKX)')
GO
SET IDENTITY_INSERT dbo.Tmp_tblLookup OFF
GO
DROP TABLE dbo.tblLookup
GO
EXECUTE sp_rename N'dbo.Tmp_tblLookup', N'tblLookup', 'OBJECT' 
GO
ALTER TABLE dbo.tblLookup ADD CONSTRAINT
	PK_tblLookup PRIMARY KEY CLUSTERED 
	(
	nLkpID
	) WITH( PAD_INDEX = OFF, FILLFACTOR = 90, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

GO
CREATE NONCLUSTERED INDEX PK_tblLookup_Index ON dbo.tblLookup
	(
	nLkpID DESC,
	cLkpKey,
	cLkpValue,
	cLkpCategory
	) WITH( PAD_INDEX = OFF, FILLFACTOR = 90, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX IX_tblLookup_Cat ON dbo.tblLookup
	(
	cLkpCategory
	) WITH( PAD_INDEX = OFF, FILLFACTOR = 90, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX IX_tblLookup_Key ON dbo.tblLookup
	(
	cLkpKey
	) WITH( PAD_INDEX = OFF, FILLFACTOR = 90, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX IX_tblLookup_Val ON dbo.tblLookup
	(
	cLkpValue
	) WITH( PAD_INDEX = OFF, FILLFACTOR = 90, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX IX_tblLookup_CatValue ON dbo.tblLookup
	(
	nLkpID
	) WITH( PAD_INDEX = OFF, FILLFACTOR = 90, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
COMMIT
