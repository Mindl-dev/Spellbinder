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
INSERT INTO `character_statistics` VALUES (2,1,3,5,0,959,549,0,0,0,0),(5,1,15,3,0,2640,816,0,0,0,0),(6,0,0,0,0,0,0,0,0,0,0),(9,0,0,0,0,0,0,0,0,0,0),(10,0,0,0,0,0,0,0,0,0,0),(11,1,0,0,0,0,90,0,0,0,0),(12,1,0,9,0,0,1626,0,0,0,0),(13,1,0,0,0,0,0,0,0,0,0),(14,1,0,1,0,0,518,0,0,0,0);
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
INSERT INTO `character_statistics_weekly` VALUES (2,1763942400,0,0,0,0,0,0,0,0,0,0),(2,1764547200,0,0,0,0,0,0,0,0,0,0),(2,1765152000,1,2,2,0,0,234,0,0,0,0),(2,1765756800,1,1,2,0,0,156,0,0,0,0),(2,1766361600,1,0,1,0,0,159,0,0,0,0),(5,1763942400,0,0,0,0,0,0,0,0,0,0),(5,1764547200,0,0,0,0,0,0,0,0,0,0),(5,1765152000,1,2,2,0,0,488,0,0,0,0),(5,1765756800,1,3,1,0,0,328,0,0,0,0),(5,1766361600,1,10,0,0,0,0,0,0,0,0),(6,1763942400,0,0,0,0,0,0,0,0,0,0),(6,1764547200,0,0,0,0,0,0,0,0,0,0),(9,1764547200,0,0,0,0,0,0,0,0,0,0),(9,1765152000,0,0,0,0,0,0,0,0,0,0),(10,1764547200,0,0,0,0,0,0,0,0,0,0),(11,1765152000,1,0,0,0,0,0,0,0,0,0),(11,1766361600,1,0,0,0,0,90,0,0,0,0),(12,1765152000,1,0,0,0,0,0,0,0,0,0),(12,1765756800,1,0,1,0,0,156,0,0,0,0),(12,1766361600,1,0,8,0,0,1470,0,0,0,0),(13,1765152000,1,0,0,0,0,0,0,0,0,0),(13,1765756800,1,0,0,0,0,0,0,0,0,0),(14,1765756800,1,0,0,0,0,143,0,0,0,0),(14,1766361600,1,0,1,0,0,375,0,0,0,0);
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
  `list_level_1` tinyint unsigned NOT NULL DEFAULT '0',
  `list_level_2` tinyint unsigned NOT NULL DEFAULT '0',
  `list_level_3` tinyint unsigned NOT NULL DEFAULT '0',
  `list_level_4` tinyint unsigned NOT NULL DEFAULT '0',
  `list_level_5` tinyint unsigned NOT NULL DEFAULT '0',
  `list_level_6` tinyint unsigned NOT NULL DEFAULT '0',
  `list_level_7` tinyint unsigned NOT NULL DEFAULT '0',
  `list_level_8` tinyint unsigned NOT NULL DEFAULT '0',
  `list_level_9` tinyint unsigned NOT NULL DEFAULT '0',
  `list_level_10` tinyint unsigned NOT NULL DEFAULT '0',
  `class` tinyint unsigned NOT NULL DEFAULT '0',
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
  `created` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`charid`),
  UNIQUE KEY `name` (`name`),
  UNIQUE KEY `unique_slot` (`accountid`,`slot`),
  CONSTRAINT `characters_ibfk_1` FOREIGN KEY (`accountid`) REFERENCES `accounts` (`AccountID`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=15 DEFAULT CHARSET=utf8mb3;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `characters`
--

LOCK TABLES `characters` WRITE;
/*!40000 ALTER TABLE `characters` DISABLE KEYS */;
INSERT INTO `characters` VALUES (2,1,0,'Mindl',100,100,100,100,100,100,100,100,100,100,0,0,0,1,2,4,18,255,255,255,255,255,255,25,25,25,25,0,0,0,0,0,0,0,25,24,201,0,2330000,0,124,294,75,0,0,295,296,125,29,71,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,'2025-11-24 05:08:18'),(5,2,0,'Farley',80,80,80,80,80,80,80,80,80,80,0,0,0,15,3,16,17,255,255,255,255,255,255,25,25,25,25,0,0,0,0,0,0,1,25,24,203,0,2330000,0,265,92,0,0,0,156,0,0,0,0,8,286,290,0,0,94,0,0,0,0,230,0,0,0,0,282,0,0,0,0,275,0,0,0,0,261,0,0,0,0,'2025-11-25 22:05:54'),(6,1,1,'Khisanith',80,80,80,80,80,80,80,80,80,80,0,0,0,6,7,8,9,10,255,255,255,255,255,25,25,25,25,25,0,0,0,0,0,3,25,0,202,0,2330000,0,152,76,114,151,109,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,'2025-11-26 21:44:38'),(9,1,3,'George',80,80,80,80,80,80,80,80,80,80,0,0,0,15,3,16,17,0,0,0,0,0,0,25,25,25,25,0,0,0,0,0,0,1,25,24,204,0,2330000,0,265,92,0,0,0,156,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,'2025-12-07 21:19:25'),(10,1,2,'Jeff',80,80,80,80,80,80,80,80,80,80,0,0,0,11,12,13,14,0,0,0,0,0,0,1,1,1,1,0,0,0,0,0,0,2,1,0,204,0,0,0,166,136,217,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,'2025-12-07 21:24:59'),(11,3,0,'Test1',100,100,100,100,100,100,100,100,100,100,0,0,0,1,2,4,18,255,255,255,255,255,255,25,25,25,25,0,0,0,0,0,0,0,25,24,201,0,2300248,0,124,294,75,0,0,295,296,125,29,71,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,'2025-11-24 05:08:18'),(12,4,0,'Test2',100,100,100,100,100,100,100,100,100,100,0,0,0,1,2,4,18,255,255,255,255,255,255,25,25,25,25,0,0,0,0,0,0,0,25,24,201,0,2302569,0,1,0,3,0,2,26,0,28,0,27,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,'2025-11-24 05:08:18'),(13,4,3,'Testt',80,80,80,80,80,80,80,80,80,80,0,0,0,15,3,16,17,0,0,0,0,0,0,1,1,1,1,0,0,0,0,0,0,1,1,0,204,0,0,0,260,267,266,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,'2025-12-14 20:01:21'),(14,3,1,'Testt1',80,80,80,80,80,80,80,80,80,80,0,0,0,6,7,8,9,10,255,255,255,255,255,25,25,25,25,25,25,0,0,0,0,3,25,24,202,0,2330000,0,155,154,69,66,67,178,177,210,159,183,201,202,204,203,200,224,168,176,174,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,'2025-11-26 21:44:38');
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
) ENGINE=InnoDB AUTO_INCREMENT=395 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `matches`
--

LOCK TABLES `matches` WRITE;
/*!40000 ALTER TABLE `matches` DISABLE KEYS */;
INSERT INTO `matches` VALUES (1,1,0,1764114420,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(2,1,0,1764114534,0,0,30,0,0,'Temple','[N] Rathespa Temple',5,0,1,0,0),(3,1,0,1764114899,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(4,1,0,1764115477,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(5,1,0,1764116095,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(6,1,0,1764116322,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(7,1,0,1764116841,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(8,1,0,1764117698,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(9,1,0,1764117795,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(10,1,0,1764121080,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(11,1,0,1764121722,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(12,1,0,1764122598,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(13,1,0,1764147402,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(14,1,0,1764148306,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(15,1,0,1764170177,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(16,1,0,1764171736,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(17,1,0,1764172515,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(18,1,0,1764172899,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(19,1,0,1764174729,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(20,2,0,1764175023,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(21,1,0,1764175153,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(22,1,0,1764175435,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(23,1,0,1764175490,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(24,1,0,1764178610,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(25,1,0,1764178950,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(26,2,0,1764179078,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(27,1,0,1764331501,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(28,1,0,1764331802,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(29,1,0,1764331959,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(30,1,0,1764332089,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(31,1,0,1764332321,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(32,1,0,1764333011,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(33,1,0,1764333736,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(34,1,0,1764333923,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(35,1,0,1764334135,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(36,1,0,1764334698,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(37,1,0,1764335742,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(38,1,0,1764335893,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(39,1,0,1764336840,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(40,1,0,1764337139,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(41,1,0,1764337907,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(42,1,0,1764338095,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(43,1,0,1764338459,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(44,1,0,1764338526,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(45,1,0,1764339199,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(46,1,0,1764340706,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(47,1,0,1764340749,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(48,1,0,1764343622,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(49,1,0,1764345890,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(50,1,0,1764345960,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(51,1,0,1764346351,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(52,1,0,1764346564,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(53,1,0,1764346865,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(54,1,0,1764347156,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(55,1,0,1764348101,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(56,2,0,1764348235,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(57,1,0,1764348802,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(58,1,0,1764349874,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(59,1,0,1764350379,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(60,1,0,1764350740,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(61,1,0,1764351861,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(62,1,0,1764352115,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(63,1,0,1764352499,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(64,1,0,1764352918,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(65,1,0,1764353204,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(66,1,0,1764353262,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(67,1,0,1764353703,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(68,1,0,1764354127,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(69,1,0,1764354884,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(70,1,0,1764355175,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(71,1,0,1764355707,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(72,1,0,1764355816,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(73,1,0,1764355925,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(74,1,0,1764356025,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(75,1,0,1764356723,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(76,1,0,1764357434,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(77,1,0,1764357871,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(78,2,0,1764358153,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(79,1,0,1764359472,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(80,1,0,1764360353,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(81,2,0,1764360463,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(82,1,0,1764360712,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(83,2,0,1764360735,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(84,1,0,1764361132,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(85,2,0,1764361157,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(86,1,0,1764361459,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(87,1,0,1764361551,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(88,2,0,1764361601,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(89,1,0,1764361930,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(90,1,0,1764362043,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(91,1,0,1764362160,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(92,1,0,1764415920,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(93,1,0,1764418491,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(94,1,0,1764418794,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(95,1,0,1764421902,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(96,1,0,1764422361,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(97,1,0,1764423818,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(98,1,0,1764424223,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(99,1,0,1764424350,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(100,1,0,1764426554,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(101,1,0,1764428341,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(102,1,0,1764428458,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(103,1,0,1764429035,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(104,1,0,1764429974,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(105,1,0,1764430115,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(106,1,0,1764432151,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(107,1,0,1764433754,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(108,1,0,1764434041,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(109,1,0,1764434492,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(110,1,0,1764434850,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(111,1,0,1764435777,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(112,1,0,1764438461,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(113,1,0,1764440793,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(114,1,0,1764442467,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(115,1,0,1764443191,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(116,1,0,1764443829,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(117,1,0,1764444343,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(118,1,0,1764444950,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(119,1,0,1764446350,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(120,1,0,1764446943,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(121,1,0,1764447532,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(122,1,0,1764448223,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(123,1,0,1764449270,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(124,1,0,1764450068,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(125,1,0,1764451206,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(126,1,0,1764452113,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(127,1,0,1764453140,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(128,1,0,1764521239,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(129,1,0,1764522012,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(130,1,0,1764522904,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(131,1,0,1764524219,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(132,1,0,1764524561,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(133,1,0,1764524910,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(134,1,0,1764525274,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(135,1,0,1764525782,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(136,1,0,1764527521,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(137,1,0,1764527874,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(138,1,0,1764528021,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(139,1,0,1764528150,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(140,1,0,1764528780,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(141,1,0,1764529325,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(142,1,0,1764530248,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(143,1,0,1764609822,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(144,1,0,1764610443,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(145,1,0,1764611202,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(146,1,0,1764611623,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(147,1,0,1764613058,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(148,1,0,1764614111,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(149,1,0,1764617158,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(150,1,0,1764617519,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(151,1,0,1764618928,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(152,1,0,1764632377,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(153,1,0,1764636020,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(154,1,0,1764638832,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(155,1,0,1764639259,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(156,1,0,1764639520,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(157,1,0,1764639852,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(158,1,0,1764640306,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(159,1,0,1764665330,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(160,1,0,1764666737,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(161,1,0,1764695566,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(162,1,0,1764695810,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(163,1,0,1764695903,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(164,1,0,1764697527,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(165,1,0,1764697632,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(166,1,0,1764698665,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(167,1,0,1764784228,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(168,1,0,1764784756,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(169,1,0,1764785325,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(170,1,0,1764786366,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(171,1,0,1764786609,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(172,1,0,1764787061,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(173,1,0,1764788936,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(174,1,0,1764867677,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(175,1,0,1764867971,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(176,1,0,1764868328,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(177,1,0,1764868719,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(178,1,0,1764869143,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(179,1,0,1764869520,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(180,1,0,1764869709,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(181,1,0,1764873765,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(182,1,0,1764874115,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(183,1,0,1764874260,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(184,1,0,1764874733,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(185,1,0,1764953916,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(186,1,0,1764956645,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(187,1,0,1764957109,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(188,1,0,1764957437,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(189,1,0,1764957614,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(190,1,0,1764958290,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(191,1,0,1764958410,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(192,1,0,1764958542,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(193,1,0,1764958852,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(194,1,0,1764959054,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(195,1,0,1764959133,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(196,1,0,1764959266,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(197,1,0,1764959701,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(198,1,0,1764960258,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(199,1,0,1764960589,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(200,1,0,1764961653,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(201,1,0,1764962498,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(202,1,0,1764962847,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(203,1,0,1764963186,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(204,1,0,1764963671,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(205,1,0,1764964659,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(206,1,0,1764965835,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(207,1,0,1764967355,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(208,1,0,1764967452,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(209,1,0,1764967939,0,0,30,0,0,'Keep','[N] Kaelgard Keep',6,0,1,0,0),(210,1,0,1764969215,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(211,1,0,1764970318,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(212,1,0,1764970769,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,1,0,0),(213,1,0,1765020372,0,0,30,0,0,'Keep','[N] Kaelgard Keep',7,0,25,0,0),(214,1,0,1765123367,0,0,30,0,0,'Keep','[N] Kaelgard Keep',7,0,25,0,0),(215,1,0,1765123698,0,0,30,0,0,'Keep','[N] Kaelgard Keep',7,0,25,0,0),(216,1,0,1765124385,0,0,30,0,0,'Keep','[N] Kaelgard Keep',7,0,25,0,0),(217,1,0,1765124755,0,0,30,0,0,'Keep','[N] Kaelgard Keep',9,0,25,0,0),(218,1,0,1765127560,0,0,30,0,0,'Keep','[N] Kaelgard Keep',9,0,25,0,0),(219,1,0,1765128341,0,0,30,0,0,'Keep','[N] Kaelgard Keep',9,0,25,0,0),(220,1,0,1765128998,0,0,30,0,0,'Keep','[N] Kaelgard Keep',9,0,25,0,0),(221,1,0,1765129334,0,0,30,0,0,'Keep','[N] Kaelgard Keep',9,0,25,0,0),(222,1,0,1765132108,0,0,30,0,0,'Keep','[N] Kaelgard Keep',9,0,25,0,0),(223,1,0,1765212761,0,0,30,0,0,'Keep','[N] Kaelgard Keep',9,0,25,0,0),(224,1,0,1765222759,0,0,30,0,0,'Keep','[N] Kaelgard Keep',9,0,25,0,0),(225,1,0,1765278451,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(226,1,0,1765279410,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(227,1,0,1765279792,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(228,1,0,1765281147,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(229,1,0,1765386587,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(230,1,0,1765386996,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(231,1,0,1765387692,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(232,1,0,1765388547,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(233,1,0,1765389632,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(234,1,0,1765392971,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(235,1,0,1765395485,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(236,1,0,1765395872,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(237,1,0,1765475377,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,25,0,0),(238,1,0,1765475625,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,25,0,0),(239,1,0,1765477579,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,25,0,0),(240,1,0,1765481564,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,25,0,0),(241,1,0,1765482282,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,25,0,0),(242,1,0,1765484675,0,0,30,0,0,'Keep','[N] Kaelgard Keep',12,0,25,0,0),(243,1,0,1765485225,0,0,30,0,0,'Keep','[N] Kaelgard Keep',5,0,25,0,0),(244,1,0,1765486071,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(245,1,0,1765486366,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(246,1,0,1765579479,0,0,30,0,0,'Keep','[N] Kaelgard Keep',12,0,25,0,0),(247,1,0,1765581274,0,0,30,0,0,'Keep','[N] Kaelgard Keep',12,0,25,0,0),(248,1,0,1765637943,0,0,30,0,0,'Keep','[N] Kaelgard Keep',12,0,25,0,0),(249,1,0,1765638631,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(250,1,0,1765639161,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(251,1,0,1765642817,0,0,30,0,0,'Keep','[N] Kaelgard Keep',12,0,25,0,0),(252,1,0,1765646100,0,0,30,0,0,'Keep','[N] Kaelgard Keep',12,0,25,0,0),(253,1,0,1765646559,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(254,1,0,1765668063,0,0,30,0,0,'Keep','[N] Kaelgard Keep',12,0,25,0,0),(255,1,0,1765668803,0,0,30,0,0,'Keep','[N] Kaelgard Keep',12,0,25,0,0),(256,1,0,1765670974,0,0,30,0,0,'Keep','[N] Kaelgard Keep',12,0,25,0,0),(257,1,0,1765671499,0,0,30,0,0,'Keep','[N] Kaelgard Keep',12,0,25,0,0),(258,1,0,1765672610,0,0,30,0,0,'Keep','[N] Kaelgard Keep',12,0,25,0,0),(259,1,0,1765672623,0,0,30,0,0,'Keep','[N] Kaelgard Keep',12,0,25,0,0),(260,1,0,1765678002,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(261,1,0,1765678016,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(262,1,0,1765678479,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(263,1,0,1765678491,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(264,1,0,1765679546,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(265,1,0,1765680724,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(266,1,0,1765680741,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(267,1,0,1765681484,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(268,1,0,1765681497,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(269,1,0,1765682052,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(270,1,0,1765682565,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',2,0,25,1,256),(271,1,0,1765682600,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',2,0,25,1,256),(272,1,0,1765683144,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',2,0,25,1,256),(273,1,0,1765683161,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(274,1,0,1765683884,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(275,1,0,1765683944,0,0,30,0,0,'Keep','[N] Kaelgard Keep',12,0,25,0,0),(276,1,0,1765684331,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(277,1,0,1765684345,0,0,30,0,0,'Keep','[N] Kaelgard Keep',12,0,25,0,0),(278,1,0,1765684773,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',2,0,25,1,256),(279,1,0,1765715874,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(280,1,0,1765716365,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',2,0,25,1,256),(281,1,0,1765716956,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(282,1,0,1765717454,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',2,0,25,1,256),(283,1,0,1765717908,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',2,0,25,1,256),(284,1,0,1765721666,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(285,1,0,1765722139,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(286,1,0,1765726510,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(287,1,0,1765751290,0,0,30,0,0,'Keep','[N] Kaelgard Keep',2,0,25,0,0),(288,1,0,1765751737,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',2,0,25,1,256),(289,1,0,1765752793,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',2,0,25,1,256),(290,1,0,1765753568,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',2,0,25,1,256),(291,1,0,1765754139,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',2,0,25,1,256),(292,1,0,1765754990,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',2,0,25,1,256),(293,1,0,1765755442,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',2,0,25,1,256),(294,1,0,1765813114,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(295,1,0,1765813766,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(296,1,0,1765814323,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(297,1,0,1765815101,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(298,1,0,1765816192,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(299,1,0,1765818611,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(300,1,0,1765819783,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(301,1,0,1765901288,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(302,1,0,1765901744,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(303,1,0,1765902304,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(304,1,0,1765906306,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(305,1,0,1765907635,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(306,1,0,1765909346,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(307,1,0,1765909979,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(308,1,0,1765910503,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(309,1,0,1765931342,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(310,1,0,1765987163,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(311,1,0,1765989312,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(312,1,0,1765990363,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(313,1,0,1765990969,0,0,30,0,0,'Keep','[N] Kaelgard Keep',14,0,25,0,0),(314,1,0,1765992626,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(315,1,0,1765995821,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(316,1,0,1765996289,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(317,1,0,1765998287,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(318,1,0,1765998875,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(319,1,0,1765999085,0,0,30,0,0,'Keep','[N] Kaelgard Keep',14,0,25,0,0),(320,1,0,1765999531,0,0,30,0,0,'Keep','[N] Kaelgard Keep',14,0,25,0,0),(321,1,0,1766009529,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(322,1,0,1766013826,0,0,30,0,0,'Keep','[N] Kaelgard Keep',12,0,25,0,0),(323,1,0,1766015567,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(324,1,0,1766016911,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(325,1,0,1766017900,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(326,1,0,1766098907,0,0,30,0,0,'Keep','[N] Kaelgard Keep',14,0,25,0,0),(327,1,0,1766178795,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(328,1,0,1766179760,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(329,1,0,1766181091,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(330,1,0,1766183164,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(331,1,0,1766184667,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(332,1,0,1766187094,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(333,1,0,1766187716,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(334,1,0,1766188084,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(335,1,0,1766189608,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(336,1,0,1766190192,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(337,1,0,1766190770,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(338,1,0,1766191482,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(339,1,0,1766195086,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(340,1,0,1766196057,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(341,1,0,1766196792,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(342,1,0,1766198188,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(343,1,0,1766199281,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(344,1,0,1766199946,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(345,1,0,1766200993,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(346,1,0,1766201771,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(347,1,0,1766202710,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(348,1,0,1766226281,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(349,1,0,1766229273,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(350,1,0,1766229687,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(351,1,0,1766230576,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(352,1,0,1766232693,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(353,1,0,1766233825,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(354,1,0,1766234159,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(355,1,0,1766234414,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(356,1,0,1766236389,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(357,1,0,1766236883,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(358,1,0,1766239752,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(359,1,0,1766240705,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(360,1,0,1766263719,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(361,1,0,1766264135,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(362,1,0,1766264514,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(363,1,0,1766266866,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(364,1,0,1766267089,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(365,1,0,1766267275,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(366,1,0,1766269277,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(367,1,0,1766271938,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(368,1,0,1766272950,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(369,1,0,1766275106,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(370,1,0,1766275746,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(371,1,0,1766276194,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(372,1,0,1766351130,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(373,1,0,1766352287,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(374,1,0,1766353098,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(375,1,0,1766353736,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(376,1,0,1766354534,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(377,1,0,1766355966,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(378,1,0,1766356410,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(379,1,0,1766356886,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(380,1,0,1766357341,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(381,1,0,1766361899,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(382,1,0,1766410645,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(383,1,0,1766412222,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(384,1,0,1766413388,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(385,1,0,1766413979,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(386,1,0,1766414852,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(387,1,0,1766415513,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(388,1,0,1766416077,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(389,1,0,1766416372,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256),(390,1,0,1766417040,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(391,1,0,1766418533,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(392,1,0,1766420141,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',12,0,25,1,256),(393,1,0,1766421628,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',14,0,25,1,256),(394,1,0,1766422217,0,0,30,0,0,'Keep','[2T] Kaelgard Keep',5,0,25,1,256);
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
  `password` varchar(32) NOT NULL,
  `email` varchar(100) DEFAULT NULL,
  `created` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `last_login` timestamp NULL DEFAULT NULL,
  `banned` tinyint DEFAULT '0',
  `ban_reason` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`accountid`),
  UNIQUE KEY `username` (`username`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb3;
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

-- Dump completed on 2025-12-22 23:47:33
