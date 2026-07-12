# Plan de modificaciones: Prevención de colapso del log segment en traspaso Sybase

## Problema

El traspaso masivo de registros DBF a Sybase ASE no gestiona el log transaccional de la base de datos destino. En transferencias grandes (>10,000 registros), el `logsegment` puede llenarse si la base de datos no tiene `trunc log on chkpt` habilitado, provocando:

- Error 1105 de Sybase ("The log segment in database 'X' has filled")
- Aborto de la transferencia con mensaje genérico
- Posible indisponibilidad temporal de la base de datos hasta que se libere espacio en el log

## Arquitectura actual

```
TryProcessBatchAsync (1000 registros por lote)
  ├─ BEGIN TRAN
  ├─ 1000× INSERT
  ├─ COMMIT
  └─ Sin DUMP TRAN entre lotes

ProcessIndividualWithFallbackAsync (fallback 1×1)
  └─ Sin transacción explícita (auto-commit)
     └─ Sin DUMP TRAN entre registros
```

No hay pre-check de espacio, no hay truncamiento periódico, no hay detección específica de log lleno.

## Modificaciones propuestas

### 1. Pre-check de espacio disponible en el logsegment

**Archivo:** `src/VisorDBF.Core/Services/SybaseExportService.cs`

**Qué hacer:**

Antes de iniciar el loop de batches (`for (int batchStart = 0; ...)` en `TransferAsync`), ejecutar una consulta SQL para verificar espacio libre en el log:

```sql
SELECT
  lct.segmap,
  sum(u.size * u.unitsize / 1024) as total_kb,
  sum(case when u.curunreservedpgs > 0
           then (u.size - u.curunreservedpgs) * u.unitsize / 1024
           else u.size * u.unitsize / 1024
      end) as used_kb
FROM master..sysusages u,
     master..syslogins l,
     master..sysdatabases d,
     sysindexes i,
     master..syssegments s,
     sysusages u2,
     master..syslctable lct
WHERE ... (consulta estándar para verificar espacio en log)
```

O alternativamente usar `sp_helpdb` o `sp_spaceused` vía ODBC.

**Criterio:** Si el log disponible es menor a un umbral (ej. 2× el tamaño estimado de un batch), advertir al usuario y permitir continuar o cancelar.

**Impacto:** Bajo. Consulta única, lectura ligera.

---

### 2. DUMP TRANSACTION periódico entre lotes (opt-in)

**Archivo:** `src/VisorDBF.Core/Services/SybaseExportService.cs`

**Qué hacer:**

Agregar un contador de lotes procesados. Cada N lotes (configurable, ej. cada 10 lotes = 10,000 registros), ejecutar:

```sql
DUMP TRANSACTION <database_name>
```

**Detalles de implementación:**

- Nuevo parámetro opcional `DumpTransactionInterval` en `SybaseConnectionConfig` (0 = deshabilitado, valor por defecto = 0)
- Crear un nuevo `OdbcCommand` independiente por cada `DUMP TRAN` (no usar el mismo command que el INSERT)
- Ejecutar fuera de la transacción activa, entre un `COMMIT` y el siguiente `BEGIN TRAN`
- Capturar errores de permisos (puede lanzar error si el usuario no tiene permisos de `dump operator`) y solo loguearlos sin abortar la transferencia

**Ubicación exacta en el código:** Después del `COMMIT` exitoso en `TryProcessBatchAsync` (línea 227), o en el loop principal de `TransferAsync` (entre líneas 123-153), en el ámbito del caller donde se tiene acceso a `config.Database`.

**Consideraciones:**

- **Permisos:** El usuario de conexión necesita rol `dump operator`. Capturar error y continuar.
- **Cadena de logs:** `DUMP TRANSACTION` sin dispositivo rompe la cadena de logs. Debe ser opt-in y documentado.
- **Opción recomendada:** Deshabilitado por defecto (`DumpTransactionInterval = 0`).

**Código sugerido:**

```csharp
// En TransferAsync, después de TryProcessBatchBatch exitoso
if (config.DumpTransactionInterval > 0 && batchCount % config.DumpTransactionInterval == 0)
{
    await DumpTransactionAsync(connection, config.Database, log);
}

// Nuevo método
private static async Task DumpTransactionAsync(OdbcConnection connection, string database, FileLogger log)
{
    try
    {
        await using var cmd = new OdbcCommand($"DUMP TRANSACTION {database}", connection);
        cmd.CommandTimeout = 60;
        await cmd.ExecuteNonQueryAsync();
        log.WriteLine($"DUMP TRANSACTION {database} completado");
    }
    catch (Exception ex)
    {
        log.WriteLine($"DUMP TRANSACTION falló (permisos?): {ex.Message}");
        // No abortar - continuar transferencia
    }
}
```

**Impacto:** Medio. Cambio localizado. Funcionalidad opt-in segura.

---

### 3. Detección específica del error 1105 (log full)

**Archivo:** `src/VisorDBF.Core/Services/SybaseExportService.cs`

**Qué hacer:**

En los bloques `catch` de `TryProcessBatchAsync` y `ProcessIndividualWithFallbackAsync` (y en el `catch` general de `TransferAsync`), analizar el mensaje de error para detectar código de error Sybase 1105.

```csharp
private static bool IsLogFullError(Exception ex)
{
    return ex.Message.Contains("1105")
        || ex.Message.Contains("log segment")
        || ex.Message.Contains("has filled")
        || ex.Message.Contains("log is full");
}

// Uso:
catch (Exception ex) when (IsLogFullError(ex))
{
    log.WriteLine($"LOG SEGMENT LLENO: {ex.Message}");
    throw new ExportException(
        "La base de datos destino quedó sin espacio en el log transaccional. " +
        "Solicite a su administrador de base de datos que ejecute 'DUMP TRANSACTION <base>' " +
        "o aumente el tamaño del logsegment antes de reintentar.",
        config.TableName, ex);
}
```

**Impacto:** Bajo. Solo cambio en cadenas de error. Mejora significativa en UX.

---

### 4. BatchSize configurable

**Archivo:** `src/VisorDBF.Core/Models/SybaseConnectionConfig.cs`
**Archivo:** `src/VisorDBF.Core/Services/SybaseExportService.cs`
**Archivo:** `src/VisorDBF.UI/ViewModels/SybaseConnectionViewModel.cs`

**Qué hacer:**

- Agregar propiedad `BatchSize` en `SybaseConnectionConfig` con valor por defecto 1000
- Modificar `SybaseExportService.TransferAsync` para aceptar `config.BatchSize` en lugar de la constante `BatchSize = 1000`
- Agregar campo opcional en `SybaseConnectionDialog.xaml` para que el usuario pueda configurarlo

```csharp
// SybaseConnectionConfig.cs
public int BatchSize { get; init; } = 1000;

// SybaseExportService.cs - en el loop de batches
int batchSize = config.BatchSize > 0 ? config.BatchSize : 1000;
for (int batchStart = 0; batchStart < total; batchStart += batchSize)
{
    int batchEnd = Math.Min(batchStart + batchSize, total);
    ...
}
```

**Impacto:** Medio-Bajo. Cambios en modelo, servicio y UI. No rompe compatibilidad (default = 1000).

---

### 5. Advertencia temprana en UI si el log está comprometido

**Archivo:** `src/VisorDBF.UI/ViewModels/SybaseConnectionViewModel.cs`
**Archivo:** `src/VisorDBF.UI/Views/SybaseConnectionDialog.xaml`

**Qué hacer:**

Después de `TestConnectionAsync`, si la conexión es exitosa, ejecutar `sp_spaceused` o la consulta de logspace y mostrar una advertencia si el espacio disponible es bajo.

```csharp
// SybaseConnectionViewModel.cs
public string? LogSpaceWarning { get; set; }

public async Task TestConnectionAsync()
{
    // ... test existente ...
    
    if (isSuccess)
    {
        var (totalLogMb, usedLogMb) = await CheckLogSpaceAsync();
        var freePercent = 100 - (usedLogMb * 100.0 / totalLogMb);
        if (freePercent < 20)
            LogSpaceWarning = $"ADVERTENCIA: El log de la base de datos '{Database}' tiene solo {freePercent:F0}% libre ({totalLogMb - usedLogMb} MB). " +
                              "Transferencias grandes podrían llenarlo. Considere aumentar el logsegment.";
    }
}
```

**Impacto:** Bajo. Solo informativo. No bloquea la operación.

---

## Resumen de cambios por archivo

| Archivo | Cambio | Prioridad |
|---------|--------|-----------|
| `SybaseConnectionConfig.cs` | Nuevas propiedades: `BatchSize`, `DumpTransactionInterval` | Alta |
| `SybaseExportService.cs` | Pre-check de log, DUMP TRAN periódico, detección error 1105, BatchSize dinámico | Alta |
| `SybaseConnectionViewModel.cs` | Verificación de espacio en log después de TestConnection | Media |
| `SybaseConnectionDialog.xaml` | Campo opcional de BatchSize + advertencia de log | Media |
| `SybaseTransferHelper.cs` | Pasar nuevos parámetros al flujo de transferencia | Baja |

## Prioridades de implementación

1. **Inmediata (crítica):** Ítem 3 (detección error 1105) + Ítem 4 (BatchSize configurable)
2. **Recomendada:** Ítem 2 (DUMP TRAN periódico opt-in)
3. **Mejora:** Ítem 1 (pre-check) + Ítem 5 (advertencia en UI)

## Notas técnicas adicionales

- Sybase ASE 15.7 usa `DUMP TRANSACTION <db> WITH NO_LOG` o `DUMP TRANSACTION <db> WITH TRUNCATE_ONLY` para truncar sin backup. Estas opciones están deprecadas en versiones más nuevas pero funcionan en 15.7.
- El pre-check de logspace se puede hacer consultando `master.dbo.sysusages` y `sysindexes` pero requiere permisos de `sa`. Alternativa más portable: `sp_helpdb <db>` parseando el resultado.
- Para no interferir con la cadena de backups productiva, `DumpTransactionInterval` debe estar **deshabilitado por defecto (0)** y el usuario debe activarlo explícitamente.
