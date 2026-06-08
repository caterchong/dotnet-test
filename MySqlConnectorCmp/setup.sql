-- MySQL 9.3 测试数据初始化脚本
-- 用途：为 MySqlConnectorCmp benchmark 提供测试库和数据
-- 运行方式：mysql -u root < setup.sql

CREATE DATABASE IF NOT EXISTS bench_test CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE bench_test;

-- 场景 C：读行集（替代 MariaDB seq_1_to_10）
CREATE TABLE IF NOT EXISTS seq10 (
    seq INT NOT NULL PRIMARY KEY
);

INSERT IGNORE INTO seq10 (seq) VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10);

-- 场景扩展：模拟真实业务数据（100 行用户记录）
CREATE TABLE IF NOT EXISTS users (
    id       INT          NOT NULL AUTO_INCREMENT PRIMARY KEY,
    name     VARCHAR(64)  NOT NULL,
    email    VARCHAR(128) NOT NULL,
    age      TINYINT UNSIGNED NOT NULL,
    created  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB;

-- 清空并重新插入，保证幂等
TRUNCATE TABLE users;

INSERT INTO users (name, email, age) VALUES
('Alice',   'alice@example.com',   28),
('Bob',     'bob@example.com',     34),
('Charlie', 'charlie@example.com', 22),
('Diana',   'diana@example.com',   41),
('Eve',     'eve@example.com',     19),
('Frank',   'frank@example.com',   55),
('Grace',   'grace@example.com',   30),
('Hank',    'hank@example.com',    47),
('Iris',    'iris@example.com',    26),
('Jack',    'jack@example.com',    38),
('Karen',   'karen@example.com',   33),
('Leo',     'leo@example.com',     29),
('Mia',     'mia@example.com',     45),
('Nathan',  'nathan@example.com',  23),
('Olivia',  'olivia@example.com',  31),
('Paul',    'paul@example.com',    50),
('Quinn',   'quinn@example.com',   27),
('Rachel',  'rachel@example.com',  36),
('Sam',     'sam@example.com',     42),
('Tina',    'tina@example.com',    24);

-- 填充到 100 行（基于已有 20 行自我复制）
INSERT INTO users (name, email, age)
SELECT CONCAT(name, '_2'), CONCAT('2_', email), age + 1 FROM users;

INSERT INTO users (name, email, age)
SELECT CONCAT(name, '_3'), CONCAT('3_', email), age + 2 FROM users LIMIT 60;

-- 验证
SELECT 'seq10 rows:' AS label, COUNT(*) AS cnt FROM seq10
UNION ALL
SELECT 'users rows:', COUNT(*) FROM users;
