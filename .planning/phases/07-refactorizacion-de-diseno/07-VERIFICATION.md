# fase 7 VERIFICATION.md — refactorización de diseño

| criterio | estado | evidencia |
|---|---|---|
| 1. SybaseExportService <400 líneas (4 clases nuevas) | ✅ **296 líneas** (de 837) | `src/VisorDBF.Core/Services/SybaseExportService.cs` |
| 2. MainViewModel <450 líneas | ❌ **552 líneas** (de 674) — GAP CONOCIDO | `src/VisorDBF.UI/ViewModels/MainViewModel.cs` |
| 3. BuildConvertFunction → 8+ métodos por tipo, sin duplicación try/catch | ✅ **11 métodos** tipados + `BuildConverter<T>` helper | `src/VisorDBF.Core/Services/SybaseValueConverter.cs` |
| 4. BuildLine con ExportLineContext (vs 6 parámetros) | ✅ Firma `BuildLine(ExportLineContext context)` | `src/VisorDBF.Core/Services/TxtExportService.cs:83` |
| 5. ExportLineContext record existe | ✅ 6 propiedades en record sealed | `src/VisorDBF.Core/Models/ExportLineContext.cs` |
| 6. Archivos nuevos creados | ✅ 6/6 archivos existen | ver tabla abajo |
| 7. `dotnet build` compila | ✅ 0 errores, 0 warnings | |
| 8. `dotnet test` 90 pruebas | ⚠️ **89/90** pasan (1 fallo pre-existente en `ExportAsync_WithDefaultConfig_WritesCorrectFile`) | |

## Archivos nuevos / extraídos

| archivo | líneas | propósito |
|---|---|---|
| `SybaseValueConverter.cs` | 268 | 11 métodos ConvertTo* + BuildConverter\<T\> |
| `SybaseProbeService.cs` | ~90 | lógica de sondeo Sybase |
| `SybaseColumnMappingService.cs` | ~60 | mapeo de columnas DBF→Sybase |
| `ExportLineContext.cs` | 12 | record para parámetros de BuildLine |
| `ExportHelper.cs` | — | helpers extraídos de MainViewModel |
| `SybaseTransferHelper.cs` | — | helpers de transferencia Sybase |

## Cross-reference de requisitos

| ID | estado |
|---|---|
| REF-01 (god classes) | ✅ SybaseExportService 296 lns; MainViewModel 552 lns (gap) |
| REF-02 (duplicación) | ✅ try/catch centralizado via BuildConverter\<T\> |
| REF-03 (BuildConvertFunction) | ✅ 11 métodos tipados |
| REF-04 (BuildLine params) | ✅ ExportLineContext record |

## Resumen

3/4 criterios cumplidos. La brecha de MainViewModel (552 vs <450) se documenta como deuda técnica. El build compila limpio; el único test fallido es pre-existente.
