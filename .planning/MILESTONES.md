# Project Milestones: VisorDBF

## v1.2 — Columnas Personalizadas en Traspaso Sybase (Shipped: 2026-07-12)

**Delivered:** Columnas adicionales configurables con tipo datetime/integer en traspaso Sybase, validación en tiempo real, persistencia entre sesiones, probe extendido, ventana Acerca De.

**Phases completed:** 3 phases, 3 plans, 23 tasks

**Key accomplishments:**

- Columna adicional tipo enum (ExtraColumnType) + record ExtraColumnConfig con virtual ColumnInfo injection en SybaseExportService
- UI en SybaseConnectionDialog con ItemsControl, add/remove commands y validación por tipo
- Persistencia automática en settings.json mediante System.Text.Json con default empty-list para legacy
- Probe extendido con columnas extra concatenadas y conversiones verificadas
- Ventana Acerca De con nombre, versión (assembly), features y año de copyright
- 12 nuevos tests, 0 regresiones, 105 tests en total

---

## v1.1 — Mejoras Técnicas (Shipped: 2026-07-11)

**Delivered:** Correcciones críticas, refactorización de diseño y optimizaciones de rendimiento .NET sobre la base v1.0.

**Phases completed:** 4 phases, 4 plans, 18 tasks

**Key accomplishments:**

- Correcciones críticas: memory leak en RelayCommand, empty catch silencioso, innerException preservation, case-sensitive Sybase lookup
- Correcciones de media severidad: GC.SuppressFinalize, ProbeResult feedback, eliminación de dead code (DbfDataReader), connection string unificada
- Refactorización de diseño: SybaseExportService reducido de 837 a <400 líneas (4 nuevas clases), MainViewModel de 674 a 552 líneas, BuildConvertFunction partida en 11 métodos, BuildLine simplificada con ExportLineContext
- Optimizaciones de rendimiento .NET: 21 clases selladas, LINQ closure eliminado de BuildLine, FrozenDictionary en mapas estáticos, capacity hint en DbfReaderService, RegisterProvider redundante eliminado, SanitizeFileName optimizado

---

## v1.0 — Visor y Exportador DBF (Shipped: 2026-07-10)

**Delivered:** Aplicación de escritorio Windows para abrir, visualizar y exportar archivos DBF a TXT con configuración completa, formatos por columna, y traspaso directo a Sybase ASE.

**Phases completed:** 1-4 (17 plans total)

**Key accomplishments:**

- Lectura DBF con detección automática de codificación via Language Driver ID
- Exportación a TXT asíncrona con separadores, formatos, progreso y cancelación
- Formatos por columna con previa en tiempo real para DATE, DATETIME, NUMERIC, FLOAT
- Persistencia completa entre sesiones (settings.json, perfiles, recientes, ventana)
- Traspaso directo a Sybase ASE via ODBC con probe de conversiones y logging
- Publicación self-contained win-x64

**Stats:**

- 102 files created/modified
- 9,244 lines of C#/XAML (+ 1,091 test LOC)
- 4 phases, 17 plans
- 4 days from start to ship (2026-07-06 → 2026-07-10)

**Git range:** `ff274f5` → `e39ab1a`

**What's next:** Planning v2 — exportación avanzada (CSV, JSON, Excel), selección de columnas, filtrado, CLI

---
