# Casos de Uso
## VisorDBF — Visor y Exportador de Archivos DBF

**Version:** 1.0  
**Fecha:** 2026-07-06  

---

## Actores

| Actor | Descripcion |
|-------|-------------|
| Usuario | Persona que opera la aplicacion para abrir, visualizar y exportar archivos DBF. |
| Sistema de archivos | Componente externo que provee acceso a los archivos DBF y al destino de exportacion. |
| Modulo de configuracion | Componente interno que persiste y recupera la configuracion del usuario. |

---

## CU-01: Abrir archivo DBF

**Actor principal:** Usuario  
**Precondicion:** La aplicacion esta abierta.  
**Postcondicion:** El contenido del archivo DBF se muestra en la grilla principal.

### Flujo principal

1. El usuario selecciona la opcion "Abrir archivo" desde el menu o el boton de la barra de herramientas.
2. El sistema presenta el dialogo de seleccion de archivos del sistema operativo, filtrado por extension DBF.
3. El usuario selecciona un archivo DBF y confirma.
4. El sistema lee el encabezado del archivo y detecta el Language Driver ID.
5. El sistema mapea el Language Driver ID a una codificacion conocida y la establece como valor por defecto.
6. El sistema carga los campos y registros del archivo en memoria.
7. El sistema muestra el contenido en la grilla principal con los nombres de columnas en el encabezado.
8. El sistema muestra en la barra de estado: nombre del archivo, cantidad de registros y codificacion detectada.

### Flujo alternativo A: Language Driver ID no reconocido (paso 5)

5a. El sistema no puede mapear el Language Driver ID a una codificacion conocida.  
5b. El sistema muestra un dialogo de advertencia informando la situacion.  
5c. El usuario selecciona manualmente la codificacion de lectura desde una lista de opciones.  
5d. El flujo continua desde el paso 6.

### Flujo alternativo B: Archivo con formato invalido (paso 6)

6a. El sistema detecta que el archivo no es un DBF valido o esta corrupto.  
6b. El sistema muestra un mensaje de error descriptivo.  
6c. El caso de uso finaliza sin cargar contenido.

### Flujo alternativo C: Apertura desde archivos recientes

1c. El usuario selecciona un archivo desde la lista de archivos recientes.  
2c. El flujo continua desde el paso 4.

---

## CU-02: Visualizar contenido del archivo DBF

**Actor principal:** Usuario  
**Precondicion:** Un archivo DBF ha sido cargado exitosamente (CU-01 completado).  
**Postcondicion:** El usuario puede inspeccionar los datos del archivo.

### Flujo principal

1. La grilla muestra las columnas con sus nombres y tipos de dato.
2. El usuario navega por los registros mediante scroll vertical y horizontal.
3. El usuario puede ordenar los registros haciendo clic en el encabezado de una columna.
4. La barra de estado muestra el numero de registro activo y el total.

### Flujo alternativo A: Registros marcados como eliminados

4a. El archivo contiene registros con el flag de eliminacion activo.  
4b. El sistema los distingue visualmente (por ejemplo, con fondo diferente o tachado).  
4c. El usuario puede optar por ocultar o mostrar los registros eliminados desde la configuracion de vista.

---

## CU-03: Configurar parametros de exportacion

**Actor principal:** Usuario  
**Precondicion:** La aplicacion esta abierta. No es necesario tener un archivo cargado.  
**Postcondicion:** La configuracion de exportacion activa queda establecida.

### Flujo principal

1. El usuario accede a la pantalla o panel de configuracion.
2. El sistema muestra los parametros actuales con sus valores por defecto o los del ultimo perfil utilizado.
3. El usuario configura el separador de columnas (coma, punto y coma, tabulador, pipe, u otro personalizado).
4. El usuario configura el delimitador final de fila (campo opcional, puede quedar vacio).
5. El usuario activa o desactiva la inclusion del encabezado.
6. El usuario selecciona el modo de limite de filas: todas las filas o las primeras N, e ingresa N si corresponde.
7. El usuario selecciona la codificacion de salida del archivo TXT (UTF-8 por defecto).
8. El usuario confirma los cambios.
9. El sistema aplica la configuracion como activa para la proxima exportacion.

### Flujo alternativo A: Guardar como perfil (paso 8)

8a. El usuario selecciona "Guardar como perfil" en lugar de solo confirmar.  
8b. El sistema solicita un nombre para el perfil.  
8c. El usuario ingresa el nombre y confirma.  
8d. El sistema guarda el perfil y lo establece como activo.

---

## CU-04: Configurar formatos por columna

**Actor principal:** Usuario  
**Precondicion:** Un archivo DBF ha sido cargado exitosamente.  
**Postcondicion:** Los formatos configurados se aplican en la grilla y en la proxima exportacion.

### Flujo principal

1. El usuario accede a la configuracion de formatos de columna desde la pantalla de configuracion o desde el encabezado de la grilla.
2. El sistema muestra la lista de columnas del archivo con su tipo de dato.
3. El sistema resalta las columnas que admiten formato configurable: DATE, DATETIME, TIME, NUMERIC.
4. El usuario selecciona una columna para configurar.
5. El sistema muestra el campo de formato con un valor por defecto sugerido segun el tipo.
6. El usuario ingresa o modifica la cadena de formato (ej: "dd/MM/yyyy", "N2").
7. El sistema muestra una previa del valor formateado con datos reales de la columna.
8. El usuario activa el formato para esa columna.
9. El usuario repite los pasos 4 a 8 para otras columnas segun necesidad.
10. El usuario confirma la configuracion.

### Flujo alternativo A: Formato invalido (paso 7)

7a. El sistema no puede aplicar el formato ingresado al tipo de dato de la columna.  
7b. El sistema muestra un mensaje de error en linea indicando el problema.  
7c. El usuario corrige el formato antes de poder confirmar.

### Flujo alternativo B: Desactivar formato de una columna (paso 8)

8b. El usuario desactiva el toggle de formato para la columna.  
8c. La columna se exporta con su representacion por defecto.

---

## CU-05: Exportar a TXT

**Actor principal:** Usuario  
**Precondicion:** Un archivo DBF ha sido cargado exitosamente. La configuracion de exportacion esta establecida.  
**Postcondicion:** Se genera un archivo TXT en la ruta seleccionada con el contenido y formato configurados.

### Flujo principal

1. El usuario selecciona la opcion "Exportar" desde el menu o el boton de la barra de herramientas.
2. El sistema presenta el dialogo de guardar archivo, con extension TXT preseleccionada.
3. El usuario selecciona la ruta y nombre del archivo de salida y confirma.
4. El sistema inicia la exportacion de forma asincrona y muestra un indicador de progreso.
5. El sistema aplica los formatos de columna configurados a cada valor.
6. El sistema escribe cada fila con el separador de columnas configurado, el delimitador final si corresponde y el salto de linea.
7. Si la opcion de encabezado esta activa, la primera linea contiene los nombres de las columnas.
8. Al finalizar, el sistema muestra un mensaje de confirmacion con la cantidad de registros exportados y la ruta del archivo.

### Flujo alternativo A: Limite de filas activo (paso 6)

6a. El modo de limite de filas es "Primeras N".  
6b. El sistema exporta unicamente los primeros N registros no eliminados.

### Flujo alternativo B: Error de escritura (paso 6)

6b. El sistema no puede escribir en la ruta seleccionada (permisos, disco lleno, etc.).  
6c. El sistema muestra un mensaje de error descriptivo.  
6d. El indicador de progreso se cierra. El caso de uso finaliza sin archivo generado.

### Flujo alternativo C: Cancelacion durante la exportacion (paso 4)

4c. El usuario selecciona "Cancelar" en el indicador de progreso.  
4d. El sistema detiene la escritura y elimina el archivo parcial si existe.  
4e. El sistema informa al usuario que la exportacion fue cancelada.

---

## CU-06: Gestionar perfiles de exportacion

**Actor principal:** Usuario  
**Precondicion:** La aplicacion esta abierta.  
**Postcondicion:** El perfil es creado, modificado, cargado o eliminado segun la accion ejecutada.

### Flujo principal: Cargar perfil

1. El usuario accede al selector de perfiles en la pantalla de configuracion.
2. El sistema muestra la lista de perfiles guardados con nombre y fecha de ultimo uso.
3. El usuario selecciona un perfil.
4. El sistema carga la configuracion del perfil como configuracion activa.

### Flujo alternativo: Eliminar perfil

3a. El usuario selecciona la opcion de eliminar junto a un perfil.  
3b. El sistema solicita confirmacion.  
3c. El usuario confirma.  
3d. El sistema elimina el perfil de la lista y del archivo de configuracion.

---

## CU-07: Gestionar archivos recientes

**Actor principal:** Usuario  
**Precondicion:** La aplicacion ha sido utilizada al menos una vez para abrir un archivo DBF.  
**Postcondicion:** El usuario reabre un archivo previo o limpia la lista.

### Flujo principal

1. El usuario accede a la lista de archivos recientes desde el menu o el panel de inicio.
2. El sistema muestra los ultimos archivos abiertos con ruta completa y fecha de ultimo acceso.
3. El usuario selecciona un archivo de la lista.
4. El flujo continua desde el paso 4 del CU-01.

### Flujo alternativo A: Archivo ya no existe

4a. El sistema detecta que el archivo en la ruta registrada ya no existe.  
4b. El sistema muestra un mensaje informativo.  
4c. El sistema ofrece al usuario eliminar esa entrada de la lista de recientes.

---

## CU-08: Cambiar codificacion de lectura

**Actor principal:** Usuario  
**Precondicion:** Un archivo DBF ha sido cargado con una codificacion activa.  
**Postcondicion:** El archivo se recarga con la nueva codificacion seleccionada.

### Flujo principal

1. El usuario selecciona la opcion de cambio de codificacion desde la barra de estado o el menu.
2. El sistema muestra un selector con las codificaciones disponibles, resaltando la activa.
3. El usuario selecciona una nueva codificacion.
4. El sistema recarga el contenido del archivo con la nueva codificacion.
5. La grilla se actualiza con los valores reinterpretados.

---

## Matriz de trazabilidad: Casos de Uso vs Requerimientos Funcionales

| Caso de Uso | RF-01 | RF-02 | RF-03 | RF-04 | RF-05 | RF-06 | RF-07 | RF-08 |
|-------------|-------|-------|-------|-------|-------|-------|-------|-------|
| CU-01 Abrir DBF | X | | | | | | | |
| CU-02 Visualizar contenido | | X | | | | | | |
| CU-03 Configurar exportacion | | | | X | | | X | |
| CU-04 Configurar formatos | | | | | X | | | |
| CU-05 Exportar a TXT | | | X | X | X | | | |
| CU-06 Gestionar perfiles | | | | | | X | X | |
| CU-07 Archivos recientes | | | | | | | X | X |
| CU-08 Cambiar codificacion | X | X | | | | | | X |
