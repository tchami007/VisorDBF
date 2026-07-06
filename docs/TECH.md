# Documento Tecnico
## VisorDBF — Visor y Exportador de Archivos DBF

**Version:** 1.0  
**Fecha:** 2026-07-06  
**Audiencia:** Desarrolladores, arquitectos de software, equipo de DevOps.

---

## 1. Resumen del Sistema

VisorDBF es una aplicacion de escritorio Windows desarrollada en C# sobre .NET que permite abrir archivos DBF, visualizar su contenido en una grilla y exportarlo a archivos TXT con configuracion flexible de separadores, formatos por columna y codificaciones. No requiere servicios externos, base de datos ni conectividad de red.

---

## 2. Pila Tecnologica

| Capa | Tecnologia | Version minima | Justificacion |
|------|-----------|---------------|---------------|
| Lenguaje | C# | 12.0 | Tipado fuerte, soporte moderno de patrones, primary constructors y async/await. |
| Runtime | .NET | 8.0 LTS | Soporte de largo plazo hasta noviembre 2026, sin dependencia de .NET Framework legacy. |
| UI Framework | WinForms o WPF | .NET 8 | Aplicacion de escritorio Windows nativa. WPF recomendado por mejor soporte de virtualizacion de grilla. |
| Serializacion | System.Text.Json | Incluido en .NET 8 | Persistencia de configuracion en JSON sin dependencias externas. |
| Codificaciones | System.Text.Encoding | Incluido en .NET 8 | Soporte de codificaciones extendidas via CodePagesEncodingProvider. |
| Pruebas unitarias | xUnit | 2.x | Framework de pruebas estandar para .NET. |
| Pruebas de integracion | xUnit + FluentAssertions | 2.x / 6.x | Aserciones legibles para validacion de exportacion. |
| Empaquetado | dotnet publish (self-contained) | .NET 8 SDK | Distribucion sin requerir instalacion previa de .NET en el equipo destino. |
| Instalador | WiX Toolset o MSIX | 4.x / SDK .NET | Instalacion opcional con acceso directo y desinstalacion limpia. |

---

## 3. Librerias y Dependencias

### 3.1 Dependencias de produccion

| Libreria | NuGet Package | Version sugerida | Uso |
|----------|--------------|-----------------|-----|
| DbfDataReader | `DbfDataReader` | 0.4.x | Lectura de archivos DBF. Soporta dBASE III, IV y FoxPro. Expone IDataReader para iteracion eficiente. |
| System.Text.Encoding.CodePages | `System.Text.Encoding.CodePages` | 8.x | Habilita codificaciones adicionales como CP1252, CP437, ISO-8859-1 no incluidas por defecto en .NET 8. |

> **Alternativa a DbfDataReader:** Si se requiere mayor control sobre variantes del formato DBF, puede implementarse un lector propio basado en la especificacion del formato. Ver seccion 7 para referencias.

### 3.2 Dependencias de desarrollo y pruebas

| Libreria | NuGet Package | Version sugerida | Uso |
|----------|--------------|-----------------|-----|
| xUnit | `xunit` | 2.6.x | Framework de pruebas unitarias. |
| xUnit Runner | `xunit.runner.visualstudio` | 2.5.x | Integracion con Visual Studio Test Explorer. |
| FluentAssertions | `FluentAssertions` | 6.x | Aserciones expresivas en pruebas. |
| Moq | `Moq` | 4.x | Mocking de dependencias en pruebas unitarias. |
| coverlet | `coverlet.collector` | 6.x | Medicion de cobertura de codigo. |

### 3.3 Herramientas de desarrollo

| Herramienta | Version | Uso |
|-------------|---------|-----|
| Visual Studio | 2022 (17.x) | IDE principal. Community Edition es suficiente. |
| .NET SDK | 8.0 o superior | Compilacion, publicacion y ejecucion de pruebas. |
| dotnet CLI | Incluido en SDK | Build, test y publish desde linea de comandos. |
| WiX Toolset | 4.x (opcional) | Generacion de instalador MSI. |

---

## 4. Requisitos del Sistema

### 4.1 Entorno de desarrollo

| Requisito | Detalle |
|-----------|---------|
| Sistema operativo | Windows 10 / Windows 11 (64-bit) |
| .NET SDK | 8.0 o superior |
| Memoria RAM | 8 GB minimo recomendado |
| Espacio en disco | 2 GB para SDK + dependencias de NuGet |
| IDE | Visual Studio 2022 o VS Code con extension C# |

### 4.2 Entorno de produccion (usuario final)

| Requisito | Detalle |
|-----------|---------|
| Sistema operativo | Windows 10 (1903 o superior) / Windows 11 |
| .NET Runtime | No requerido si se publica como self-contained |
| Memoria RAM | 512 MB minimo; 2 GB recomendado para archivos grandes |
| Espacio en disco | ~150 MB (publicacion self-contained) |
| Permisos | Lectura sobre archivos DBF de origen; escritura sobre carpeta de destino de exportacion |

---

## 5. Estructura del Proyecto

```
VisorDBF/
|
+-- VisorDBF.sln                          # Solucion Visual Studio
|
+-- src/
|   |
|   +-- VisorDBF.UI/                      # Proyecto principal: interfaz grafica
|   |   +-- VisorDBF.UI.csproj
|   |   +-- Program.cs                    # Punto de entrada
|   |   +-- App.xaml / App.xaml.cs        # (WPF) Configuracion de aplicacion
|   |   |
|   |   +-- Views/                        # Vistas / Formularios
|   |   |   +-- MainWindow.xaml/.cs       # Ventana principal con grilla
|   |   |   +-- ExportConfigWindow.xaml/.cs    # Pantalla de configuracion de exportacion
|   |   |   +-- ColumnFormatsWindow.xaml/.cs   # Pantalla de formatos por columna
|   |   |   +-- EncodingPickerDialog.xaml/.cs  # Dialogo de seleccion de codificacion
|   |   |   +-- ExportProgressDialog.xaml/.cs  # Dialogo de progreso de exportacion
|   |   |
|   |   +-- ViewModels/                   # (WPF) ViewModels para patron MVVM
|   |   |   +-- MainViewModel.cs
|   |   |   +-- ExportConfigViewModel.cs
|   |   |   +-- ColumnFormatsViewModel.cs
|   |   |
|   |   +-- Controls/                     # Controles personalizados reutilizables
|   |       +-- EmptyStatePanel.xaml/.cs  # Panel "sin archivo cargado"
|   |       +-- StatusBarControl.xaml/.cs
|   |
|   +-- VisorDBF.Core/                    # Proyecto de logica de negocio (sin dependencias UI)
|   |   +-- VisorDBF.Core.csproj
|   |   |
|   |   +-- Models/                       # Entidades del dominio
|   |   |   +-- DbfFile.cs
|   |   |   +-- DbfField.cs
|   |   |   +-- DbfFieldType.cs           # Enum de tipos de campo
|   |   |   +-- DbfRecord.cs
|   |   |   +-- ExportConfiguration.cs
|   |   |   +-- ColumnFormatConfiguration.cs
|   |   |   +-- ExportProfile.cs
|   |   |   +-- ApplicationSettings.cs
|   |   |   +-- RowLimitMode.cs           # Enum: All / FirstN
|   |   |
|   |   +-- Services/                     # Servicios de dominio
|   |   |   +-- IDbfReaderService.cs      # Interfaz de lectura DBF
|   |   |   +-- DbfReaderService.cs       # Implementacion con DbfDataReader
|   |   |   +-- IExportService.cs         # Interfaz de exportacion
|   |   |   +-- TxtExportService.cs       # Implementacion de exportacion a TXT
|   |   |   +-- ISettingsService.cs       # Interfaz de persistencia de configuracion
|   |   |   +-- JsonSettingsService.cs    # Implementacion con System.Text.Json
|   |   |   +-- EncodingDetectionService.cs  # Mapeo Language Driver ID a Encoding
|   |   |   +-- ColumnFormatService.cs    # Aplicacion de formatos a valores
|   |   |
|   |   +-- Exceptions/
|   |       +-- DbfReadException.cs
|   |       +-- ExportException.cs
|   |       +-- UnknownEncodingException.cs
|   |
+-- tests/
|   |
|   +-- VisorDBF.Core.Tests/              # Pruebas unitarias e integracion de Core
|   |   +-- VisorDBF.Core.Tests.csproj
|   |   +-- Services/
|   |   |   +-- DbfReaderServiceTests.cs
|   |   |   +-- TxtExportServiceTests.cs
|   |   |   +-- EncodingDetectionServiceTests.cs
|   |   |   +-- ColumnFormatServiceTests.cs
|   |   +-- Fixtures/
|   |       +-- sample_cp1252.dbf         # Archivos DBF de prueba
|   |       +-- sample_utf8.dbf
|   |       +-- sample_deleted_records.dbf
|   |       +-- sample_all_types.dbf
|   |
+-- docs/
|   +-- PRD.md
|   +-- CASOS_DE_USO.md
|   +-- MANUAL_UI.md
|   +-- TECH.md                           # Este documento
|
+-- .gitignore
+-- README.md
```

---

## 6. Arquitectura del Sistema

### 6.1 Capas

```
+--------------------------------------------------+
|           VisorDBF.UI                            |
|  Views / ViewModels / Controls                   |
|  (WPF MVVM o WinForms MVP)                       |
+---------------------------+----------------------+
                            |
                    Interfaces (IDbfReaderService,
                    IExportService, ISettingsService)
                            |
+---------------------------+----------------------+
|           VisorDBF.Core                          |
|  Models | Services | Exceptions                  |
+--------------------------------------------------+
          |                        |
  DbfDataReader (NuGet)    System.Text.Json
  System.Text.Encoding     System.IO
```

- **VisorDBF.UI** depende de **VisorDBF.Core** exclusivamente a traves de interfaces. Nunca referencia implementaciones concretas directamente.
- **VisorDBF.Core** no tiene dependencia de ningun componente de UI. Es completamente testeable de forma aislada.
- La inyeccion de dependencias se resuelve en el punto de entrada (`Program.cs` o `App.xaml.cs`) sin contenedor IoC externo (instanciacion manual), dado el alcance reducido del proyecto. Si el proyecto crece, puede incorporarse `Microsoft.Extensions.DependencyInjection`.

### 6.2 Patron de UI

- **WPF:** Patron MVVM (Model-View-ViewModel). Las vistas no contienen logica de negocio. Los ViewModels exponen propiedades observables y comandos.
- **WinForms (alternativa):** Patron MVP (Model-View-Presenter). Los formularios implementan interfaces de vista y delegan en Presenters.

### 6.3 Flujo de datos: Apertura de archivo

```
Usuario selecciona archivo DBF
  -> MainViewModel.OpenFileCommand
     -> IDbfReaderService.ReadAsync(filePath, encoding)
        -> DbfReaderService lee cabecera
           -> EncodingDetectionService.DetectFromLanguageDriverId(byte)
              -> Devuelve Encoding (o lanza UnknownEncodingException)
           -> Itera registros -> List<DbfRecord>
           -> Devuelve DbfFile
        -> MainViewModel actualiza coleccion observable
           -> Vista (DataGrid) se actualiza via binding
```

### 6.4 Flujo de datos: Exportacion

```
Usuario inicia exportacion
  -> MainViewModel.ExportCommand
     -> IExportService.ExportAsync(DbfFile, ExportConfiguration, outputPath, cancellationToken)
        -> TxtExportService abre StreamWriter con OutputEncoding
        -> Si IncludeHeader: escribe linea de encabezado
        -> Por cada DbfRecord (hasta RowLimit si aplica):
           -> Por cada DbfField:
              -> ColumnFormatService.Format(value, field, columnFormat)
              -> Concatena con ColumnSeparator
           -> Agrega TrailingDelimiter si configurado
           -> Escribe linea + salto de linea
           -> Reporta progreso via IProgress<int>
        -> Cierra StreamWriter
        -> Devuelve ExportResult (registros exportados, ruta)
```

---

## 7. Formato DBF: Referencia Tecnica

### 7.1 Estructura del encabezado DBF

| Offset | Longitud | Descripcion |
|--------|----------|-------------|
| 0x00 | 1 byte | Version del archivo (0x03 = dBASE III, 0x30 = Visual FoxPro, etc.) |
| 0x01-0x03 | 3 bytes | Fecha de ultima modificacion (YY/MM/DD) |
| 0x04-0x07 | 4 bytes | Numero total de registros |
| 0x08-0x09 | 2 bytes | Bytes de cabecera |
| 0x0A-0x0B | 2 bytes | Bytes por registro |
| 0x1D | 1 byte | Language Driver ID (codificacion) |

### 7.2 Tipos de campo DBF soportados

| Codigo | Tipo | Descripcion | Formato configurable |
|--------|------|-------------|---------------------|
| C | Character | Cadena de texto de longitud fija | No |
| N | Numeric | Numero almacenado como texto | Si (decimales, separadores) |
| F | Float | Igual que N, punto flotante | Si |
| D | Date | Fecha en formato YYYYMMDD | Si |
| T | DateTime | Fecha y hora (FoxPro) | Si |
| L | Logical | Booleano (T/F/Y/N) | No |
| M | Memo | Puntero a bloque memo (.FPT/.DBT) | No |
| I | Integer | Entero binario (FoxPro) | No |

### 7.3 Tabla de Language Driver ID

Seleccion de los mas frecuentes:

| ID (hex) | Codificacion | Descripcion |
|----------|-------------|-------------|
| 0x01 | CP437 | DOS USA |
| 0x02 | CP850 | DOS Multilingue |
| 0x03 | CP1252 | Windows ANSI |
| 0x57 | CP1252 | Windows ANSI (alternativo) |
| 0x58 | CP1252 | Windows ANSI (FoxPro) |
| 0x59 | CP1252 | Windows ANSI (FoxPro) |
| 0x64 | CP852 | DOS Europa del Este |
| 0xC8 | CP1250 | Windows Europa Central |
| 0xC9 | CP1251 | Windows Cirилico |
| 0x00 | Desconocido | Requiere seleccion manual |

Referencias completas: especificacion dBASE y documentacion de Visual FoxPro Language Driver IDs.

---

## 8. Configuracion de la Aplicacion

### 8.1 Ruta del archivo de configuracion

```
%APPDATA%\VisorDBF\settings.json
```

Ejemplo: `C:\Users\nombreusuario\AppData\Roaming\VisorDBF\settings.json`

### 8.2 Esquema del archivo settings.json

```json
{
  "defaultColumnSeparator": ";",
  "defaultTrailingDelimiter": "",
  "defaultIncludeHeader": true,
  "defaultRowLimitMode": "All",
  "defaultRowLimit": 1000,
  "defaultOutputEncoding": "UTF-8",
  "recentFiles": [
    "C:\\datos\\clientes.dbf",
    "C:\\datos\\ventas.dbf"
  ],
  "exportProfiles": [
    {
      "profileName": "Exportacion estandar",
      "createdDate": "2026-07-06T00:00:00Z",
      "lastUsedDate": "2026-07-06T00:00:00Z",
      "configuration": {
        "columnSeparator": ";",
        "trailingDelimiter": "",
        "includeHeader": true,
        "rowLimitMode": "All",
        "rowLimit": 0,
        "outputEncoding": "UTF-8",
        "columnFormats": [
          {
            "fieldName": "FECHA",
            "fieldType": "Date",
            "formatString": "dd/MM/yyyy",
            "isEnabled": true
          },
          {
            "fieldName": "MONTO",
            "fieldType": "Numeric",
            "formatString": "#,##0.00",
            "isEnabled": true
          }
        ]
      }
    }
  ],
  "windowState": {
    "left": 100,
    "top": 100,
    "width": 1200,
    "height": 800,
    "isMaximized": false
  }
}
```

### 8.3 Manejo de configuracion corrupta

Si el archivo `settings.json` no puede ser deserializado (corrupto o version incompatible), la aplicacion:

1. Registra el error en el log de eventos de Windows (EventLog) o en un archivo `error.log` en la misma carpeta.
2. Crea una copia de seguridad del archivo corrupto con extension `.bak`.
3. Inicia con la configuracion por defecto y genera un nuevo `settings.json`.
4. Informa al usuario mediante un aviso no bloqueante en la barra de estado.

---

## 9. Manejo de Errores y Logging

### 9.1 Estrategia de manejo de errores

| Capa | Estrategia |
|------|-----------|
| DbfReaderService | Captura excepciones de IO y formato. Lanza `DbfReadException` con mensaje descriptivo. |
| TxtExportService | Captura excepciones de IO. Lanza `ExportException`. Elimina archivo parcial en caso de falla. |
| EncodingDetectionService | Retorna `null` si el Language Driver ID no es reconocido; no lanza excepcion. |
| ColumnFormatService | Si el formato es invalido para el tipo, retorna el valor original sin formatear y registra una advertencia. |
| ViewModels / Presenters | Capturan excepciones de servicio y las traducen a mensajes de usuario. Nunca exponen stack traces al usuario final. |

### 9.2 Logging

Para la version 1.0 se implementa logging minimo sin libreria externa:

- Errores criticos: se escriben en `%APPDATA%\VisorDBF\error.log` con timestamp, tipo de excepcion y mensaje.
- Formato de entrada de log: `[2026-07-06 14:30:00] [ERROR] DbfReaderService: mensaje de error`.

Si en versiones futuras se requiere logging estructurado, se recomienda incorporar `Microsoft.Extensions.Logging` con proveedor de archivo (Serilog o NLog).

---

## 10. Pruebas

### 10.1 Estrategia de pruebas

| Nivel | Alcance | Herramienta |
|-------|---------|-------------|
| Unitarias | Servicios de Core de forma aislada con dependencias mockeadas. | xUnit + Moq |
| Integracion | Lectura de archivos DBF reales de fixtures y validacion de salida exportada. | xUnit + FluentAssertions |
| Manual / UAT | Verificacion de flujos de UI segun casos de uso documentados. | Manual |

### 10.2 Casos de prueba prioritarios

| ID | Descripcion |
|----|-------------|
| TP-01 | Lectura exitosa de DBF con Language Driver ID conocido (CP1252). |
| TP-02 | Lectura de DBF con Language Driver ID 0x00 (desconocido). |
| TP-03 | Exportacion completa con separador de columnas personalizado. |
| TP-04 | Exportacion con limite de N filas. |
| TP-05 | Exportacion con y sin encabezado. |
| TP-06 | Aplicacion de formato de fecha "dd/MM/yyyy" a columna tipo DATE. |
| TP-07 | Aplicacion de formato numerico "#,##0.00" a columna tipo NUMERIC. |
| TP-08 | Formato invalido en columna: verificar que se exporta el valor original. |
| TP-09 | Cancelacion de exportacion en curso: verificar eliminacion del archivo parcial. |
| TP-10 | Carga y guardado de configuracion en settings.json. |
| TP-11 | Recuperacion ante settings.json corrupto. |
| TP-12 | Lectura de DBF con registros marcados como eliminados. |

### 10.3 Ejecucion de pruebas

```bash
# Ejecutar todas las pruebas
dotnet test

# Con reporte de cobertura
dotnet test --collect:"XPlat Code Coverage"

# Solo pruebas de un proyecto
dotnet test tests/VisorDBF.Core.Tests/
```

---

## 11. Build y Publicacion

### 11.1 Compilar en modo Debug

```bash
dotnet build VisorDBF.sln --configuration Debug
```

### 11.2 Compilar en modo Release

```bash
dotnet build VisorDBF.sln --configuration Release
```

### 11.3 Publicar como ejecutable self-contained (sin .NET instalado en destino)

```bash
dotnet publish src/VisorDBF.UI/VisorDBF.UI.csproj \
  --configuration Release \
  --runtime win-x64 \
  --self-contained true \
  --output ./publish/win-x64
```

El resultado en `./publish/win-x64/` es un directorio con el ejecutable y todas las dependencias. Se distribuye como ZIP o se empaqueta con instalador.

### 11.4 Publicar como ejecutable unico (single-file)

```bash
dotnet publish src/VisorDBF.UI/VisorDBF.UI.csproj \
  --configuration Release \
  --runtime win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  --output ./publish/single-file
```

> Nota: El modo single-file aumenta el tiempo de inicio en el primer lanzamiento porque extrae binarios a una carpeta temporal. Para aplicaciones de escritorio de uso frecuente se recomienda la publicacion directa (11.3) sobre single-file.

### 11.5 Publicar dependiente del runtime (requiere .NET instalado)

```bash
dotnet publish src/VisorDBF.UI/VisorDBF.UI.csproj \
  --configuration Release \
  --runtime win-x64 \
  --self-contained false \
  --output ./publish/framework-dependent
```

Genera un ejecutable mas pequeno (~5 MB vs ~150 MB self-contained) pero requiere .NET 8 Runtime en el equipo del usuario.

---

## 12. Instalacion

### 12.1 Distribucion sin instalador (portable)

1. Copiar el contenido de `./publish/win-x64/` a la carpeta destino en el equipo del usuario.
2. Ejecutar `VisorDBF.exe` directamente.
3. La configuracion se almacena en `%APPDATA%\VisorDBF\` de forma automatica en el primer inicio.

No requiere privilegios de administrador.

### 12.2 Distribucion con instalador MSI (WiX Toolset)

Requiere WiX Toolset v4 instalado en la maquina de build.

```bash
# Construir el instalador (desde la carpeta del proyecto WiX)
dotnet build installer/VisorDBF.Installer.wixproj --configuration Release
```

El instalador generado:

- Registra la aplicacion en "Agregar o quitar programas".
- Crea acceso directo en el menu Inicio.
- Asocia la extension `.dbf` con VisorDBF (opcional, configurable).
- Instala en `%ProgramFiles%\VisorDBF\` por defecto.
- Requiere privilegios de administrador para la instalacion inicial.

### 12.3 Distribucion via MSIX (Microsoft Store o sideload empresarial)

Alternativa moderna para entornos corporativos con politicas de despliegue centralizado.

```bash
dotnet publish src/VisorDBF.UI/VisorDBF.UI.csproj \
  --configuration Release \
  --runtime win-x64 \
  --self-contained true \
  -p:GenerateAppxPackageOnBuild=true
```

Requiere certificado de firma de codigo (autofirmado para sideload, certificado de confianza para Store).

---

## 13. Actualizacion de la Aplicacion

Para la version 1.0 no se implementa mecanismo de actualizacion automatica. El proceso es manual:

1. Distribuir el nuevo paquete al usuario.
2. Reemplazar los archivos de la instalacion existente (o ejecutar el nuevo instalador).
3. La configuracion en `%APPDATA%\VisorDBF\settings.json` se preserva entre versiones, siempre que el esquema sea retrocompatible.

Si en versiones futuras se requiere actualizacion automatica, evaluar **Squirrel.Windows** o **MSIX with App Installer**.

---

## 14. Consideraciones de Seguridad

| Area | Consideracion |
|------|--------------|
| Lectura de archivos | La aplicacion solo tiene acceso de lectura al archivo DBF. No modifica el archivo de origen bajo ninguna circunstancia. |
| Escritura de configuracion | El archivo `settings.json` se escribe unicamente en `%APPDATA%`, carpeta de usuario sin privilegios elevados. |
| Caracteres especiales en datos | Los valores de campos que contengan el caracter separador configurado deben ser encerrados entre comillas dobles en la exportacion, o el separador debe ser escapado segun el comportamiento configurado. |
| Archivos DBF de origen desconocido | No se ejecuta ningun codigo embebido en el archivo DBF. El riesgo de ejecucion de codigo malicioso es inexistente por el tipo de formato. |

---

## 15. Decisiones de Diseno

| Decision | Alternativa descartada | Razon |
|----------|----------------------|-------|
| .NET 8 como target | .NET Framework 4.8 | .NET 8 es multiplataforma, tiene mejor rendimiento y soporte LTS activo hasta noviembre 2026. El requisito de solo Windows no obliga a usar Framework. |
| WPF con MVVM | WinForms con MVP | WPF ofrece mejor virtualizacion de DataGrid para archivos grandes y binding declarativo mas expresivo. |
| DbfDataReader como libreria de lectura | Implementacion propia | Reduce tiempo de desarrollo inicial. Si se detectan limitaciones con variantes del formato, se reemplaza por implementacion propia sin cambiar la interfaz `IDbfReaderService`. |
| System.Text.Json para configuracion | Newtonsoft.Json | Incluido en .NET 8, sin dependencia adicional. Para los casos de uso de este proyecto es suficiente. |
| Publicacion self-contained por defecto | Dependiente del runtime | Elimina la variable de version de .NET en el equipo del usuario final, simplificando el soporte. |
| Sin contenedor IoC externo en v1.0 | Microsoft.Extensions.DependencyInjection | El numero de servicios es reducido y la inyeccion manual en el punto de entrada es legible y suficiente. Se puede incorporar sin cambios arquitecturales si el proyecto crece. |
