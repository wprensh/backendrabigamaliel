# Backend · I.E. Rabí Gamaliel

API REST de la plataforma web y académica de la **Institución Educativa Rabí Gamaliel**.
Construida con **.NET 9 Minimal APIs**, **Arquitectura Limpia**, **Entity Framework Core 9** (Code-First) y **PostgreSQL**.

Frontend: [frontendrabigamaliel](https://github.com/wprensh/frontendrabigamaliel)

## Arquitectura

```
backendrabigamaliel/
├── src/
│   ├── RabiGamaliel.Domain/            # Entidades y reglas de negocio (sin dependencias)
│   │   ├── Common/Entity.cs            #   Base: Id Guid v7 + fechas de auditoría
│   │   ├── Noticias/                   #   Noticia, CategoriaNoticia
│   │   ├── Certificaciones/            #   Certificado, TipoCertificado, TipoDocumento
│   │   ├── Usuarios/                   #   Usuario, Permiso (módulo + acción)
│   │   ├── Campus/                     #   MiembroCampus (estudiante o docente), GradosEscolares
│   │   ├── Elecciones/                 #   Eleccion, Candidato, Voto, ParticipacionVoto, ReglasCargo
│   │   └── Pqrs/                       #   SolicitudPqrs, TipoPqrs, EstadoPqrs
│   ├── RabiGamaliel.Application/       # Casos de uso (servicios), DTOs, validaciones, puertos
│   │   ├── Abstractions/IUnitOfWork.cs
│   │   ├── Common/                     #   Result<T>, Error, PagedResult<T>
│   │   ├── Noticias/                   #   INoticiaService, NoticiaService, INoticiaRepository, validador
│   │   └── Pqrs/
│   ├── RabiGamaliel.Infrastructure/    # EF Core 9 + PostgreSQL (adaptadores)
│   │   └── Persistence/
│   │       ├── ApplicationDbContext.cs
│   │       ├── Configurations/         #   Fluent API por entidad
│   │       ├── Interceptors/           #   Auditoría automática (SaveChangesInterceptor)
│   │       ├── Repositories/
│   │       ├── Seed/                   #   Datos iniciales (UseAsyncSeeding de EF Core 9)
│   │       └── Migrations/
│   └── RabiGamaliel.Api/               # Minimal APIs, DI, CORS, ProblemDetails, OpenAPI
│       ├── Endpoints/                  #   NoticiasEndpoints, PqrsEndpoints
│       └── Program.cs
├── tests/RabiGamaliel.Application.Tests/   # Pruebas unitarias (xUnit)
├── db/InicialColegio.sql               # Script SQL idempotente generado desde las migraciones
└── docker-compose.yml                  # PostgreSQL 17 local
```

Las dependencias apuntan hacia adentro: `Api → Infrastructure → Application → Domain`.
La capa de aplicación define interfaces (`INoticiaRepository`, `IUnitOfWork`) y la infraestructura las implementa.

## Requisitos

- [.NET SDK 9](https://dotnet.microsoft.com/download/dotnet/9.0)
- PostgreSQL 15+ (o Docker)

## Ejecutar en local

```bash
# 1. Base de datos
docker compose up -d

# 2. Herramientas locales (dotnet-ef)
dotnet tool restore

# 3. API (en Development aplica las migraciones y los datos iniciales al arrancar)
dotnet run --project src/RabiGamaliel.Api
```

- Documentación interactiva (Scalar): http://localhost:5075/scalar/v1
- OpenAPI: http://localhost:5075/openapi/v1.json
- Salud: http://localhost:5075/health

## Configuración

La cadena de conexión está en `src/RabiGamaliel.Api/appsettings.json` (`ConnectionStrings:DefaultConnection`).
En producción **no** la guardes en el repositorio: usa una variable de entorno o un gestor de secretos.

```bash
# Variable de entorno (el doble guion bajo equivale a ":")
ConnectionStrings__DefaultConnection="Host=...;Database=...;Username=...;Password=..."
```

Los orígenes permitidos por CORS se configuran en `Cors:OrigenesPermitidos`.

## Migraciones (Code-First)

```bash
# Crear una migración después de cambiar el modelo
dotnet ef migrations add NombreDelCambio -p src/RabiGamaliel.Infrastructure -s src/RabiGamaliel.Api -o Persistence/Migrations

# Aplicarla
dotnet ef database update -p src/RabiGamaliel.Infrastructure -s src/RabiGamaliel.Api

# Script SQL idempotente para producción (db/Certificaciones.sql, db/Usuarios.sql, db/EleccionesCampus.sql, db/PlataformaAcademica.sql, db/Horarios.sql y db/BancoPreguntas.sql agregan cada tabla nueva)
dotnet ef migrations script -p src/RabiGamaliel.Infrastructure -s src/RabiGamaliel.Api --idempotent -o db/migracion.sql
```

## Endpoints

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/noticias?buscar=&categoria=&soloPublicadas=&pagina=&tamanoPagina=` | Lista paginada |
| GET | `/api/noticias/{id}` | Detalle |
| POST | `/api/noticias` | Crear |
| PUT | `/api/noticias/{id}` | Actualizar |
| DELETE | `/api/noticias/{id}` | Eliminar |
| POST | `/api/pqrs` | Radicar PQRS (formulario de contacto, limitado a 5/min por IP) |
| GET | `/api/pqrs?estado=` | Listar PQRS |
| PUT | `/api/pqrs/{id}/respuesta` | Responder PQRS |
| GET | `/api/certificados?buscar=&tipo=&pagina=&tamanoPagina=` | Listar certificados expedidos (busca por nombre, documento o código) |
| GET | `/api/certificados/{id}` | Detalle de un certificado |
| POST | `/api/certificados` | Expedir un certificado (asigna el código de verificación `CERT-AAAA-XXXXXX`) |
| PUT | `/api/certificados/{id}/anulacion` | Anular un certificado |
| GET | `/api/certificados/verificar/{codigo}` | Verificación pública (limitada a 20/min por IP; el documento se muestra parcialmente) |
| GET | `/api/auth/estado` | Indica si falta crear el primer superadministrador |
| POST | `/api/auth/configuracion-inicial` | Crea el primer superadministrador (solo sin usuarios y con `X-Clave-Admin`) |
| POST | `/api/auth/iniciar-sesion` | Usuario y contraseña → token (limitado a 10/min por IP) |
| GET | `/api/auth/perfil` | Usuario de la sesión y sus permisos |
| PUT | `/api/auth/perfil/clave` | Cambiar la propia contraseña |
| GET | `/api/usuarios?buscar=&pagina=&tamanoPagina=` | Listar usuarios |
| GET | `/api/usuarios/permisos` | Módulos y acciones asignables |
| GET | `/api/usuarios/{id}` | Detalle de un usuario |
| POST | `/api/usuarios` | Crear usuario con sus permisos |
| PUT | `/api/usuarios/{id}` | Actualizar datos, estado y permisos |
| PUT | `/api/usuarios/{id}/clave` | Restablecer la contraseña (cierra sus sesiones y lo desbloquea) |
| DELETE | `/api/usuarios/{id}` | Eliminar usuario |
| GET | `/api/comunidad?buscar=&rol=&grado=&grupo=&pagina=&tamanoPagina=` | Estudiantes y docentes del campus |
| POST | `/api/comunidad` | Registrar un estudiante o docente |
| POST | `/api/comunidad/importar` | Crear o actualizar desde una lista (documento, nombre, grado, grupo) |
| PUT | `/api/comunidad/{id}` | Actualizar nombre, grado, grupo y estado |
| POST | `/api/comunidad/claves` | Generar claves de acceso (se devuelven una sola vez, para imprimirlas) |
| DELETE | `/api/comunidad/{id}` | Eliminar (solo sin historial electoral) |
| GET/POST | `/api/elecciones` | Listar y crear elecciones |
| GET/PUT/DELETE | `/api/elecciones/{id}` | Ver, editar o eliminar (eliminar solo sin votos) |
| GET/POST | `/api/elecciones/{id}/candidatos` | Candidatos y su inscripción con perfil (lema, propuestas, foto) |
| PUT/DELETE | `/api/elecciones/{id}/candidatos/{candidatoId}` | Editar el perfil o retirar al candidato |
| GET | `/api/elecciones/{id}/aspirantes?cargo=&buscar=` | Estudiantes que cumplen el grado del cargo |
| GET | `/api/elecciones/{id}/resultados` | Conteo en vivo y participación por grado |
| GET | `/api/academico/horario?anio=&docenteId=&grado=&grupo=` | Horario de un docente o de un curso |
| POST · PUT · DELETE | `/api/academico/horario[/{id}]` | Ubicar, mover o quitar una clase del horario (sin cruces) |
| GET | `/api/campus/horario?estudianteId=` | Horario del docente, del estudiante o del hijo del acudiente |
| POST | `/api/campus/iniciar-sesion` | Ingreso al campus con documento y clave de acceso |
| GET | `/api/campus/elecciones` | Elecciones recientes y si el estudiante ya votó |
| GET | `/api/campus/elecciones/{id}/tarjeton` | Candidatos de las contiendas del estudiante |
| POST | `/api/campus/elecciones/{id}/votar` | Voto secreto: uno por cargo o en blanco |
| GET | `/api/campus/elecciones/{id}/resultados` | Resultados al cierre (docentes: participación durante la jornada) |
| GET | `/health` | Estado de la API y de PostgreSQL |

Los errores siguen el estándar **ProblemDetails (RFC 9457)**; los de validación incluyen el diccionario `errors` por campo.
Los enums viajan como texto (`"Cultural"`, `"Academica"`, ...).

## Características de C# 13 / .NET 9 usadas

- **Constructores primarios** en servicios, repositorios, DbContext e interceptores.
- **Expresiones de colección** (`[]`, `[.. items]`).
- **Colecciones `params` (C# 13)** en `Error.Validacion(params IEnumerable<...>)`.
- **`Guid.CreateVersion7()` (.NET 9)**: ids ordenables por tiempo, mejores para índices B-tree.
- **OpenAPI nativo** (`AddOpenApi` / `MapOpenApi`) sin Swashbuckle, con **Scalar** como UI.
- **EF Core 9 `UseSeeding` / `UseAsyncSeeding`** para los datos iniciales.
- **`TypedResults` + `Results<...>`**: respuestas tipadas y documentadas automáticamente.
- `TimeProvider`, `IExceptionHandler`, `LoggerMessage` (logging generado en compilación), rate limiting nativo.

> Sobre los **interceptores de C#** (característica del compilador): no se usan directamente porque son una
> herramienta para generadores de código; ASP.NET Core ya los aprovecha internamente con el Request Delegate
> Generator en escenarios AOT. En este proyecto sí se usa un **interceptor de EF Core** para la auditoría.

## Pruebas

```bash
dotnet test
```

## Usuarios y permisos

El panel administrativo se usa con usuario y contraseña. Cada usuario tiene, por módulo (`Noticias`, `Certificados`,
`Pqrs`, `Usuarios`), los permisos **Ver**, **Crear**, **Editar** y **Eliminar**; crear, editar o eliminar incluyen ver.
El superadministrador tiene todos. En certificados, *Eliminar* equivale a anular (un certificado nunca se borra).

- `POST /api/auth/iniciar-sesion` devuelve un token JWT (8 horas, `Autenticacion:DuracionSesionHoras`) que el
  frontend envía como `Authorization: Bearer ...`.
- Cada petición vuelve a leer el usuario: desactivarlo, cambiarle los permisos o restablecer su contraseña
  surte efecto de inmediato en sus sesiones abiertas.
- Las contraseñas se guardan con el hasher de ASP.NET Core Identity (PBKDF2). Mínimo 8 caracteres con letras y números.
- Tras 5 contraseñas erradas seguidas la cuenta se bloquea 15 minutos.
- Siempre queda al menos un superadministrador activo, y nadie puede desactivarse ni eliminarse a sí mismo.
- Para agregar un módulo nuevo: un valor más en `ModuloSistema` y `.RequierePermiso(ModuloSistema.X, AccionPermiso.Y)`
  en sus endpoints.

### Primer superadministrador y clave de emergencia

Mientras no exista ningún usuario, la página `/admin/login` del sitio pide la clave de administración
(`Administracion:ClaveApi`, mínimo 16 caracteres) para crear el primer superadministrador. Después la clave sigue
funcionando en el encabezado `X-Clave-Admin` como llave de emergencia con acceso total; si no está configurada,
nadie tiene ese acceso.

Los tokens se firman con `Autenticacion:ClaveFirma` (mínimo 32 caracteres). Si no se configura, la firma se deriva
de `Administracion:ClaveApi`, así que no hace falta una variable nueva en Render.

En desarrollo la clave se guarda con *user-secrets* (fuera del repositorio):

```bash
dotnet user-secrets set "Administracion:ClaveApi" "una-clave-larga-y-privada" --project src/RabiGamaliel.Api
dotnet user-secrets list --project src/RabiGamaliel.Api
```

## Elecciones escolares y campus virtual

- **Comunidad** (permiso `Campus`): estudiantes y docentes que entran al campus con su número de documento y una
  clave de acceso de 8 caracteres (`ABCD-2345`) que genera el colegio. Las claves solo se ven al generarlas, para
  imprimirlas; generar otra invalida la anterior. Se pueden importar listas completas (p. ej. desde el SIMAT).
- **Elecciones** (permiso `Elecciones`): cada jornada tiene horario de apertura y cierre y los cargos que se eligen.
  Reglas por cargo (`ReglasCargo`): personero de 11°; contralor de 10° u 11°; un vocero por grado (transición a 3°
  eligen uno solo entre los de 3°); representante de cada curso. El representante ante el Consejo Directivo no se
  vota aquí: lo elige el Consejo de Estudiantes.
- **Voto secreto**: la tabla `participaciones_voto` dice quién votó y `votos` por quién, sin relación entre ellas ni
  fecha en el voto (id aleatorio). Un índice único impide votar dos veces. Si el voto en blanco supera a todos los
  candidatos de una contienda, no hay ganador.
- Con votos registrados no se agregan ni retiran candidatos o cargos, y la elección no se puede eliminar.
- Los tokens del campus llevan otra audiencia (`campus-virtual`) y duran 60 minutos
  (`Autenticacion:DuracionSesionCampusMinutos`); no sirven en el panel. El ingreso admite 120 intentos por minuto por
  IP (la sala de informática comparte IP) y bloquea cada cuenta 15 minutos tras 5 claves erradas.

## Plataforma académica

Módulos agregados en noviembre de 2026. Cada uno tiene su permiso en el catálogo del panel.

- **Acudientes** (rol `Acudiente` del campus, permiso `Campus`): se vinculan a uno o más estudiantes
  (`/api/comunidad/{id}/vinculos`, importación masiva en `/api/comunidad/acudientes/importar`). En el campus ven
  boletines, asistencia, observador, circulares y carné de sus hijos. Correo y teléfono son opcionales.
- **Académico** (permiso `Academico`): asignaturas, periodos (con porcentaje, notas abiertas/cerradas y publicación),
  asignación docente por curso. Escala del Decreto 1290: Superior ≥ 4,6; Alto ≥ 4,0; Básico ≥ 3,0; Bajo. El docente
  registra notas y fallas en `/api/campus/docente/clases/...`; el boletín (con acumulado ponderado y puesto) solo se
  ve cuando el periodo está publicado, y al publicarlo se avisa a las familias.
- **Horario semanal** (permiso `Academico`): cada clase de la carga docente se ubica en un día (lunes a sábado) y una
  franja entre 5:00 a. m. y 10:00 p. m. (`/api/academico/horario`). No se permiten cruces: un docente no puede estar
  en dos cursos a la vez, ni un curso tener dos clases a la vez (una clase de todo el grado ocupa a todos sus grupos).
  La respuesta compara las horas programadas con la intensidad horaria de cada asignación. En el campus,
  `GET /api/campus/horario` muestra el del docente, el del curso del estudiante o el del estudiante del acudiente
  (`?estudianteId=`). Referencia: 22 horas efectivas semanales de asignación académica por docente de secundaria y
  media (Decreto 1850 de 2002, compilado en el Decreto 1075 de 2015); la intensidad de cada área la fija el PEI.
- **Asistencia**: el docente toma lista del día o de los 7 días anteriores (hora de Colombia). Cada ausencia o
  llegada tarde crea un aviso para el acudiente (uno por día) y, si es ausencia y hay correo, un correo. La respuesta
  trae el teléfono del acudiente para que el frontend abra WhatsApp (`wa.me`); la API de WhatsApp de pago no está
  incluida.
- **Asistencia y observador** (permiso `Convivencia`): reporte de inasistencias por curso (también con `Academico`) y observador del estudiante con anotaciones positivas, observaciones y
  situaciones tipo I, II y III (Ley 1620 de 2013), seguimientos y estado del caso. El acudiente marca "enterado".
- **Simulacros** (permiso `Simulacros`): pruebas tipo Saber 11 y Saber 3°, 5° y 9° con preguntas de selección
  múltiple por área, tiempo límite, guardado de cada respuesta y entrega automática al agotarse el tiempo. El puntaje
  global de Saber 11 se estima como ((LC + M + SC + CN) × 3 + Inglés) / 13 × 5 sobre 500.
  - **Banco de preguntas** (`/api/simulacros/banco`, mismo permiso): preguntas reutilizables por área, competencia,
    grados y dificultad. `POST /api/simulacros/{id}/preguntas/banco` las copia al simulacro, elegidas
    (`preguntaIds`) o al azar (`cantidad`, repartidas por igual entre las áreas de la prueba), sin repetir.
    Corregir el banco no cambia simulacros ya armados.
  - `db/BancoSaber11.sql` carga 40 preguntas originales tipo Saber 11 (8 por área) y `db/PruebaSaber11Grado11.sql`
    crea la prueba de grado 11 con 20 de ellas (4 por área). Ambos son idempotentes.
- **Circulares** (permiso `Circulares`): numeradas por año ("Circular 001 de 2026") para estudiantes, docentes,
  acudientes o todos, por grados, con confirmación de lectura y lista de pendientes.
- **Calendario escolar** (permiso `Calendario`): eventos públicos (`GET /api/calendario`, sin sesión) e internos del campus.
- **Admisiones** (permiso `Admisiones`): pre-matrícula pública (`POST /api/admisiones`) con radicado `ADM-XXXXXX`,
  consulta de estado y carga de documentos (PDF, JPG o PNG de hasta 3 MB, guardados en la base de datos). Al
  matricular se crean el estudiante, el acudiente y su vínculo en el campus.
- **Carné con QR** (permiso `Carnets` para listar e imprimir; también con `Campus`): cada miembro del campus tiene un
  código de carné único; `GET /api/carnets/{codigo}` (público) confirma si el carné está vigente, sin exponer el
  documento completo.
- **Indicadores** (permiso `Indicadores`): tablero del rector con comunidad, asistencia del día y de 30 días,
  rendimiento académico, convivencia, simulacros, admisiones y circulares.

## Siete mejoras (noviembre de 2026)

- **App instalable y notificaciones push**: el sitio es una PWA con el escudo como ícono. Cada aviso del campus
  (`INotificador`) también se envía por Web Push a los dispositivos suscritos (`/api/campus/push`). Las suscripciones
  vencidas se borran solas.
- **Alerta temprana** (`GET /api/indicadores/alertas`, permiso `Indicadores`, `Convivencia` o `Evaluacion`): puntaje de
  riesgo de deserción y de pérdida del año con inasistencias de 30 días y del año, asignaturas perdidas en el periodo y
  en el acumulado, promedio, situaciones tipo I/II/III abiertas y el último simulacro. Los umbrales están en
  `CalculoRiesgo`.
- **Aula virtual** (campus): el docente publica material y tareas con archivo (PDF, imagen u Office de 5 MB) o enlace,
  califica las entregas y lleva el promedio de las tareas del periodo a la planilla. Estudiantes y acudientes reciben
  aviso de cada tarea y nota.
- **Planeador docente** (campus) y **plan de estudios** (permiso `Planeacion`): mallas por asignatura, grado y periodo
  con estándares, DBA, competencias, contenidos e indicadores; planeación semanal en borrador o enviada; coordinación la
  aprueba o la devuelve con observaciones.
- **Recuperaciones y comisión de evaluación** (permiso `Evaluacion`): el docente asigna el plan de mejoramiento a quien
  perdió el periodo y registra la nota de la recuperación; si aprueba, la nota del periodo queda en 3.0 y se conserva la
  original. Actas de la comisión de evaluación y promoción por curso con candidatos y decisión sugerida.
- **Reportes oficiales** (permiso `Reportes`): matrícula en formato tipo SIMAT (completa los datos con la solicitud de
  admisión cuando existe), libro final de calificaciones por curso y certificado de estudios con notas que se expide en
  el módulo de certificados con su código de verificación.
- **Mensajería** (campus): conversaciones privadas entre docentes y familias sobre un estudiante, solo con los docentes
  que le dictan clase este año.

### Notificaciones push

Se necesitan claves VAPID, que se generan una sola vez (`npx web-push generate-vapid-keys`). Sin ellas el campus
funciona igual y solo se ocultan las notificaciones al celular.

| Variable | Uso |
|---|---|
| `Push__ClavePublica` | Clave pública VAPID (base64url) |
| `Push__ClavePrivada` | Clave privada VAPID; es secreta |
| `Push__Asunto` | Contacto del remitente: `mailto:` del colegio o `https://rabigamaliel.edu.co` |

### Correo

Los correos se encolan en memoria y los envía un servicio en segundo plano por SMTP. Si no se configura el servidor,
los avisos quedan solo en el campus.

| Variable | Uso |
|---|---|
| `Correo__Servidor` | Servidor SMTP (p. ej. `smtp.gmail.com` o el de Microsoft 365) |
| `Correo__Puerto` | Puerto (587 por defecto) |
| `Correo__Usuario` / `Correo__Clave` | Cuenta que envía (en Gmail, una contraseña de aplicación) |
| `Correo__Remitente` / `Correo__NombreRemitente` | Dirección y nombre que ven las familias |

## Despliegue en Render + Neon

El repositorio incluye `Dockerfile` y `render.yaml` (Blueprint).

1. **Base de datos:** crear una cuenta en [neon.com](https://neon.com), un proyecto en la región *AWS US East 2
   (Ohio)* y copiar la *connection string* (`postgresql://...`).
2. **API:** en [render.com](https://render.com), *New → Blueprint*, conectar este repositorio de GitHub.
3. Cuando Render lo pida, pegar la cadena de Neon en `ConnectionStrings__DefaultConnection`. Se acepta tal cual,
   en formato URL.
4. Al terminar el despliegue, la API queda en `https://rabigamaliel-api.onrender.com`. Al arrancar aplica las
   migraciones y carga los datos iniciales.
5. La clave de administración la genera Render: *Environment → Administracion__ClaveApi*. Se escribe una sola vez
   en `/admin/login` del sitio para crear el primer superadministrador.
6. Si Render asigna otra dirección, actualizar `apiBaseUrl` en `src/environments/environment.ts` del frontend.

Variables de entorno (definidas en `render.yaml`):

| Variable | Uso |
|---|---|
| `ConnectionStrings__DefaultConnection` | Cadena de conexión de PostgreSQL (Neon) |
| `Administracion__ClaveApi` | Clave de emergencia y de la configuración inicial; también deriva la firma de los tokens |
| `Autenticacion__ClaveFirma` | Opcional: clave propia para firmar los tokens (mínimo 32 caracteres) |
| `Cors__OrigenesPermitidos__0` | Sitio que puede llamar a la API (`https://wprensh.github.io`) |
| `Database__AplicarMigraciones` | `true`: aplica migraciones al arrancar |
| `DetrasDeProxy` | `true`: confía en `X-Forwarded-For` del proxy de Render para la IP real |

## Pendiente

- Inicio de sesión con Microsoft Entra ID, aprovechando el Microsoft 365 del colegio.
- Actualizar a .NET 10 (LTS): el soporte de .NET 9 termina el 10 de noviembre de 2026.
