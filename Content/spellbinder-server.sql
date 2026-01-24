-- MySQL dump 10.13  Distrib 8.0.44, for Win64 (x86_64)
--
-- Host: localhost    Database: spellbinder
-- ------------------------------------------------------
-- Server version	8.0.44

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
-- Table structure for table `accounts`
--

DROP TABLE IF EXISTS `accounts`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `accounts` (
  `AccountID` int NOT NULL AUTO_INCREMENT,
  `username` varchar(32) NOT NULL DEFAULT '',
  `password` varchar(32) NOT NULL,
  `email` varchar(64) DEFAULT NULL,
  `created` datetime DEFAULT CURRENT_TIMESTAMP,
  `last_login` datetime DEFAULT NULL,
  `banned` tinyint DEFAULT '0',
  `Admin` int DEFAULT NULL,
  PRIMARY KEY (`AccountID`),
  UNIQUE KEY `username` (`username`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb3;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `accounts`
--

LOCK TABLES `accounts` WRITE;
/*!40000 ALTER TABLE `accounts` DISABLE KEYS */;
INSERT INTO `accounts` VALUES (1,'mindl','test1',NULL,'2025-11-22 01:19:40',NULL,0,1),(2,'farley','test1',NULL,'2025-11-25 16:14:33',NULL,0,1),(3,'test1','test1',NULL,'2025-11-25 16:14:33',NULL,0,1),(4,'test2','test2',NULL,'2025-11-25 16:14:33',NULL,0,1);
/*!40000 ALTER TABLE `accounts` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `banned_ips`
--

DROP TABLE IF EXISTS `banned_ips`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `banned_ips` (
  `ip` varchar(45) NOT NULL,
  `reason` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`ip`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `banned_ips`
--

LOCK TABLES `banned_ips` WRITE;
/*!40000 ALTER TABLE `banned_ips` DISABLE KEYS */;
/*!40000 ALTER TABLE `banned_ips` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `banned_names`
--

DROP TABLE IF EXISTS `banned_names`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `banned_names` (
  `name` varchar(32) NOT NULL,
  PRIMARY KEY (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `banned_names`
--

LOCK TABLES `banned_names` WRITE;
/*!40000 ALTER TABLE `banned_names` DISABLE KEYS */;
/*!40000 ALTER TABLE `banned_names` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `banned_serials`
--

DROP TABLE IF EXISTS `banned_serials`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `banned_serials` (
  `serial` varchar(64) NOT NULL,
  PRIMARY KEY (`serial`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `banned_serials`
--

LOCK TABLES `banned_serials` WRITE;
/*!40000 ALTER TABLE `banned_serials` DISABLE KEYS */;
/*!40000 ALTER TABLE `banned_serials` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `cabals`
--

DROP TABLE IF EXISTS `cabals`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `cabals` (
  `cabalid` int NOT NULL AUTO_INCREMENT,
  `cabalname` varchar(24) NOT NULL,
  `cabaltag` varchar(4) NOT NULL,
  `caballeader` varchar(32) NOT NULL,
  PRIMARY KEY (`cabalid`),
  UNIQUE KEY `cabalname_UNIQUE` (`cabalname`),
  UNIQUE KEY `cabaltag_UNIQUE` (`cabaltag`),
  UNIQUE KEY `caballeader_UNIQUE` (`caballeader`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `cabals`
--

LOCK TABLES `cabals` WRITE;
/*!40000 ALTER TABLE `cabals` DISABLE KEYS */;
INSERT INTO `cabals` VALUES (0,'','',''),(1,'Testers','TEST','Test2'),(2,'Binders','BIND','Jayz'),(3,'Wackers','WACK','Mindl'),(4,'Readers','READ','Khisanith');
/*!40000 ALTER TABLE `cabals` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `after_cabal_delete` AFTER DELETE ON `cabals` FOR EACH ROW BEGIN
    UPDATE spellbinder.characters SET cabalId = 0 WHERE cabalId = OLD.cabalId;
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Table structure for table `character_statistics`
--

DROP TABLE IF EXISTS `character_statistics`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `character_statistics` (
  `charid` int NOT NULL,
  `hidden` tinyint NOT NULL DEFAULT '0',
  `kills` int unsigned NOT NULL DEFAULT '0',
  `deaths` int unsigned NOT NULL DEFAULT '0',
  `raises` int unsigned NOT NULL DEFAULT '0',
  `damagedone` bigint unsigned NOT NULL DEFAULT '0',
  `damagetaken` bigint unsigned NOT NULL DEFAULT '0',
  `healingdone` bigint unsigned NOT NULL DEFAULT '0',
  `healingtaken` bigint unsigned NOT NULL DEFAULT '0',
  `wins` int unsigned NOT NULL DEFAULT '0',
  `losses` int unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`charid`),
  CONSTRAINT `character_statistics_ibfk_1` FOREIGN KEY (`charid`) REFERENCES `characters` (`charid`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `character_statistics`
--

LOCK TABLES `character_statistics` WRITE;
/*!40000 ALTER TABLE `character_statistics` DISABLE KEYS */;
INSERT INTO `character_statistics` VALUES (2,0,5,5,0,1285,549,0,0,0,0),(5,0,15,3,0,2679,1190,0,0,0,0),(6,0,0,0,0,0,92,0,0,0,0),(9,0,0,0,0,0,0,0,0,0,0),(10,0,0,0,0,0,0,0,0,0,0),(11,0,0,2,0,509,455,0,0,0,0),(12,0,2,9,0,1411,1669,0,0,0,0),(13,1,0,0,0,0,0,0,0,0,0),(14,0,0,6,0,92,3708,0,0,0,0),(16,1,0,0,0,0,0,0,0,0,0),(17,1,3,0,0,1714,92,0,0,0,0),(18,1,0,0,0,0,0,0,0,0,0),(19,0,0,0,0,0,0,0,0,0,0),(21,0,0,0,0,0,0,0,0,0,0),(23,0,0,0,0,0,0,0,0,0,0);
/*!40000 ALTER TABLE `character_statistics` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `character_statistics_weekly`
--

DROP TABLE IF EXISTS `character_statistics_weekly`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `character_statistics_weekly` (
  `charid` int NOT NULL,
  `date` bigint NOT NULL,
  `hidden` tinyint NOT NULL DEFAULT '0',
  `kills` int unsigned NOT NULL DEFAULT '0',
  `deaths` int unsigned NOT NULL DEFAULT '0',
  `raises` int unsigned NOT NULL DEFAULT '0',
  `damagedone` bigint unsigned NOT NULL DEFAULT '0',
  `damagetaken` bigint unsigned NOT NULL DEFAULT '0',
  `healingdone` bigint unsigned NOT NULL DEFAULT '0',
  `healingtaken` bigint unsigned NOT NULL DEFAULT '0',
  `wins` int unsigned NOT NULL DEFAULT '0',
  `losses` int unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`charid`,`date`),
  CONSTRAINT `character_statistics_weekly_ibfk_1` FOREIGN KEY (`charid`) REFERENCES `characters` (`charid`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `character_statistics_weekly`
--

LOCK TABLES `character_statistics_weekly` WRITE;
/*!40000 ALTER TABLE `character_statistics_weekly` DISABLE KEYS */;
INSERT INTO `character_statistics_weekly` VALUES (2,1763942400,0,0,0,0,0,0,0,0,0,0),(2,1764547200,0,0,0,0,0,0,0,0,0,0),(2,1765152000,1,2,2,0,0,234,0,0,0,0),(2,1765756800,1,1,2,0,0,156,0,0,0,0),(2,1766361600,1,0,1,0,0,159,0,0,0,0),(2,1768176000,0,0,0,0,0,0,0,0,0,0),(2,1768780800,0,2,0,0,0,0,0,0,0,0),(5,1763942400,0,0,0,0,0,0,0,0,0,0),(5,1764547200,0,0,0,0,0,0,0,0,0,0),(5,1765152000,1,2,2,0,0,488,0,0,0,0),(5,1765756800,1,3,1,0,0,328,0,0,0,0),(5,1766361600,1,10,0,0,0,374,0,0,0,0),(5,1768780800,0,0,0,0,0,0,0,0,0,0),(6,1763942400,0,0,0,0,0,0,0,0,0,0),(6,1764547200,0,0,0,0,0,0,0,0,0,0),(6,1768780800,0,0,0,0,0,92,0,0,0,0),(9,1764547200,0,0,0,0,0,0,0,0,0,0),(9,1765152000,0,0,0,0,0,0,0,0,0,0),(9,1768780800,0,0,0,0,0,0,0,0,0,0),(10,1764547200,0,0,0,0,0,0,0,0,0,0),(10,1768176000,0,0,0,0,0,0,0,0,0,0),(10,1768780800,0,0,0,0,0,0,0,0,0,0),(11,1765152000,1,0,0,0,0,0,0,0,0,0),(11,1766361600,1,0,0,0,0,129,0,0,0,0),(11,1767571200,1,0,0,0,0,0,0,0,0,0),(11,1768176000,0,0,0,0,0,0,0,0,0,0),(11,1768780800,0,0,2,0,0,326,0,0,0,0),(12,1765152000,1,0,0,0,0,0,0,0,0,0),(12,1765756800,1,0,1,0,0,156,0,0,0,0),(12,1766361600,1,0,8,0,0,1470,0,0,0,0),(12,1766966400,1,1,0,0,0,0,0,0,0,0),(12,1767571200,1,1,0,0,0,0,0,0,0,0),(12,1768176000,1,0,0,0,0,43,0,0,0,0),(12,1768780800,0,0,0,0,0,0,0,0,0,0),(13,1765152000,1,0,0,0,0,0,0,0,0,0),(13,1765756800,1,0,0,0,0,0,0,0,0,0),(13,1768176000,1,0,0,0,0,0,0,0,0,0),(13,1768780800,1,0,0,0,0,0,0,0,0,0),(14,1765756800,1,0,0,0,0,143,0,0,0,0),(14,1766361600,1,0,2,0,0,1135,0,0,0,0),(14,1766966400,1,0,3,0,0,1443,0,0,0,0),(14,1767571200,1,0,1,0,0,203,0,0,0,0),(14,1768176000,0,0,0,0,0,784,0,0,0,0),(14,1768780800,0,0,0,0,0,0,0,0,0,0),(16,1766361600,1,0,0,0,0,0,0,0,0,0),(17,1766361600,1,1,0,0,0,0,0,0,0,0),(17,1766966400,1,2,0,0,0,92,0,0,0,0),(17,1768176000,1,0,0,0,0,0,0,0,0,0),(18,1766361600,1,0,0,0,0,0,0,0,0,0),(19,1768176000,1,0,0,0,0,0,0,0,0,0),(19,1768780800,0,0,0,0,0,0,0,0,0,0),(21,1768780800,0,0,0,0,0,0,0,0,0,0),(23,1768780800,0,0,0,0,0,0,0,0,0,0);
/*!40000 ALTER TABLE `character_statistics_weekly` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `characters`
--

DROP TABLE IF EXISTS `characters`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `characters` (
  `charid` int NOT NULL AUTO_INCREMENT,
  `accountid` int NOT NULL,
  `slot` tinyint unsigned NOT NULL DEFAULT '0',
  `name` varchar(32) NOT NULL,
  `agility` tinyint unsigned NOT NULL DEFAULT '10',
  `constitution` tinyint unsigned NOT NULL DEFAULT '10',
  `memory` tinyint unsigned NOT NULL DEFAULT '10',
  `reasoning` tinyint unsigned NOT NULL DEFAULT '10',
  `discipline` tinyint unsigned NOT NULL DEFAULT '10',
  `empathy` tinyint unsigned NOT NULL DEFAULT '10',
  `intuition` tinyint unsigned NOT NULL DEFAULT '10',
  `presence` tinyint unsigned NOT NULL DEFAULT '10',
  `quickness` tinyint unsigned NOT NULL DEFAULT '10',
  `strength` tinyint unsigned NOT NULL DEFAULT '10',
  `spent_stat` int unsigned NOT NULL DEFAULT '0',
  `bonus_stat` int unsigned NOT NULL DEFAULT '0',
  `bonus_spent` int unsigned NOT NULL DEFAULT '0',
  `list_1` tinyint unsigned NOT NULL DEFAULT '0',
  `list_2` tinyint unsigned NOT NULL DEFAULT '0',
  `list_3` tinyint unsigned NOT NULL DEFAULT '0',
  `list_4` tinyint unsigned NOT NULL DEFAULT '0',
  `list_5` tinyint unsigned NOT NULL DEFAULT '0',
  `list_6` tinyint unsigned NOT NULL DEFAULT '0',
  `list_7` tinyint unsigned NOT NULL DEFAULT '0',
  `list_8` tinyint unsigned NOT NULL DEFAULT '0',
  `list_9` tinyint unsigned NOT NULL DEFAULT '0',
  `list_10` tinyint unsigned NOT NULL DEFAULT '0',
  `list_level_1` tinyint unsigned NOT NULL DEFAULT '1',
  `list_level_2` tinyint unsigned NOT NULL DEFAULT '1',
  `list_level_3` tinyint unsigned NOT NULL DEFAULT '1',
  `list_level_4` tinyint unsigned NOT NULL DEFAULT '1',
  `list_level_5` tinyint unsigned NOT NULL DEFAULT '1',
  `list_level_6` tinyint unsigned NOT NULL DEFAULT '1',
  `list_level_7` tinyint unsigned NOT NULL DEFAULT '1',
  `list_level_8` tinyint unsigned NOT NULL DEFAULT '1',
  `list_level_9` tinyint unsigned NOT NULL DEFAULT '1',
  `list_level_10` tinyint unsigned NOT NULL DEFAULT '1',
  `class` tinyint unsigned NOT NULL DEFAULT '1',
  `level` tinyint unsigned NOT NULL DEFAULT '1',
  `spell_picks` tinyint unsigned NOT NULL DEFAULT '0',
  `model` tinyint unsigned NOT NULL DEFAULT '0',
  `oplevel` tinyint unsigned NOT NULL DEFAULT '0',
  `experience` bigint unsigned NOT NULL DEFAULT '0',
  `flags` int unsigned NOT NULL DEFAULT '0',
  `spell_key_1` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_2` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_3` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_4` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_5` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_6` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_7` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_8` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_9` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_10` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_11` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_12` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_13` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_14` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_15` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_16` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_17` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_18` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_19` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_20` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_21` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_22` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_23` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_24` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_25` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_26` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_27` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_28` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_29` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_30` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_31` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_32` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_33` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_34` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_35` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_36` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_37` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_38` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_39` smallint unsigned NOT NULL DEFAULT '0',
  `spell_key_40` smallint unsigned NOT NULL DEFAULT '0',
  `cabalid` int DEFAULT '0',
  `created` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`charid`),
  UNIQUE KEY `name` (`name`),
  UNIQUE KEY `unique_slot` (`accountid`,`slot`),
  KEY `fk_character_cabal_idx` (`cabalid`),
  CONSTRAINT `characters_ibfk_1` FOREIGN KEY (`accountid`) REFERENCES `accounts` (`AccountID`) ON DELETE CASCADE,
  CONSTRAINT `fk_characters_cabal` FOREIGN KEY (`cabalid`) REFERENCES `cabals` (`cabalid`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=24 DEFAULT CHARSET=utf8mb3;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `characters`
--

LOCK TABLES `characters` WRITE;
/*!40000 ALTER TABLE `characters` DISABLE KEYS */;
INSERT INTO `characters` VALUES (2,1,0,'Mindl',100,100,100,100,100,100,100,100,100,100,0,0,0,1,2,4,18,255,255,255,255,255,255,25,25,25,25,0,0,0,0,0,0,0,25,233,201,0,2330000,0,124,294,75,0,0,295,296,125,29,71,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,3,'2025-11-24 05:08:18'),(5,2,0,'Farley',80,80,80,80,80,80,80,80,80,80,0,0,0,15,3,16,17,255,255,255,255,255,255,25,25,25,25,0,0,0,0,0,0,1,25,233,203,0,2330000,0,265,92,0,0,0,156,0,0,0,0,8,286,290,0,0,94,0,0,0,0,230,0,0,0,0,282,0,0,0,0,275,0,0,0,0,261,0,0,0,0,0,'2025-11-25 22:05:54'),(6,1,1,'Khisanith',80,80,80,80,80,80,80,80,80,80,0,0,0,6,7,8,9,10,255,255,255,255,255,25,25,25,25,25,25,0,0,0,0,3,25,209,202,0,2330000,0,152,76,114,151,109,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,4,'2025-11-26 21:44:38'),(9,1,3,'George',80,80,80,80,80,80,80,80,80,80,0,0,0,15,3,16,17,0,0,0,0,0,0,25,25,25,25,0,0,0,0,0,0,1,16,224,204,0,800000,0,265,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,'2025-12-07 21:19:25'),(10,4,2,'Jeff',80,80,80,80,80,80,80,80,80,80,0,0,0,11,12,13,14,0,0,0,0,0,18,25,25,25,25,0,0,0,0,0,0,2,25,233,204,0,2330000,0,247,192,88,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,'2025-12-07 21:24:59'),(11,3,0,'Test1',100,100,100,100,100,100,100,100,100,100,0,0,0,1,2,4,18,0,0,0,0,0,18,25,25,25,25,0,0,0,0,0,0,0,25,233,201,0,2316901,0,124,294,75,0,0,295,296,125,29,71,4,16,72,298,229,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,4,'2025-11-24 05:08:18'),(12,4,0,'Test2',100,100,100,100,100,100,100,100,100,100,0,0,0,1,2,4,18,0,0,0,0,0,18,25,25,25,25,0,0,0,0,0,0,0,25,233,201,0,2328506,0,29,4,16,0,2,26,0,29,0,27,16,28,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,4,'2025-11-24 05:08:18'),(13,4,3,'Testt',80,80,80,80,80,80,80,80,80,80,0,0,0,15,3,16,17,0,0,0,0,0,18,25,25,25,25,0,0,0,0,0,0,1,25,233,204,0,2330000,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,'2025-12-14 20:01:21'),(14,3,1,'Testt1',80,80,80,80,80,80,80,80,80,80,0,0,0,6,7,8,9,10,5,0,0,0,18,25,25,25,25,25,25,0,0,0,0,3,25,209,202,0,2330000,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,176,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,3,'2025-11-26 21:44:38'),(16,2,1,'Samus',80,80,80,80,80,80,80,80,80,80,0,0,0,11,12,13,14,0,0,0,0,0,0,1,1,1,1,0,0,0,0,0,0,2,1,0,202,0,0,0,166,136,217,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,'2025-12-23 06:33:21'),(17,2,2,'Dinkus',100,100,100,100,100,100,100,100,100,100,0,0,0,1,2,4,18,255,255,255,255,255,255,25,25,25,25,0,0,0,0,0,0,0,25,24,201,0,2330000,0,124,294,75,0,0,295,296,125,29,71,16,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,'2025-11-24 05:08:18'),(18,2,3,'WilSmith',80,80,80,80,80,80,80,80,80,80,0,0,0,15,3,16,17,0,0,0,0,0,0,1,1,1,1,0,0,0,0,0,0,1,1,0,203,0,0,0,260,267,266,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,'2025-12-23 06:41:46'),(19,4,1,'Healbot',80,80,80,80,80,80,80,80,80,80,0,0,0,6,7,8,9,10,5,0,0,0,18,2,2,2,2,2,2,0,0,0,0,3,2,255,203,0,500,0,152,76,114,151,109,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,'2026-01-16 22:01:58'),(21,3,2,'Holly',80,80,80,80,80,80,80,80,80,80,0,0,0,15,3,16,17,0,0,0,0,0,0,20,20,20,20,0,0,0,0,0,0,1,20,238,202,0,1380000,0,260,267,266,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,'2026-01-20 03:59:40'),(23,1,2,'Jayz',80,80,80,80,80,80,80,80,80,80,0,0,0,11,12,13,14,0,0,0,0,0,0,15,15,15,15,0,0,0,0,0,0,2,15,243,201,0,680000,0,166,136,217,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2,'2026-01-20 06:39:00');
/*!40000 ALTER TABLE `characters` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `matches`
--

DROP TABLE IF EXISTS `matches`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `matches` (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `arenaid` int NOT NULL,
  `tableid` int NOT NULL,
  `creation_time` int NOT NULL,
  `player_count` int NOT NULL DEFAULT '0',
  `highest_player_count` int NOT NULL DEFAULT '0',
  `max_players` int NOT NULL,
  `current_state` int NOT NULL,
  `end_state` int NOT NULL,
  `short_name` varchar(64) NOT NULL,
  `long_name` varchar(128) NOT NULL,
  `founder_charid` int NOT NULL,
  `duration` int NOT NULL,
  `level_range` int NOT NULL,
  `mode` int NOT NULL,
  `rules` int NOT NULL,
  PRIMARY KEY (`id`),
  KEY `idx_arena_table` (`arenaid`,`tableid`),
  KEY `idx_creation_time` (`creation_time`),
  KEY `idx_founder` (`founder_charid`)
) ENGINE=InnoDB AUTO_INCREMENT=764 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `matches`
--

LOCK TABLES `matches` WRITE;
/*!40000 ALTER TABLE `matches` DISABLE KEYS */;
INSERT INTO `matches` VALUES (1,1,0,1764114420,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(2,1,0,1764114534,0,0,30,0,0,'Temple','[N] Rathespa Temple',5,0,1,0,0),(3,1,0,1764114899,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(4,1,0,1764115477,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(5,1,0,1764116095,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(6,1,0,1764116322,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(7,1,0,1764116841,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(8,1,0,1764117698,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(9,1,0,1764117795,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(10,1,0,1764121080,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(11,1,0,1764121722,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(12,1,0,1764122598,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(13,1,0,1764147402,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(14,1,0,1764148306,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(15,1,0,1764170177,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(16,1,0,1764171736,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(17,1,0,1764172515,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(18,1,0,1764172899,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(19,1,0,1764174729,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(20,2,0,1764175023,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(21,1,0,1764175153,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(22,1,0,1764175435,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(23,1,0,1764175490,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(24,1,0,1764178610,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(25,1,0,1764178950,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(26,2,0,1764179078,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(27,1,0,1764331501,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(28,1,0,1764331802,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(29,1,0,1764331959,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(30,1,0,1764332089,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(31,1,0,1764332321,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(32,1,0,1764333011,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(33,1,0,1764333736,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(34,1,0,1764333923,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(35,1,0,1764334135,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(36,1,0,1764334698,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(37,1,0,1764335742,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(38,1,0,1764335893,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(39,1,0,1764336840,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(40,1,0,1764337139,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(41,1,0,1764337907,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(42,1,0,1764338095,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(43,1,0,1764338459,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(44,1,0,1764338526,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(45,1,0,1764339199,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(46,1,0,1764340706,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(47,1,0,1764340749,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(48,1,0,1764343622,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(49,1,0,1764345890,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(50,1,0,1764345960,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(51,1,0,1764346351,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(52,1,0,1764346564,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(53,1,0,1764346865,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(54,1,0,1764347156,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(55,1,0,1764348101,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(56,2,0,1764348235,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(57,1,0,1764348802,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(58,1,0,1764349874,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(59,1,0,1764350379,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(60,1,0,1764350740,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(61,1,0,1764351861,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(62,1,0,1764352115,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(63,1,0,1764352499,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(64,1,0,1764352918,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(65,1,0,1764353204,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(66,1,0,1764353262,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(67,1,0,1764353703,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(68,1,0,1764354127,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(69,1,0,1764354884,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(70,1,0,1764355175,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(71,1,0,1764355707,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(72,1,0,1764355816,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(73,1,0,1764355925,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(74,1,0,1764356025,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(75,1,0,1764356723,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(76,1,0,1764357434,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(77,1,0,1764357871,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(78,2,0,1764358153,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(79,1,0,1764359472,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(80,1,0,1764360353,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(81,2,0,1764360463,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(82,1,0,1764360712,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(83,2,0,1764360735,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(84,1,0,1764361132,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(85,2,0,1764361157,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(86,1,0,1764361459,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(87,1,0,1764361551,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(88,2,0,1764361601,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(89,1,0,1764361930,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(90,1,0,1764362043,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(91,1,0,1764362160,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(92,1,0,1764415920,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(93,1,0,1764418491,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(94,1,0,1764418794,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(95,1,0,1764421902,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(96,1,0,1764422361,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(97,1,0,1764423818,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(98,1,0,1764424223,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(99,1,0,1764424350,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(100,1,0,1764426554,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(101,1,0,1764428341,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(102,1,0,1764428458,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(103,1,0,1764429035,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(104,1,0,1764429974,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(105,1,0,1764430115,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(106,1,0,1764432151,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(107,1,0,1764433754,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(108,1,0,1764434041,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(109,1,0,1764434492,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(110,1,0,1764434850,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(111,1,0,1764435777,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(112,1,0,1764438461,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(113,1,0,1764440793,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(114,1,0,1764442467,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(115,1,0,1764443191,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(116,1,0,1764443829,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(117,1,0,1764444343,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(118,1,0,1764444950,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(119,1,0,1764446350,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(120,1,0,1764446943,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(121,1,0,1764447532,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(122,1,0,1764448223,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(123,1,0,1764449270,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(124,1,0,1764450068,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(125,1,0,1764451206,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(126,1,0,1764452113,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(127,1,0,1764453140,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(128,1,0,1764521239,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(129,1,0,1764522012,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(130,1,0,1764522904,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(131,1,0,1764524219,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(132,1,0,1764524561,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(133,1,0,1764524910,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(134,1,0,1764525274,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(135,1,0,1764525782,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(136,1,0,1764527521,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(137,1,0,1764527874,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(138,1,0,1764528021,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(139,1,0,1764528150,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(140,1,0,1764528780,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(141,1,0,1764529325,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(142,1,0,1764530248,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(143,1,0,1764609822,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(144,1,0,1764610443,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(145,1,0,1764611202,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(146,1,0,1764611623,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(147,1,0,1764613058,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(148,1,0,1764614111,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(149,1,0,1764617158,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(150,1,0,1764617519,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(151,1,0,1764618928,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(152,1,0,1764632377,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(153,1,0,1764636020,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(154,1,0,1764638832,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(155,1,0,1764639259,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(156,1,0,1764639520,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(157,1,0,1764639852,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(158,1,0,1764640306,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(159,1,0,1764665330,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(160,1,0,1764666737,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(161,1,0,1764695566,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(162,1,0,1764695810,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(163,1,0,1764695903,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(164,1,0,1764697527,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(165,1,0,1764697632,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(166,1,0,1764698665,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(167,1,0,1764784228,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(168,1,0,1764784756,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(169,1,0,1764785325,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(170,1,0,1764786366,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(171,1,0,1764786609,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(172,1,0,1764787061,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(173,1,0,1764788936,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(174,1,0,1764867677,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(175,1,0,1764867971,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(176,1,0,1764868328,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(177,1,0,1764868719,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(178,1,0,1764869143,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(179,1,0,1764869520,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(180,1,0,1764869709,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(181,1,0,1764873765,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(182,1,0,1764874115,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(183,1,0,1764874260,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(184,1,0,1764874733,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(185,1,0,1764953916,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(186,1,0,1764956645,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(187,1,0,1764957109,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(188,1,0,1764957437,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(189,1,0,1764957614,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(190,1,0,1764958290,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(191,1,0,1764958410,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(192,1,0,1764958542,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(193,1,0,1764958852,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(194,1,0,1764959054,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(195,1,0,1764959133,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(196,1,0,1764959266,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(197,1,0,1764959701,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(198,1,0,1764960258,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(199,1,0,1764960589,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(200,1,0,1764961653,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(201,1,0,1764962498,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(202,1,0,1764962847,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(203,1,0,1764963186,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(204,1,0,1764963671,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(205,1,0,1764964659,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(206,1,0,1764965835,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(207,1,0,1764967355,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(208,1,0,1764967452,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(209,1,0,1764967939,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(210,1,0,1764969215,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(211,1,0,1764970318,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(212,1,0,1764970769,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(213,1,0,1765020372,0,0,30,0,0,'Keep','[N] Kaelgard Keep',7,0,25,0,0),(214,1,0,1765123367,0,0,30,0,0,'Keep','[N] Kaelgard Keep',7,0,25,0,0),(215,1,0,1765123698,0,0,30,0,0,'Keep','[N] Kaelgard Keep',7,0,25,0,0),(216,1,0,1765124385,0,0,30,0,0,'Keep','[N] Kaelgard Keep',7,0,25,0,0),(217,1,0,1765124755,0,0,30,0,0,'Keep','[N] Kaelgard Keep',9,0,25,0,0),(218,1,0,1765127560,0,0,30,0,0,'Keep','[N] Kaelgard Keep',9,0,25,0,0),(219,1,0,1765128341,0,0,30,0,0,'Keep','[N] Kaelgard Keep',9,0,25,0,0),(220,1,0,1765128998,0,0,30,0,0,'Keep','[N] Kaelgard Keep',9,0,25,0,0),(221,1,0,1765129334,0,0,30,0,0,'Keep','[N] Kaelgard Keep',9,0,25,0,0),(222,1,0,1765132108,0,0,30,0,0,'Keep','[N] Kaelgard Keep',9,0,25,0,0),(223,1,0,1765212761,0,0,30,0,0,'Keep','[N] Kaelgard Keep',9,0,25,0,0),(224,1,0,1765222759,0,0,30,0,0,'Keep','[N] Kaelgard Keep',9,0,25,0,0),(225,1,0,1765278451,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(226,1,0,1765279410,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(227,1,0,1765279792,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(228,1,0,1765281147,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(229,1,0,1765386587,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(230,1,0,1765386996,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(231,1,0,1765387692,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(232,1,0,1765388547,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(233,1,0,1765389632,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(234,1,0,1765392971,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(235,1,0,1765395485,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(236,1,0,1765395872,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(237,1,0,1765475377,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,25,0,0),(238,1,0,1765475625,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,25,0,0),(239,1,0,1765477579,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,25,0,0),(240,1,0,1765481564,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,25,0,0),(241,1,0,1765482282,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,25,0,0),(242,1,0,1765484675,0,0,30,0,0,'Keep','[N] Kaelgard Keep',12,0,25,0,0),(243,1,0,1765485225,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,25,0,0),(244,1,0,1765486071,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(245,1,0,1765486366,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(246,1,0,1765579479,0,0,30,0,0,'Keep','[N] Kaelgard Keep',12,0,25,0,0),(247,1,0,1765581274,0,0,30,0,0,'Keep','[N] Kaelgard Keep',12,0,25,0,0),(248,1,0,1765637943,0,0,30,0,0,'Keep','[N] Kaelgard Keep',12,0,25,0,0),(249,1,0,1765638631,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(250,1,0,1765639161,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(251,1,0,1765642817,0,0,30,0,0,'Keep','[N] Kaelgard Keep',12,0,25,0,0),(252,1,0,1765646100,0,0,30,0,0,'Keep','[N] Kaelgard Keep',12,0,25,0,0),(253,1,0,1765646559,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(254,1,0,1765668063,0,0,30,0,0,'Keep','[N] Kaelgard Keep',12,0,25,0,0),(255,1,0,1765668803,0,0,30,0,0,'Keep','[N] Kaelgard Keep',12,0,25,0,0),(256,1,0,1765670974,0,0,30,0,0,'Keep','[N] Kaelgard Keep',12,0,25,0,0),(257,1,0,1765671499,0,0,30,0,0,'Keep','[N] Kaelgard Keep',12,0,25,0,0),(258,1,0,1765672610,0,0,30,0,0,'Keep','[N] Kaelgard Keep',12,0,25,0,0),(259,1,0,1765672623,0,0,30,0,0,'Keep','[N] Kaelgard Keep',12,0,25,0,0),(260,1,0,1765678002,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(261,1,0,1765678016,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(262,1,0,1765678479,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(263,1,0,1765678491,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(264,1,0,1765679546,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(265,1,0,1765680724,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(266,1,0,1765680741,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(267,1,0,1765681484,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(268,1,0,1765681497,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(269,1,0,1765682052,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(270,1,0,1765682565,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',2,0,25,1,256),(271,1,0,1765682600,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',2,0,25,1,256),(272,1,0,1765683144,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',2,0,25,1,256),(273,1,0,1765683161,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(274,1,0,1765683884,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(275,1,0,1765683944,0,0,30,0,0,'Keep','[N] Kaelgard Keep',12,0,25,0,0),(276,1,0,1765684331,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(277,1,0,1765684345,0,0,30,0,0,'Keep','[N] Kaelgard Keep',12,0,25,0,0),(278,1,0,1765684773,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',2,0,25,1,256),(279,1,0,1765715874,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(280,1,0,1765716365,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',2,0,25,1,256),(281,1,0,1765716956,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(282,1,0,1765717454,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',2,0,25,1,256),(283,1,0,1765717908,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',2,0,25,1,256),(284,1,0,1765721666,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(285,1,0,1765722139,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(286,1,0,1765726510,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(287,1,0,1765751290,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(288,1,0,1765751737,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',2,0,25,1,256),(289,1,0,1765752793,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',2,0,25,1,256),(290,1,0,1765753568,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',2,0,25,1,256),(291,1,0,1765754139,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',2,0,25,1,256),(292,1,0,1765754990,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',2,0,25,1,256),(293,1,0,1765755442,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',2,0,25,1,256),(294,1,0,1765813114,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(295,1,0,1765813766,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(296,1,0,1765814323,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(297,1,0,1765815101,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(298,1,0,1765816192,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(299,1,0,1765818611,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(300,1,0,1765819783,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(301,1,0,1765901288,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(302,1,0,1765901744,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(303,1,0,1765902304,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(304,1,0,1765906306,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(305,1,0,1765907635,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(306,1,0,1765909346,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(307,1,0,1765909979,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(308,1,0,1765910503,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(309,1,0,1765931342,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(310,1,0,1765987163,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(311,1,0,1765989312,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(312,1,0,1765990363,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(313,1,0,1765990969,0,0,30,0,0,'Keep','[N] Kaelgard Keep',14,0,25,0,0),(314,1,0,1765992626,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(315,1,0,1765995821,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(316,1,0,1765996289,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(317,1,0,1765998287,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(318,1,0,1765998875,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(319,1,0,1765999085,0,0,30,0,0,'Keep','[N] Kaelgard Keep',14,0,25,0,0),(320,1,0,1765999531,0,0,30,0,0,'Keep','[N] Kaelgard Keep',14,0,25,0,0),(321,1,0,1766009529,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(322,1,0,1766013826,0,0,30,0,0,'Keep','[N] Kaelgard Keep',12,0,25,0,0),(323,1,0,1766015567,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(324,1,0,1766016911,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(325,1,0,1766017900,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(326,1,0,1766098907,0,0,30,0,0,'Keep','[N] Kaelgard Keep',14,0,25,0,0),(327,1,0,1766178795,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(328,1,0,1766179760,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(329,1,0,1766181091,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(330,1,0,1766183164,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(331,1,0,1766184667,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(332,1,0,1766187094,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(333,1,0,1766187716,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(334,1,0,1766188084,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(335,1,0,1766189608,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(336,1,0,1766190192,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(337,1,0,1766190770,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(338,1,0,1766191482,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(339,1,0,1766195086,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(340,1,0,1766196057,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(341,1,0,1766196792,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(342,1,0,1766198188,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(343,1,0,1766199281,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(344,1,0,1766199946,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(345,1,0,1766200993,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(346,1,0,1766201771,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(347,1,0,1766202710,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(348,1,0,1766226281,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(349,1,0,1766229273,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(350,1,0,1766229687,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(351,1,0,1766230576,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(352,1,0,1766232693,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(353,1,0,1766233825,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(354,1,0,1766234159,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(355,1,0,1766234414,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(356,1,0,1766236389,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(357,1,0,1766236883,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(358,1,0,1766239752,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(359,1,0,1766240705,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(360,1,0,1766263719,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(361,1,0,1766264135,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(362,1,0,1766264514,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(363,1,0,1766266866,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(364,1,0,1766267089,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(365,1,0,1766267275,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(366,1,0,1766269277,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(367,1,0,1766271938,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(368,1,0,1766272950,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(369,1,0,1766275106,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(370,1,0,1766275746,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(371,1,0,1766276194,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(372,1,0,1766351130,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(373,1,0,1766352287,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(374,1,0,1766353098,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(375,1,0,1766353736,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(376,1,0,1766354534,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(377,1,0,1766355966,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(378,1,0,1766356410,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(379,1,0,1766356886,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(380,1,0,1766357341,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(381,1,0,1766361899,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(382,1,0,1766410645,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(383,1,0,1766412222,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(384,1,0,1766413388,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(385,1,0,1766413979,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(386,1,0,1766414852,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(387,1,0,1766415513,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(388,1,0,1766416077,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(389,1,0,1766416372,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(390,1,0,1766417040,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(391,1,0,1766418533,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(392,1,0,1766420141,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(393,1,0,1766421628,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(394,1,0,1766422217,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(395,1,0,1766454487,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',11,0,25,1,256),(396,1,0,1766474878,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(397,1,0,1766503851,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(398,1,0,1766505445,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(399,1,0,1766508006,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',17,0,25,1,256),(400,1,0,1766509118,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(401,1,0,1766510375,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',17,0,25,1,256),(402,1,0,1766513406,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',17,0,25,1,256),(403,1,0,1766514793,0,0,30,0,0,'Keep','[N] Kaelgard Keep',17,0,25,0,0),(404,1,0,1766516127,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(405,1,0,1766530308,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(406,1,0,1766531460,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(407,1,0,1766561666,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(408,1,0,1766563073,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(409,1,0,1766564219,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(410,1,0,1766564462,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(411,1,0,1766565871,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(412,1,0,1766566729,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(413,1,0,1766567700,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(414,1,0,1766569289,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(415,1,0,1766570582,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(416,1,0,1766571011,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(417,1,0,1766572413,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(418,1,0,1766572997,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(419,1,0,1766573482,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(420,1,0,1766574002,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(421,1,0,1766574942,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(422,1,0,1766575200,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(423,1,0,1766575522,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(424,1,0,1766658016,0,0,30,0,0,'Keep','[N] Kaelgard Keep',14,0,25,0,0),(425,1,0,1766710616,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(426,1,0,1766751668,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(427,1,0,1766796208,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(428,1,0,1766796434,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(429,1,0,1766796821,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(430,1,0,1766798153,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(431,1,0,1766798999,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(432,1,0,1766801277,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(433,1,0,1766802137,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(434,1,0,1766839585,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(435,1,0,1766840381,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(436,1,0,1766840946,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(437,1,0,1766841653,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(438,1,0,1766844663,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(439,1,0,1766846109,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(440,1,0,1766848651,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(441,1,0,1766850173,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(442,1,0,1766850488,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(443,1,0,1766851244,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(444,1,0,1766912106,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(445,1,0,1766912452,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(446,1,0,1766932840,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(447,1,0,1766937942,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(448,1,0,1766939864,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(449,1,0,1766941207,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(450,1,0,1766966329,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(451,1,0,1766968937,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(452,1,0,1767027029,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(453,1,0,1767028620,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(454,1,0,1767029111,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(455,1,0,1767029953,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(456,1,0,1767033139,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(457,1,0,1767033861,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(458,1,0,1767065096,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(459,1,0,1767112110,0,0,30,0,0,'Glacier','[N] Xaxak Glacier',14,0,25,0,0),(460,1,0,1767114241,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(461,1,0,1767115320,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(462,1,0,1767115568,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(463,1,0,1767117131,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(464,1,0,1767119057,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',17,0,25,1,256),(465,1,0,1767119960,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',17,0,25,1,256),(466,1,0,1767135332,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(467,1,0,1767136550,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(468,1,0,1767138095,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(469,1,0,1767138846,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(470,1,0,1767139302,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(471,1,0,1767139770,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(472,1,0,1767140396,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(473,1,0,1767141477,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(474,1,0,1767141880,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(475,1,0,1767142497,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(476,1,0,1767142991,0,0,30,0,0,'Keep','[N] Kaelgard Keep',14,0,25,0,0),(477,1,0,1767143777,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(478,1,0,1767144160,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(479,1,0,1767144560,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(480,1,0,1767147162,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(481,1,0,1767203676,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(482,1,0,1767206066,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(483,1,0,1767207127,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',17,0,25,1,256),(484,1,0,1767207509,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',17,0,25,1,256),(485,1,0,1767278003,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(486,1,0,1767278300,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(487,1,0,1767278516,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(488,1,0,1767278783,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(489,1,0,1767391523,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(490,1,0,1767394706,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(491,1,0,1767395124,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(492,1,0,1767395503,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(493,1,0,1767396338,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(494,1,0,1767396871,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(495,1,0,1767399501,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(496,1,0,1767400351,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(497,1,0,1767401196,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(498,1,0,1767402627,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(499,1,0,1767403668,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(500,1,0,1767404254,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(501,1,0,1767404417,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(502,1,0,1767405253,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(503,1,0,1767410246,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(504,1,0,1767411190,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(505,1,0,1767413615,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(506,1,0,1767414493,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(507,1,0,1767414613,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(508,1,0,1767415322,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(509,1,0,1767415803,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(510,1,0,1767416324,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(511,1,0,1767461981,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(512,1,0,1767462369,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(513,1,0,1767463912,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(514,1,0,1767464344,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(515,1,0,1767464921,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(516,1,0,1767465281,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(517,1,0,1767480608,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(518,1,0,1767481170,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(519,1,0,1767481526,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(520,1,0,1767482300,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(521,1,0,1767492693,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(522,1,0,1767493529,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(523,1,0,1767494486,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(524,1,0,1767495039,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(525,1,0,1767495824,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(526,1,0,1767535347,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(527,1,0,1767536119,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(528,1,0,1767537879,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(529,1,0,1767538749,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(530,1,0,1767539470,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(531,1,0,1767540319,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(532,1,0,1767541066,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(533,1,0,1767543327,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(534,1,0,1767549574,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(535,1,0,1767550353,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(536,1,0,1767550990,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(537,1,0,1767551410,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(538,1,0,1767551757,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(539,1,0,1767552254,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(540,1,0,1767554132,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(541,1,0,1767554585,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(542,1,0,1767554752,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(543,1,0,1767555899,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(544,1,0,1767556444,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(545,1,0,1767557269,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(546,1,0,1767558157,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(547,1,0,1767559209,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(548,1,0,1767560545,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(549,1,0,1767561201,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(550,1,0,1767635846,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(551,1,0,1767636351,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(552,1,0,1767645387,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(553,1,0,1767645887,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(554,1,0,1767646316,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(555,1,0,1767648550,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(556,1,0,1767649145,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(557,1,0,1767650633,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(558,1,0,1767655251,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(559,1,0,1767655595,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(560,1,0,1767656552,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(561,1,0,1767656908,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(562,1,0,1767657799,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(563,1,0,1767658092,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(564,1,0,1767658862,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(565,1,0,1767659408,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(566,1,0,1767661645,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(567,1,0,1767661787,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(568,1,0,1767662319,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(569,1,0,1767662813,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(570,1,0,1767664310,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(571,1,0,1767664505,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(572,1,0,1767664745,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(573,1,0,1767665951,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(574,1,0,1767666657,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(575,1,0,1767666969,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(576,1,0,1767667865,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(577,1,0,1767690395,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(578,1,0,1767715367,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(579,1,0,1767716466,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(580,1,0,1767716895,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(581,1,0,1767740116,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(582,1,0,1767741482,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(583,1,0,1767747559,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(584,1,0,1767747785,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(585,1,0,1767751537,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(586,1,0,1767826126,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(587,1,0,1767826334,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(588,1,0,1767826887,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(589,1,0,1767827711,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(590,1,0,1767829464,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(591,1,0,1767830847,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(592,1,0,1767831211,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(593,1,0,1767831945,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(594,1,0,1767834219,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(595,1,0,1767835301,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(596,1,0,1767835828,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(597,1,0,1767836145,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(598,1,0,1767836922,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(599,1,0,1767837971,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(600,1,0,1767838405,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(601,1,0,1767839711,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(602,2,0,1767840100,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,60,25,1,256),(603,1,0,1767892373,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(604,1,0,1767892727,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(605,1,0,1767893543,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(606,1,0,1767894151,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(607,1,0,1767897464,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(608,1,0,1767898326,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(609,1,0,1767898545,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(610,1,0,1767899574,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(611,1,0,1767902131,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(612,1,0,1767909847,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(613,1,0,1767911418,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(614,1,0,1767912801,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(615,1,0,1767921560,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(616,1,0,1767922173,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(617,1,0,1767922531,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(618,1,0,1767923807,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(619,1,0,1767924614,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(620,1,0,1767925484,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(621,1,0,1767925978,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(622,1,0,1768100330,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',11,60,25,1,256),(623,1,0,1768179234,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(624,1,0,1768181530,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(625,1,0,1768182676,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(626,1,0,1768240915,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(627,1,0,1768241133,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',17,60,25,1,256),(628,1,0,1768241421,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(629,1,0,1768254250,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(630,1,0,1768255605,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(631,1,0,1768257125,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(632,1,0,1768257502,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(633,1,0,1768258414,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(634,1,0,1768259247,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(635,1,0,1768259999,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(636,1,0,1768260794,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(637,1,0,1768261947,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(638,1,0,1768262570,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(639,1,0,1768264061,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(640,1,0,1768264920,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(641,1,0,1768266049,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(642,1,0,1768266690,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(643,1,0,1768267195,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(644,1,0,1768267433,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(645,1,0,1768267729,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(646,1,0,1768268410,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(647,1,0,1768269420,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(648,1,0,1768270143,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(649,1,0,1768270525,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(650,1,0,1768271545,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(651,1,0,1768271974,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(652,1,0,1768272475,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(653,1,0,1768272734,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(654,1,0,1768325691,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(655,1,0,1768326804,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(656,1,0,1768327688,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(657,1,0,1768337672,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(658,1,0,1768338225,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(659,1,0,1768339498,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(660,1,0,1768339985,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(661,1,0,1768340996,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(662,1,0,1768343028,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(663,1,0,1768343427,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(664,1,0,1768344451,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(665,1,0,1768345143,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(666,1,0,1768346566,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(667,1,0,1768347337,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(668,1,0,1768348825,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(669,1,0,1768349416,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(670,1,0,1768349652,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(671,1,0,1768349780,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(672,1,0,1768349927,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(673,1,0,1768350077,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(674,1,0,1768350984,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(675,1,0,1768351235,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(676,1,0,1768351462,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(677,1,0,1768351594,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(678,1,0,1768352171,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(679,1,0,1768413918,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(680,1,0,1768414314,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(681,1,0,1768414922,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(682,2,0,1768415058,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,60,25,1,256),(683,1,0,1768415591,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(684,1,0,1768420583,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(685,1,0,1768421184,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(686,1,0,1768422664,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(687,1,0,1768423262,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,60,25,1,256),(688,2,0,1768423278,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(689,1,0,1768423837,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(690,1,0,1768424428,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(691,1,0,1768424913,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(692,1,0,1768427285,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(693,1,0,1768427995,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(694,1,0,1768428582,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(695,1,0,1768429351,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(696,1,0,1768430042,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(697,1,0,1768430671,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(698,1,0,1768431085,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(699,1,0,1768431653,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(700,1,0,1768432405,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(701,1,0,1768433012,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(702,1,0,1768433888,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(703,1,0,1768435323,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(704,1,0,1768435612,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(705,1,0,1768436213,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(706,1,0,1768437628,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(707,1,0,1768437849,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(708,1,0,1768438528,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(709,1,0,1768444878,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(710,1,0,1768539148,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(711,1,0,1768613948,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(712,1,0,1768624649,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',10,60,25,1,256),(713,1,0,1768700181,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',13,60,15,1,256),(714,1,0,1768700264,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(715,1,0,1768701037,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,60,25,1,256),(716,1,0,1768701779,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,60,25,1,256),(717,1,0,1768702322,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,60,25,1,256),(718,1,0,1768702901,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,60,25,1,256),(719,1,0,1768703780,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,60,25,1,256),(720,1,0,1768710696,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',19,60,25,1,256),(721,1,0,1768711270,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',19,60,25,1,256),(722,1,0,1768740196,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(723,1,0,1768743317,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(724,2,0,1768744439,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',20,60,1,1,256),(725,1,0,1768744564,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',19,60,10,1,256),(726,1,0,1768744715,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',19,60,2,1,256),(727,1,0,1768750829,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',19,60,2,1,256),(728,1,0,1768751667,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',19,60,2,1,256),(729,1,0,1768752681,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',19,60,2,1,256),(730,1,0,1768753765,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,60,3,1,256),(731,1,0,1768755204,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,60,3,1,256),(732,1,0,1768755615,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',19,60,2,1,256),(733,1,0,1768760761,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(734,1,0,1768763725,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(735,1,0,1768763993,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(736,1,0,1768765872,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(737,1,0,1768768850,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,60,25,1,256),(738,1,0,1768771493,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,60,25,1,256),(739,1,0,1768773000,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,60,25,1,256),(740,1,0,1768774208,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,60,25,1,256),(741,1,0,1768777253,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',11,60,25,1,256),(742,1,0,1768787512,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',11,60,25,1,256),(743,1,0,1768789384,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',11,60,25,1,256),(744,1,0,1768790057,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',11,60,25,1,256),(745,1,0,1768790694,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',11,60,25,1,256),(746,1,0,1768791875,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',11,60,25,1,256),(747,1,0,1768837679,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',11,60,25,1,256),(748,1,0,1768843198,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',11,60,25,1,256),(749,1,0,1768857218,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',11,60,25,1,256),(750,1,0,1768857650,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',11,60,25,1,256),(751,1,0,1768859147,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',2,60,25,1,256),(752,1,0,1768859560,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',2,60,25,1,256),(753,1,0,1768860918,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',11,60,25,1,256),(754,1,0,1768862405,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',11,60,25,1,256),(755,1,0,1768862961,0,0,30,0,0,'Keep','[N] Kaelgard Keep',11,60,25,0,0),(756,2,0,1768863590,0,0,30,0,0,'Keep','[N] Kaelgard Keep',21,60,1,0,0),(757,1,0,1768864198,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',21,60,20,1,256),(758,1,0,1768865026,0,0,30,0,0,'Keep','[N] Kaelgard Keep',14,60,25,0,0),(759,1,0,1768865577,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,60,25,1,256),(760,1,0,1768866590,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',6,60,25,1,256),(761,2,0,1768866641,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',2,60,25,1,256),(762,1,0,1768867084,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',11,60,25,1,256),(763,1,0,1769109722,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',2,60,25,1,256);
/*!40000 ALTER TABLE `matches` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `online_accounts`
--

DROP TABLE IF EXISTS `online_accounts`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `online_accounts` (
  `accountid` int NOT NULL AUTO_INCREMENT,
  `username` varchar(32) NOT NULL,
  PRIMARY KEY (`accountid`),
  UNIQUE KEY `username` (`username`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb3;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `online_accounts`
--

LOCK TABLES `online_accounts` WRITE;
/*!40000 ALTER TABLE `online_accounts` DISABLE KEYS */;
/*!40000 ALTER TABLE `online_accounts` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `online_characters`
--

DROP TABLE IF EXISTS `online_characters`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `online_characters` (
  `charid` int NOT NULL,
  `arenaid` tinyint unsigned NOT NULL,
  `tableid` tinyint unsigned NOT NULL,
  `arenashortname` varchar(32) NOT NULL,
  PRIMARY KEY (`charid`),
  KEY `idx_charid` (`charid`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `online_characters`
--

LOCK TABLES `online_characters` WRITE;
/*!40000 ALTER TABLE `online_characters` DISABLE KEYS */;
/*!40000 ALTER TABLE `online_characters` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `server_settings`
--

DROP TABLE IF EXISTS `server_settings`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `server_settings` (
  `id` int NOT NULL AUTO_INCREMENT,
  `exp_multiplier` float NOT NULL DEFAULT '1',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `server_settings`
--

LOCK TABLES `server_settings` WRITE;
/*!40000 ALTER TABLE `server_settings` DISABLE KEYS */;
INSERT INTO `server_settings` VALUES (1,1);
/*!40000 ALTER TABLE `server_settings` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-01-23 20:10:51
