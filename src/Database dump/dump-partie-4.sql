CREATE DATABASE  IF NOT EXISTS `librarydb` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci */;
USE `librarydb`;
-- MySQL dump 10.13  Distrib 8.0.36, for Linux (x86_64)
--
-- Host: 127.0.0.1    Database: librarydb
-- ------------------------------------------------------
-- Server version	5.5.5-10.11.8-MariaDB-0ubuntu0.24.04.1

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `AspNetRoleClaims`
--

DROP TABLE IF EXISTS `AspNetRoleClaims`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `AspNetRoleClaims` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `RoleId` varchar(255) NOT NULL,
  `ClaimType` longtext DEFAULT NULL,
  `ClaimValue` longtext DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_AspNetRoleClaims_RoleId` (`RoleId`),
  CONSTRAINT `FK_AspNetRoleClaims_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `AspNetRoles` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `AspNetRoleClaims`
--

LOCK TABLES `AspNetRoleClaims` WRITE;
/*!40000 ALTER TABLE `AspNetRoleClaims` DISABLE KEYS */;
/*!40000 ALTER TABLE `AspNetRoleClaims` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `AspNetRoles`
--

DROP TABLE IF EXISTS `AspNetRoles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `AspNetRoles` (
  `Id` varchar(255) NOT NULL,
  `Name` varchar(256) DEFAULT NULL,
  `NormalizedName` varchar(256) DEFAULT NULL,
  `ConcurrencyStamp` longtext DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `RoleNameIndex` (`NormalizedName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `AspNetRoles`
--

LOCK TABLES `AspNetRoles` WRITE;
/*!40000 ALTER TABLE `AspNetRoles` DISABLE KEYS */;
INSERT INTO `AspNetRoles` VALUES ('9e585caa-9d72-40a3-ab61-403399ace3d6','Admin','ADMIN',NULL),('aba5b8fd-0b0f-48a7-8d85-ed8cce30b7dd','User','USER',NULL);
/*!40000 ALTER TABLE `AspNetRoles` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `AspNetUserClaims`
--

DROP TABLE IF EXISTS `AspNetUserClaims`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `AspNetUserClaims` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `UserId` varchar(255) NOT NULL,
  `ClaimType` longtext DEFAULT NULL,
  `ClaimValue` longtext DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_AspNetUserClaims_UserId` (`UserId`),
  CONSTRAINT `FK_AspNetUserClaims_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `AspNetUserClaims`
--

LOCK TABLES `AspNetUserClaims` WRITE;
/*!40000 ALTER TABLE `AspNetUserClaims` DISABLE KEYS */;
/*!40000 ALTER TABLE `AspNetUserClaims` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `AspNetUserLogins`
--

DROP TABLE IF EXISTS `AspNetUserLogins`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `AspNetUserLogins` (
  `LoginProvider` varchar(255) NOT NULL,
  `ProviderKey` varchar(255) NOT NULL,
  `ProviderDisplayName` longtext DEFAULT NULL,
  `UserId` varchar(255) NOT NULL,
  PRIMARY KEY (`LoginProvider`,`ProviderKey`),
  KEY `IX_AspNetUserLogins_UserId` (`UserId`),
  CONSTRAINT `FK_AspNetUserLogins_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `AspNetUserLogins`
--

LOCK TABLES `AspNetUserLogins` WRITE;
/*!40000 ALTER TABLE `AspNetUserLogins` DISABLE KEYS */;
/*!40000 ALTER TABLE `AspNetUserLogins` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `AspNetUserRoles`
--

DROP TABLE IF EXISTS `AspNetUserRoles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `AspNetUserRoles` (
  `UserId` varchar(255) NOT NULL,
  `RoleId` varchar(255) NOT NULL,
  PRIMARY KEY (`UserId`,`RoleId`),
  KEY `IX_AspNetUserRoles_RoleId` (`RoleId`),
  CONSTRAINT `FK_AspNetUserRoles_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `AspNetRoles` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_AspNetUserRoles_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `AspNetUserRoles`
--

LOCK TABLES `AspNetUserRoles` WRITE;
/*!40000 ALTER TABLE `AspNetUserRoles` DISABLE KEYS */;
INSERT INTO `AspNetUserRoles` VALUES ('4ae40925-fec9-4b16-9457-e7496c381c97','9e585caa-9d72-40a3-ab61-403399ace3d6'),('9ad524da-9fb2-4eca-9d53-d8ea943182e2','aba5b8fd-0b0f-48a7-8d85-ed8cce30b7dd');
/*!40000 ALTER TABLE `AspNetUserRoles` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `AspNetUserTokens`
--

DROP TABLE IF EXISTS `AspNetUserTokens`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `AspNetUserTokens` (
  `UserId` varchar(255) NOT NULL,
  `LoginProvider` varchar(255) NOT NULL,
  `Name` varchar(255) NOT NULL,
  `Value` longtext DEFAULT NULL,
  PRIMARY KEY (`UserId`,`LoginProvider`,`Name`),
  CONSTRAINT `FK_AspNetUserTokens_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `AspNetUserTokens`
--

LOCK TABLES `AspNetUserTokens` WRITE;
/*!40000 ALTER TABLE `AspNetUserTokens` DISABLE KEYS */;
/*!40000 ALTER TABLE `AspNetUserTokens` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `AspNetUsers`
--

DROP TABLE IF EXISTS `AspNetUsers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `AspNetUsers` (
  `Id` varchar(255) NOT NULL,
  `FullName` longtext DEFAULT NULL,
  `Description` longtext DEFAULT NULL,
  `ProfilePicture` longtext DEFAULT NULL,
  `UserName` varchar(256) DEFAULT NULL,
  `NormalizedUserName` varchar(256) DEFAULT NULL,
  `Email` varchar(256) DEFAULT NULL,
  `NormalizedEmail` varchar(256) DEFAULT NULL,
  `EmailConfirmed` tinyint(1) NOT NULL,
  `PasswordHash` longtext DEFAULT NULL,
  `SecurityStamp` longtext DEFAULT NULL,
  `ConcurrencyStamp` longtext DEFAULT NULL,
  `PhoneNumber` longtext DEFAULT NULL,
  `PhoneNumberConfirmed` tinyint(1) NOT NULL,
  `TwoFactorEnabled` tinyint(1) NOT NULL,
  `LockoutEnd` datetime(6) DEFAULT NULL,
  `LockoutEnabled` tinyint(1) NOT NULL,
  `AccessFailedCount` int(11) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UserNameIndex` (`NormalizedUserName`),
  KEY `EmailIndex` (`NormalizedEmail`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `AspNetUsers`
--

LOCK TABLES `AspNetUsers` WRITE;
/*!40000 ALTER TABLE `AspNetUsers` DISABLE KEYS */;
INSERT INTO `AspNetUsers` VALUES ('4ae40925-fec9-4b16-9457-e7496c381c97','Admin','Administrator Account',NULL,'admin@library.com','ADMIN@LIBRARY.COM','admin@library.com','ADMIN@LIBRARY.COM',0,'AQAAAAIAAYagAAAAELhRin8TAZ4vFjNVQbX0f3iXZ3OHnmEVtU3qQ8pyz0kqKlOaMDgyU/RDu6v1VWl7jQ==','CKB5NME2OCPOMCO4VKOCB7ROSXM6DKQA','d8e6c9c6-76ff-42c1-984f-6cee627f7e13',NULL,0,0,NULL,1,0),('9ad524da-9fb2-4eca-9d53-d8ea943182e2','Ndx','sa description','/images/profiles/9ad524da-9fb2-4eca-9d53-d8ea943182e2_Ndx.jpeg','nix@nix.fr','NIX@NIX.FR','nix@nix.fr','NIX@NIX.FR',0,'AQAAAAIAAYagAAAAELTGV1ksbXeiVor81yHO89/bhE46VJT2fNnG13ksfAegnFTAhGlgUIDNuBYQj6Ax2w==','U4TW3SKPVBXJNHUVPIVIABTPXRCKMPCE','256f88bf-ad58-4f73-ac44-a120fd175fc3',NULL,0,0,NULL,1,0);
/*!40000 ALTER TABLE `AspNetUsers` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `Authors`
--

DROP TABLE IF EXISTS `Authors`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Authors` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Name` longtext NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `Authors`
--

LOCK TABLES `Authors` WRITE;
/*!40000 ALTER TABLE `Authors` DISABLE KEYS */;
INSERT INTO `Authors` VALUES (1,'Isaac Asimov'),(2,'Arthur C. Clarke'),(3,'Leonardo da Vinci'),(4,'Walter Isaacson');
/*!40000 ALTER TABLE `Authors` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `BooksMagazines`
--

DROP TABLE IF EXISTS `BooksMagazines`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `BooksMagazines` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Title` longtext NOT NULL,
  `Description` longtext NOT NULL,
  `Tags` longtext NOT NULL,
  `FilePath` longtext NOT NULL,
  `CoverImagePath` longtext NOT NULL,
  `UploadDate` datetime(6) NOT NULL,
  `AuthorId` int(11) NOT NULL DEFAULT 0,
  `CategoryId` int(11) NOT NULL DEFAULT 0,
  `ViewCount` int(11) NOT NULL DEFAULT 0,
  `DownloadCount` int(11) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`),
  KEY `IX_BooksMagazines_AuthorId` (`AuthorId`),
  KEY `IX_BooksMagazines_CategoryId` (`CategoryId`),
  KEY `IX_BooksMagazines_Title` (`Title`(768)),
  KEY `IX_BooksMagazines_Description` (`Description`(768)),
  KEY `IX_BooksMagazines_Tags` (`Tags`(768)),
  CONSTRAINT `FK_BooksMagazines_Authors_AuthorId` FOREIGN KEY (`AuthorId`) REFERENCES `Authors` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_BooksMagazines_Categories_CategoryId` FOREIGN KEY (`CategoryId`) REFERENCES `Categories` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `BooksMagazines`
--

LOCK TABLES `BooksMagazines` WRITE;
/*!40000 ALTER TABLE `BooksMagazines` DISABLE KEYS */;
INSERT INTO `BooksMagazines` VALUES (1,'Foundation','A science fiction classic','space, empire, future','/files/foundation.pdf','/images/covers/foundation.jpg','2024-10-17 17:12:36.000000',1,1,25,15),(2,'2001: A Space Odyssey','Space exploration and AI','space, future, AI','/files/2001_space_odyssey.pdf','/images/covers/2001.jpg','2024-10-17 17:12:36.000000',2,1,30,20),(3,'The Mona Lisa','A study of Leonardo da Vinci’s art','art, renaissance, painting','/files/mona_lisa.pdf','/images/covers/mona_lisa.jpg','2024-10-17 17:12:36.000000',3,3,43,26),(4,'Steve Jobs','Biography of Steve Jobs','technology, biography, innovation','/files/steve_jobs.pdf','/images/covers/steve_jobs.jpg','2024-10-17 17:12:36.000000',4,2,50,35);
/*!40000 ALTER TABLE `BooksMagazines` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `Categories`
--

DROP TABLE IF EXISTS `Categories`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Categories` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Name` longtext NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `Categories`
--

LOCK TABLES `Categories` WRITE;
/*!40000 ALTER TABLE `Categories` DISABLE KEYS */;
INSERT INTO `Categories` VALUES (1,'Science Fiction'),(2,'Technology'),(3,'Art'),(4,'History');
/*!40000 ALTER TABLE `Categories` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `UserFavorites`
--

DROP TABLE IF EXISTS `UserFavorites`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `UserFavorites` (
  `UserId` varchar(255) NOT NULL,
  `BookMagazineId` int(11) NOT NULL,
  PRIMARY KEY (`UserId`,`BookMagazineId`),
  KEY `BookMagazineId` (`BookMagazineId`),
  CONSTRAINT `UserFavorites_ibfk_1` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `UserFavorites_ibfk_2` FOREIGN KEY (`BookMagazineId`) REFERENCES `BooksMagazines` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `UserFavorites`
--

LOCK TABLES `UserFavorites` WRITE;
/*!40000 ALTER TABLE `UserFavorites` DISABLE KEYS */;
INSERT INTO `UserFavorites` VALUES ('9ad524da-9fb2-4eca-9d53-d8ea943182e2',1),('9ad524da-9fb2-4eca-9d53-d8ea943182e2',2),('9ad524da-9fb2-4eca-9d53-d8ea943182e2',3);
/*!40000 ALTER TABLE `UserFavorites` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `UserReadingHistory`
--

DROP TABLE IF EXISTS `UserReadingHistory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `UserReadingHistory` (
  `UserId` varchar(255) NOT NULL,
  `BookMagazineId` int(11) NOT NULL,
  `LastReadDate` datetime NOT NULL,
  PRIMARY KEY (`UserId`,`BookMagazineId`),
  KEY `BookMagazineId` (`BookMagazineId`),
  CONSTRAINT `UserReadingHistory_ibfk_1` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `UserReadingHistory_ibfk_2` FOREIGN KEY (`BookMagazineId`) REFERENCES `BooksMagazines` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `UserReadingHistory`
--

LOCK TABLES `UserReadingHistory` WRITE;
/*!40000 ALTER TABLE `UserReadingHistory` DISABLE KEYS */;
INSERT INTO `UserReadingHistory` VALUES ('9ad524da-9fb2-4eca-9d53-d8ea943182e2',1,'2024-10-17 17:12:36'),('9ad524da-9fb2-4eca-9d53-d8ea943182e2',2,'2024-10-17 17:12:36'),('9ad524da-9fb2-4eca-9d53-d8ea943182e2',4,'2024-10-17 16:41:39');
/*!40000 ALTER TABLE `UserReadingHistory` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `__EFMigrationsHistory`
--

DROP TABLE IF EXISTS `__EFMigrationsHistory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `__EFMigrationsHistory` (
  `MigrationId` varchar(150) NOT NULL,
  `ProductVersion` varchar(32) NOT NULL,
  PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `__EFMigrationsHistory`
--

LOCK TABLES `__EFMigrationsHistory` WRITE;
/*!40000 ALTER TABLE `__EFMigrationsHistory` DISABLE KEYS */;
INSERT INTO `__EFMigrationsHistory` VALUES ('20240904193251_InitialCreate','8.0.8'),('20240904193510_MakeProfilePictureNullable','8.0.8'),('20240905170541_AddCoverImageToBookMagazine','8.0.8'),('20240905171259_AddAuthorAndCategoryTablesv2','8.0.8'),('20240916214840_AddPart2Tables','8.0.8'),('20241015143322_AddUserFavoritesAndReadingHistory','8.0.8'),('20241017145140_AddViewCountToBooksMagazines','8.0.8');
/*!40000 ALTER TABLE `__EFMigrationsHistory` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Dumping events for database 'librarydb'
--

--
-- Dumping routines for database 'librarydb'
--
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2024-10-17 19:31:30
