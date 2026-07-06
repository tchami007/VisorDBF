# Product Requirements Document (PRD)
## VisorDBF — Visor y Exportador de Archivos DBF

**Version:** 1.0  
**Fecha:** 2026-07-06  
**Estado:** Borrador validado  

---

## 1. Objetivo del Producto

VisorDBF es una aplicacion de escritorio desarrollada en C# (.NET) con interfaz WinForms o WPF que permite a los usuarios abrir archivos DBF, visualizar su contenido en forma tabular y exportarlo a archivos de texto plano (TXT) con separadores y formatos configurables.

El producto resuelve la necesidad de usuarios tecnicos y no tecnicos que trabajan con archivos DBF heredados y requieren una herramienta rapida, sin dependencias externas, para inspeccionar y transformar esos datos hacia un formato portable.

---

## 2. Usuarios Objetivo

| Perfil | Descripcion |
|--------|-------------|
| Analista de datos | Necesita inspeccionar el contenido de archivos DBF y exportarlos para procesarlos en otras herramientas. |
| Desarrollador de integraciones | Requiere extraer datos de sistemas legacy que generan archivos DBF. |
| Usuario administrativo | Necesita abrir y revisar tablas DBF sin instalar herramientas complejas. |

---

## 3. Alcance del Producto

### 3.1 Dentro del alcance

- Apertura y lectura de archivos DBF (formatos dBASE III, IV, FoxPro).
- Visualizacion del contenido en grilla con soporte de scroll.
- Exportacion a archivo TXT con configuracion de separadores.
- Configuracion de formatos por columna para tipos especiales.
- Persistencia de configuracion y perfiles de exportacion entre sesiones.
- Deteccion automatica de codificacion de origen via Language Driver ID.

### 3.2 Fuera del alcance (version 1.0)

- Edicion o modificacion de registros DBF.
- Exportacion a formatos distintos de TXT (CSV estructurado, Excel, JSON).
- Soporte multi-archivo o merge de tablas.
- Conexion a bases de datos remotas.
- Modo linea de comandos (CLI).

---

## 4. Requerimientos Funcionales

### RF-01 Apertura de archivo DBF

- El usuario puede seleccionar un archivo DBF mediante dialogo de sistema operativo.
- La aplicacion lee el encabezado del archivo y detecta la codificacion via Language Driver ID.
- La codificacion detectada se muestra al usuario como valor por defecto, modificable.
- En caso de que el Language Driver ID no sea reconocido, la aplicacion muestra una advertencia y solicita seleccion manual.
- El contenido del archivo se carga en memoria y se muestra en la grilla principal.

### RF-02 Visualizacion en grilla

- El contenido se presenta en una tabla con columnas correspondientes a los campos del DBF.
- La primera fila muestra los nombres de los campos.
- Se indica el tipo de dato de cada columna (CHARACTER, NUMERIC, DATE, DATETIME, LOGICAL, MEMO).
- Se muestra el total de registros cargados.
- Los registros marcados como eliminados (deleted flag) se distinguen visualmente o se excluyen segun preferencia del usuario.

### RF-03 Exportacion a TXT

- El usuario inicia la exportacion desde la interfaz principal.
- La aplicacion aplica la configuracion activa (separadores, formatos, filas, encabezado, codificacion).
- El usuario selecciona la ruta y nombre del archivo de salida mediante dialogo de sistema.
- Al finalizar se muestra confirmacion con la cantidad de registros exportados.

### RF-04 Configuracion de exportacion

- Separador de columnas: coma, punto y coma, tabulador, pipe, o caracter personalizado.
- Delimitador final de fila: caracter o cadena opcional que se inserta al final de cada fila antes del salto de linea. Puede estar vacio.
- Salto de linea: fijo segun el sistema operativo en ejecucion.
- Inclusion de encabezado: activable o desactivable.
- Limite de filas: exportar todos los registros o las primeras N filas (valor N ingresado por el usuario).
- Codificacion de salida: seleccionable por el usuario, UTF-8 por defecto.

### RF-05 Configuracion de formatos por columna

- El usuario puede asignar un formato de representacion a columnas de tipo DATE, DATETIME, TIME y NUMERIC.
- Para tipos de fecha y hora: cadena de formato compatible con .NET (ej: "yyyy-MM-dd", "dd/MM/yyyy HH:mm:ss").
- Para tipo NUMERIC: patron de formato numerico (ej: "N2", "#,##0.00").
- La configuracion de formato puede activarse o desactivarse por columna individualmente.
- Los formatos configurados se reflejan en la previa de la grilla y en la exportacion.

### RF-06 Perfiles de exportacion

- El usuario puede guardar la configuracion de exportacion actual como un perfil con nombre.
- Los perfiles guardados pueden cargarse, editarse o eliminarse.
- Al abrir la aplicacion se carga el ultimo perfil utilizado.

### RF-07 Persistencia de configuracion

- La configuracion de la aplicacion se persiste en un archivo local (JSON).
- Se almacenan: separadores por defecto, codificacion por defecto, preferencias de encabezado, limite de filas por defecto, lista de archivos recientes y perfiles de exportacion.

### RF-08 Archivos recientes

- La aplicacion mantiene una lista de los ultimos archivos DBF abiertos.
- El usuario puede reabrir un archivo reciente desde el menu o panel de inicio.

---

## 5. Requerimientos No Funcionales

| ID | Requerimiento |
|----|---------------|
| RNF-01 | La aplicacion debe ejecutarse en Windows 10 y Windows 11 sin dependencias adicionales al runtime .NET instalado. |
| RNF-02 | La carga de un archivo DBF de hasta 100.000 registros no debe superar los 5 segundos en hardware estandar. |
| RNF-03 | La interfaz debe responder a acciones del usuario en menos de 200ms para operaciones de navegacion y configuracion. |
| RNF-04 | La exportacion de 500.000 registros no debe bloquear la interfaz grafica (operacion asincrona con indicador de progreso). |
| RNF-05 | El archivo de configuracion JSON debe ser legible y editable manualmente por usuarios avanzados. |
| RNF-06 | La aplicacion no requiere instalacion de base de datos ni servicios externos. |

---

## 6. Entidades del Dominio

| Entidad | Descripcion |
|---------|-------------|
| DbfFile | Archivo DBF cargado en memoria con su cabecera, campos y registros. |
| DbfField | Definicion de una columna: nombre, tipo, longitud, decimales, posicion. |
| DbfRecord | Una fila de datos con sus valores indexados por nombre de campo y flag de eliminacion. |
| ExportConfiguration | Parametros completos de una operacion de exportacion. |
| ColumnFormatConfiguration | Formato de representacion asignado a una columna especifica. |
| ApplicationSettings | Preferencias globales del usuario, persistidas en disco. |
| ExportProfile | Configuracion de exportacion con nombre, reutilizable entre sesiones. |

---

## 7. Criterios de Aceptacion Generales

- Un usuario puede abrir un archivo DBF y ver su contenido en menos de 5 segundos.
- Un usuario puede exportar el contenido completo a TXT con la configuracion deseada en una sola operacion.
- La configuracion se conserva al cerrar y reabrir la aplicacion.
- Los formatos de fecha y numero se aplican correctamente en la exportacion y en la grilla.
- La aplicacion no genera errores no controlados ante archivos DBF con codificaciones no reconocidas.

---

## 8. Dependencias Tecnicas

| Componente | Detalle |
|------------|---------|
| Plataforma | .NET 8 o superior |
| UI Framework | WinForms o WPF |
| Lectura DBF | Libreria .NET para DBF (ej: dBASE.NET, DbfDataReader) o implementacion propia |
| Serializacion | System.Text.Json para persistencia de configuracion |
| Codificaciones | System.Text.Encoding con registro completo (Encoding.RegisterProvider) |

---

## 9. Riesgos

| Riesgo | Mitigacion |
|--------|------------|
| Variantes no estandar del formato DBF | Implementar manejo de errores por campo y registro, omitiendo registros corruptos con advertencia. |
| Archivos DBF de gran tamano (>1M registros) | Implementar carga paginada o virtualizada para la grilla. Evaluar en RNF-04. |
| Language Driver ID desconocido | Mostrar advertencia clara y permitir seleccion manual de codificacion antes de cargar. |
| Caracteres especiales en valores de campos | Validar y escapar caracteres que coincidan con el separador configurado. |
