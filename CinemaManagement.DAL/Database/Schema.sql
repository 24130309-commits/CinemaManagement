PRAGMA foreign_keys = ON;

-- 1. Người dùng & Phân quyền
CREATE TABLE IF NOT EXISTS Users (
                                     Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                     Username TEXT NOT NULL UNIQUE,
                                     PasswordHash TEXT NOT NULL,
                                     FullName TEXT NOT NULL,
                                     Email TEXT,
                                     Role TEXT NOT NULL DEFAULT 'Staff', -- 'Admin', 'Staff'
                                     IsActive INTEGER NOT NULL DEFAULT 1,
                                     CreatedAt TEXT NOT NULL DEFAULT (datetime('now', 'localtime'))
    );

-- 2. Nhật ký hệ thống (AuditLog)
CREATE TABLE IF NOT EXISTS AuditLogs (
                                         Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                         UserId INTEGER,
                                         Action TEXT NOT NULL,
                                         EntityName TEXT,
                                         EntityId INTEGER,
                                         Details TEXT,
                                         CreatedAt TEXT NOT NULL DEFAULT (datetime('now', 'localtime')),
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL
    );

-- 3. Cấu hình hệ thống (AppSetting)
CREATE TABLE IF NOT EXISTS AppSettings (
                                           Key TEXT PRIMARY KEY,
                                           Value TEXT NOT NULL,
                                           Description TEXT
);

-- 4. Phim (Movies)
CREATE TABLE IF NOT EXISTS Movies (
                                      Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                      Title TEXT NOT NULL,
                                      Genre TEXT,
                                      Duration INTEGER NOT NULL,
                                      ReleaseDate TEXT,
                                      Status TEXT NOT NULL DEFAULT 'NowShowing',
                                      PosterPath TEXT
);

-- 5. Phòng chiếu & Ghế (Rooms, Seats)
CREATE TABLE IF NOT EXISTS Rooms (
                                     Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                     Name TEXT NOT NULL UNIQUE,
                                     Capacity INTEGER NOT NULL,
                                     IsActive INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS Seats (
                                     Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                     RoomId INTEGER NOT NULL,
                                     RowLabel TEXT NOT NULL,
                                     SeatNumber INTEGER NOT NULL,
                                     SeatType TEXT NOT NULL DEFAULT 'Standard', -- 'Standard', 'VIP', 'Couple'
                                     FOREIGN KEY (RoomId) REFERENCES Rooms(Id) ON DELETE CASCADE
    );

-- 6. Suất chiếu (Showtimes)
CREATE TABLE IF NOT EXISTS Showtimes (
                                         Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                         MovieId INTEGER NOT NULL,
                                         RoomId INTEGER NOT NULL,
                                         StartTime TEXT NOT NULL,
                                         EndTime TEXT NOT NULL,
                                         BasePrice REAL NOT NULL,
                                         FOREIGN KEY (MovieId) REFERENCES Movies(Id),
    FOREIGN KEY (RoomId) REFERENCES Rooms(Id)
    );

-- 7. Quy tắc giá & Khuyến mãi (PricingRules, Promotions)
CREATE TABLE IF NOT EXISTS PricingRules (
                                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                            DayType TEXT NOT NULL,   -- 'Weekday', 'Weekend'
                                            SeatType TEXT NOT NULL,  -- 'Standard', 'VIP', 'Couple'
                                            ExtraFee REAL NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS Promotions (
                                          Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                          Code TEXT NOT NULL UNIQUE,
                                          DiscountPercent REAL NOT NULL,
                                          MaxDiscountAmount REAL NOT NULL DEFAULT 0,
                                          StartDate TEXT NOT NULL,
                                          EndDate TEXT NOT NULL,
                                          IsActive INTEGER NOT NULL DEFAULT 1
);

-- 8. Hóa đơn & Vé bán (Invoices, Tickets)
CREATE TABLE IF NOT EXISTS Invoices (
                                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                        StaffId INTEGER NOT NULL,
                                        PromotionCode TEXT,
                                        TotalAmount REAL NOT NULL,
                                        CreatedAt TEXT NOT NULL DEFAULT (datetime('now', 'localtime')),
    FOREIGN KEY (StaffId) REFERENCES Users(Id)
    );

CREATE TABLE IF NOT EXISTS Tickets (
                                       Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                       InvoiceId INTEGER NOT NULL,
                                       ShowtimeId INTEGER NOT NULL,
                                       SeatId INTEGER NOT NULL,
                                       FinalPrice REAL NOT NULL,
                                       FOREIGN KEY (InvoiceId) REFERENCES Invoices(Id) ON DELETE CASCADE,
    FOREIGN KEY (ShowtimeId) REFERENCES Showtimes(Id),
    FOREIGN KEY (SeatId) REFERENCES Seats(Id),
    UNIQUE (ShowtimeId, SeatId)
    );