DROP DATABASE IF EXISTS library_db;
CREATE DATABASE library_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE library_db;

-- ---------------- Справочники ----------------
CREATE TABLE roles (
    role_id    TINYINT UNSIGNED NOT NULL AUTO_INCREMENT,
    role_name  VARCHAR(30) NOT NULL,
    CONSTRAINT pk_roles PRIMARY KEY (role_id),
    CONSTRAINT uq_roles_name UNIQUE (role_name)
) ENGINE=InnoDB;

CREATE TABLE reader_categories (
    category_id    TINYINT UNSIGNED NOT NULL AUTO_INCREMENT,
    category_name  VARCHAR(30) NOT NULL,
    loan_days      SMALLINT UNSIGNED NOT NULL,
    CONSTRAINT pk_reader_categories PRIMARY KEY (category_id),
    CONSTRAINT uq_reader_categories_name UNIQUE (category_name),
    CONSTRAINT ck_reader_categories_days CHECK (loan_days BETWEEN 1 AND 365)
) ENGINE=InnoDB;

CREATE TABLE reader_statuses (
    status_id    TINYINT UNSIGNED NOT NULL AUTO_INCREMENT,
    status_name  VARCHAR(30) NOT NULL,
    CONSTRAINT pk_reader_statuses PRIMARY KEY (status_id),
    CONSTRAINT uq_reader_statuses_name UNIQUE (status_name)
) ENGINE=InnoDB;

CREATE TABLE copy_statuses (
    status_id    TINYINT UNSIGNED NOT NULL AUTO_INCREMENT,
    status_name  VARCHAR(30) NOT NULL,
    CONSTRAINT pk_copy_statuses PRIMARY KEY (status_id),
    CONSTRAINT uq_copy_statuses_name UNIQUE (status_name)
) ENGINE=InnoDB;

CREATE TABLE reservation_statuses (
    status_id    TINYINT UNSIGNED NOT NULL AUTO_INCREMENT,
    status_name  VARCHAR(30) NOT NULL,
    CONSTRAINT pk_reservation_statuses PRIMARY KEY (status_id),
    CONSTRAINT uq_reservation_statuses_name UNIQUE (status_name)
) ENGINE=InnoDB;

-- ---------------- Основные таблицы ----------------
CREATE TABLE readers (
    reader_id      INT UNSIGNED NOT NULL AUTO_INCREMENT,
    ticket_number  VARCHAR(20) NOT NULL DEFAULT '',
    last_name      VARCHAR(50) NOT NULL,
    first_name     VARCHAR(50) NOT NULL,
    middle_name    VARCHAR(50) NULL,
    category_id    TINYINT UNSIGNED NOT NULL,
    status_id      TINYINT UNSIGNED NOT NULL DEFAULT 1,
    phone          VARCHAR(20) NULL,
    email          VARCHAR(100) NULL,
    registered_on  DATE NOT NULL,
    valid_until    DATE NOT NULL,
    CONSTRAINT pk_readers PRIMARY KEY (reader_id),
    CONSTRAINT uq_readers_ticket UNIQUE (ticket_number),
    CONSTRAINT fk_readers_category FOREIGN KEY (category_id) REFERENCES reader_categories (category_id),
    CONSTRAINT fk_readers_status FOREIGN KEY (status_id) REFERENCES reader_statuses (status_id),
    CONSTRAINT ck_readers_dates CHECK (valid_until >= registered_on)
) ENGINE=InnoDB;

CREATE TABLE employees (
    employee_id  INT UNSIGNED NOT NULL AUTO_INCREMENT,
    last_name    VARCHAR(50) NOT NULL,
    first_name   VARCHAR(50) NOT NULL,
    middle_name  VARCHAR(50) NULL,
    post         VARCHAR(60) NOT NULL,
    CONSTRAINT pk_employees PRIMARY KEY (employee_id)
) ENGINE=InnoDB;

CREATE TABLE users (
    user_id        INT UNSIGNED NOT NULL AUTO_INCREMENT,
    login          VARCHAR(50) NOT NULL,
    password_hash  CHAR(64) NOT NULL,
    salt           CHAR(32) NOT NULL,
    role_id        TINYINT UNSIGNED NOT NULL,
    reader_id      INT UNSIGNED NULL,
    employee_id    INT UNSIGNED NULL,
    is_active      TINYINT(1) NOT NULL DEFAULT 1,
    created_at     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_users PRIMARY KEY (user_id),
    CONSTRAINT uq_users_login UNIQUE (login),
    CONSTRAINT uq_users_reader UNIQUE (reader_id),
    CONSTRAINT uq_users_employee UNIQUE (employee_id),
    CONSTRAINT fk_users_role FOREIGN KEY (role_id) REFERENCES roles (role_id),
    CONSTRAINT fk_users_reader FOREIGN KEY (reader_id) REFERENCES readers (reader_id) ON DELETE CASCADE,
    CONSTRAINT fk_users_employee FOREIGN KEY (employee_id) REFERENCES employees (employee_id),
    CONSTRAINT ck_users_owner CHECK (reader_id IS NULL OR employee_id IS NULL)
) ENGINE=InnoDB;

CREATE TABLE editions (
    edition_id  INT UNSIGNED NOT NULL AUTO_INCREMENT,
    title       VARCHAR(200) NOT NULL,
    author      VARCHAR(150) NOT NULL,
    pub_year    SMALLINT UNSIGNED NOT NULL,
    isbn        VARCHAR(20) NOT NULL,
    CONSTRAINT pk_editions PRIMARY KEY (edition_id),
    CONSTRAINT uq_editions_isbn UNIQUE (isbn),
    CONSTRAINT ck_editions_year CHECK (pub_year BETWEEN 1400 AND 2100)
) ENGINE=InnoDB;

CREATE TABLE copies (
    copy_id           INT UNSIGNED NOT NULL AUTO_INCREMENT,
    inventory_number  VARCHAR(20) NOT NULL,
    edition_id        INT UNSIGNED NOT NULL,
    status_id         TINYINT UNSIGNED NOT NULL DEFAULT 1,
    CONSTRAINT pk_copies PRIMARY KEY (copy_id),
    CONSTRAINT uq_copies_inventory UNIQUE (inventory_number),
    CONSTRAINT fk_copies_edition FOREIGN KEY (edition_id) REFERENCES editions (edition_id),
    CONSTRAINT fk_copies_status FOREIGN KEY (status_id) REFERENCES copy_statuses (status_id)
) ENGINE=InnoDB;

CREATE TABLE loans (
    loan_id      INT UNSIGNED NOT NULL AUTO_INCREMENT,
    loan_number  VARCHAR(20) NOT NULL DEFAULT '',
    reader_id    INT UNSIGNED NOT NULL,
    copy_id      INT UNSIGNED NOT NULL,
    employee_id  INT UNSIGNED NOT NULL,
    issue_date   DATE NOT NULL,
    due_date     DATE NOT NULL,
    return_date  DATE NULL,
    CONSTRAINT pk_loans PRIMARY KEY (loan_id),
    CONSTRAINT uq_loans_number UNIQUE (loan_number),
    CONSTRAINT fk_loans_reader FOREIGN KEY (reader_id) REFERENCES readers (reader_id),
    CONSTRAINT fk_loans_copy FOREIGN KEY (copy_id) REFERENCES copies (copy_id),
    CONSTRAINT fk_loans_employee FOREIGN KEY (employee_id) REFERENCES employees (employee_id),
    CONSTRAINT ck_loans_due CHECK (due_date >= issue_date),
    CONSTRAINT ck_loans_return CHECK (return_date IS NULL OR return_date >= issue_date)
) ENGINE=InnoDB;

CREATE TABLE fines (
    fine_id      INT UNSIGNED NOT NULL AUTO_INCREMENT,
    loan_id      INT UNSIGNED NOT NULL,
    amount       DECIMAL(8,2) NOT NULL,
    accrued_on   DATE NOT NULL,
    paid_on      DATE NULL,
    CONSTRAINT pk_fines PRIMARY KEY (fine_id),
    CONSTRAINT fk_fines_loan FOREIGN KEY (loan_id) REFERENCES loans (loan_id),
    CONSTRAINT ck_fines_amount CHECK (amount > 0)
) ENGINE=InnoDB;

CREATE TABLE reservations (
    reservation_id      INT UNSIGNED NOT NULL AUTO_INCREMENT,
    reservation_number  VARCHAR(20) NOT NULL DEFAULT '',
    reader_id           INT UNSIGNED NOT NULL,
    edition_id          INT UNSIGNED NOT NULL,
    reserved_on         DATE NOT NULL,
    status_id           TINYINT UNSIGNED NOT NULL DEFAULT 1,
    CONSTRAINT pk_reservations PRIMARY KEY (reservation_id),
    CONSTRAINT uq_reservations_number UNIQUE (reservation_number),
    CONSTRAINT fk_reservations_reader FOREIGN KEY (reader_id) REFERENCES readers (reader_id),
    CONSTRAINT fk_reservations_edition FOREIGN KEY (edition_id) REFERENCES editions (edition_id),
    CONSTRAINT fk_reservations_status FOREIGN KEY (status_id) REFERENCES reservation_statuses (status_id)
) ENGINE=InnoDB;

-- Счётчики автоматической нумерации документов (по типу и году)
CREATE TABLE doc_counters (
    doc_type    CHAR(4) NOT NULL,
    doc_year    SMALLINT UNSIGNED NOT NULL,
    counter_value  INT UNSIGNED NOT NULL DEFAULT 0,
    CONSTRAINT pk_doc_counters PRIMARY KEY (doc_type, doc_year)
) ENGINE=InnoDB;

CREATE INDEX ix_readers_fio ON readers (last_name, first_name);
CREATE INDEX ix_editions_title ON editions (title);
CREATE INDEX ix_editions_author ON editions (author);
CREATE INDEX ix_copies_edition ON copies (edition_id);
CREATE INDEX ix_loans_reader ON loans (reader_id);
CREATE INDEX ix_loans_copy ON loans (copy_id);
CREATE INDEX ix_reservations_reader ON reservations (reader_id);

-- ---------------- Триггеры автоматической нумерации ----------------
DELIMITER $$

CREATE TRIGGER trg_readers_bi BEFORE INSERT ON readers FOR EACH ROW
BEGIN
    DECLARE n INT UNSIGNED;
    DECLARE y SMALLINT UNSIGNED;
    IF NEW.ticket_number IS NULL OR NEW.ticket_number = '' THEN
        SET y = YEAR(NEW.registered_on);
        INSERT INTO doc_counters (doc_type, doc_year, counter_value) VALUES ('READ', y, 0)
            ON DUPLICATE KEY UPDATE counter_value = counter_value;
        UPDATE doc_counters SET counter_value = counter_value + 1 WHERE doc_type = 'READ' AND doc_year = y;
        SELECT counter_value INTO n FROM doc_counters WHERE doc_type = 'READ' AND doc_year = y;
        SET NEW.ticket_number = CONCAT('ЧБ-', y, '-', LPAD(n, 4, '0'));
    END IF;
END$$

CREATE TRIGGER trg_loans_bi BEFORE INSERT ON loans FOR EACH ROW
BEGIN
    DECLARE n INT UNSIGNED;
    DECLARE y SMALLINT UNSIGNED;
    IF NEW.loan_number IS NULL OR NEW.loan_number = '' THEN
        SET y = YEAR(NEW.issue_date);
        INSERT INTO doc_counters (doc_type, doc_year, counter_value) VALUES ('LOAN', y, 0)
            ON DUPLICATE KEY UPDATE counter_value = counter_value;
        UPDATE doc_counters SET counter_value = counter_value + 1 WHERE doc_type = 'LOAN' AND doc_year = y;
        SELECT counter_value INTO n FROM doc_counters WHERE doc_type = 'LOAN' AND doc_year = y;
        SET NEW.loan_number = CONCAT('ВД-', y, '-', LPAD(n, 6, '0'));
    END IF;
END$$

CREATE TRIGGER trg_reservations_bi BEFORE INSERT ON reservations FOR EACH ROW
BEGIN
    DECLARE n INT UNSIGNED;
    DECLARE y SMALLINT UNSIGNED;
    IF NEW.reservation_number IS NULL OR NEW.reservation_number = '' THEN
        SET y = YEAR(NEW.reserved_on);
        INSERT INTO doc_counters (doc_type, doc_year, counter_value) VALUES ('RESV', y, 0)
            ON DUPLICATE KEY UPDATE counter_value = counter_value;
        UPDATE doc_counters SET counter_value = counter_value + 1 WHERE doc_type = 'RESV' AND doc_year = y;
        SELECT counter_value INTO n FROM doc_counters WHERE doc_type = 'RESV' AND doc_year = y;
        SET NEW.reservation_number = CONCAT('БР-', y, '-', LPAD(n, 4, '0'));
    END IF;
END$$

DELIMITER ;

-- ---------------- Данные справочников ----------------
INSERT INTO roles (role_name) VALUES ('Администратор'), ('Библиотекарь'), ('Читатель');
INSERT INTO reader_categories (category_name, loan_days) VALUES ('Студент', 30), ('Преподаватель', 60), ('Сотрудник', 45);
INSERT INTO reader_statuses (status_name) VALUES ('Действителен'), ('Просрочен'), ('Заблокирован');
INSERT INTO copy_statuses (status_name) VALUES ('Свободен'), ('Выдан'), ('Забронирован'), ('Списан');
INSERT INTO reservation_statuses (status_name) VALUES ('Активна'), ('Выполнена'), ('Отменена');

-- ---------------- Сотрудники ----------------
INSERT INTO employees (last_name, first_name, middle_name, post) VALUES
 ('Соловьёв','Андрей','Викторович','Администратор системы'),
 ('Громова','Елена','Павловна','Библиотекарь абонемента'),
 ('Тихонов','Руслан','Алексеевич','Библиотекарь абонемента'),
 ('Белова','Нина','Степановна','Заведующая книгохранилищем');

-- ---------------- Читатели (30 записей, номера билетов формирует триггер) ----------------
INSERT INTO readers (last_name, first_name, middle_name, category_id, status_id, phone, email, registered_on, valid_until) VALUES
('Иванов','Алексей','Сергеевич',1,1,'+7 911 107-01-13','reader01@example.edu','2025-09-04','2027-08-31'),
('Петрова','Мария','Андреевна',1,1,'+7 912 114-02-26','reader02@example.edu','2025-09-07','2027-08-31'),
('Сидоров','Дмитрий','Игоревич',1,1,'+7 913 121-03-39','reader03@example.edu','2025-09-10','2027-08-31'),
('Кузнецова','Анна','Викторовна',1,1,'+7 914 128-04-52','reader04@example.edu','2025-09-13','2027-08-31'),
('Смирнов','Илья','Олегович',1,3,'+7 915 135-05-65','reader05@example.edu','2025-09-16','2027-08-31'),
('Попова','Екатерина','Дмитриевна',1,1,'+7 916 142-06-78','reader06@example.edu','2025-09-19','2027-08-31'),
('Васильев','Никита','Павлович',1,1,'+7 917 149-07-91','reader07@example.edu','2025-09-22','2027-08-31'),
('Соколова','Ольга','Николаевна',1,2,'+7 918 156-08-04','reader08@example.edu','2025-09-25','2026-09-30'),
('Михайлов','Артём','Андреевич',1,1,'+7 919 163-09-17','reader09@example.edu','2025-09-28','2027-08-31'),
('Новикова','Дарья','Сергеевна',1,1,'+7 920 170-10-30','reader10@example.edu','2025-10-01','2027-08-31'),
('Фёдоров','Максим','Алексеевич',1,1,'+7 921 177-11-43','reader11@example.edu','2025-10-04','2027-08-31'),
('Морозова','Полина','Игоревна',1,3,'+7 922 184-12-56','reader12@example.edu','2025-10-07','2027-08-31'),
('Волков','Егор','Владимирович',1,1,'+7 923 191-13-69','reader13@example.edu','2025-10-10','2027-08-31'),
('Алексеева','Виктория','Романовна',1,1,'+7 924 198-14-82','reader14@example.edu','2025-09-03','2027-08-31'),
('Лебедев','Кирилл','Денисович',1,1,'+7 925 205-15-95','reader15@example.edu','2025-09-06','2027-08-31'),
('Семёнова','Елизавета','Максимовна',1,1,'+7 926 212-16-08','reader16@example.edu','2025-09-09','2027-08-31'),
('Егоров','Тимур','Русланович',1,2,'+7 927 219-17-21','reader17@example.edu','2025-09-12','2026-09-30'),
('Павлова','Алиса','Артёмовна',1,1,'+7 928 226-18-34','reader18@example.edu','2025-09-15','2027-08-31'),
('Козлов','Матвей','Ильич',1,1,'+7 929 233-19-47','reader19@example.edu','2025-09-18','2027-08-31'),
('Степанова','София','Кирилловна',1,1,'+7 930 240-20-60','reader20@example.edu','2025-09-21','2027-08-31'),
('Николаев','Сергей','Петрович',2,3,'+7 931 247-21-73','reader21@example.edu','2025-09-24','2028-08-31'),
('Орлова','Татьяна','Ивановна',2,1,'+7 932 254-22-86','reader22@example.edu','2025-09-27','2028-08-31'),
('Андреев','Владимир','Николаевич',2,1,'+7 933 261-23-99','reader23@example.edu','2025-09-30','2028-08-31'),
('Макарова','Людмила','Борисовна',2,1,'+7 934 268-24-12','reader24@example.edu','2025-10-03','2028-08-31'),
('Захаров','Игорь','Анатольевич',2,1,'+7 935 275-25-25','reader25@example.edu','2025-10-06','2028-08-31'),
('Зайцева','Галина','Михайловна',2,1,'+7 936 282-26-38','reader26@example.edu','2025-10-09','2028-08-31'),
('Борисов','Андрей','Юрьевич',3,1,'+7 937 289-27-51','reader27@example.edu','2025-09-02','2028-08-31'),
('Королёва','Наталья','Сергеевна',3,1,'+7 938 296-28-64','reader28@example.edu','2025-09-05','2028-08-31'),
('Герасимов','Павел','Евгеньевич',3,1,'+7 939 303-29-77','reader29@example.edu','2025-09-08','2028-08-31'),
('Пономарёва','Ирина','Леонидовна',3,1,'+7 940 310-30-90','reader30@example.edu','2025-09-11','2028-08-31');

-- ---------------- Издания (30 записей, ISBN условные) ----------------
INSERT INTO editions (title, author, pub_year, isbn) VALUES
('Современные операционные системы','Таненбаум Э.',2015,'978-5-9999-0001-2'),
('Компьютерные сети','Таненбаум Э., Уэзеролл Д.',2012,'978-5-9999-0002-9'),
('Чистый код','Мартин Р.',2013,'978-5-9999-0003-6'),
('Приёмы объектно-ориентированного проектирования. Паттерны','Гамма Э. и др.',2015,'978-5-9999-0004-3'),
('Совершенный код','Макконнелл С.',2017,'978-5-9999-0005-0'),
('Алгоритмы: построение и анализ','Кормен Т. и др.',2013,'978-5-9999-0006-7'),
('Введение в системы баз данных','Дейт К. Дж.',2019,'978-5-9999-0007-4'),
('SQL. Полное руководство','Грофф Дж., Вайнберг П.',2014,'978-5-9999-0008-1'),
('Язык программирования C# и платформа .NET','Троелсен Э.',2022,'978-5-9999-0009-8'),
('UML. Основы','Фаулер М.',2004,'978-5-9999-0010-4'),
('Бизнес-процессы компании','Репин В.',2015,'978-5-9999-0011-1'),
('Теория вероятностей и математическая статистика','Гмурман В.',2019,'978-5-9999-0012-8'),
('Математический анализ','Ильин В., Садовничий В.',2018,'978-5-9999-0013-5'),
('Курс общей физики','Савельев И.',2016,'978-5-9999-0014-2'),
('Дискретная математика','Яблонский С.',2014,'978-5-9999-0015-9'),
('Основы программной инженерии','Соммервилл И.',2016,'978-5-9999-0016-6'),
('Проектирование информационных систем','Грекул В.',2017,'978-5-9999-0017-3'),
('Информационная безопасность','Партыка Т.',2018,'978-5-9999-0018-0'),
('Экономика предприятия','Волков О.',2015,'978-5-9999-0019-7'),
('Менеджмент','Мескон М.',2017,'978-5-9999-0020-3'),
('История России','Орлов А.',2019,'978-5-9999-0021-0'),
('Философия','Спиркин А.',2018,'978-5-9999-0022-7'),
('Русский язык и культура речи','Голуб И.',2016,'978-5-9999-0023-4'),
('Английский язык для IT-специалистов','Бонк Н.',2020,'978-5-9999-0024-1'),
('Python. Программирование для начинающих','Лутц М.',2019,'978-5-9999-0025-8'),
('Структуры данных и алгоритмы','Вирт Н.',2017,'978-5-9999-0026-5'),
('Тестирование программного обеспечения','Канер С.',2015,'978-5-9999-0027-2'),
('Основы ITIL','Беннет Д.',2016,'978-5-9999-0028-9'),
('Сетевые технологии','Олифер В.',2020,'978-5-9999-0029-6'),
('Операционные системы: принципы','Столлингс В.',2015,'978-5-9999-0030-2');

-- ---------------- Экземпляры (45 записей) ----------------
INSERT INTO copies (inventory_number, edition_id, status_id) VALUES
('INV-000001',1,1),
('INV-000002',2,3),
('INV-000003',3,1),
('INV-000004',4,1),
('INV-000005',5,2),
('INV-000006',6,2),
('INV-000007',7,2),
('INV-000008',8,1),
('INV-000009',9,1),
('INV-000010',10,1),
('INV-000011',11,1),
('INV-000012',12,1),
('INV-000013',13,1),
('INV-000014',14,1),
('INV-000015',15,1),
('INV-000016',16,2),
('INV-000017',17,2),
('INV-000018',18,2),
('INV-000019',19,1),
('INV-000020',20,1),
('INV-000021',21,1),
('INV-000022',22,1),
('INV-000023',23,1),
('INV-000024',24,1),
('INV-000025',25,1),
('INV-000026',26,1),
('INV-000027',27,1),
('INV-000028',28,2),
('INV-000029',29,2),
('INV-000030',30,4),
('INV-000031',1,1),
('INV-000032',2,1),
('INV-000033',3,1),
('INV-000034',5,1),
('INV-000035',6,1),
('INV-000036',7,1),
('INV-000037',8,1),
('INV-000038',9,1),
('INV-000039',11,2),
('INV-000040',15,2),
('INV-000041',16,1),
('INV-000042',17,1),
('INV-000043',19,1),
('INV-000044',25,1),
('INV-000045',26,4);

-- ---------------- Пользователи (хеш PBKDF2-SHA256) ----------------
INSERT INTO users (login, password_hash, salt, role_id, reader_id, employee_id) VALUES
('admin','67c97a0fac3dc94a1778561401476731a481be8ccc0b6f4d12610c9f27f6ddd2','45684e9fb230d6db65a66d6661030b85',1,NULL,1),
('librarian1','cf9fb5e8e22004e27de4b2617059fd36121e2084f87c766627e867d080766790','2648da4a7480447d3d26bec038672934',2,NULL,2),
('librarian2','b489a69c1eddd8d00d7dfc6d4f14551d4a6180156492901ca55264688f1751f8','90beda6d85cd9356f00b8a63a78b9683',2,NULL,3),
('reader01','121daaa029bcac3eae8c4870eea0f8f533031501fc9eaf8b348e7d8597ae3337','d84de38d5e0d3dd1e2899e303606a45c',3,1,NULL),
('reader02','287d076b4c4aad943e9f945a27f1211f927b2d64377081013f7cc58a4c06f9e5','650819ba317e83e8eae6d11de1afdbc4',3,2,NULL),
('reader03','1f18a75dbbfdf1999fbad8bf2d884fab6ff20d92e84982582c892281f2e2392d','c6ab6e614a82a3267b1aee40f8d7c1e7',3,3,NULL);

-- ---------------- Выдачи (30 записей, номера формирует триггер) ----------------
INSERT INTO loans (reader_id, copy_id, employee_id, issue_date, due_date, return_date) VALUES
(8,12,2,'2026-09-03','2026-10-03','2026-09-10'),
(15,23,3,'2026-09-05','2026-10-05','2026-09-17'),
(5,34,2,'2026-09-03','2026-10-03','2026-10-10'),
(29,45,3,'2026-09-09','2026-10-24','2026-10-02'),
(6,11,2,'2026-09-11','2026-10-11','2026-10-01'),
(13,22,3,'2026-09-13','2026-10-13','2026-10-04'),
(12,33,2,'2026-09-07','2026-10-07','2026-10-16'),
(27,44,3,'2026-09-17','2026-11-01','2026-09-26'),
(4,10,2,'2026-09-19','2026-10-19','2026-10-09'),
(11,21,3,'2026-09-21','2026-10-21','2026-09-26'),
(18,32,2,'2026-09-23','2026-10-23','2026-10-12'),
(21,43,3,'2026-08-13','2026-10-12','2026-10-21'),
(2,9,2,'2026-09-27','2026-10-27','2026-10-09'),
(9,20,3,'2026-09-29','2026-10-29','2026-10-17'),
(16,31,2,'2026-10-01','2026-10-31','2026-10-10'),
(2,42,3,'2026-09-16','2026-10-16','2026-10-19'),
(30,8,2,'2026-10-05','2026-11-19','2026-10-23'),
(7,19,3,'2026-10-07','2026-11-06','2026-10-31'),
(14,30,2,'2026-10-09','2026-11-08','2026-11-03'),
(21,41,3,'2026-10-11','2026-12-10','2026-11-02'),
(3,7,2,'2026-09-18','2026-10-18',NULL),
(6,18,3,'2026-09-19','2026-10-19',NULL),
(9,29,2,'2026-09-20','2026-10-20',NULL),
(19,40,3,'2026-10-25','2026-11-24',NULL),
(26,6,2,'2026-10-26','2026-12-25',NULL),
(3,17,3,'2026-10-27','2026-11-26',NULL),
(10,28,2,'2026-10-28','2026-11-27',NULL),
(17,39,3,'2026-10-29','2026-11-28',NULL),
(24,5,2,'2026-10-30','2026-12-29',NULL),
(1,16,3,'2026-10-31','2026-11-30',NULL);

-- ---------------- Штрафы (4 записи) ----------------
INSERT INTO fines (loan_id, amount, accrued_on, paid_on) VALUES
(3,35.00,'2026-10-10',NULL),
(7,45.00,'2026-10-16',NULL),
(12,45.00,'2026-10-21',NULL),
(16,15.00,'2026-10-19','2026-10-21');

-- ---------------- Бронирования (20 записей, номера формирует триггер) ----------------
INSERT INTO reservations (reader_id, edition_id, reserved_on, status_id) VALUES
(6,4,'2026-10-02',1),
(11,7,'2026-10-03',1),
(16,10,'2026-10-04',1),
(21,13,'2026-10-05',1),
(26,16,'2026-10-06',1),
(1,19,'2026-10-07',1),
(6,22,'2026-10-08',1),
(11,25,'2026-10-09',1),
(16,28,'2026-10-10',2),
(21,1,'2026-10-11',2),
(26,4,'2026-10-12',2),
(1,7,'2026-10-13',2),
(6,10,'2026-10-14',2),
(11,13,'2026-10-15',2),
(16,16,'2026-10-16',2),
(21,19,'2026-10-17',2),
(26,22,'2026-10-18',3),
(1,25,'2026-10-19',3),
(6,28,'2026-10-20',3),
(11,1,'2026-10-21',3);

-- ---------------- Представления для форм приложения ----------------
CREATE VIEW v_editions_availability AS
SELECT e.edition_id, e.title, e.author, e.pub_year, e.isbn,
       COUNT(c.copy_id) AS total_copies,
       COALESCE(SUM(c.status_id = 1), 0) AS free_copies
FROM editions e LEFT JOIN copies c ON c.edition_id = e.edition_id AND c.status_id <> 4
GROUP BY e.edition_id, e.title, e.author, e.pub_year, e.isbn;

CREATE VIEW v_loans_full AS
SELECT l.loan_id, l.loan_number, r.reader_id, r.ticket_number,
       CONCAT(r.last_name, ' ', r.first_name) AS reader_name,
       c.copy_id, c.inventory_number, e.title,
       CONCAT(em.last_name, ' ', LEFT(em.first_name, 1), '.') AS employee_name,
       l.issue_date, l.due_date, l.return_date,
       CASE WHEN l.return_date IS NOT NULL THEN 'Возвращена'
            WHEN l.due_date < CURRENT_DATE THEN 'Просрочена'
            ELSE 'На руках' END AS loan_state
FROM loans l
JOIN readers r ON r.reader_id = l.reader_id
JOIN copies c ON c.copy_id = l.copy_id
JOIN editions e ON e.edition_id = c.edition_id
JOIN employees em ON em.employee_id = l.employee_id;

CREATE VIEW v_reservations_full AS
SELECT rs.reservation_id, rs.reservation_number, r.reader_id, r.ticket_number,
       CONCAT(r.last_name, ' ', r.first_name) AS reader_name,
       e.edition_id, e.title, rs.reserved_on, st.status_name
FROM reservations rs
JOIN readers r ON r.reader_id = rs.reader_id
JOIN editions e ON e.edition_id = rs.edition_id
JOIN reservation_statuses st ON st.status_id = rs.status_id;

CREATE VIEW v_fines_full AS
SELECT f.fine_id, f.loan_id, l.loan_number, r.reader_id, r.ticket_number,
       CONCAT(r.last_name, ' ', r.first_name) AS reader_name,
       f.amount, f.accrued_on, f.paid_on,
       CASE WHEN f.paid_on IS NULL THEN 'Не оплачен' ELSE 'Оплачен' END AS fine_state
FROM fines f
JOIN loans l ON l.loan_id = f.loan_id
JOIN readers r ON r.reader_id = l.reader_id;
