<p align="center">
  <img src="wwwroot/assets/logo-uniandes.png" alt="Universidad UNIANDES Inspira" width="320">
</p>

# ENUNCIADO DE LA TAREA: Usted debe crear todos los procedimientos necesarios para crear mediante  C# el CRUD de la tabla clientes en un entorno web minimo 10 campos

# CRUD de Clientes — C# (ASP.NET Core) + JavaScript + MySQL

Aplicación web para crear, consultar, actualizar y eliminar clientes. La tabla `clientes` tiene **13 campos** y todas las operaciones sobre la base de datos se hacen mediante **procedimientos almacenados**.

## Arquitectura

```text
Navegador (HTML + JavaScript)  ──fetch/JSON──▶  API REST en C#  ──procedimientos──▶  MySQL / MariaDB
   wwwroot/                                       Controllers/                          sql/esquema.sql
                                                  Repositories/
                                                  Models/
```

```text
ClientesWeb/
├── Program.cs                          # Arranque: servicios, archivos estáticos y rutas
├── appsettings.json                    # Cadena de conexión y puerto (5080)
├── Models/Cliente.cs                   # Objeto Cliente: 13 campos y validaciones
├── Repositories/
│   ├── IClienteRepository.cs           # Contrato del CRUD
│   ├── ClienteRepository.cs            # CRUD: invoca los procedimientos almacenados
│   └── Excepciones.cs                  # Cliente no encontrado / duplicado
├── Controllers/ClientesController.cs   # API REST /api/clientes
├── sql/esquema.sql                     # Base de datos, tabla y procedimientos
└── wwwroot/                            # Frontend
    ├── index.html
    ├── css/style.css
    └── js/
        ├── api.js                      # Llamadas fetch a la API
        └── app.js                      # Tabla, búsqueda, formulario, detalle y eliminación
```

## Campos de la tabla `clientes`

| # | Campo | Tipo | Regla |
|---|---|---|---|
| 1 | `id` | INT, autoincremental | Clave primaria |
| 2 | `cedula` | CHAR(10) | 10 dígitos, **única** |
| 3 | `nombres` | VARCHAR(80) | Obligatorio |
| 4 | `apellidos` | VARCHAR(80) | Obligatorio |
| 5 | `fecha_nacimiento` | DATE | Obligatoria, no futura |
| 6 | `genero` | CHAR(1) | M, F u O |
| 7 | `email` | VARCHAR(150) | Correo válido |
| 8 | `telefono` | VARCHAR(10) | 7 a 10 dígitos |
| 9 | `direccion` | VARCHAR(200) | Opcional |
| 10 | `ciudad` | VARCHAR(80) | Obligatoria |
| 11 | `provincia` | VARCHAR(80) | Obligatoria |
| 12 | `activo` | TINYINT(1) | Activo / inactivo |
| 13 | `fecha_registro` | DATETIME | La asigna la base de datos |

## Procedimientos almacenados

| Operación | Procedimiento | Qué hace |
|---|---|---|
| Create | `sp_cliente_crear(...)` | Inserta y devuelve el id. Error si la cédula ya existe |
| Read | `sp_cliente_obtener(p_id)` | Devuelve un cliente |
| Read | `sp_cliente_listar(p_busqueda)` | Lista todos, o filtra por cédula, nombres, apellidos, correo o ciudad |
| Update | `sp_cliente_actualizar(p_id, ...)` | Actualiza. Error si no existe o la cédula es de otro cliente |
| Delete | `sp_cliente_eliminar(p_id)` | Elimina. Error si no existe |

Los errores se lanzan con `SIGNAL` y la API los traduce: `45000` → HTTP 409 (duplicado), `45404` → HTTP 404 (no encontrado).

## API REST

| Método | Ruta | Respuesta |
|---|---|---|
| GET | `/api/clientes?q=texto` | 200 con la lista |
| GET | `/api/clientes/{id}` | 200 con el cliente, o 404 |
| POST | `/api/clientes` | 201 con el cliente creado, 400 (datos inválidos) o 409 (cédula repetida) |
| PUT | `/api/clientes/{id}` | 200 con el cliente actualizado, 400, 404 o 409 |
| DELETE | `/api/clientes/{id}` | 204, o 404 |

## Cómo ejecutarlo

**Requisitos:** [.NET 8 SDK](https://dotnet.microsoft.com/download) y XAMPP (solo MySQL/MariaDB; phpMyAdmin necesita Apache únicamente para importar el esquema).

1. **Iniciar MySQL** en el panel de XAMPP (y Apache, si va a usar phpMyAdmin).
2. **Importar la base de datos:** en phpMyAdmin → **Importar** → elegir `sql/esquema.sql` → **Importar**. Debe aparecer `crud_clientes_web` con la tabla `clientes` y los 5 procedimientos (sección *Rutinas*).
3. **Revisar la conexión** en `appsettings.json`. Por defecto: `root` sin clave, `localhost:3306`. Si su MySQL tiene clave, cámbiela en `Password=`.
4. **Ejecutar:**
   ```bash
   cd ClientesWeb
   dotnet run
   ```
5. Abrir **http://localhost:5080**.

Si cambia el puerto de la aplicación, edite `"Urls"` en `appsettings.json`.

## Seguridad y buenas prácticas

- Procedimientos almacenados con parámetros tipados (sin concatenar SQL → sin inyección SQL).
- Validación en el navegador (respuesta inmediata) **y** en el servidor (la que cuenta).
- El frontend inserta los datos con `textContent`, nunca con `innerHTML` (sin XSS).
- Los errores inesperados no exponen detalles internos del servidor.
