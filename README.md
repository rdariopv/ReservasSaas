# ReservasSaaS — Solución Completa .NET 10 (Arquitectura Hexagonal)

Bienvenido a la solución integral de **ReservasSaaS**, una plataforma SaaS multitenant para gestión de reservas, citas y servicios de alta disponibilidad construida sobre **.NET 10** bajo los principios rigurosos de **Arquitectura Hexagonal (Ports & Adapters)**, **Domain-Driven Design (DDD)**, revalidación atómica de concurrencia y aislamiento estricto de inquilinos (**Tenant Isolation**).

---

## 🏛️ Arquitectura Hexagonal y Estructura de Proyectos

La solución desacopla el núcleo de negocio de la infraestructura y puntos de entrada, asegurando que el dominio no dependa de frameworks, bases de datos o librerías de terceros:

```
+--------------------------------------------------------------------------------------------------+
|                                        ReservasSaaS.Api                                          |
|  - Adaptadores Primarios: Controladores REST (Bookings, Agenda, Services, Professionals, etc.)   |
|  - Soporte de idempotencia HTTP vía cabecera 'Idempotency-Key'                                   |
|  - Middlewares: FirebaseAuthenticationMiddleware, TenantAuthorizationMiddleware, RFC 7807       |
|  - Inyección de Dependencias (Composition Root)                                                  |
+--------------------------------------------------------------------------------------------------+
       |                                                               |
       v (usa)                                                         v (configura)
+---------------------------------------------------+     +----------------------------------------+
|             ReservasSaaS.Application              |     |       ReservasSaaS.Infrastructure      |
|  - Puertos de Entrada (Use Cases de Reservas)     |<----|  - Adaptadores Secundarios             |
|  - Motor Centralizado de Disponibilidad (Slots)   |     |  - Firebase Cloud Firestore Provider   |
|  - Revalidación Atómica de Capacidad y Horarios   |     |  - Repositorios Firestore Multitenant  |
|  - DTOs, Contratos de Solicitud y Respuestas      |     |  - Firebase Token Validator            |
|  - Contextos de Seguridad (Tenant & User Context) |     |  - Serilog Logging & Diagnóstico       |
+---------------------------------------------------+     +----------------------------------------+
       |                                                               |
       v (depende exclusivamente)                                      |
+---------------------------------------------------+                  |
|                ReservasSaaS.Domain                |<-----------------+
|  - Entidad Raíz: Booking (Agregado DDD)           |
|  - Máquina de Estados: 11 Estados de Reserva      |
|  - Desacoplamiento de Estados de Pago             |
|  - Reglas Invariantes y Transiciones              |
|  - Roles (Owner, Admin, Reception, etc.) y Permis.|
|  - Puertos de Salida (IBookingRepository, etc.)   |
|  - Excepciones Fuertemente Tipadas de Dominio     |
+---------------------------------------------------+
```

---

## 📅 Módulo de Reservas (Bookings & Appointment Lifecycle)

### 1. Estados de Reserva (11 Estados Mínimos)

El ciclo de vida de una reserva sigue una máquina de estados determinista y auditable:

| Estado | Código | Descripción |
|---|---|---|
| `PendingAssignment` | 1 | Reserva creada bajo modalidad `BusinessAssignsProfessional`, esperando que la administración asigne al profesional adecuado según capacidad real. |
| `PendingPayment` | 2 | Reserva creada donde el servicio exige anticipo/seña (`DepositPolicy`), esperando el pago del cliente dentro de la ventana de expiración. |
| `PaymentReview` | 3 | El cliente ha subido o reportado el comprobante de pago; pendiente de revisión y validación administrativa. |
| `Confirmed` | 4 | Reserva confirmada: profesional asignado, cupo bloqueado en la agenda y política de pago satisfecha. |
| `CheckedIn` | 5 | El cliente llegó a las instalaciones y realizó el registro de llegada (Check-In). |
| `InProgress` | 6 | El servicio se encuentra en ejecución activa con el profesional. |
| `Completed` | 7 | Servicio ejecutado y finalizado exitosamente. |
| `Cancelled` | 8 | Reserva cancelada por el cliente o negocio antes de su ejecución, registrando motivo y liberando el cupo inmediatamente. |
| `Expired` | 9 | Reserva cancelada automáticamente por el sistema al vencerse la ventana de tiempo (`ExpiresAtUtc`) para el pago o asignación. |
| `Rejected` | 10 | Reserva rechazada por la empresa (p. ej., comprobante de pago inválido o falta de disponibilidad). |
| `NoShow` | 11 | El cliente no se presentó a su cita en la fecha y hora programada. |

### 2. Separación Estricta de Estados de Pago (`PaymentStatus`)

Para cumplir con la separación de responsabilidades y modelos financieros reales, los estados de pago son independientes de los estados de la reserva:

- `NotRequired`: El servicio no exige depósito ni anticipo.
- `Pending`: Anticipo pendiente de pago por parte del cliente.
- `SubmittedForReview`: Comprobante recibido y en revisión.
- `DepositPaid`: Anticipo verificado y abonado exitosamente.
- `PaidInFull`: Total del servicio pagado por adelantado.
- `Refunded`: Anticipo o pago reembolsado tras cancelación o política comercial.
- `Failed`: Pago rechazado o fallido en pasarela/revisión.

---

## ⚡ Concurrencia, Revalidación de Disponibilidad e Idempotencia

### Revalidación de Disponibilidad y Bloqueo de Concurrencia
Para impedir que dos usuarios reserven simultáneamente el mismo recurso o profesional en horarios solapados (`startA < endB && endA > startB`):
1. **Verificación Previa:** El caso de uso consulta `IBookingRepository.GetOverlappingBookingsAsync(companyId, professionalId, startUtc, endUtc)`.
2. **Exclusión de Estados Inactivos:** Solo se consideran reservas activas (`Confirmed`, `PendingPayment`, `PaymentReview`, `CheckedIn`, `InProgress`). Las reservas canceladas, expiradas o rechazadas no bloquean el recurso.
3. **Manejo de Conflicto (409 Conflict):** Si existe solapamiento, se arroja `ConcurrencyException`, retornando una respuesta RFC 7807 `ProblemDetails` con código de estado HTTP 409 y mensaje descriptivo.

### Modalidad de Asignación y Capacidad Real
- **`ClientSelectsProfessional`:** Requiere obligatoriamente un `ProfessionalId`. Se valida su jornada laboral, descansos programados, excepciones de fecha y reservas existentes.
- **`BusinessAssignsProfessional`:** Si el cliente no especifica profesional, el sistema evalúa la capacidad de **todos** los profesionales habilitados para el servicio. Si **ninguno** tiene disponibilidad en el horario solicitado, se rechaza la creación (`ValidationException`). Si hay capacidad, se reserva el horario en estado `PendingAssignment`.
- **Garantía de Confirmación:** Una reserva **nunca** puede transicionar a `Confirmed` si no tiene un profesional asignado y su política de anticipo satisfecha.

### Estrategia de Idempotencia
- Las peticiones de creación soportan la cabecera HTTP `Idempotency-Key` (o campo `idempotencyKey` en el cuerpo).
- Al recibir una solicitud, el backend verifica `IBookingRepository.GetByIdempotencyKeyAsync(companyId, key)`.
- Si ya existe una reserva previa con esa clave para la empresa, se retorna inmediatamente la reserva existente (`HTTP 200/201`) **sin duplicar registros, sin descontar cupos adicionales y sin procesar dobles cobros**.

---

## 🌐 Contratos de API REST (Endpoints del Módulo de Reservas)

Todos los endpoints están aislados por inquilino y prefijados bajo:
`api/v1/companies/{companyId}/bookings`

| Método | Endpoint | Descripción |
|---|---|---|
| `POST` | `/` | Crea una nueva reserva con idempotencia (`Idempotency-Key`) y validación de disponibilidad. |
| `GET` | `/` | Búsqueda y filtrado paginado (por `status`, `professionalId`, rango de fechas). |
| `GET` | `/{bookingId}` | Consulta el detalle completo de una reserva. |
| `POST` | `/{bookingId}/reschedule` | Reprograma la reserva a un nuevo horario y/o profesional con revalidación de cupos. |
| `POST` | `/{bookingId}/assign-professional` | Asigna profesional a una reserva en `PendingAssignment`. |
| `POST` | `/{bookingId}/confirm` | Confirma la reserva garantizando profesional y estado de pago. |
| `POST` | `/{bookingId}/cancel` | Cancela la reserva con motivo (`Reason`) y libera el horario. |
| `POST` | `/{bookingId}/reject` | Rechaza la reserva por motivo justificado. |
| `POST` | `/{bookingId}/check-in` | Registra la llegada del cliente a las instalaciones. |
| `POST` | `/{bookingId}/start` | Marca el servicio como iniciado e `InProgress`. |
| `POST` | `/{bookingId}/complete` | Marca el servicio como `Completed`. |
| `POST` | `/{bookingId}/no-show` | Registra inasistencia del cliente (`NoShow`). |
| `POST` | `/{bookingId}/payment-status` | Actualiza el estado de pago independiente (`PaymentStatus`). |
| `POST` | `/expire-stale` | Proceso por lotes para expirar reservas pendientes fuera de plazo. |

---

## 🔒 Configuración de Ambientes y Credenciales Firebase

**Ninguna credencial sensible ni archivo JSON de clave privada se almacena en el repositorio**.

### 1. Ambiente Local (Firebase Emulator Suite)
```bash
export FIRESTORE_EMULATOR_HOST="localhost:8080"
export FIREBASE_AUTH_EMULATOR_HOST="localhost:9099"
export Firestore__ProjectId="reservassaas-local"
export Firestore__UseEmulator=true
```

### 2. Ambiente de Pruebas / Staging
```bash
export GOOGLE_APPLICATION_CREDENTIALS="/etc/secrets/firebase-sa-staging.json"
export Firestore__ProjectId="reservassaas-staging"
export Firestore__UseEmulator=false
```

### 3. Ambiente de Producción
En Google Cloud Run o GKE, se recomienda **Workload Identity** (sin claves estáticas):
```bash
export Firestore__ProjectId="reservassaas-prod"
export Firestore__UseEmulator=false
export FirebaseAuthentication__ProjectId="reservassaas-prod"
export FirebaseAuthentication__RequireHttpsMetadata=true
```

---

## 🧪 Pruebas Unitarias y de Aislamiento (`ReservasSaaS.UnitTests`)

La solución contiene un proyecto de pruebas con **xUnit**, **FluentAssertions** y **Moq**:

1. **`BookingDomainTests`:**
   - Inicialización según modalidad y anticipo (`PendingAssignment`, `PendingPayment`, `Confirmed`).
   - Independencia entre `PaymentStatus` y `BookingStatus`.
   - Prohibición de confirmar sin profesional asignado o sin pago exigido.
   - Ciclo de atención: `CheckIn` -> `StartService` -> `CompleteService`.
   - Inmutabilidad de estados terminales (no se puede cancelar ni reprogramar una reserva completada).
2. **`BookingIdempotencyAndDuplicateTests`:**
   - Verificación de que peticiones repetidas con la misma clave de idempotencia retornan la reserva original sin duplicación en base de datos.
3. **`BookingConcurrencyAndConflictTests`:**
   - Detección de solapamiento en reservas concurrentes arrojando `ConcurrencyException` (409 Conflict).
   - Verificación de rechazo en modalidad `BusinessAssignsProfessional` cuando no existe capacidad real.
4. **`BookingCancellationAndExpirationTests`:**
   - Cancelación con motivo y liberación de recursos.
   - Expiración automática de reservas en `PendingPayment` o `PendingAssignment` cuyo plazo `ExpiresAtUtc` haya expirado.
5. **`BookingTenantIsolationTests`:**
   - Validación estricta de aislamiento: peticiones de la Empresa B hacia reservas de la Empresa A son rechazadas sin filtrar datos.
6. **`HexagonalArchitectureBoundaryTests`:**
   - Pruebas de reflexión que garantizan que `Domain` permanezca 100% puro sin dependencias de I/O ni capas externas.

### Comandos de Validación:
```bash
# Restaurar paquetes
dotnet restore ReservasSaaS.sln

# Compilar toda la solución
dotnet build ReservasSaaS.sln -c Release --no-restore

# Ejecutar las pruebas unitarias
dotnet test tests/ReservasSaaS.UnitTests/ReservasSaaS.UnitTests.csproj -c Release --verbosity normal

# Iniciar la API
dotnet run --project src/ReservasSaaS.Api/ReservasSaaS.Api.csproj
```

*Nota sobre la ejecución:* En entornos donde el SDK de .NET no esté preinstalado en el contenedor de compilación, ejecute los comandos anteriores dentro de un entorno con .NET 10 SDK o a través del `Dockerfile` multi-stage provisto.
# ReservasSaas
