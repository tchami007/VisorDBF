# VisorDBF

## What This Is

VisorDBF es una aplicacion de escritorio Windows desarrollada en C# (.NET 8) con interfaz WPF que permite abrir archivos DBF (dBASE III, IV, FoxPro), visualizar su contenido en una grilla tabular y exportarlo a archivos de texto plano (TXT) con separadores, formatos por columna y codificaciones completamente configurables. Incluye traspaso directo a Sybase ASE via ODBC. Esta dirigida a usuarios tecnicos y administrativos que trabajan con datos legacy en formato DBF y necesitan inspeccionarlos y transformarlos sin instalar herramientas complejas.

## Core Value

Un usuario puede abrir cualquier archivo DBF, ver su contenido inmediatamente y exportarlo a TXT con la configuracion exacta que necesita — en menos de tres clicks desde el inicio.

## Current State

**Shipped:** v1.0 — Visor y Exportador DBF (2026-07-10)

**Capabilities:**
- Apertura de archivos DBF con deteccion automatica de codificacion via Language Driver ID
- Visualizacion en grilla tabular con scroll, registros eliminados distinguidos, panel vacio
- Exportacion a TXT con separador configurable, delimitador final, encabezado, limite de filas, codificacion de salida
- Formatos por columna para DATE, DATETIME, NUMERIC, FLOAT con previa en tiempo real
- Perfiles de exportacion guardables y recargables
- Persistencia de configuracion entre sesiones (settings.json en %APPDATA%)
- Lista de archivos recientes (15 max) con deteccion de archivos faltantes
- Persistencia de posicion y tamano de ventana
- Traspaso directo a Sybase ASE via ODBC con probe de conversiones y logging
- Publicacion self-contained win-x64

**Codebase:** 102 files, ~9,244 LOC C#/XAML, 1,091 LOC tests, 90 tests
**Tech stack:** .NET 8 LTS, WPF/MVVM, DbfDataReader, System.Text.Json, ODBC

## Current Milestone: v1.2 — Columnas Personalizadas en Traspaso Sybase

**Goal:** Permitir al usuario configurar columnas adicionales (nombre, tipo datetime/integer, valor fijo) que se inyectan en cada INSERT al destino Sybase, más ventana Acerca De.

**Target features:**
- Configuración de columnas adicionales con nombre, tipo y valor en UI de traspaso Sybase
- Soporte de tipos datetime e integer con validación de entrada
- Persistencia de configuración de columnas adicionales
- Ventana Acerca De con nombre, versión, features y año

## Requirements

### Validated

- ✓ Apertura de archivos DBF con deteccion automatica de codificacion via Language Driver ID — v1.0
- ✓ Visualizacion del contenido en grilla tabular con nombres de columna y tipos de dato — v1.0
- ✓ Codificacion de lectura configurable (Language Driver ID como defecto, seleccion manual disponible) — v1.0
- ✓ Exportacion a TXT con separador de columnas configurable (coma, punto y coma, tabulador, pipe, personalizado) — v1.0
- ✓ Delimitador final de fila opcional configurable — v1.0
- ✓ Inclusion de encabezado en exportacion configurable — v1.0
- ✓ Limite de filas exportadas configurable (todas o primeras N) — v1.0
- ✓ Codificacion de exportacion configurable (UTF-8 por defecto) — v1.0
- ✓ Formatos por columna para DATE, DATETIME, NUMERIC, FLOAT con previa en tiempo real — v1.0
- ✓ Perfiles de exportacion con nombre, reutilizables entre sesiones — v1.0
- ✓ Persistencia de configuracion entre sesiones (settings.json en %APPDATA%) — v1.0
- ✓ Lista de archivos recientes con acceso rapido (15 max) — v1.0
- ✓ Persistencia de posicion y tamano de ventana — v1.0
- ✓ Traspaso directo a Sybase ASE via ODBC con probe y logging — v1.0
- ✓ CRIT-01: Memory leak en RelayCommand — v1.1
- ✓ CRIT-02: Empty catch silencioso en DbfReaderService — v1.1
- ✓ CRIT-03: innerException al wrappear excepciones — v1.1
- ✓ CRIT-04: Case-sensitive Sybase lookup — v1.1
- ✓ MED-01: GC.SuppressFinalize en FileLogger — v1.1
- ✓ MED-02: ProbeResult feedback al usuario — v1.1
- ✓ MED-03: Dead code eliminado (DbfDataReader) — v1.1
- ✓ MED-04: Connection string Sybase unificada — v1.1
- ✓ REF-01: Extraer SybaseExportService (837→296 lines) — v1.1
- ✓ REF-02: Extraer MainViewModel (674→552 lines) — v1.1
- ✓ REF-03: BuildConvertFunction en 11 métodos — v1.1
- ✓ REF-04: ExportLineContext para BuildLine — v1.1
- ✓ PERF-01: 21 clases selladas — v1.1
- ✓ PERF-02: BuildLine sin LINQ closure — v1.1
- ✓ PERF-03: FrozenDictionary en mapas estáticos — v1.1
- ✓ PERF-04: Capacity hint en DbfReaderService — v1.1
- ✓ PERF-05: RegisterProvider redundante eliminado — v1.1
- ✓ PERF-06: SanitizeFileName optimizado — v1.1

### Active

(v1.2 — requirements defined in REQUIREMENTS.md)

### Out of Scope

- Edicion o modificacion de registros DBF — herramienta de solo lectura
- Exportacion a formatos distintos de TXT (Excel, JSON, CSV estructurado) — v2+
- Soporte multi-archivo o merge de tablas — complejidad no justificada en v1
- Conexion a bases de datos remotas — fuera del dominio de archivos locales
- Modo CLI — interfaz grafica es el canal principal

## Context

El formato DBF es ampliamente utilizado en sistemas legacy (Clipper, FoxPro, dBASE) que siguen generando archivos que deben ser procesados por sistemas modernos. Los usuarios necesitan una herramienta liviana, portable y sin dependencias que funcione sobre Windows sin configuracion adicional.

**v1.0 shipped:** 4 dias de desarrollo, 16 feat commits, 90 tests, publicacion self-contained win-x64.

**v1.1 shipped:** 2026-07-11 — 4 fases adicionales (5-8), 18 tareas de correcciones y optimizaciones. 0 regresiones, 90 tests siguen pasando. Codebase: ~9,500 LOC C#/XAML, 1,091 LOC tests.

**v1.2 started:** Columnas Personalizadas en Traspaso Sybase + Acerca De.

El proyecto cuenta con documentacion:
- `docs/PRD.md` — Product Requirements Document con 8 RF y 6 RNF
- `docs/CASOS_DE_USO.md` — 8 casos de uso con flujos principales y alternativos
- `docs/MANUAL_UI.md` — Manual de interfaz con estructura de pantallas y flujos de navegacion
- `docs/TECH.md` — Documento tecnico con arquitectura, librerias, estructura de proyecto y decisiones de diseno
- `docs/TRASPASO_SYBASE.md` — Documentacion del traspaso directo a Sybase ASE

## Constraints

- **Tech Stack**: C# 12 / .NET 8 LTS / WPF — definido en documentacion tecnica validada
- **UI Framework**: WPF con patron MVVM — para virtualizacion de DataGrid y mejor soporte de binding
- **Runtime**: Self-contained win-x64 — sin requerir .NET instalado en el equipo del usuario
- **Plataforma objetivo**: Windows 10 (1903+) / Windows 11 unicamente
- **Sin servicios externos**: No requiere base de datos, red ni servicios de terceros
- **Sin edicion**: La aplicacion es de solo lectura sobre los archivos DBF de origen
- **Dependencias NuGet**: DbfDataReader para lectura DBF; System.Text.Encoding.CodePages para codificaciones extendidas

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| .NET 8 como target framework | LTS activo hasta noviembre 2026, mejor rendimiento que .NET 6 | ✓ Validated |
| WPF con MVVM | WPF ofrece mejor virtualizacion de DataGrid y binding declarativo | ✓ Validated |
| DbfDataReader como libreria de lectura | Reduce tiempo de desarrollo; interfaz permite reemplazo | ✓ Validated |
| System.Text.Json sobre Newtonsoft.Json | Incluido en .NET 8 sin dependencia adicional | ✓ Validated |
| Publicacion self-contained | Elimina variable de version de .NET en equipo destino | ✓ Validated |
| Separacion VisorDBF.UI / VisorDBF.Core | Core sin dependencias de UI, completamente testeable | ✓ Validated |
| Language Driver ID como codificacion por defecto | DBF embebe codificacion en header; reduce friccion | ✓ Validated |
| CurrentCulture en grilla, InvariantCulture en exportacion | D-08: grid muestra formato local, export produce datos portables | ✓ Validated |
| SybaseConfig sin password en persistencia | Seguridad por diseno — password se ingresa cada sesion | ✓ Validated |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd-complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---

*Last updated: 2026-07-11 after v1.2 milestone started*
