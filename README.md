# 🎬 MoviesApp - API REST con Clean Architecture

## 📋 Descripción del Proyecto

API REST en C# con .NET 8 para gestión básica de películas, implementando **Clean Architecture** y buenas prácticas de desarrollo. La solución incluye autenticación JWT, Azure Functions para procesamiento de archivos CSV y una base sólida para futuras expansiones.

## 🏗️ Arquitectura Clean Architecture

```
MoviesApp/
├── 🎯 MoviesApp.API                    # Capa de Presentación
│   ├── Controllers/                   # Controladores REST (Movies, Auth)
│   ├── Middleware/                    # Middleware personalizado
│   ├── Filters/                       # Filtros de la API
│   └── Program.cs                     # Configuración DI y pipeline
│
├── 🧠 MoviesApp.Application           # Capa de Aplicación
│   ├── Services/                      # Lógica de negocio (MovieService, AuthService)
│   ├── DTOs/                         # Objetos de transferencia
│   ├── Interfaces/                   # Contratos de servicios
│   ├── Validators/                   # Validaciones FluentValidation
│   ├── Mappings/                     # Perfiles AutoMapper
│   └── Helpers/                      # Utilidades
│
├── 🏛️ MoviesApp.Domain               # Capa de Dominio
│   ├── Entities/                     # Entidades (Movie, User)
│   └── Interfaces/                   # Contratos de repositorios
│
├── 💾 MoviesApp.Infrastructure        # Capa de Infraestructura
│   ├── Data/                         # DbContext y configuraciones
│   ├── Repositories/                 # Implementaciones de repositorios
│   └── Helpers/                      # Servicios de infraestructura
│
├── ⚡ MoviesApp.Functions             # Azure Functions
│   ├── Triggers/                     # CsvBlobTrigger, CsvDailyCleanup
│   └── Helpers/                      # Servicios compartidos
│
└── 🧪 MoviesApp.Tests                # Pruebas Unitarias
    ├── Application.Tests/            # Tests de servicios
    └── Infrastructure.Tests/         # Tests de repositorios
```

## 🚀 Funcionalidades Implementadas

### ✅ **Gestión Básica de Películas**
- **GET** película por ID
- **GET** lista de películas con paginación y ordenamiento
- **POST** nueva película
- **Validaciones** con FluentValidation

### ✅ **Autenticación JWT**
- **Login** con credenciales
- **Registro** de nuevos usuarios  
- **Obtener información** de usuario
- **Tokens JWT** seguros con expiración

### ✅ **Procesamiento Automático CSV** (Azure Functions)
- **CsvBlobTrigger**: Procesa archivos CSV automáticamente al subirlos a Blob Storage
- **CsvDailyCleanup**: Limpieza diaria de archivos procesados

### ✅ **Tecnologías Core**
- **.NET 8** con ASP.NET Core
- **Entity Framework Core** con SQL Server/Azure SQL
- **FluentValidation** para validaciones robustas
- **AutoMapper** para mapeo de objetos
- **JWT Bearer Authentication**
- **Swagger/OpenAPI** para documentación

## 📊 Modelo de Datos

### **Entidad Movie**
```csharp
public class Movie
{
    public Guid MovieId { get; set; }      // Primary Key (GUID)
    public int Id { get; set; }            // Business ID (único)
    public string Film { get; set; }       // Nombre de la película
    public string Genre { get; set; }      // Género
    public string Studio { get; set; }     // Estudio productor  
    public int Score { get; set; }         // Puntuación (0-100)
    public int Year { get; set; }          // Año de estreno
    public DateTime CreatedAt { get; set; } // Auditoría
    public DateTime? UpdatedAt { get; set; } // Auditoría
}
```

### **Entidad User**
```csharp
public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

## 🛠️ Configuración y Ejecución

### **Prerrequisitos**
```bash
# Verificar versiones requeridas
dotnet --version  # >= 8.0
```

### **1. Configurar Base de Datos**
Actualizar `MoviesApp.API/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MoviesAppDb;Trusted_Connection=true;MultipleActiveResultSets=true;"
  }
}
```

### **2. Ejecutar Migraciones**
```bash
cd MoviesApp.API
dotnet ef database update
```

### **3. Ejecutar la API**
```bash
cd MoviesApp.API
dotnet restore
dotnet build
dotnet run
```

**URL local**: `https://localhost:7041`
**Swagger**: `https://localhost:7041/swagger`

### **4. Ejecutar Azure Functions (Opcional)**
```bash
# Terminal 1: Azurite Storage Emulator
npm install -g azurite
azurite --silent --location azurite

# Terminal 2: Azure Functions  
cd MoviesApp.Functions
func start
```

## 🔗 Endpoints de la API

### **🌐 Producción**: `https://movieappsoftwarecolombia.azurewebsites.net`
### **🔧 Desarrollo**: `https://localhost:7041`

---

## 🔐 Autenticación

### **Iniciar Sesión**
```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "admin@moviesapp.com",
  "password": "Admin123!"
}
```

**Respuesta (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh_token_here",
  "expiresIn": 3600,
  "user": {
    "id": 1,
    "username": "admin@moviesapp.com",
    "email": "admin@moviesapp.com"
  }
}
```

### **Registrar Usuario**
```http
POST /api/auth/register
Content-Type: application/json

{
  "username": "nuevo@usuario.com",
  "email": "nuevo@usuario.com", 
  "password": "Password123!"
}
```

### **Obtener Información de Usuario**
```http
GET /api/auth/user/{id}
Authorization: Bearer {token}
```

---

## 🎬 Gestión de Películas

> **Nota**: Todos los endpoints de películas requieren autenticación JWT

### **Obtener Película por ID**
```http
GET /api/movie?id=7
Authorization: Bearer {token}
```

**Respuesta (200 OK):**
```json
{
  "movieId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "id": 7,
  "film": "The Matrix",
  "genre": "Sci-Fi",
  "studio": "Warner Bros",
  "score": 85,
  "year": 1999,
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": null
}
```

### **Listar Películas con Paginación**
```http
GET /api/movies?total=10&order=desc
Authorization: Bearer {token}
```

**Parámetros disponibles:**
- `total`: Número de películas a retornar (1-1000, default: 50)
- `order`: Orden "asc" o "desc" (default: "asc")

**Respuesta (200 OK):**
```json
[
  {
    "movieId": "guid1",
    "id": 1,
    "film": "Casablanca", 
    "genre": "Drama",
    "studio": "Warner Bros",
    "score": 92,
    "year": 1942,
    "createdAt": "2024-01-15T10:30:00Z",
    "updatedAt": null
  }
]
```

### **Crear Nueva Película**
```http
POST /api/movie
Authorization: Bearer {token}
Content-Type: application/json

{
  "id": 1001,
  "film": "Nueva Película",
  "genre": "Action", 
  "studio": "Marvel Studios",
  "score": 95,
  "year": 2024
}
```

**Respuesta (201 Created):**
```json
{
  "movieId": "new-guid-here",
  "id": 1001,
  "film": "Nueva Película",
  "genre": "Action",
  "studio": "Marvel Studios", 
  "score": 95,
  "year": 2024,
  "createdAt": "2024-01-15T11:30:00Z",
  "updatedAt": null
}
```

---

## ✅ Validaciones Implementadas

### **Reglas de Validación**

| Campo | Reglas |
|-------|--------|
| `Id` | ✅ Requerido, positivo, único |
| `Film` | ✅ Requerido, 1-255 caracteres |
| `Genre` | ✅ Requerido, máximo 100 caracteres |
| `Studio` | ✅ Requerido, máximo 150 caracteres |
| `Score` | ✅ Requerido, entre 0-100 |
| `Year` | ✅ Requerido, entre 1900-2100 |

### **Ejemplo de Error de Validación (400 Bad Request)**
```json
{
  "title": "Error de validación",
  "status": 400,
  "detail": "Los datos proporcionados no son válidos",
  "errors": [
    {
      "field": "Score",
      "message": "La puntuación debe estar entre 0 y 100",
      "code": "SCORE_OUT_OF_RANGE"
    }
  ],
  "timestamp": "2024-01-15T10:30:00Z"
}
```

---

## ⚡ Azure Functions

### **CsvBlobTrigger**
- **Activación**: Al subir archivo CSV al contenedor `movies-csv`
- **Función**: Procesa automáticamente el CSV y actualiza la base de datos
- **Validaciones**: Formato, duplicados, integridad de datos

### **CsvDailyCleanup** 
- **Activación**: Diariamente a las 2:00 AM UTC
- **Función**: Limpieza de archivos procesados y mantenimiento
- **Retención**: Respaldos por 30 días

### **Configuración Local**
```bash
# Azure Functions Core Tools
npm install -g azure-functions-core-tools@4 --unsafe-perm true

# Ejecutar localmente
cd MoviesApp.Functions
func start
```

---

## 🧪 Pruebas

### **Ejecutar Todas las Pruebas**
```bash
dotnet test
```

### **Con Cobertura de Código**
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### **Pruebas Específicas**
```bash
# Solo Application Tests
dotnet test --filter "FullyQualifiedName~Application.Tests"

# Solo Infrastructure Tests  
dotnet test --filter "FullyQualifiedName~Infrastructure.Tests"
```

---

## 🚀 Despliegue en Azure

### **Infraestructura Actual**
- **App Service**: `movieappsoftwarecolombia.azurewebsites.net`
- **Azure Functions**: `moviesapp-functions.azurewebsites.net`  
- **SQL Database**: `movieappsoftwarecolombiaserver.database.windows.net`
- **Storage Account**: Para archivos CSV y Azure Functions

### **CI/CD con GitHub Actions**
El proyecto incluye un pipeline completo en `.github/workflows/azure-deploy.yml`:

- ✅ Build y Test automatizado
- ✅ Deploy a producción en Azure
- ✅ Deploy de Azure Functions
- ✅ Health checks post-deploy

---

## 📈 Roadmap de Funcionalidades Futuras

### **🔄 Pendientes de Implementación**
- [ ] **CRUD Completo**: PUT y DELETE para películas
- [ ] **Filtros Avanzados**: Por género, año, puntuación mínima
- [ ] **Endpoints de CSV**: Upload, validación y carga de archivos
- [ ] **Estadísticas**: Métricas y reportes de películas
- [ ] **Health Check**: Endpoint de monitoreo
- [ ] **Paginación Avanzada**: Skip, take, total count
- [ ] **Búsqueda por Texto**: Filtros de texto en películas

### **🎯 Mejoras Técnicas**
- [ ] **Caching**: Redis para mejorar performance
- [ ] **Rate Limiting**: Protección contra abuso
- [ ] **Logging Avanzado**: Application Insights integrado
- [ ] **Refresh Tokens**: Manejo completo de sesiones
- [ ] **Roles y Permisos**: Sistema de autorización granular

---

## 📚 Tecnologías y Patrones

### **Stack Tecnológico**
| Categoría | Tecnología | Versión |
|-----------|------------|---------|
| **Framework** | .NET | 8.0 |
| **Web API** | ASP.NET Core | 8.0 |
| **ORM** | Entity Framework Core | 8.0 |
| **Validación** | FluentValidation | 11.8+ |
| **Mapeo** | AutoMapper | 12.0+ |
| **Testing** | xUnit + Moq | Latest |
| **Docs** | Swagger/OpenAPI | Latest |
| **Auth** | JWT Bearer | Latest |
| **Cloud** | Azure Functions | 4.x |

### **Patrones Implementados**
- ✅ **Repository Pattern** - Abstracción de acceso a datos
- ✅ **Service Pattern** - Lógica de negocio encapsulada  
- ✅ **DTO Pattern** - Transferencia de datos desacoplada
- ✅ **Dependency Injection** - Inversión de control
- ✅ **Clean Architecture** - Separación por capas
- ✅ **SOLID Principles** - Código mantenible y extensible

---

## 🤝 Contribuir al Proyecto

Para contribuir o reportar issues:

1. **Fork** el repositorio
2. **Crear branch**: `git checkout -b feature/nueva-funcionalidad`
3. **Commit**: `git commit -m "feat: descripción clara"`
4. **Push**: `git push origin feature/nueva-funcionalidad`  
5. **Pull Request** con descripción detallada

---

## 🎯 Estado Actual

**Versión**: 1.0.0 (MVP)  
**Estado**: ✅ Funcional para operaciones básicas  
**Cobertura de Tests**: ~80%  
**Funcionalidades Core**: Completadas  
**Próximos Hitos**: Filtros avanzados y carga de CSV

---

**¡Happy Coding! 🚀** 