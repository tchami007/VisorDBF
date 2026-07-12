# Plan de Mejoras Tecnicas — Milestone V1

**Version:** 1.0  
**Fecha:** 2026-07-10  
**Audiencia:** Desarrolladores, equipo de QA.  
**Fuente:** Auditoria de code smells con `dotnet-csharp-code-smells`

---

## Resumen

Se detectaron 13 hallazgos clasificados en tres categorias de severidad. Este plan detalla las tareas necesarias para resolverlos, agrupadas en fases de ejecucion con dependencias explicitas.

---

## Fase 1: Correcciones Criticas (Alto Impacto)

### 1.1 Memory leak — RelayCommand y CommandManager.RequerySuggested

**Archivo:** `src/VisorDBF.UI/ViewModels/RelayCommand.cs`  
**Problema:** El evento `CanExecuteChanged` se suscribe al estatico `CommandManager.RequerySuggested` pero nunca se desuscribe. Esto mantiene una cadena de referencias que impide al GC recolectar ViewModels.

**Solucion:** Usar `CommandManager.RequerySuggested` para `InvalidateRequerySuggested()` solo cuando sea necesario. Implementar `RelayCommand` con suscripcion debil (WeakEvent) o almacenar la suscripcion para desuscribirse. Alternativa: quitar la suscripcion automatica y usar `CommandManager.InvalidateRequerySuggested()` manual desde los ViewModel.

**Criterio de aceptacion:**
- Los ViewModels creados para dialogos modales son recolectables tras cerrar el dialogo.
- `CanExecuteChanged` se sigue disparando correctamente ante cambios de estado de UI.

### 1.2 Empty catch — DbfReaderService.ParseDateValue

**Archivo:** `src/VisorDBF.Core/Services/DbfReaderService.cs:298-300`  
**Problema:** Catch vacio que traga todas las excepciones. Riesgo de `IndexOutOfRangeException`, `ArgumentOutOfRangeException`, etc. sin registro.

**Solucion:** Reemplazar `catch { }` por captura de excepcion con log (o al menos no silenciar completamente). Separar la logica de parseo con `DateTime.TryParseExact` para evitar excepciones.

**Criterio de aceptacion:**
- No hay bloques `catch { }` sin contenido.
- Fechas invalidas retornan `null` sin lanzar excepcion.

### 1.3 Missing innerException al wrappear excepciones

**Archivos:**
- `src/VisorDBF.Core/Services/SybaseExportService.cs:78`
- `src/VisorDBF.Core/Services/SybaseExportService.cs:373-374`
- `src/VisorDBF.Core/Services/SybaseExportService.cs:516-517`

**Problema:** Se lanza `ExportException` sin pasar la excepcion original como `innerException`. Se pierde la causa raiz y el stack trace.

**Solucion:** Pasar `ex` como tercer parametro al constructor de `ExportException`.

**Criterio de aceptacion:**
- Toda excepcion envuelta preserva la excepcion original via `InnerException`.

### 1.4 Case-sensitive dictionary lookup en Sybase transfer

**Archivo:** `src/VisorDBF.Core/Services/SybaseExportService.cs:295`  
**Problema:** `record.Values` usa `Dictionary<string, object?>` con comparador ordinal por defecto (case-sensitive). `columns[p].Name` viene de `syscolumns` de Sybase y puede tener casing diferente al DBF. La busqueda falla silenciosamente.

**Solucion:** Una de:
- a) Normalizar los nombres de columna Sybase al casing del DBF durante el mapeo en `LoadColumnTypesAsync`.
- b) Crear `record.Values` con `StringComparer.OrdinalIgnoreCase` y que el binding de WPF tambien sea case-insensitive.
- c) Hacer la busqueda con `FirstOrDefault` sobre las keys del diccionario ignorando casing.

**Criterio de aceptacion:**
- Registros con columnas en Sybase cuyo casing difiere del DBF se transfieren correctamente.
- El binding del DataGrid sigue funcionando.

---

## Fase 2: Correcciones de Media Severidad

### 2.1 Missing GC.SuppressFinalize en FileLogger

**Archivo:** `src/VisorDBF.Core/Logging/FileLogger.cs:46-53`  
**Problema:** CA1816 — `Dispose()` no llama a `GC.SuppressFinalize(this)`. La clase es sealed, pero la llamada es necesaria por si se agrega un finalizer en el futuro.

**Solucion:** Agregar `GC.SuppressFinalize(this);` al inicio de `Dispose()`.

**Criterio de aceptacion:**
- No hay warning CA1816 en analisis estatico.

### 2.2 Log-and-swallow en ProbeFirstRecordAsync

**Archivo:** `src/VisorDBF.Core/Services/SybaseExportService.cs:445-448, 472-474`  
**Problema:** Se captura `Exception`, se loguea, y se retorna `false`. El caller no sabe que fallo ni por que. El log esta en archivo temporal que el usuario desconoce.

**Solucion:** Propagar informacion del error al caller. Puede ser:
- Incluir el mensaje de error en el valor de retorno (ej. `(bool success, string errorMessage)`)
- O lanzar excepcion con el detalle y capturarla en el caller para mostrarla al usuario.

**Criterio de aceptacion:**
- El usuario ve un mensaje explicativo cuando el probe falla, no solo "fallo".

### 2.3 Dead code — DbfDataReader y MapColumnType

**Archivos:**
- `src/VisorDBF.Core/VisorDBF.Core.csproj:18`
- `src/VisorDBF.Core/Services/DbfReaderService.cs:344-356`

**Problema:** El paquete `DbfDataReader` 0.4.* esta referenciado pero el parser es completamente custom. El metodo `MapColumnType` no es llamado por nadie.

**Solucion:**
1. Eliminar el metodo muerto `MapColumnType`.
2. Remover el `PackageReference` de `DbfDataReader` del `.csproj`.
3. Verificar que no haya otros usos del paquete con `dotnet list package --include-transitive`.

**Criterio de aceptacion:**
- El proyecto compila y pasa todas las pruebas sin `DbfDataReader`.
- `dotnet list package` no muestra `DbfDataReader`.

### 2.4 Duplicacion de connection string Sybase

**Archivos:**
- `src/VisorDBF.Core/Services/SybaseExportService.cs:403-410` (metodo `BuildConnectionString`)
- `src/VisorDBF.UI/ViewModels/SybaseConnectionViewModel.cs:113-120` (propiedad `ConnectionString`)
- `src/VisorDBF.UI/ViewModels/SybaseConnectionViewModel.cs:162-168` (metodo `TestConnectionAsync`)
- `src/VisorDBF.UI/ViewModels/SybaseConnectionViewModel.cs:181-187` (variable `masked` en catch)

**Problema:** La misma cadena de conexion Sybase se construye en 4 lugares con ligeras variaciones (con y sin password, con PWD=***).

**Solucion:** Extraer a un helper compartido. Por ejemplo:
- Agregar metodo `SybaseConnectionConfig.ToConnectionString(bool maskPassword = false)` en el modelo.
- Usar `SybaseConnectionConfig.ToConnectionString()` en los 4 lugares.

**Criterio de aceptacion:**
- Un solo punto de definicion del formato de connection string.
- Todos los lugares lo referencian.

---

## Fase 3: Refactorización de Diseño

### 3.1 Extraer lógica de SybaseExportService

**Archivo:** `src/VisorDBF.Core/Services/SybaseExportService.cs` (837 lines)  
**Problema:** God class. Supera las 500 lineas. Mezcla:
- Conexion y timeouts
- Carga de esquema de tabla via `syscolumns`
- Mapeo de tipos Sybase
- Construccion de funciones de conversion (~270 lineas)
- Batch processing con transacciones
- Fallback individual con reconexion
- Probe de conversiones
- Construccion de SQL INSERT
- Logging

**Estrategia de extraccion (propuesta):**
| Clase nueva | Responsabilidad | Metodos a mover | Lineas estimadas |
|-------------|----------------|-----------------|------------------|
| `SybaseConnectionFactory` | Construir y abrir conexion, timeouts | `BuildConnectionString`, logica de `OpenAsync` con timeout | ~60 |
| `SybaseSchemaLoader` | Cargar columnas de `syscolumns`, mapear tipos | `LoadColumnTypesAsync`, `MapSybaseTypeToOdbc` | ~80 |
| `SybaseTypeConverter` | Construir funciones de conversion por tipo | `BuildConvertFunction` (partir cada tipo en metodo propio) | ~300 |
| `SybaseBatchInserter` | Batch processing, transacciones, fallback | `TryProcessBatchAsync`, `ProcessIndividualWithFallbackAsync` | ~120 |

### 3.2 Extraer lógica de MainViewModel

**Archivo:** `src/VisorDBF.UI/ViewModels/MainViewModel.cs` (674 lines)  
**Problema:** God class. Supera las 500 lineas. Mezcla logica de:
- Apertura y recarga de archivos DBF
- Seleccion de encoding con dialogo
- Exportacion a TXT con progreso
- Transferencia a Sybase con probe
- Gestion de perfiles de exportacion
- Gestion de archivos recientes
- Persistencia de configuracion

**Estrategia de extraccion (propuesta):**
| Clase / Servicio nuevo | Responsabilidad |
|------------------------|-----------------|
| `FileLoadService` (o metodos privados en un partial) | Logica de `LoadFileAsync`, `OpenFileAsync`, `ChangeEncodingAsync` |
| `RecentFilesManager` | Gestion de archivos recientes (CRUD + persistencia) |
| Mover `ExportAsync` y `TransferToSybaseAsync` a servicios dedicados o al menos extraer a metodos mas pequenos |

### 3.3 Partir BuildConvertFunction en metodos por tipo

**Archivo:** `src/VisorDBF.Core/Services/SybaseExportService.cs:549-818`  
**Problema:** 270 lineas con 13+ switch arms, cada uno con el mismo patron try/catch duplicado.

**Solucion:** Crear una clase o metodos estaticos separados, uno por tipo:
- `ConvertToInt`, `ConvertToSmallInt`, `ConvertToTinyInt`, `ConvertToBigInt`
- `ConvertToNumeric`, `ConvertToFloat`, `ConvertToReal`
- `ConvertToMoney`, `ConvertToDateTime`, `ConvertToDate`, `ConvertToBit`
- `ConvertToString`

Cada metodo ~15-20 lineas en lugar de ~50 lineas por arm con duplicacion.

### 3.4 Reducir parametros de BuildLine

**Archivo:** `src/VisorDBF.Core/Services/TxtExportService.cs:83-89`  
**Problema:** 6 parametros (excede umbral de 5).

**Solucion:** Crear un objeto `ExportLineContext` que agrupe `ExportConfiguration`, `ColumnFormatConfiguration`, `IColumnFormatService`, `CultureInfo`.

---

## Fase 4: Optimizaciones de Rendimiento .NET

**Fuente:** Auditoria con skill `analyzing-dotnet-performance` — escaneo de anti-patrones de rendimiento sobre .NET 8.0.

**Hallazgos:** 7 items (2 🟡 Moderate, 5 ℹ️ Info), 0 🔴 Critical.

### 4.1 Sellar clases concretas no selladas (21 de 29)

**Archivos:** Todos los `public class`/`internal class` en `DbfReadException.cs`, `ExportException.cs`, `UnknownEncodingException.cs`, `DbfFile.cs`, `DbfRecord.cs`, `DbfReaderService.cs`, `EncodingDetectionService.cs`, `SybaseExportService.cs`, `TxtExportService.cs`, `BoolToVisibilityConverter.cs`, `ColumnFormatConverter.cs`, `InvertBoolConverter.cs`, `StringEqualsConverter.cs`, `StringNotEmptyToVisibilityConverter.cs`, `EncodingPickerViewModel.cs`, `ExportConfigurationViewModel.cs`, `ExportProgressDialogViewModel.cs`, `MainViewModel.cs`, `RelayCommand.cs`, `SaveProfileViewModel.cs`, `SybaseConnectionViewModel.cs`

**Problema:** El 72% de las clases concretas no estan selladas. El JIT no puede devirtualizar llamadas virtuales — hasta 500x mas lentas. Ninguna de estas clases tiene subclases en el codebase.

**Solucion:** Agregar `sealed` a las 21 clases. Escanear con:
```bash
grep -rn 'public class \|internal class ' src/ --include='*.cs' | grep -v 'sealed\|abstract\|static'
```

**Criterio de aceptacion:**
- Compila sin errores.
- `grep -c 'sealed class'` en `src/` aumenta de 8 a 29.

### 4.2 LINQ closure allocation por registro en TxtExportService.BuildLine (hot path)

**Archivo:** `src/VisorDBF.Core/Services/TxtExportService.cs:92-97`

**Problema:** `fields.Select(f => FormatValue(...))` en `BuildLine` se ejecuta **por cada registro exportado**. La lambda captura `record`, `columnFormats`, `formatService`, `numberCulture` — crea un closure + delegado por llamada. Para 100K registros son ~200K+ asignaciones en el GC heap.

**Solucion:** Reemplazar `Select` + `string.Join` con `StringBuilder` + `for` loop directo:

```csharp
var sb = new StringBuilder();
for (int i = 0; i < fields.Count; i++)
{
    if (i > 0) sb.Append(config.ColumnSeparator);
    sb.Append(FormatValue(record.Values.GetValueOrDefault(fields[i].Name),
        fields[i].Name, columnFormats, formatService, numberCulture));
}
if (!string.IsNullOrEmpty(config.RowEndDelimiter))
    sb.Append(config.RowEndDelimiter);
return sb.ToString();
```

**Criterio de aceptacion:**
- `BuildLine` no tiene llamadas a `fields.Select` ni `string.Join`.
- Pruebas de exportacion pasan con mismo output.

### 4.3 FrozenDictionary para diccionarios estaticos de solo lectura

**Archivos:**
- `src/VisorDBF.Core/Services/EncodingDetectionService.cs:15` — `LanguageDriverMap` (50 entradas)
- `src/VisorDBF.UI/ViewModels/ColumnFormatsViewModel.cs:10` — `PresetFormats` (4 entradas)

**Problema:** Ambos diccionarios son `static readonly Dictionary<,>` que nunca mutan despues de inicializacion. En .NET 8+, `FrozenDictionary` ofrece ~50% mejor rendimiento de busqueda y ~14x vs `ImmutableDictionary`.

**Solucion:** Agregar `.ToFrozenDictionary()` al inicializar:
```csharp
using System.Collections.Frozen;
private static readonly FrozenDictionary<byte, string> LanguageDriverMap =
    new Dictionary<byte, string> { ... }.ToFrozenDictionary();
```

**Criterio de aceptacion:**
- Tipos cambiados a `FrozenDictionary<,>`.
- Busquedas existentes (`TryGetValue`) siguen funcionando sin cambios.

### 4.4 Capacidad inicial para lista de registros en DbfReaderService

**Archivo:** `src/VisorDBF.Core/Services/DbfReaderService.cs:82`

**Problema:** `new List<Models.DbfRecord>()` sin capacidad inicial. El numero de registros esta disponible en el header del DBF (bytes 4-7) pero no se lee, causando multiples reallocaciones internas del `List<>` durante la lectura.

**Solucion:** Leer el record count del header y pasarlo como hint:
```csharp
int recordCount = BitConverter.ToInt32(header, 4);
var records = new List<Models.DbfRecord>(recordCount);
```

**Criterio de aceptacion:**
- `records.Capacity` >= `recordCount` despues de la construccion.
- DBFs de todos los tamanos se leen correctamente.

### 4.5 Redundant Encoding.RegisterProvider en metodos de instancia

**Archivos:**
- `src/VisorDBF.Core/Services/EncodingDetectionService.cs:100`
- `src/VisorDBF.UI/ViewModels/EncodingPickerViewModel.cs:103`
- `src/VisorDBF.UI/ViewModels/ExportConfigurationViewModel.cs:386`

**Problema:** `Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)` ya se llama en `App.xaml.cs` al inicio. Las llamadas redundantes en metodos de instancia son innecesarias (aunque idempotentes).

**Solucion:** Eliminar las 3 llamadas redundantes. Si se necesita garantia en pruebas unitarias, mantener solo en el constructor estatico de la clase o en el module initializer.

**Criterio de aceptacion:**
- `Encoding.GetEncoding("IBM850")` funciona sin `RegisterProvider` en esos metodos.
- Pruebas unitarias de `EncodingDetectionService` pasan.

### 4.6 LINQ Contains en char[] en SanitizeFileName

**Archivo:** `src/VisorDBF.Core/Services/JsonSettingsService.cs:92`

**Problema:** `invalid.Contains(c)` en `SanitizeFileName` usa LINQ sobre `char[]`, asignando enumerador + delegado. Metodo llamado solo en respaldo por corrupcion (cold path), pero reemplazo trivial.

**Solucion:** Usar `Array.IndexOf`:
```csharp
return string.Concat(name.Select(c => Array.IndexOf(invalid, c) >= 0 ? '_' : c));
```
O mejor aun, un `HashSet<char>` estatico con `Path.GetInvalidFileNameChars()`.

**Criterio de aceptacion:**
- Mismo comportamiento para nombres de archivo con caracteres invalidos.
- Sin cambios en API publica.

### 4.7 Per-call List/Dictionary allocations en metodos (cold paths)

**Archivos:** `ExportConfigurationViewModel.cs:285,308,327,368,397`, `SybaseExportService.cs:318,495,521`, `MainViewModel.cs:205`

**Problema:** Varios metodos crean `new List<...>(...)` o `new Dictionary<...>()` por cada invocacion. Al ser cold paths (acciones de usuario: CRUD de perfiles, carga de esquema), el impacto es minimo.

**Solucion:** No accionable a menos que el profiling muestre impacto. Registrado como deuda tecnica para revision futura.

**Criterio de aceptacion:**
- N/A — observacion, no tarea.

---

## Resumen de Tareas

| ID | Tarea | Archivo(s) | Esfuerzo | Prioridad |
|----|-------|-----------|----------|-----------|
| 1.1 | Fix memory leak RelayCommand | `RelayCommand.cs` | 1h | Critica |
| 1.2 | Eliminar empty catch en ParseDateValue | `DbfReaderService.cs` | 0.5h | Critica |
| 1.3 | Agregar innerException a ExportException | `SybaseExportService.cs` | 0.5h | Critica |
| 1.4 | Fix case-sensitive lookup Sybase | `SybaseExportService.cs` | 1h | Critica |
| 2.1 | Agregar GC.SuppressFinalize | `FileLogger.cs` | 0.25h | Media |
| 2.2 | Mejorar feedback de ProbeFirstRecordAsync | `SybaseExportService.cs` | 1h | Media |
| 2.3 | Eliminar dead code DbfDataReader | `VisorDBF.Core.csproj`, `DbfReaderService.cs` | 0.5h | Media |
| 2.4 | Unificar connection string Sybase | `SybaseExportService.cs`, `SybaseConnectionViewModel.cs` | 1h | Media |
| 3.1 | Extraer SybaseExportService en clases | `SybaseExportService.cs` | 4h | Diseno |
| 3.2 | Extraer MainViewModel | `MainViewModel.cs` | 3h | Diseno |
| 3.3 | Partir BuildConvertFunction | `SybaseExportService.cs` | 2h | Diseno |
| 3.4 | Reducir parametros BuildLine | `TxtExportService.cs` | 0.5h | Diseno |
| 4.1 | Sellar 21 clases concretas | Multiples archivos (ver §4.1) | 1h | Media |
| 4.2 | Eliminar LINQ closure en BuildLine | `TxtExportService.cs` | 1h | Media |
| 4.3 | FrozenDictionary para mapas estaticos | `EncodingDetectionService.cs`, `ColumnFormatsViewModel.cs` | 0.5h | Info |
| 4.4 | Capacidad inicial en lista de registros | `DbfReaderService.cs` | 0.5h | Info |
| 4.5 | Eliminar RegisterProvider redundante | `EncodingDetectionService.cs`, `EncodingPickerViewModel.cs`, `ExportConfigurationViewModel.cs` | 0.25h | Info |
| 4.6 | Optimizar SanitizeFileName | `JsonSettingsService.cs` | 0.25h | Info |

---

## Pasos Siguientes

1. **Ejecutar Fase 1** antes de cualquier otro cambio (bugs funcionales y memory leak).
2. **Ejecutar Fase 2** (limpieza y robustez).
3. **Ejecutar Fase 3** solo si hay presupuesto para refactorización estructural.
4. **Ejecutar Fase 4** (optimizaciones de rendimiento) — priorizar 4.1 y 4.2 si hay exportaciones grandes. 4.3-4.6 son de baja urgencia.
5. Despues de cada fase, ejecutar `dotnet build` y `dotnet test` para verificar que no se introdujeron regresiones.
