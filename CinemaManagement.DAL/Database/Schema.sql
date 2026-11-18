
PRAGMA foreign_keys = ON;

-- =====================================================
-- A. USERS - TÀI KHOẢN NGƯỜI DÙNG
-- =====================================================

CREATE TABLE IF NOT EXISTS Users
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Username TEXT NOT NULL COLLATE NOCASE UNIQUE,
    PasswordHash TEXT NOT NULL,
    FullName TEXT NOT NULL,
    Role INTEGER NOT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1
    CHECK (IsActive IN (0, 1))
    );

-- =====================================================
-- B. AUDIT LOGS - NHẬT KÝ HỆ THỐNG
-- UserId = 0 dành cho thao tác không có người dùng
-- đang đăng nhập.
-- =====================================================

CREATE TABLE IF NOT EXISTS AuditLogs
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL DEFAULT 0,
    Action TEXT NOT NULL,
    Detail TEXT NOT NULL DEFAULT '',
    CreatedAt TEXT NOT NULL
);

-- =====================================================
-- C. SETTINGS - CẤU HÌNH HỆ THỐNG
-- =====================================================

CREATE TABLE IF NOT EXISTS Settings
(
    [Key] TEXT PRIMARY KEY NOT NULL,
[Value] TEXT NOT NULL
);

-- =====================================================
-- D. CUSTOMERS - KHÁCH HÀNG
-- =====================================================

CREATE TABLE IF NOT EXISTS Customers
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FullName TEXT NOT NULL,
    Phone TEXT NOT NULL UNIQUE,
    Email TEXT NOT NULL DEFAULT '',
    Points INTEGER NOT NULL DEFAULT 0
    CHECK (Points >= 0),
    Tier TEXT NOT NULL DEFAULT 'Standard'
    );

-- =====================================================
-- E. GENRES - THỂ LOẠI PHIM
-- =====================================================

CREATE TABLE IF NOT EXISTS Genres
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL COLLATE NOCASE UNIQUE
);

-- =====================================================
-- F. MOVIES - PHIM
-- =====================================================

CREATE TABLE IF NOT EXISTS Movies
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Title TEXT NOT NULL,
    DurationMinutes INTEGER NOT NULL
    CHECK (DurationMinutes > 0),
    Description TEXT NOT NULL DEFAULT '',
    PosterPath TEXT NOT NULL DEFAULT '',
    TrailerUrl TEXT NOT NULL DEFAULT '',
    AgeLimit INTEGER NOT NULL DEFAULT 0
    CHECK (AgeLimit >= 0),
    ReleaseDate TEXT NOT NULL,
    Status INTEGER NOT NULL DEFAULT 0
    );

-- =====================================================
-- G. MOVIE GENRES - PHIM VÀ THỂ LOẠI
-- =====================================================

CREATE TABLE IF NOT EXISTS MovieGenres
(
    MovieId INTEGER NOT NULL,
    GenreId INTEGER NOT NULL,

    PRIMARY KEY (MovieId, GenreId),

    FOREIGN KEY (MovieId)
    REFERENCES Movies(Id)
    ON DELETE CASCADE,

    FOREIGN KEY (GenreId)
    REFERENCES Genres(Id)
    ON DELETE CASCADE
    );

-- =====================================================
-- H. ROOMS - PHÒNG CHIẾU
-- =====================================================

CREATE TABLE IF NOT EXISTS Rooms
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL COLLATE NOCASE UNIQUE,
    TotalRows INTEGER NOT NULL CHECK (TotalRows > 0),
    TotalCols INTEGER NOT NULL CHECK (TotalCols > 0),
    ScreenType INTEGER NOT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1
    CHECK (IsActive IN (0, 1))
    );

-- =====================================================
-- I. SEATS - GHẾ
-- =====================================================

CREATE TABLE IF NOT EXISTS Seats
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    RoomId INTEGER NOT NULL,
    RowLabel TEXT NOT NULL,
    ColNumber INTEGER NOT NULL CHECK (ColNumber > 0),
    SeatType INTEGER NOT NULL DEFAULT 0,

    UNIQUE (RoomId, RowLabel, ColNumber),

    FOREIGN KEY (RoomId)
    REFERENCES Rooms(Id)
    ON DELETE CASCADE
    );

-- =====================================================
-- J. SHOWTIMES - SUẤT CHIẾU
-- =====================================================

CREATE TABLE IF NOT EXISTS Showtimes
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    MovieId INTEGER NOT NULL,
    RoomId INTEGER NOT NULL,
    StartTime TEXT NOT NULL,
    EndTime TEXT NOT NULL,
    BasePrice NUMERIC NOT NULL CHECK (BasePrice >= 0),
    IsActive INTEGER NOT NULL DEFAULT 1
    CHECK (IsActive IN (0, 1)),

    FOREIGN KEY (MovieId)
    REFERENCES Movies(Id)
    ON DELETE RESTRICT,

    FOREIGN KEY (RoomId)
    REFERENCES Rooms(Id)
    ON DELETE RESTRICT,

    CHECK (datetime(EndTime) > datetime(StartTime))
    );

-- =====================================================
-- K. PRODUCTS - SẢN PHẨM
-- =====================================================

CREATE TABLE IF NOT EXISTS Products
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Price NUMERIC NOT NULL CHECK (Price >= 0),
    Category TEXT NOT NULL DEFAULT '',
    IsActive INTEGER NOT NULL DEFAULT 1
    CHECK (IsActive IN (0, 1))
    );

-- =====================================================
-- L. PROMOTIONS - KHUYẾN MÃI
-- =====================================================

CREATE TABLE IF NOT EXISTS Promotions
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Code TEXT NOT NULL COLLATE NOCASE UNIQUE,
    Type TEXT NOT NULL,
    Value NUMERIC NOT NULL CHECK (Value > 0),
    StartDate TEXT NOT NULL,
    EndDate TEXT NOT NULL,
    MinOrder NUMERIC NOT NULL DEFAULT 0
    CHECK (MinOrder >= 0),
    UsageLimit INTEGER NOT NULL DEFAULT 0
    CHECK (UsageLimit >= 0),
    UsedCount INTEGER NOT NULL DEFAULT 0
    CHECK (UsedCount >= 0),

    CHECK (datetime(EndDate) >= datetime(StartDate))
    );

-- =====================================================
-- M. PRICING RULES - QUY TẮC GIÁ
-- =====================================================

CREATE TABLE IF NOT EXISTS PricingRules
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SeatType INTEGER NOT NULL,
    ScreenType INTEGER NOT NULL,
    DayType TEXT NOT NULL,
    TimeFrom TEXT NOT NULL,
    TimeTo TEXT NOT NULL,
    Multiplier NUMERIC NOT NULL CHECK (Multiplier > 0)
    );

-- =====================================================
-- N. BOOKINGS - ĐƠN ĐẶT VÉ
-- =====================================================

CREATE TABLE IF NOT EXISTS Bookings
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Code TEXT NOT NULL UNIQUE,
    CustomerId INTEGER NULL,
    UserId INTEGER NOT NULL,
    CreatedAt TEXT NOT NULL,
    Subtotal NUMERIC NOT NULL DEFAULT 0
    CHECK (Subtotal >= 0),
    Discount NUMERIC NOT NULL DEFAULT 0
    CHECK (Discount >= 0),
    Total NUMERIC NOT NULL DEFAULT 0
    CHECK (Total >= 0),
    PaymentMethod INTEGER NOT NULL,
    Status INTEGER NOT NULL DEFAULT 0,

    FOREIGN KEY (CustomerId)
    REFERENCES Customers(Id)
    ON DELETE SET NULL,

    FOREIGN KEY (UserId)
    REFERENCES Users(Id)
    ON DELETE RESTRICT
    );

-- =====================================================
-- O. TICKETS - VÉ
-- =====================================================

CREATE TABLE IF NOT EXISTS Tickets
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    BookingId INTEGER NOT NULL,
    ShowtimeId INTEGER NOT NULL,
    SeatId INTEGER NOT NULL,
    Price NUMERIC NOT NULL CHECK (Price >= 0),
    Status TEXT NOT NULL DEFAULT 'Active',
    QrCode TEXT NOT NULL DEFAULT '',

    FOREIGN KEY (BookingId)
    REFERENCES Bookings(Id)
    ON DELETE CASCADE,

    FOREIGN KEY (ShowtimeId)
    REFERENCES Showtimes(Id)
    ON DELETE RESTRICT,

    FOREIGN KEY (SeatId)
    REFERENCES Seats(Id)
    ON DELETE RESTRICT
    );

-- Một ghế chỉ được bán một lần trong một suất chiếu.
-- Vé đã hủy không bị giới hạn bởi index này.

CREATE UNIQUE INDEX IF NOT EXISTS
    UX_Tickets_Active_Showtime_Seat
    ON Tickets(ShowtimeId, SeatId)
    WHERE Status = 'Active';

-- =====================================================
-- P. BOOKING PRODUCTS - SẢN PHẨM TRONG ĐƠN
-- =====================================================

CREATE TABLE IF NOT EXISTS BookingProducts
(
    BookingId INTEGER NOT NULL,
    ProductId INTEGER NOT NULL,
    Qty INTEGER NOT NULL CHECK (Qty > 0),
    UnitPrice NUMERIC NOT NULL CHECK (UnitPrice >= 0),

    PRIMARY KEY (BookingId, ProductId),

    FOREIGN KEY (BookingId)
    REFERENCES Bookings(Id)
    ON DELETE CASCADE,

    FOREIGN KEY (ProductId)
    REFERENCES Products(Id)
    ON DELETE RESTRICT
    );

-- =====================================================
-- Q. SEAT HOLDS - GIỮ GHẾ TẠM THỜI
-- =====================================================

CREATE TABLE IF NOT EXISTS SeatHolds
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ShowtimeId INTEGER NOT NULL,
    SeatId INTEGER NOT NULL,
    SessionId TEXT NOT NULL,
    ExpiresAt TEXT NOT NULL,

    UNIQUE (ShowtimeId, SeatId),

    FOREIGN KEY (ShowtimeId)
    REFERENCES Showtimes(Id)
    ON DELETE CASCADE,

    FOREIGN KEY (SeatId)
    REFERENCES Seats(Id)
    ON DELETE CASCADE
    );

-- =====================================================
-- INDEXES - TỐI ƯU TRUY VẤN
-- =====================================================

CREATE INDEX IF NOT EXISTS IX_AuditLogs_User_CreatedAt
    ON AuditLogs(UserId, CreatedAt);

CREATE INDEX IF NOT EXISTS IX_Showtimes_StartTime
    ON Showtimes(StartTime);

CREATE INDEX IF NOT EXISTS IX_Showtimes_Room_Start_End
    ON Showtimes(RoomId, StartTime, EndTime);

CREATE INDEX IF NOT EXISTS IX_Tickets_Showtime_Status
    ON Tickets(ShowtimeId, Status);

CREATE INDEX IF NOT EXISTS IX_Bookings_CreatedAt_Status
    ON Bookings(CreatedAt, Status);

CREATE INDEX IF NOT EXISTS IX_SeatHolds_ExpiresAt
    ON SeatHolds(ExpiresAt);

CREATE INDEX IF NOT EXISTS IX_SeatHolds_SessionId
    ON SeatHolds(SessionId);
