-- WARNING: RUNNING THIS WILL DESTROY THE OLD DATABASES!

-- Ensure the DB exists
CREATE DATABASE IF NOT EXISTS dotahost;

-- Use our DB
USE dotahost;

-- Drop old tables
--DROP TABLE IF EXISTS betaSlots;
--DROP TABLE IF EXISTS betaUsers;
--DROP TABLE IF EXISTS bans;
DROP TABLE IF EXISTS sessionKeys;
DROP TABLE IF EXISTS steamUsers;

-- Ensure steamUsers exists
CREATE TABLE IF NOT EXISTS steamUsers (
    steamID INT UNSIGNED NOT NULL,
    avatar VARCHAR(2083) NOT NULL,
    personaname VARCHAR(32) NOT NULL,
    profileurl VARCHAR(2083) NOT NULL,
    badges TINYINT UNSIGNED NOT NULL,
    cosmetics TINYINT UNSIGNED NOT NULL,
    PRIMARY KEY(steamID)
);

-- Ensure sessionKeys exist
CREATE TABLE IF NOT EXISTS sessionKeys (
    steamID INT UNSIGNED NOT NULL,
    token CHAR(32) NOT NULL,
    PRIMARY KEY(steamID),
    FOREIGN KEY (steamID)
        REFERENCES steamUsers(steamID)
        ON DELETE CASCADE
);

-- Bans tables
CREATE TABLE IF NOT EXISTS bans (
    banID INT UNSIGNED AUTO_INCREMENT NOT NULL,
    steamID INT UNSIGNED NOT NULL,
    expiration TIMESTAMP NOT NULL,
    reason TEXT NOT NULL,
    PRIMARY KEY(banID)
);

-- Beta table
CREATE TABLE IF NOT EXISTS betaUsers (
    steamID INT UNSIGNED NOT NULL,
    PRIMARY KEY(steamID),
    FOREIGN KEY (steamID)
        REFERENCES steamUsers(steamID)
        ON DELETE CASCADE
);

-- Beta Slots table
DROP TABLE IF EXISTS betaSlots;
CREATE TABLE IF NOT EXISTS betaSlots (
    lck TINYINT DEFAULT 0 NOT NULL,
    slots SMALLINT UNSIGNED NOT NULL,
    PRIMARY KEY(lck)
);
