# Traspaso directo a Sybase ASE

## Concepto

Volcar las filas del DBF actual directamente a una tabla existente en una base de datos Sybase ASE, usando ODBC en lugar de un archivo SQL intermedio.

## Arquitectura general

```
MainViewModel (orquestador)
  │
  ├─ ConfigureSybaseAsync()
  │    └─ SybaseConnectionViewModel → SybaseConnectionDialog
  │         ├─ TestConnectionAsync()    ← conexión ODBC directa (sin probe de tabla)
  │         └─ Save() → guarda SybaseConnectionConfig
  │
  └─ TransferToSybaseAsync()
       ├─ _exportCts = new CancellationTokenSource()
       ├─ ExportProgressDialog (ventana modal con progreso)
       │
       └─ Task.Run:
            ├─ 1. ProbeFirstRecordAsync()   ← conexión propia (aislada)
            │      si falla → MessageBox + cierra diálogo, FIN
            │      si ok    → continúa
            │
            └─ 2. TransferAsync()           ← conexión separada (solo si probe pasó)
                   └─ batch loop (1000) con fallback individual
```

**ProbeFirstRecordAsync** y **TransferAsync** usan conexiones ODBC independientes. Si el probe falla, nunca se abre la conexión de transferencia. Si el probe se cuelga en el close del driver ODBC, la transferencia no se ve afectada.

## Archivos

| Archivo | Descripción |
|---|---|
| `src/VisorDBF.Core/Models/SybaseConnectionConfig.cs` | Record con config de conexión |
| `src/VisorDBF.Core/Services/SybaseExportService.cs` | Servicio principal: probe + transferencia |
| `src/VisorDBF.Core/Services/ISybaseExportService.cs` | Interfaz del servicio |
| `src/VisorDBF.Core/Logging/FileLogger.cs` | Logger a archivo con timestamp |
| `src/VisorDBF.UI/ViewModels/MainViewModel.cs` | Orchestrador: configuración → probe → transfer |
| `src/VisorDBF.UI/ViewModels/SybaseConnectionViewModel.cs` | ViewModel del diálogo de conexión (test + save) |
| `src/VisorDBF.UI/Views/SybaseConnectionDialog.xaml` | Diálogo de configuración de conexión |
| `src/VisorDBF.UI/Views/SybaseConnectionDialog.xaml.cs` | Code-behind: sincroniza PasswordBox |

## Constantes globales (SybaseExportService)

| Constante | Valor | Uso |
|---|---|---|
| `BatchSize` | 1000 | Registros por transacción |
| `ConnectionTimeoutSec` | 30 | Timeout en connection string ODBC |
| `CommandTimeoutSec` | 300 | Timeout de comandos ODBC (INSERT, SELECT) |
| `OpenAsyncTimeoutSec` | 60 | Timeout para `connection.OpenAsync()` vía `Task.WhenAny` |
| `MaxConsecutiveErrors` | 1 | Fallos consecutivos permitidos antes de abortar |

El timeout de apertura asíncrona (60s) es independiente del `Connection Timeout=30` del connection string. Se implementa con `Task.WhenAny(openTask, Task.Delay(60s))`. Si gana el delay, se lanza `ExportException`.

## SybaseConnectionConfig

```csharp
public sealed record SybaseConnectionConfig
{
    string Host { get; init; } = "";
    int    Port { get; init; } = 5000;
    string Database { get; init; } = "";
    string Username { get; init; } = "";
    string Password { get; init; } = "";
    string TableName { get; init; } = "";
    bool   IsValid { get; }  // Host, Port, Database, Username, TableName deben tener valor
}
```

`IsValid` no exige Password — la contraseña puede estar vacía si la autenticación no la requiere.

## Conexión ODBC

Se usa `System.Data.Odbc` con el driver **Adaptive Server Enterprise** de Sybase (ODBC nativo de Windows).

### Connection string

```
DRIVER={Adaptive Server Enterprise};
Server={Host};
Port={Port};
Database={Database};
UID={Username};
PWD={Password};
Connection Timeout=30;
```

### SybaseConnectionViewModel (diálogo de conexión)

El ViewModel del diálogo (`SybaseConnectionViewModel`) orquesta la interacción:

#### TestConnectionAsync

Abre una conexión ODBC directa (sin consultar esquema de tabla) para verificar que el servidor responde. No incluye `Connection Timeout` en el connection string de prueba.

Éxito → `IsTestSuccessful = true`, mensaje "Conexion exitosa."
Fallo → `IsTestSuccessful = false`, mensaje de error, `DetailedError` con stack trace completo, y `CopyDetailsText` con connection string (PWD enmascarado) + error.

#### CopyDetailsCommand

Copia al portapapeles el detalle técnico (connection string + error) para diagnóstico.

#### PasswordBox

WPF `PasswordBox` no soporta binding bidireccional. El code-behind sincroniza manualmente:
- `OnLoaded`: copia `vm.Password` → `PasswordBox.Password`
- `PropertyChanged("Password")`: refleja cambios del VM hacia el PasswordBox
- `BtnGuardar_Click`: llama `vm.SetPasswordFromDialog(PasswordBox.Password)` antes de ejecutar SaveCommand

#### SaveCommand

Construye `SybaseConnectionConfig` y lo entrega al `MainViewModel` vía callback. Solo se habilita si `BuildConfig().IsValid` es true.

### Diálogo SybaseConnectionDialog

Campos: Host, Puerto, Base de datos, Usuario, Contraseña (PasswordBox), Tabla destino.
Botones: "Probar conexion" (TestConnectionCommand), "Guardar" (SaveCommand), "Cancelar".
Sección expandible "Ver detalle tecnico" con error completo + botón "Copiar detalle".
El connection string se muestra enmascarado (PWD=***) debajo del resultado.

## ProbeFirstRecordAsync (validación previa)

Método público en `ISybaseExportService`:

```csharp
Task<bool> ProbeFirstRecordAsync(
    DbfFile file,
    SybaseConnectionConfig config,
    CancellationToken cancellationToken);
```

### Lógica interna

```
1. Crear su PROPIA OdbcConnection (independiente de TransferAsync)
2. Abrir conexión con timeout asíncrono de 60s
3. Si no puede conectar → log + return false
4. Cargar columnas de la tabla destino vía LoadColumnTypesAsync
5. Tomar el primer registro no-deleted del DBF
6. Si no hay registros → return true (no hay nada que probar)
7. Ejecutar ProbeConversionsViaSybaseAsync
8. Si ProbeConversionsViaSybaseAsync lanza ExportException → log + return false
9. Si ok → return true
10. Cerrar conexión (finally vía await using)
```

### Conexión aislada

ProbeFirstRecordAsync **no comparte conexión** con TransferAsync. Esto asegura que:
- Si el ODBC driver se cuelga al cerrar la conexión del probe, solo afecta al probe.
- Si el probe pasa, TransferAsync abre una conexión fresca y saludable.
- El probe puede portarse a otra funcionalidad independiente.

### ProbeConversionsViaSybaseAsync (método privado)

Loop interno que prueba la conversión de cada columna del primer registro contra Sybase.

```csharp
foreach (var col in columns)
{
    var rawValue = firstRecord.Values.GetValueOrDefault(col.Name);
    object? converted;
    try { converted = col.Convert(rawValue); }
    catch { errors.Add(...); continue; }          // C# falla → error acumulado

    if (converted is null or DBNull) continue;    // nulls no se prueban

    string? convertType = GetConvertTypeForProbe(col);
    if (convertType is null) continue;             // varchar/text no necesita CONVERT

    // Enviar SELECT CONVERT(type, ?) a Sybase
    try { await cmd.ExecuteScalarAsync(...); }
    catch { errors.Add(...); }                     // Sybase rechaza → error acumulado
}
```

Si hay errores → lanza `ExportException` con todos los errores acumulados (que el caller captura como `return false`).

### GetConvertTypeForProbe

Mapea type names Sybase a strings de CONVERT. Cuando `Precision == 0` (no reportada por la tabla), usa `NUMERIC(18,2)` como valor por defecto.

| DbTypeName | CONVERT |
|---|---|
| int/integer | INT |
| smallint | SMALLINT |
| tinyint | TINYINT |
| bigint | BIGINT |
| numeric/decimal | NUMERIC(p,s) o NUMERIC(18,2) |
| float | FLOAT |
| real | REAL |
| money/smallmoney | NUMERIC(p,s) o NUMERIC(18,2) |
| date | DATE |
| datetime/smalldatetime | DATETIME |
| varchar/char/text/binary/etc | `null` (no se prueba) |

## TransferAsync (transferencia masiva)

```csharp
Task TransferAsync(
    DbfFile file,
    SybaseConnectionConfig config,
    IProgress<int> progress,
    CancellationToken cancellationToken);
```

### Lógica interna

```
1. Crear archivo de log en %TEMP%
2. Loggear encabezado: host, database, table, recordCount, MaxConsecutiveErrors, log path
3. Crear OdbcConnection (conexión independiente del probe)
4. Abrir conexión con timeout asíncrono de 60s
5. Cargar columnas vía LoadColumnTypesAsync
6. Construir INSERT con CONVERT() para columnas numeric/decimal/money/smallmoney
7. Recorrer file.Records en batches de BatchSize (1000):
   a. TryProcessBatchAsync:
      - BEGIN TRAN (OdbcTransaction)
      - Para cada fila:
         * Si IsDeleted → skip (contado como skipped)
         * SetParameterValues() → asigna valores convertidos
         * command.ExecuteNonQueryAsync()
      - COMMIT TRAN
      - Retorna (processed, skipped, failed=false)
   b. Si el batch falla → ROLLBACK + ProcessIndividualWithFallbackAsync:
      - Sin transacción
      - Por cada fila:
         * Si IsDeleted → skip
         * SetParameterValues() → ExecuteNonQueryAsync
         * Si falla: consecutiveErrors++
           - Si consecutiveErrors >= MaxConsecutiveErrors → ExportException + abort
           - Si no → skip + reset consecutiveErrors
   c. Reportar progreso via IProgress<int>
   d. Cada 10k registros: loggear progreso con elapsed time
8. Cerrar conexión en finally externo (solo si está Open)
```

### INSERT SQL generado

Solo las columnas **numeric/decimal/money/smallmoney** se envuelven en `CONVERT(NUMERIC(p,s), @p)`. Las demás columnas usan `@p` directamente.

```sql
INSERT INTO table_name (
  col_numeric, col_varchar, col_date, col_money
) VALUES (
  CONVERT(NUMERIC(18,2), @p0),
  @p1,
  @p2,
  CONVERT(NUMERIC(18,2), @p3)
)
```

### Fallos de precisión

Si `Precision == 0` o `Scale == 0` en la columna destino, se usan `NUMERIC(18,0)` como default. `SetParameterValues` loggea una advertencia cuando un valor decimal excede la capacidad entera de la columna (`info.Precision - info.Scale`).

## LoadColumnTypesAsync

Consulta el esquema de la tabla destino vía SQL directo:

```sql
SELECT c.name, t.name, c.prec, c.scale
FROM syscolumns c, sysobjects o, systypes t
WHERE c.id = o.id AND o.name = ? AND c.usertype = t.usertype
ORDER BY c.colid
```

### Validaciones

- Si `dbCols.Count == 0` → `ExportException`: "La tabla '{tableName}' no existe o no tiene columnas."
- Si después del matching por nombre contra `file.Fields` el resultado es 0 → `ExportException`: "Ninguna columna del DBF coincide..."
- El matching de nombres es case-insensitive (`StringComparer.OrdinalIgnoreCase`).

### MapSybaseTypeToOdbc

Siempre retorna `OdbcType.VarChar`. Todos los parámetros se envían como strings.

## ColumnInfo

Estructura interna que mapea una columna destino:

```csharp
ColumnInfo(
    string Name,          // nombre en Sybase
    OdbcType OdbcType,    // siempre VarChar
    string DbTypeName,    // type name nativo (int, numeric, varchar...)
    byte Precision,
    byte Scale,
    Func<object?, object?> Convert);  // función de conversión C#
```

## BuildConvertFunction

Retorna una función lambda que convierte el valor raw del DBF al string que Sybase espera. Cada función incluye validación de tipo y lanza `ExportException` si la conversión falla.

### Tipos con ValidatePrecision (decimal/numeric/money/smallmoney)

Validan que la parte entera del valor no exceda `(Precision - Scale)` dígitos. Si excede, lanzan `ExportException`.

### Tabla de conversiones

| Tipo Sybase | Conversión C# | String de salida |
|---|---|---|
| int/integer | `int.TryParse` o `Convert.ToInt32` | `"12345"` |
| smallint | `short.TryParse` o `Convert.ToInt16` | `"123"` |
| tinyint | `byte.TryParse` o `Convert.ToByte` | `"1"` |
| bigint | `long.TryParse` o `Convert.ToInt64` | `"123456789"` |
| numeric/decimal | `decimal.TryParse` + `ValidatePrecision` + `Convert.ToDecimal` | `"1360221.37"` (InvariantCulture) |
| float | `double.TryParse` o `Convert.ToDouble` | `"123.456"` |
| real | `float.TryParse` o `Convert.ToSingle` | `"123.456"` |
| money/smallmoney | `decimal.TryParse` + `ValidatePrecision` + `Convert.ToDecimal` | `"1360221.3700"` |
| datetime/smalldatetime | DateTime.TryParse cascade → ISO | `"2026-01-31 00:00:00"` |
| date | DateTime.TryParse cascade → ISO | `"2026-01-31"` |
| bit | bool/string extendido ("T","Y","TRUE","YES","1") → "1"/"0" | `"1"` o `"0"` (catch-all → `"0"`) |
| varchar/char/nchar/nvarchar | `.TrimEnd()` directo | sin cambios |
| text u otros | `.TrimEnd()` (default wildcard) | sin cambios |

### DateTime parsing cascade

Para `datetime`, `smalldatetime` y `date`, cuando el valor llega como string:

1. `CultureInfo.CurrentCulture` (es-AR → acepta dd/MM/yyyy)
2. `CultureInfo.InvariantCulture` (acepta MM/dd/yyyy)
3. `TryParseExact` con formatos: `"dd/MM/yyyy"`, `"dd/MM/yyyy HH:mm:ss"`, `"yyyy-MM-dd"`, `"yyyy-MM-dd HH:mm:ss"`

Output siempre ISO (`yyyy-MM-dd HH:mm:ss` o `yyyy-MM-dd`).

## FileLogger

Escribe logs con timestamp a `%TEMP%\VisorDBF_Sybase_yyyyMMdd_HHmmss.log` (transferencia) o `%TEMP%\VisorDBF_Probe_yyyyMMdd_HHmmss.log` (probe).

### Propiedades

- `LogPath` (readonly): expone la ruta del archivo de log.

### Métodos

```csharp
void WriteLine(string message,
    [CallerMemberName] string member = "",
    [CallerLineNumber] int line = 0);

void WriteLine(string message, TimeSpan elapsed,
    [CallerMemberName] string member = "",
    [CallerLineNumber] int line = 0);
```

### Formato

```
[2026-01-31 14:30:00.123] [TransferAsync:35] === Sybase Transfer Start ===
[2026-01-31 14:30:00.456] [TransferAsync:87] Connection opened successfully (elapsed: 1.234s)
```

### Detalles de implementación

- `AutoFlush = true`: cada `WriteLine` escribe inmediatamente a disco.
- Thread-safe: las operaciones de escritura se serializan con `lock (_lock)`.
- `Dispose()` escribe "=== Log ended ===" antes de cerrar el StreamWriter.

## MainViewModel.TransferToSybaseAsync

```csharp
private async Task TransferToSybaseAsync()
{
    _exportCts = new CancellationTokenSource();
    var progressVm = new ExportProgressDialogViewModel(...);
    var progressDialog = new ExportProgressDialog { DataContext = progressVm };

    var progress = new Progress<int>(processed =>
    {
        progressVm.ProcessedRecords = processed;
        ExportProgressPercent = (double)processed / totalRecords * 100;
    });

    IsExporting = true;

    var transferTask = Task.Run(async () =>
    {
        // 1. Probe con conexión propia
        var probeOk = await _sybaseExportService.ProbeFirstRecordAsync(...);
        if (!probeOk)
        {
            syncContext.Post(_ => { MessageBox.Show(...); progressDialog.Close(); }, null);
            return;
        }

        // 2. Transferencia
        await _sybaseExportService.TransferAsync(...);

        syncContext.Post(_ => progressVm.IsComplete = true, null);
    });

    progressDialog.ShowDialog();
    await transferTask;

    // finally: IsExporting = false, _exportCts?.Dispose()
}
```

### Flujo completo con UI

1. `ConfigureSybaseAsync()`: abre `SybaseConnectionDialog`. El usuario configura y guarda.
2. `TransferToSybaseAsync()`:
   - Crea `CancellationTokenSource` y `ExportProgressDialog` (modal).
   - Muestra el diálogo de progreso y ejecuta probe + transferencia en `Task.Run`.
   - El progreso se reporta vía `Progress<int>` → actualiza barra y `ExportProgressPercent`.
   - Si probe falla: MessageBox + cierra diálogo.
   - Si transferencia falla (`ExportException` o genérica): MessageBox + cierra diálogo.
   - Si cancelación (`OperationCanceledException`): `progressVm.IsCancelled = true`.
   - En `finally`: `IsExporting = false`, dispose del CTS.
   - Catch externo de `OperationCanceledException`: vacío (caso raro de `ShowDialog` cancelado).

## Error handling

| Escenario | Comportamiento |
|---|---|
| No se puede conectar (timeout 60s o error) | ExportException con mensaje claro, log |
| Conexión timeout asíncrono (60s) | ExportException: "No se pudo conectar... en 60s" |
| Probe: tabla no existe | ExportException en LoadColumnTypesAsync → return false |
| Probe: DBF vacío (sin registros) | Return true (no hay nada que probar) |
| Probe: C# conversion falla | Acumula error, continúa con otras columnas |
| Probe: Sybase CONVERT falla | Acumula error, continúa con otras columnas |
| Probe: errores acumulados > 0 | ExportException → catch en probe → return false |
| Transfer: batch falla | Rollback + reintento individual |
| Transfer: fila individual falla | skip + consecutiveErrors++ |
| Transfer: consecutiveErrors >= MaxConsecutiveErrors (1) | ExportException + abort |
| Transfer: valor decimal excede precisión de columna | ValidatePrecision → ExportException en esa fila |
| Cancelación del usuario (CTS.Cancel) | OperationCanceledException + progressVm.IsCancelled |
| Connection.Close() se cuelga (driver ODBC) | Solo afecta al probe (conexión aislada). Transferencia usa conexión separada. |
| finally de conexión | Solo ejecuta `connection.Close()` si `connection.State == ConnectionState.Open` |
