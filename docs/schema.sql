-- Database schema of the Comments SPA.
--
-- The application works with MS SQL Server and creates the tables with EF Core migrations
-- (backend/CommentsApi/Migrations). This file describes the same tables in MySQL syntax, so the schema
-- can be opened in MySQL Workbench: File -> Open SQL Script, run it, and then
-- Database -> Reverse Engineer to get the EER diagram.
-- The script was run on MySQL 8.4, the keys work as in the application: a comment with replies can not be
-- deleted, an attachment is deleted together with its comment, a comment has not more than one attachment.

CREATE SCHEMA IF NOT EXISTS `CommentsDb` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `CommentsDb`;

-- A comment. A reply is a comment with ParentId, so replies can be nested without a limit.
CREATE TABLE `Comments` (
  `Id`        INT           NOT NULL AUTO_INCREMENT,
  `ParentId`  INT           NULL COMMENT 'The comment this one replies to, NULL for a top-level comment',
  `UserName`  VARCHAR(50)   NOT NULL COMMENT 'Latin letters and digits',
  `Email`     VARCHAR(254)  NOT NULL,
  `HomePage`  VARCHAR(2048) NULL,
  `Text`      MEDIUMTEXT    NOT NULL COMMENT 'Safe HTML (up to 5000 characters before escaping): only a, code, i, strong',
  `CreatedAt` DATETIME(6)   NOT NULL COMMENT 'UTC',
  `IpAddress` VARCHAR(45)   NOT NULL COMMENT 'Data that helps to identify the client',
  `UserAgent` VARCHAR(512)  NOT NULL COMMENT 'Data that helps to identify the client',
  PRIMARY KEY (`Id`),
  INDEX `IX_Comments_ParentId` (`ParentId`),
  INDEX `IX_Comments_CreatedAt` (`CreatedAt`),
  INDEX `IX_Comments_UserName` (`UserName`),
  INDEX `IX_Comments_Email` (`Email`),
  CONSTRAINT `FK_Comments_Comments_ParentId`
    FOREIGN KEY (`ParentId`) REFERENCES `Comments` (`Id`)
    ON DELETE RESTRICT
) ENGINE = InnoDB;

-- A file attached to a comment: a picture (JPG, GIF, PNG) or a text file (TXT). Not more than one per comment.
CREATE TABLE `Attachments` (
  `Id`             INT           NOT NULL AUTO_INCREMENT,
  `CommentId`      INT           NOT NULL,
  `Kind`           VARCHAR(20)   NOT NULL COMMENT 'Image or Text',
  `FileName`       VARCHAR(255)  NOT NULL COMMENT 'The name given by the user, it is only shown',
  `StoredFileName` VARCHAR(100)  NOT NULL COMMENT 'The name of the file on the disk, it is made by the server',
  `ContentType`    VARCHAR(100)  NOT NULL,
  `Size`           BIGINT        NOT NULL COMMENT 'Bytes',
  `Width`          INT           NULL COMMENT 'Only for pictures',
  `Height`         INT           NULL COMMENT 'Only for pictures',
  PRIMARY KEY (`Id`),
  UNIQUE INDEX `IX_Attachments_CommentId` (`CommentId`),
  CONSTRAINT `FK_Attachments_Comments_CommentId`
    FOREIGN KEY (`CommentId`) REFERENCES `Comments` (`Id`)
    ON DELETE CASCADE
) ENGINE = InnoDB;
