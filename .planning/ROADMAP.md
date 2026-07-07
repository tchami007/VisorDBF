# Roadmap: VisorDBF

**Milestone:** v1.0 — Visor y Exportador DBF
**Granularity:** Coarse
**Mode:** Horizontal Layers (Core → Export → Formats → Polish)
**Total Phases:** 4

---

## Milestone v1.0

### Phase 1: Cimientos — Solucion, Lectura DBF y Grilla

**Goal:** Tener una solucion C# / WPF compilable con la arquitectura base (Core + UI), capacidad de abrir y leer archivos DBF correctamente — incluyendo deteccion de codificacion via Language Driver ID — y visualizar su contenido en la grilla principal.

**Success Criteria:**

1. La solucion Visual Studio compila sin errores con los proyectos VisorDBF.Core y VisorDBF.UI separados.
2. El usuario puede abrir un archivo DBF y ver sus registros y columnas en la grilla.
3. La codificacion se detecta automaticamente via Language Driver ID y se muestra en la barra de estado.
4. Si el Language Driver ID es desconocido, aparece una advertencia y el usuario puede seleccionar la codificacion manualmente.
5. Los registros marcados como eliminados se distinguen visualmente.
6. El panel de bienvenida se muestra cuando no hay archivo cargado.

**Requirements covered:**
OPEN-01, OPEN-02, OPEN-03, OPEN-04, ENC-01, VIEW-01, VIEW-02, VIEW-03, VIEW-04, VIEW-05

**Plans:**
5/5 plans complete

- [x] 1.2 — Entidades del dominio (DbfFile, DbfField, DbfFieldType, DbfRecord, enums) ✓ 2026-07-06
- [x] 1.3 — Servicios de lectura DBF (IDbfReaderService, DbfReaderService con DbfDataReader, EncodingDetectionService) ✓ 2026-07-06
- [x] 1.4 — Ventana principal y grilla (MainWindow, MainViewModel, DataGrid con binding, barra de estado) ✓ 2026-07-06
- [x] 1.5 — Flujo de apertura de archivo (dialogo de sistema, carga asincrona, dialogo de codificacion manual) ✓ 2026-07-06

---

### Phase 2: Exportacion — Configuracion y Escritura TXT

**Goal:** El usuario puede configurar todos los parametros de exportacion (separadores, encabezado, filas, codificacion de salida) y exportar el contenido completo a un archivo TXT de forma asincrona con indicador de progreso y capacidad de cancelacion.

**Success Criteria:**

1. El usuario puede abrir la pantalla de configuracion y modificar separador de columnas, delimitador final, encabezado, limite de filas y codificacion de salida.
2. La exportacion genera un archivo TXT con el separador configurado entre columnas.
3. El delimitador final de fila, si esta configurado, aparece despues del ultimo campo en cada linea.
4. La inclusion del encabezado es correcta segun la configuracion activa.
5. El limite de filas respeta la seleccion (todas o primeras N).
6. La exportacion muestra progreso en tiempo real y puede cancelarse; el archivo parcial se elimina al cancelar.
7. Al finalizar se muestra confirmacion con cantidad de registros exportados.

**Requirements covered:**
EXPO-01, EXPO-02, EXPO-03, EXPO-04, EXPO-05, SEP-01, SEP-02, ROWS-01, ROWS-02, ENC-02

**Plans:**
4/4 plans complete

- [x] 2.1 — Modelo ExportConfiguration y pantalla de configuracion de exportacion (UI + ViewModel)
- [x] 2.2 — Servicio de exportacion TXT (IExportService, TxtExportService con StreamWriter, IProgress<int>, CancellationToken)

**Wave 2** *(blocked on Wave 1 — progress dialog depends on TxtExportService)*

- [x] 2.3 — Dialogo de progreso de exportacion (ExportProgressDialog, barra determinista, boton cancelar)

**Wave 3** *(blocked on Waves 1-2 — integration requires all components)*

- [x] 2.4 — Integracion: flujo completo apertura → configuracion → exportacion → confirmacion

---

### Phase 3: Formatos por Columna

**Goal:** El usuario puede asignar cadenas de formato especificas a columnas de tipo DATE, DATETIME, TIME y NUMERIC. Los formatos se aplican tanto en la grilla como en la exportacion. La pantalla de formatos muestra una previa en tiempo real.

**Success Criteria:**

1. La pantalla de formatos muestra todas las columnas del DBF cargado con su tipo de dato.
2. Solo las columnas de tipo DATE, DATETIME, TIME y NUMERIC tienen el campo de formato habilitado.
3. Al ingresar una cadena de formato valida, la columna "Previa" muestra el valor formateado con datos reales en tiempo real.
4. Si el formato es invalido, la previa muestra "ERROR" en rojo y el usuario no puede confirmar.
5. Los formatos activados se aplican correctamente en el archivo TXT exportado.
6. Los formatos se aplican en la grilla de visualizacion principal con el toggle activo.

**Requirements covered:**
FMT-01, FMT-02, FMT-03, FMT-04, FMT-05

**Plans:**

- [ ] 3.1 — Modelo ColumnFormatConfiguration y servicio de formateo (ColumnFormatService con validacion)
- [ ] 3.2 — Pantalla de formatos de columna (ColumnFormatsWindow con DataGrid editable, columna Previa, panel de ayuda)
- [ ] 3.3 — Integracion de formatos en TxtExportService y en la grilla principal

---

### Phase 4: Perfiles, Persistencia y Polish

**Goal:** La configuracion del usuario persiste entre sesiones, los perfiles de exportacion son guardables y recargables, la lista de recientes funciona correctamente, y la aplicacion esta lista para distribucion como ejecutable self-contained.

**Success Criteria:**

1. Al cerrar y reabrir la aplicacion, la configuracion anterior (separadores, encabezado, formatos, ultimo perfil) se restaura.
2. El usuario puede guardar, cargar y eliminar perfiles de exportacion con nombre.
3. Al iniciar, se carga automaticamente el ultimo perfil utilizado.
4. La lista de archivos recientes muestra los ultimos archivos abiertos con ruta y fecha.
5. Si un archivo reciente ya no existe, la aplicacion informa y ofrece eliminarlo de la lista.
6. Si settings.json esta corrupto, la aplicacion inicia con valores por defecto y crea backup del archivo danado.
7. La posicion y tamano de la ventana se restauran entre sesiones.
8. La aplicacion publica correctamente como self-contained win-x64 y ejecuta sin .NET instalado.

**Requirements covered:**
OPEN-05, PROF-01, PROF-02, PROF-03, PROF-04, PERS-01, PERS-02, PERS-03

**Plans:**

- [ ] 4.1 — Servicio de persistencia (ISettingsService, JsonSettingsService, esquema ApplicationSettings + ExportProfile)
- [ ] 4.2 — Gestion de perfiles en UI (selector en barra de herramientas, CRUD de perfiles en pantalla de configuracion)
- [ ] 4.3 — Lista de archivos recientes (menu Recientes, deteccion de archivos faltantes, limite de entradas)
- [ ] 4.4 — Persistencia de estado de ventana y recuperacion ante settings.json corrupto
- [ ] 4.5 — Build de distribucion (dotnet publish self-contained win-x64, validacion de ejecucion sin .NET previo)

---

## Phase Status Legend

| Symbol | Meaning |
|--------|---------|
| [ ] | Not started |
| [~] | In progress |
| [x] | Complete |
| [!] | Blocked |

---

## Dependency Order

```
Phase 1 (Core + Apertura + Grilla)
  → Phase 2 (Exportacion — requiere lectura y visualizacion)
     → Phase 3 (Formatos — requiere exportacion base funcional)
        → Phase 4 (Perfiles + Persistencia — requiere configuracion completa)
```

Fases secuenciales. Dentro de cada fase, los planes pueden ejecutarse en paralelo donde no haya dependencias directas (ver indicacion en cada plan).

---
*Roadmap created: 2026-07-06*
*Last updated: 2026-07-07 after Phase 2 planning (4 plans in 3 waves, verified)*
