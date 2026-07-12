# Roadmap: VisorDBF v1.2 — Columnas Personalizadas en Traspaso Sybase

**Milestone:** v1.2
**Started:** 2026-07-11
**Previous milestone:** v1.1 ended at Phase 8

## Summary

**3 phases** | **8 requirements** | All covered ✓

| # | Phase | Goal | Requirements | Success Criteria |
|---|-------|------|--------------|------------------|
| 9 | Columnas Adicionales — Modelo + Servicio | Implementar modelo ExtraColumnConfig e inyectar columnas extra en INSERT/parámetros | SYB-04 | 4 |
| 10 | Columnas Adicionales — UI | Agregar UI en diálogo Sybase para gestionar columnas extra con validación | SYB-01, SYB-02, SYB-03 | 5 |
| 11 | Persistencia + Acerca De + Probe | 1/1 | Complete    | 2026-07-12 |

---

## Phase 9: Columnas Adicionales — Modelo + Servicio

**Goal:** Implementar el modelo de datos para columnas adicionales y modificar `SybaseExportService` para incluirlas en cada INSERT.

**Requirements:** SYB-04

**Success criteria:**

1. `ExtraColumnConfig` record existe con propiedades `ColumnName`, `Type` (DateTime|Integer), `RawValue`
2. `SybaseConnectionConfig` tiene propiedad `ExtraColumns` con default empty list
3. `BuildInsertSql` genera column names + parámetros incluyendo columnas extra
4. `SetParameterValues` asigna valores fijos convertidos a los parámetros extra

**Tasks:**

- Crear `ExtraColumnConfig` model y `ExtraColumnType` enum
- Agregar `ExtraColumns` a `SybaseConnectionConfig`
- Modificar `BuildInsertSql` para incluir columnas extra
- Modificar `SetParameterValues` para asignar valores extra
- Modificar `SybaseExportService.TransferToSybaseAsync` para recibir extra columns
- Modificar `SybaseTransferHelper` para pasar extra columns
- Tests unitarios para BuildInsertSql con extra columns
- Tests unitarios para SetParameterValues con extra columns

---

## Phase 10: Columnas Adicionales — UI

**Goal:** Agregar sección en el diálogo de configuración Sybase para gestionar columnas adicionales con validación en tiempo real.

**Requirements:** SYB-01, SYB-02, SYB-03

**Success criteria:**

1. Grupo "Columnas adicionales" visible en `SybaseConnectionDialog` debajo de "Tabla destino"
2. Usuario puede agregar columna (nombre + tipo + valor) mediante botón "Agregar"
3. Validación de valor: datetime rechaza strings no-fecha, integer rechaza no-numéricos
4. Usuario puede eliminar columnas de la lista con botón "Eliminar"
5. Las columnas configuradas se pasan al servicio al iniciar traspaso

**Tasks:**

- Agregar `ExtraColumns` observable collection a `SybaseConnectionViewModel`
- Agregar `AddExtraColumnCommand`, `RemoveExtraColumnCommand`
- Agregar validación de valor según tipo (tiempo real)
- Modificar `SybaseConnectionDialog.xaml` con ListView + campos
- Agregar `SybaseConnectionViewModel.IsValid` para incluir validación de extra columns
- Tests de ViewModel para add/remove/validate

---

## Phase 11: Persistencia + Acerca De + Probe

**Goal:** Persistir configuración de columnas extra entre sesiones, mostrarlas en el probe, y agregar ventana Acerca De.

**Requirements:** SYB-05, SYB-06, ACERCA-01, ACERCA-02

**Success criteria:**

1. ExtraColumns se guarda y restaura desde settings.json
2. Probe muestra columnas extra con sus valores convertidos
3. Menú Ayuda > "Acerca de VisorDBF" abre ventana modal
4. Ventana Acerca De muestra nombre, versión, features, año
5. Settings legacy sin extraColumns no causan error de deserialización

**Tasks:**

- Verificar serialización/deserialización de `ExtraColumns` en `JsonSettingsService`
- Modificar `SybaseProbeService` o resultado del probe para incluir columnas extra
- Crear `AboutDialog.xaml` + `AboutDialog.xaml.cs`
- Agregar `AboutCommand` a `MainViewModel`
- Agregar menú "Ayuda" con "Acerca de VisorDBF"
- Tests de deserialización con settings legacy
- Tests de AboutViewModel (si aplica)

---

## Phase dependency graph

```
Phase 9 (Modelo+Servicio) ──► Phase 10 (UI) ──► Phase 11 (Persistencia+AcercaDe)
```

Phase 9 tiene que completarse antes que la 10 (la UI necesita el modelo). Phase 11 puede empezar después de la 9 (persistencia) pero la parte Acerca De es independiente.

---

## Traceability

| Requirement | Phase |
|-------------|-------|
| SYB-01 | Phase 10 |
| SYB-02 | Phase 10 |
| SYB-03 | Phase 10 |
| SYB-04 | Phase 9 |
| SYB-05 | Phase 11 |
| SYB-06 | Phase 11 |
| ACERCA-01 | Phase 11 |
| ACERCA-02 | Phase 11 |

**Coverage:**

- v1.2 requirements: 8 total
- Mapped to phases: 8 ✓
- Unmapped: 0 ✓
