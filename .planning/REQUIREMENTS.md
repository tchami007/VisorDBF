# Requirements: VisorDBF

**Defined:** 2026-07-06
**Core Value:** Un usuario puede abrir cualquier archivo DBF, ver su contenido inmediatamente y exportarlo a TXT con la configuracion exacta que necesita — en menos de tres clicks desde el inicio.

## v1 Requirements

### Apertura de Archivo

- [x] **OPEN-01**: El usuario puede seleccionar un archivo DBF mediante dialogo del sistema operativo
- [x] **OPEN-02**: La aplicacion detecta automaticamente la codificacion de lectura via Language Driver ID del header DBF
- [x] **OPEN-03**: Si el Language Driver ID no es reconocido, la aplicacion muestra una advertencia y solicita seleccion manual de codificacion
- [x] **OPEN-04**: El usuario puede cambiar la codificacion de lectura en cualquier momento y el archivo se recarga con la nueva codificacion
- [x] **OPEN-05**: La aplicacion mantiene una lista de archivos recientes y permite reabrirlos con un click

### Visualizacion

- [x] **VIEW-01**: El contenido del archivo DBF se muestra en una grilla con nombres de columnas y tipos de dato visibles
- [x] **VIEW-02**: La grilla soporta scroll vertical y horizontal para archivos con muchas columnas o registros
- [x] **VIEW-03**: La barra de estado muestra nombre del archivo, total de registros y codificacion activa
- [x] **VIEW-04**: Los registros marcados como eliminados (deleted flag) se distinguen visualmente del resto
- [x] **VIEW-05**: La grilla muestra un panel de estado vacio con acceso directo cuando no hay archivo cargado

### Exportacion

- [x] **EXPO-01**: El usuario puede exportar el contenido a un archivo TXT desde la interfaz principal
- [x] **EXPO-02**: La exportacion aplica la configuracion activa (separadores, formatos, encabezado, filas, codificacion)
- [x] **EXPO-03**: La exportacion se ejecuta de forma asincrona mostrando un indicador de progreso
- [x] **EXPO-04**: El usuario puede cancelar una exportacion en curso y el archivo parcial es eliminado
- [x] **EXPO-05**: Al finalizar la exportacion se muestra confirmacion con cantidad de registros exportados y ruta del archivo

### Configuracion de Separadores

- [x] **SEP-01**: El usuario puede seleccionar el separador de columnas entre: coma, punto y coma, tabulador, pipe, o caracter personalizado
- [x] **SEP-02**: El usuario puede configurar un delimitador final de fila opcional (caracter o cadena insertada despues del ultimo campo, antes del salto de linea)

### Configuracion de Filas y Encabezado

- [x] **ROWS-01**: El usuario puede elegir exportar todos los registros o las primeras N filas (valor N configurable)
- [x] **ROWS-02**: La inclusion del encabezado (nombres de columnas) en la exportacion es activable y desactivable por el usuario

### Configuracion de Codificacion

- [x] **ENC-01**: El usuario puede seleccionar la codificacion del archivo DBF de origen (Language Driver ID como defecto)
- [x] **ENC-02**: El usuario puede seleccionar la codificacion del archivo TXT exportado (UTF-8 por defecto)

### Formatos por Columna

- [x] **FMT-01**: El usuario puede asignar un formato de representacion a columnas de tipo DATE (ej: "dd/MM/yyyy", "yyyy-MM-dd")
- [x] **FMT-02**: El usuario puede asignar un formato a columnas de tipo DATETIME y TIME (ej: "dd/MM/yyyy HH:mm:ss")
- [x] **FMT-03**: El usuario puede asignar un formato a columnas de tipo NUMERIC (ej: "N2", "#,##0.00")
- [x] **FMT-04**: El formato por columna puede activarse o desactivarse individualmente
- [x] **FMT-05**: La pantalla de formatos muestra una previa del valor formateado con datos reales de la columna en tiempo real

### Perfiles de Exportacion

- [x] **PROF-01**: El usuario puede guardar la configuracion de exportacion actual como un perfil con nombre
- [x] **PROF-02**: El usuario puede cargar un perfil guardado desde la pantalla de configuracion
- [x] **PROF-03**: El usuario puede eliminar perfiles existentes con confirmacion previa
- [x] **PROF-04**: Al iniciar la aplicacion se carga automaticamente el ultimo perfil utilizado

### Persistencia

- [x] **PERS-01**: La configuracion de la aplicacion se persiste en %APPDATA%\VisorDBF\settings.json
- [x] **PERS-02**: Si settings.json esta corrupto, la aplicacion inicia con valores por defecto y crea una copia de seguridad del archivo corrupto
- [x] **PERS-03**: La posicion y tamano de la ventana principal se recuerdan entre sesiones

## v2 Requirements

### Exportacion Avanzada

- **EXPO-V2-01**: Exportacion a CSV estructurado (RFC 4180) con manejo de comillas para valores que contienen el separador
- **EXPO-V2-02**: Exportacion a JSON (array de objetos con nombres de campo como claves)
- **EXPO-V2-03**: Exportacion a Excel (.xlsx) via libreria de escritura de hojas de calculo

### Seleccion de Columnas

- **COL-V2-01**: El usuario puede seleccionar cuales columnas incluir en la exportacion
- **COL-V2-02**: El usuario puede reordenar columnas en la exportacion
- **COL-V2-03**: El usuario puede renombrar columnas en el encabezado de la exportacion

### Filtrado

- **FILT-V2-01**: El usuario puede filtrar registros por valor de columna antes de exportar
- **FILT-V2-02**: El usuario puede ordenar registros por columna antes de exportar

### CLI

- **CLI-V2-01**: Modo linea de comandos para automatizar exportaciones sin interfaz grafica

## Out of Scope

| Feature | Reason |
|---------|--------|
| Edicion de registros DBF | Herramienta de solo lectura — modificar el origen es fuera del alcance y aumenta el riesgo de corrupcion |
| Soporte multi-archivo / merge | Complejidad arquitectural no justificada para v1; usuarios no reportaron esta necesidad |
| Conexion a bases de datos remotas | Dominio de archivos locales; integracion con BD remotas es un producto diferente |
| Soporte macOS / Linux | Aplicacion WPF es Windows-only; el usuario objetivo trabaja en Windows |
| Instalacion sin privilegios de administrador (portable) | Ya cubierto en la modalidad de distribucion ZIP sin instalador |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| OPEN-01 | Phase 1 | Complete |
| OPEN-02 | Phase 1 | Complete |
| OPEN-03 | Phase 1 | Complete |
| OPEN-04 | Phase 1 | Complete |
| OPEN-05 | Phase 4 | Complete |
| VIEW-01 | Phase 1 | Complete |
| VIEW-02 | Phase 1 | Complete |
| VIEW-03 | Phase 1 | Complete |
| VIEW-04 | Phase 1 | Complete |
| VIEW-05 | Phase 1 | Complete |
| EXPO-01 | Phase 2 | Complete |
| EXPO-02 | Phase 2 | Complete |
| EXPO-03 | Phase 2 | Complete |
| EXPO-04 | Phase 2 | Complete |
| EXPO-05 | Phase 2 | Complete |
| SEP-01 | Phase 2 | Complete |
| SEP-02 | Phase 2 | Complete |
| ROWS-01 | Phase 2 | Complete |
| ROWS-02 | Phase 2 | Complete |
| ENC-01 | Phase 1 | Complete |
| ENC-02 | Phase 2 | Complete |
| FMT-01 | Phase 3 | Complete |
| FMT-02 | Phase 3 | Complete |
| FMT-03 | Phase 3 | Complete |
| FMT-04 | Phase 3 | Complete |
| FMT-05 | Phase 3 | Complete |
| PROF-01 | Phase 4 | Complete |
| PROF-02 | Phase 4 | Complete |
| PROF-03 | Phase 4 | Complete |
| PROF-04 | Phase 4 | Complete |
| PERS-01 | Phase 4 | Complete |
| PERS-02 | Phase 4 | Complete |
| PERS-03 | Phase 4 | Complete |

**Coverage:**
- v1 requirements: 33 total
- Mapped to phases: 33
- Unmapped: 0 ✓

---
*Requirements defined: 2026-07-06*
*Last updated: 2026-07-06 after initial definition*
