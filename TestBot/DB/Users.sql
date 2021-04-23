BEGIN TRANSACTION;
DROP TABLE IF EXISTS "Settings";
CREATE TABLE IF NOT EXISTS "Settings" (
	"SettingId"	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,
	"Key"	TEXT NOT NULL UNIQUE,
	"Value"	TEXT NOT NULL
);
DROP TABLE IF EXISTS "TelegramUsers";
CREATE TABLE IF NOT EXISTS "TelegramUsers" (
	"UserId"	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,
	"ChatId"	INTEGER NOT NULL UNIQUE,
	"FirstName"	TEXT,
	"LastName"	TEXT,
	"UserName"	TEXT,
	"DateRegisteredUtc"	TEXT NOT NULL,
	"UserIsSubscribedToCoronaUpdates"	INTEGER NOT NULL,
	"UserIsAdmin"	INTEGER NOT NULL,
	"UserLocationLatitude"	REAL,
	"UserLocationLongitude"	REAL
);
CREATE UNIQUE INDEX "IndexChatId" ON "TelegramUsers" (
	"ChatId"
);
INSERT INTO "Settings" VALUES (1,'DB_VERSION','8');
COMMIT;
