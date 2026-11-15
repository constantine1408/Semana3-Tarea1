-- ============================================================================
--  CRUD de clientes (ASP.NET Core + JavaScript)  —  MySQL / MariaDB
--  Tabla de 13 campos y 5 procedimientos almacenados.
--
--  Importar desde phpMyAdmin (pestaña "Importar"), o con:
--      mysql -u root -p < sql/esquema.sql
-- ============================================================================

CREATE DATABASE IF NOT EXISTS crud_clientes_web
    CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE crud_clientes_web;

-- ----------------------------------------------------------------------------
--  Tabla clientes
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS clientes (
    id               INT          NOT NULL AUTO_INCREMENT,
    cedula           CHAR(10)     NOT NULL,
    nombres          VARCHAR(80)  NOT NULL,
    apellidos        VARCHAR(80)  NOT NULL,
    fecha_nacimiento DATE         NOT NULL,
    genero           CHAR(1)      NOT NULL,                       -- M, F u O
    email            VARCHAR(150) NOT NULL,
    telefono         VARCHAR(10)  NOT NULL,
    direccion        VARCHAR(200) NOT NULL DEFAULT '',
    ciudad           VARCHAR(80)  NOT NULL,
    provincia        VARCHAR(80)  NOT NULL,
    activo           TINYINT(1)   NOT NULL DEFAULT 1,
    fecha_registro   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uq_clientes_cedula (cedula)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ----------------------------------------------------------------------------
--  Procedimientos almacenados
--
--  Errores que lanzan (la API en C# los traduce a respuestas HTTP):
--      SQLSTATE '45000'  → conflicto (cédula repetida)      → HTTP 409
--      SQLSTATE '45404'  → cliente no encontrado            → HTTP 404
-- ----------------------------------------------------------------------------
DELIMITER $$

-- CREATE ---------------------------------------------------------------------
DROP PROCEDURE IF EXISTS sp_cliente_crear$$
CREATE PROCEDURE sp_cliente_crear(
    IN p_cedula           CHAR(10),
    IN p_nombres          VARCHAR(80),
    IN p_apellidos        VARCHAR(80),
    IN p_fecha_nacimiento DATE,
    IN p_genero           CHAR(1),
    IN p_email            VARCHAR(150),
    IN p_telefono         VARCHAR(10),
    IN p_direccion        VARCHAR(200),
    IN p_ciudad           VARCHAR(80),
    IN p_provincia        VARCHAR(80),
    IN p_activo           TINYINT(1)
)
BEGIN
    IF EXISTS (SELECT 1 FROM clientes WHERE cedula = p_cedula) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Ya existe un cliente con esa cédula.';
    END IF;

    INSERT INTO clientes
        (cedula, nombres, apellidos, fecha_nacimiento, genero, email,
         telefono, direccion, ciudad, provincia, activo)
    VALUES
        (p_cedula, p_nombres, p_apellidos, p_fecha_nacimiento, p_genero, p_email,
         p_telefono, p_direccion, p_ciudad, p_provincia, p_activo);

    SELECT LAST_INSERT_ID() AS id;
END$$

-- READ (uno) -----------------------------------------------------------------
DROP PROCEDURE IF EXISTS sp_cliente_obtener$$
CREATE PROCEDURE sp_cliente_obtener(IN p_id INT)
BEGIN
    SELECT id, cedula, nombres, apellidos, fecha_nacimiento, genero, email,
           telefono, direccion, ciudad, provincia, activo, fecha_registro
    FROM clientes
    WHERE id = p_id;
END$$

-- READ (lista, con búsqueda opcional) ----------------------------------------
DROP PROCEDURE IF EXISTS sp_cliente_listar$$
CREATE PROCEDURE sp_cliente_listar(IN p_busqueda VARCHAR(100))
BEGIN
    SELECT id, cedula, nombres, apellidos, fecha_nacimiento, genero, email,
           telefono, direccion, ciudad, provincia, activo, fecha_registro
    FROM clientes
    WHERE p_busqueda IS NULL
       OR p_busqueda = ''
       OR cedula    LIKE CONCAT('%', p_busqueda, '%')
       OR nombres   LIKE CONCAT('%', p_busqueda, '%')
       OR apellidos LIKE CONCAT('%', p_busqueda, '%')
       OR email     LIKE CONCAT('%', p_busqueda, '%')
       OR ciudad    LIKE CONCAT('%', p_busqueda, '%')
    ORDER BY apellidos, nombres;
END$$

-- UPDATE ---------------------------------------------------------------------
DROP PROCEDURE IF EXISTS sp_cliente_actualizar$$
CREATE PROCEDURE sp_cliente_actualizar(
    IN p_id               INT,
    IN p_cedula           CHAR(10),
    IN p_nombres          VARCHAR(80),
    IN p_apellidos        VARCHAR(80),
    IN p_fecha_nacimiento DATE,
    IN p_genero           CHAR(1),
    IN p_email            VARCHAR(150),
    IN p_telefono         VARCHAR(10),
    IN p_direccion        VARCHAR(200),
    IN p_ciudad           VARCHAR(80),
    IN p_provincia        VARCHAR(80),
    IN p_activo           TINYINT(1)
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM clientes WHERE id = p_id) THEN
        SIGNAL SQLSTATE '45404' SET MESSAGE_TEXT = 'No se encontró el cliente.';
    END IF;

    IF EXISTS (SELECT 1 FROM clientes WHERE cedula = p_cedula AND id <> p_id) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Ya existe otro cliente con esa cédula.';
    END IF;

    UPDATE clientes
    SET cedula           = p_cedula,
        nombres          = p_nombres,
        apellidos        = p_apellidos,
        fecha_nacimiento = p_fecha_nacimiento,
        genero           = p_genero,
        email            = p_email,
        telefono         = p_telefono,
        direccion        = p_direccion,
        ciudad           = p_ciudad,
        provincia        = p_provincia,
        activo           = p_activo
    WHERE id = p_id;
END$$

-- DELETE ---------------------------------------------------------------------
DROP PROCEDURE IF EXISTS sp_cliente_eliminar$$
CREATE PROCEDURE sp_cliente_eliminar(IN p_id INT)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM clientes WHERE id = p_id) THEN
        SIGNAL SQLSTATE '45404' SET MESSAGE_TEXT = 'No se encontró el cliente.';
    END IF;

    DELETE FROM clientes WHERE id = p_id;
END$$

DELIMITER ;
