# VisorDBF

Aplicación de escritorio WPF para visualizar y exportar archivos DBF (dBASE / FoxPro). Soporta apertura de archivos `.dbf`, visualización en grid interactivo, exportación a TXT con formato configurable y transferencia directa a bases de datos Sybase ASE vía ODBC.

## Características

- **Visor de archivos DBF** — Abre archivos dBASE III, IV y FoxPro con detección automática de encoding mediante Language Driver ID.
- **Grid interactivo** — Visualización de registros con ordenamiento por columna, colores alternos, registros eliminados resaltados y panel de estado vacío.
- **Exportación a TXT** — Exporta registros a texto plano con separadores configurables (coma, punto y coma, tab, pipe o personalizado), delimitadores de fila, límite de registros, cabecera y encoding de salida seleccionable.
- **Formato de columnas** — Configura formatos de presentación/exportación para columnas DATE, DATETIME, TIME y NUMERIC usando cadenas de formato .NET.
- **Perfiles de exportación** — Guarda, carga y elimina configuraciones de exportación nombradas. Recuerda el último perfil usado.
- **Transferencia a Sybase ASE** — Conexión ODBC con configuración, testeo, probing de esquema e inserción por lotes (1000 registros por transacción) con columnas extra configurables.
- **Persistencia de configuración** — Ventana, archivos recientes, perfiles y conexión Sybase se guardan en `%APPDATA%\VisorDBF\settings.json`.
- **Operaciones asíncronas** — Toda operación de I/O corre en segundo plano con barra de progreso y cancelación.

## Requisitos

### Desarrollo
- Windows 10 / 11
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (opcional, recomendado)

### Producción
- Windows 10 (1903+) o Windows 11
- Para funcionalidad Sybase: driver ODBC de Sybase ASE instalado en el equipo

## Compilar y ejecutar

```bash
# Ejecutar en modo debug
dotnet run --project src\VisorDBF.UI -c Debug

# Compilar solución completa
dotnet build VisorDBF.sln --configuration Release

# Ejecutar pruebas
dotnet test

# Publicar como ejecutable autónomo (no requiere .NET Runtime)
dotnet publish src\VisorDBF.UI\VisorDBF.UI.csproj `
  --configuration Release --runtime win-x64 --self-contained true `
  --output ./publish/win-x64
```

## Estructura del proyecto

```
VisorDBF/
├── src/
│   ├── VisorDBF.Core/        # Capa de dominio y servicios (sin dependencias UI)
│   │   ├── Models/           # Entidades del dominio
│   │   ├── Services/         # Lógica de negocio (lectura DBF, exportación, Sybase)
│   │   ├── Exceptions/       # Excepciones personalizadas
│   │   └── Logging/          # Logger a archivo
│   └── VisorDBF.UI/          # Capa de presentación WPF (MVVM)
│       ├── Views/            # Ventanas y diálogos
│       ├── ViewModels/       # ViewModels
│       ├── Converters/       # Value converters
│       └── Helpers/          # Utilidades de UI
├── tests/
│   └── VisorDBF.Core.Tests/  # Pruebas unitarias (xUnit)
└── docs/                     # Documentación del proyecto
```

## Tecnologías

- **Lenguaje:** C# 12.0
- **Framework:** .NET 8.0
- **UI:** WPF (MVVM)
- **Base de datos:** ODBC (System.Data.Odbc)
- **Encoding:** System.Text.Encoding.CodePages
- **Testing:** xUnit, FluentAssertions, Moq, coverlet

## Licencia

Sin licencia — todos los derechos reservados.
