# MediTurno — API de Gestión de Citas Médicas

API REST para la programación, seguimiento y control de citas médicas en un
centro de atención ambulatoria.

Proyecto Final — **Programación III**, Instituto Tecnológico de Las Américas (ITLA).
Sustentante: **Josue Hidalgo** (2025-1065) · Facilitador: **Kelyn Tejada**.

---

## Estado

| | |
|---|---|
| Pruebas automatizadas | **133 / 133 en verde** |
| Cobertura de líneas | **94.35 %** (objetivo ≥ 70 %) |
| Cobertura de ramas | **77.72 %** |
| Endpoints publicados | 22 |

---

## Tecnología

| Área | Herramienta |
|---|---|
| Lenguaje | C# 12 |
| Framework | ASP.NET Core Web API (.NET 10 LTS) |
| ORM | Entity Framework Core 10 |
| Base de datos | SQL Server 2022 Express |
| Autenticación | JWT Bearer + `PasswordHasher` (PBKDF2) |
| Documentación | Swagger / OpenAPI |
| Pruebas | xUnit · Moq · FluentAssertions · `WebApplicationFactory` · SQLite en memoria |
| Cobertura | Coverlet |
| Integración continua | GitHub Actions |

---

## Estructura

```
MediTurno/
├── src/MediTurno.Api/
│   ├── Common/          Resultado de operación y opciones de JWT
│   ├── Controllers/     Endpoints REST
│   ├── Data/            DbContext y datos semilla
│   ├── Dtos/            Contratos de entrada y salida
│   ├── Entities/        Modelo de dominio
│   ├── Middleware/      Manejador global de excepciones
│   └── Services/        Reglas de negocio
└── tests/MediTurno.Tests/
    ├── Unit/            Pruebas de reglas de negocio
    ├── Integration/     Pruebas de endpoints
    ├── Performance/     Prueba de carga
    └── Helpers/         Escenarios y fábrica de la API
```

---

## Cómo ejecutar

**Requisitos:** .NET 10 SDK y SQL Server (instancia local).

```bash
dotnet run --project src/MediTurno.Api
```

La base de datos se crea automáticamente al arrancar y se cargan los datos
semilla. Swagger UI queda disponible en `https://localhost:<puerto>/swagger`.

## Cómo ejecutar las pruebas

```bash
dotnet test
```

Con reporte de cobertura:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

---

## Usuarios de prueba

| Rol | Correo | Contraseña |
|---|---|---|
| Administrador | `admin@mediturno.do` | `Admin123*` |
| Recepcionista | `recepcion@mediturno.do` | `Recepcion123*` |
| Médico | `carmen.reyes@mediturno.do` | `Medico123*` |

Autentíquese en `POST /api/auth/login` y use el token devuelto en el botón
**Authorize** de Swagger.

---

## Endpoints

### Seguridad
| Método | Ruta | Rol |
|---|---|---|
| POST | `/api/auth/login` | Público |
| POST | `/api/usuarios` | Administrador |
| GET | `/api/usuarios` | Administrador |

### Pacientes
| Método | Ruta | Rol |
|---|---|---|
| POST | `/api/pacientes` | Administrador, Recepcionista |
| GET | `/api/pacientes` | Todos |
| GET | `/api/pacientes/{id}` | Todos |
| PUT | `/api/pacientes/{id}` | Administrador, Recepcionista |
| DELETE | `/api/pacientes/{id}` | Administrador, Recepcionista |
| GET | `/api/pacientes/{id}/historial` | Administrador, Médico |

### Médicos y agenda
| Método | Ruta | Rol |
|---|---|---|
| POST | `/api/especialidades` | Administrador |
| GET | `/api/especialidades` | Todos |
| DELETE | `/api/especialidades/{id}` | Administrador |
| POST | `/api/medicos` | Administrador |
| GET | `/api/medicos` | Todos |
| GET | `/api/medicos/{id}` | Todos |
| PUT | `/api/medicos/{id}/horarios` | Administrador |
| GET | `/api/medicos/{id}/horarios` | Todos |
| GET | `/api/medicos/{id}/disponibilidad` | Todos |

### Citas y atención
| Método | Ruta | Rol |
|---|---|---|
| POST | `/api/citas` | Administrador, Recepcionista |
| GET | `/api/citas` | Todos |
| GET | `/api/citas/{id}` | Todos |
| PUT | `/api/citas/{id}/reprogramar` | Administrador, Recepcionista |
| PUT | `/api/citas/{id}/cancelar` | Administrador, Recepcionista |
| PUT | `/api/citas/{id}/confirmar` | Administrador, Recepcionista |
| POST | `/api/citas/marcar-ausentes` | Administrador, Recepcionista |
| POST | `/api/citas/{id}/atencion` | Administrador, Médico |

### Reportes
| Método | Ruta | Rol |
|---|---|---|
| GET | `/api/reportes/citas` | Administrador |
| GET | `/api/reportes/medicos/{id}` | Administrador |

---

## Reglas de negocio

1. Un médico no puede tener dos citas vigentes en el mismo bloque horario.
2. Un paciente no puede tener dos citas vigentes en el mismo horario.
3. Las citas deben reservarse con al menos **2 horas** de antelación.
4. El horario solicitado debe coincidir con un bloque generado a partir del
   horario del médico y la duración de su consulta.
5. Una cita cancelada libera su bloque, que vuelve a estar disponible.
6. Solo el médico asignado —o un administrador— puede registrar la atención.
7. Ciclo de vida de la cita:

```
Pendiente ──confirmar──> Confirmada ──atender──> Atendida
    │                         │
    ├──cancelar──> Cancelada  ├──cancelar──> Cancelada
    └──reprogramar──┐         └──no asistir──> Ausente
                    └─> Pendiente
```

Cualquier transición fuera de este diagrama devuelve `409 Conflict`.
