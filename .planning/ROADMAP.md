# Roadmap: VisorDBF v1.1 — Mejoras Técnicas

**4 phases** | **18 requirements** | All covered ✓
**Continues from:** v1.0 (phases 1-4) → v1.1 starts at Phase 5

---

## Phase 5: Correcciones Críticas

**Goal:** Resolver bugs funcionales con impacto directo en estabilidad y memory leaks

**Requirements:** CRIT-01, CRIT-02, CRIT-03, CRIT-04

**Success criteria:**

1. ViewModels de diálogos modales son recolectables por GC tras cerrar el diálogo
2. No hay bloques `catch { }` vacíos en el codebase
3. Toda excepción envuelta preserva `InnerException`
4. Registros con columnas Sybase en casing diferente al DBF se transfieren correctamente

**Files affected:** `RelayCommand.cs`, `DbfReaderService.cs`, `SybaseExportService.cs`

---

## Phase 6: Correcciones de Media Severidad ✓

**Goal:** Eliminar code smells y mejorar robustez en logging y manejo de errores

**Requirements:** MED-01, MED-02, MED-03, MED-04

**Status:** Complete (2026-07-10) — 1 plan, 4 tasks
**Wave dependencies:**

- Wave 1: All tasks (no cross-wave dependencies)

**Cross-cutting constraints:**

- Sybase connection string must be built from `SybaseConnectionConfig.BuildConnectionString()` — exact ODBC DRIVER format must be identical in all consumers
- Probe error details must flow from `SybaseExportService.ProbeFirstRecordAsync` through `ProbeResult` to the `MainViewModel` MessageBox

**Success criteria:**

1. `Dispose()` llama a `GC.SuppressFinalize(this)` — sin warning CA1816
2. El usuario ve mensaje explicativo cuando el probe de Sybase falla
3. `DbfDataReader` eliminado del `.csproj` y del código; proyecto compila y tests pasan
4. Un solo punto de definición del connection string Sybase; todos los lugares lo referencian

**Files affected:** `FileLogger.cs`, `SybaseExportService.cs`, `VisorDBF.Core.csproj`, `SybaseConnectionViewModel.cs`, `SybaseConnectionConfig.cs`, `ISybaseExportService.cs`, `MainViewModel.cs`

---

## Phase 7: Refactorización de Diseño

**Goal:** Reducir complejidad de god classes y eliminar duplicación de código

**Requirements:** REF-01, REF-02, REF-03, REF-04

**Status:** Planned (1 plan, 4 tasks)
**Wave dependencies:**

- Wave 1: All tasks (REF-03 depends on REF-01 extraction)

**Cross-cutting constraints:**

- REF-01 (extract classes) must complete before REF-03 (split BuildConvertFunction) since REF-03 operates on the extracted `SybaseValueConverter` class
- All extractions must preserve existing public API contracts so callers don't break

**Success criteria:**

1. `SybaseExportService` reducido de 837 a <400 líneas (4 nuevas clases)
2. `MainViewModel` reducido de 674 a <450 líneas
3. `BuildConvertFunction` partida en 8+ métodos por tipo, sin duplicación try/catch
4. `BuildLine` reducida de 6 parámetros a objeto `ExportLineContext`

**Files affected:** `SybaseExportService.cs`, `MainViewModel.cs`, `TxtExportService.cs`, + 6 nuevas clases (SybaseColumnMappingService, SybaseValueConverter, SybaseProbeService, SybaseTransferHelper, ExportHelper, ExportLineContext)

---

## Phase 8: Optimizaciones de Rendimiento .NET

**Goal:** Aplicar optimizaciones de rendimiento identificadas en auditoría .NET

**Requirements:** PERF-01, PERF-02, PERF-03, PERF-04, PERF-05, PERF-06

**Success criteria:**

1. 21 clases selladas — `grep -c 'sealed class'` aumenta de 8 a 29
2. `TxtExportService.BuildLine` sin `fields.Select` ni `string.Join` (elimina closure por registro)
3. `EncodingDetectionService.LanguageDriverMap` y `ColumnFormatsViewModel.PresetFormats` usan `FrozenDictionary`
4. `DbfReaderService` usa `List<DbfRecord>(recordCount)` con capacidad inicial
5. `Encoding.RegisterProvider` eliminado de 3 métodos de instancia
6. `JsonSettingsService.SanitizeFileName` sin LINQ `Contains` en `char[]`

**Files affected:** 21 archivos (sealed), `TxtExportService.cs`, `EncodingDetectionService.cs`, `ColumnFormatsViewModel.cs`, `DbfReaderService.cs`, `EncodingPickerViewModel.cs`, `ExportConfigurationViewModel.cs`, `JsonSettingsService.cs`

---

## Summary

| Phase | Goal | Reqs | Plans | Status |
|-------|------|------|-------|--------|
| 5 | Correcciones Críticas | 4 | 1 | ✓ Complete |
| 6 | Media Severidad | 4 | 1 | ✓ Complete |
| 7 | Refactorización | 1/1 | Complete    | 2026-07-10 |
| 8 | Rendimiento .NET | 6 | — | ○ Pending |
| **Total** | | **18** | **2** | |

---

*Roadmap created: 2026-07-10*
*Last updated: 2026-07-10*
