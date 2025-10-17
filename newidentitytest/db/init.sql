CREATE DATABASE IF NOT EXISTS obstacledb;
USE obstacledb;

CREATE TABLE IF NOT EXISTS `reports` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `ObstacleName` VARCHAR(200) NOT NULL,
  `ObstacleHeight` INT NULL,
  `ObstacleDescription` TEXT NULL,
  `ObstacleLocation` LONGTEXT NULL,
  `UserId` VARCHAR(255) NULL,        -- Identity user id (ingen FK pga rekkefølge)
  `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  INDEX `IX_reports_UserId` (`UserId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
