# Manual de Interfaz de Usuario
## VisorDBF — Visor y Exportador de Archivos DBF

**Version:** 1.0  
**Fecha:** 2026-07-06  
**Audiencia:** Usuarios finales y equipos de desarrollo de referencia de diseno.

---

## 1. Estructura General de la Aplicacion

La aplicacion se organiza en tres areas principales:

```
+----------------------------------------------------------+
|  BARRA DE MENU                                           |
|  Archivo | Exportar | Configuracion | Ayuda              |
+----------------------------------------------------------+
|  BARRA DE HERRAMIENTAS                                   |
|  [Abrir]  [Exportar]  [Configuracion]  | Perfil: [----v] |
+----------------------------------------------------------+
|                                                          |
|  AREA DE GRILLA (panel principal)                        |
|  Columna1 | Columna2 | Columna3 | ...                    |
|  valor    | valor    | valor    | ...                    |
|  ...                                                     |
|                                                          |
+----------------------------------------------------------+
|  BARRA DE ESTADO                                         |
|  Archivo: nombre.dbf | Registros: 1.250 | Cod: CP1252    |
+----------------------------------------------------------+
```

---

## 2. Pantallas y Componentes

---

### 2.1 Pantalla Principal

Es la ventana raiz de la aplicacion. Se muestra al iniciar y contiene la grilla de visualizacion.

#### 2.1.1 Barra de Menu

| Menu | Opcion | Accion |
|------|--------|--------|
| Archivo | Abrir... | Abre el dialogo de seleccion de archivo DBF. |
| Archivo | Recientes | Submenu con los ultimos archivos abiertos. |
| Archivo | Cerrar | Cierra el archivo actual y limpia la grilla. |
| Archivo | Salir | Cierra la aplicacion. |
| Exportar | Exportar a TXT... | Inicia el proceso de exportacion con la configuracion activa. |
| Configuracion | Configuracion de exportacion... | Abre la pantalla de configuracion (ver seccion 2.2). |
| Configuracion | Formatos de columnas... | Abre la pantalla de formatos (ver seccion 2.3). |
| Configuracion | Codificacion de lectura... | Abre el selector de codificacion. |
| Ayuda | Acerca de... | Muestra version y datos del producto. |

#### 2.1.2 Barra de Herramientas

Accesos rapidos a las acciones mas frecuentes:

- **Abrir:** Equivalente a Archivo > Abrir...
- **Exportar:** Equivalente a Exportar > Exportar a TXT...
- **Configuracion:** Equivalente a Configuracion > Configuracion de exportacion...
- **Selector de perfil:** Lista desplegable con los perfiles de exportacion guardados. El perfil activo se muestra como valor seleccionado.

#### 2.1.3 Grilla de Datos

Componente central de la pantalla principal.

- Muestra los registros del archivo DBF cargado.
- La primera fila (encabezado fijo) muestra los nombres de los campos.
- Debajo del nombre de cada columna se muestra el tipo de dato entre parentesis: (C), (N), (D), (DT), (L), (M).
- Soporta scroll vertical y horizontal.
- Permite ordenamiento ascendente/descendente al hacer clic en el encabezado de una columna.
- Los registros con flag de eliminacion activo se muestran con fondo gris claro y texto tachado.
- Las columnas con formato personalizado activo muestran un icono indicador en su encabezado.

**Comportamiento sin archivo cargado:**  
La grilla muestra un panel vacio con el mensaje: "Abra un archivo DBF para comenzar" y un boton de acceso directo "Abrir archivo".

#### 2.1.4 Barra de Estado

Ubicada en la parte inferior de la ventana. Muestra de izquierda a derecha:

- Nombre del archivo activo (o "Sin archivo" si no hay ninguno cargado).
- Total de registros del archivo.
- Codificacion de lectura activa. Al hacer clic sobre ella se abre el selector de codificacion.
- Indicador de progreso de exportacion (visible solo durante una exportacion activa).

---

### 2.2 Pantalla de Configuracion de Exportacion

Accesible desde: Menu Configuracion > Configuracion de exportacion, o boton de la barra de herramientas.

Puede abrirse como ventana modal o como panel lateral, segun el framework elegido.

#### Secciones y controles

**Separadores**

| Control | Tipo | Descripcion |
|---------|------|-------------|
| Separador de columnas | Radio buttons + campo de texto | Opciones: Coma (,) / Punto y coma (;) / Tabulador (\t) / Pipe (\|) / Personalizado. Si se selecciona Personalizado, se habilita un campo de texto para ingresar el caracter. |
| Delimitador final de fila | Campo de texto | Caracter o cadena que se agrega al final de cada fila, antes del salto de linea. Puede quedar en blanco. Muestra una previa del efecto: "campo1;campo2;campo3[delimitador]". |

**Filas a exportar**

| Control | Tipo | Descripcion |
|---------|------|-------------|
| Todas las filas | Radio button | Exporta la totalidad de los registros. |
| Primeras N filas | Radio button + campo numerico | Habilita un campo para ingresar la cantidad de filas. Solo acepta valores enteros positivos. |

**Encabezado**

| Control | Tipo | Descripcion |
|---------|------|-------------|
| Incluir encabezado | Checkbox | Cuando esta activo, la primera linea del TXT exportado contiene los nombres de las columnas. |

**Codificacion de salida**

| Control | Tipo | Descripcion |
|---------|------|-------------|
| Codificacion del archivo TXT | Lista desplegable | Listado de codificaciones disponibles. Valor por defecto: UTF-8. |

**Perfiles**

| Control | Tipo | Descripcion |
|---------|------|-------------|
| Perfil activo | Lista desplegable | Muestra el perfil actualmente cargado. |
| Cargar perfil | Boton | Carga la configuracion del perfil seleccionado. |
| Guardar como perfil | Boton | Solicita un nombre y guarda la configuracion actual como nuevo perfil. |
| Eliminar perfil | Boton | Solicita confirmacion y elimina el perfil seleccionado. |

**Botones de accion**

- **Aceptar:** Guarda los cambios y cierra la pantalla.
- **Cancelar:** Descarta los cambios y cierra la pantalla.
- **Aplicar:** Guarda los cambios sin cerrar la pantalla.

---

### 2.3 Pantalla de Formatos de Columna

Accesible desde: Menu Configuracion > Formatos de columnas, o desde el encabezado de la grilla (clic derecho sobre el nombre de una columna).

#### Estructura

```
+--------------------------------------------------------------+
|  Formatos de columna                                         |
+------+----------+--------+-----------------+--------+-------+
| #    | Columna  | Tipo   | Formato         | Activo | Previa|
+------+----------+--------+-----------------+--------+-------+
|  1   | FECHA    | DATE   | dd/MM/yyyy      |  [X]   | 06/07 |
|  2   | MONTO    | NUM    | #,##0.00        |  [X]   | 1.250 |
|  3   | HORA     | TIME   | HH:mm:ss        |  [ ]   |       |
|  4   | NOMBRE   | CHAR   | (no aplica)     |   -    |       |
+------+----------+--------+-----------------+--------+-------+
```

#### Columnas de la tabla

| Columna | Descripcion |
|---------|-------------|
| # | Numero ordinal de la columna en el DBF. |
| Columna | Nombre del campo en el archivo DBF. |
| Tipo | Tipo de dato del campo. |
| Formato | Campo de texto editable directamente en la tabla. Habilitado solo para tipos DATE, DATETIME, TIME y NUMERIC. Para los demas tipos muestra el texto "(no aplica)" en gris. |
| Activo | Checkbox que habilita o deshabilita el formato para esa columna. Deshabilitado para tipos que no admiten formato. |
| Previa | Muestra el primer valor no nulo del campo formateado segun la cadena ingresada. Se actualiza en tiempo real. Si el formato es invalido muestra "ERROR" en rojo. |

#### Panel de ayuda de formatos

Un panel expansible debajo de la tabla muestra ejemplos de cadenas de formato segun el tipo seleccionado:

**Para DATE y DATETIME:**
```
yyyy-MM-dd          -> 2026-07-06
dd/MM/yyyy          -> 06/07/2026
MM-dd-yyyy HH:mm    -> 07-06-2026 14:30
```

**Para NUMERIC:**
```
N2                  -> 1250.50
#,##0.00            -> 1,250.50
0.####              -> 1250.5
```

#### Botones de accion

- **Aceptar:** Aplica y guarda los formatos configurados.
- **Cancelar:** Descarta los cambios.
- **Restablecer todos:** Limpia todos los formatos y desactiva los toggles.

---

### 2.4 Dialogo de Seleccion de Codificacion

Accesible desde: Menu Configuracion > Codificacion de lectura, o clic sobre la codificacion en la barra de estado.

- Lista desplegable o lista con busqueda de codificaciones disponibles.
- La codificacion detectada automaticamente (Language Driver ID) aparece resaltada con la etiqueta "(detectada)".
- Si el Language Driver ID no fue reconocido, aparece una advertencia en color amarillo en la parte superior del dialogo.
- Boton **Aceptar** aplica la codificacion y recarga el archivo.
- Boton **Cancelar** cierra sin cambios.

---

### 2.5 Dialogo de Progreso de Exportacion

Se muestra de forma modal mientras la exportacion esta en curso.

```
+--------------------------------------+
|  Exportando...                       |
|                                      |
|  Registros procesados: 12.450 / 50.000 |
|  [=========================>         ] |
|                                      |
|                    [Cancelar]        |
+--------------------------------------+
```

- La barra de progreso es determinista cuando se conoce el total de registros.
- El boton Cancelar interrumpe la exportacion y elimina el archivo parcial.
- Al finalizar exitosamente, el dialogo se reemplaza por un mensaje de confirmacion con la ruta del archivo generado y un boton "Abrir carpeta".

---

### 2.6 Panel de Inicio (estado sin archivo)

Cuando no hay ningun archivo cargado, el area de la grilla muestra un panel centrado con:

- Mensaje principal: "No hay ningun archivo abierto"
- Boton prominente: "Abrir archivo DBF"
- Lista de archivos recientes (si existen), con nombre, ruta y fecha de ultimo acceso.

---

## 3. Flujos de Navegacion

### Flujo 1: Primera apertura y exportacion rapida

```
Inicio (sin archivo)
  -> Clic "Abrir archivo DBF"
     -> Dialogo de seleccion de archivo
        -> Seleccion del DBF
           -> Grilla con datos cargados
              -> Clic "Exportar"
                 -> Dialogo de guardar archivo TXT
                    -> Dialogo de progreso
                       -> Confirmacion de exportacion exitosa
```

### Flujo 2: Configuracion antes de exportar

```
Grilla con datos cargados
  -> Menu Configuracion > Configuracion de exportacion
     -> Pantalla de configuracion
        -> Ajuste de separadores, filas, encabezado, codificacion
           -> Guardar como perfil (opcional)
              -> Aceptar
                 -> Clic "Exportar"
                    -> Dialogo de guardar archivo TXT
                       -> Exportacion
```

### Flujo 3: Configurar formatos de columna

```
Grilla con datos cargados
  -> Clic derecho sobre encabezado de columna DATE o NUMERIC
     -> Opcion "Configurar formato"
        -> Pantalla de formatos (columna preseleccionada)
           -> Ingreso de cadena de formato
              -> Verificacion de previa
                 -> Aceptar
                    -> Grilla actualizada con el formato aplicado
```

---

## 4. Mensajes del Sistema

| Situacion | Tipo | Mensaje sugerido |
|-----------|------|-----------------|
| Archivo abierto exitosamente | Barra de estado | "nombre.dbf cargado. 1.250 registros." |
| Language Driver ID desconocido | Dialogo de advertencia | "No se pudo detectar la codificacion del archivo. Seleccione manualmente la codificacion de lectura." |
| Archivo DBF invalido o corrupto | Dialogo de error | "El archivo seleccionado no es un DBF valido o esta corrupto. Verifique el archivo e intente nuevamente." |
| Formato de columna invalido | Indicador en linea (rojo) | "Formato invalido para el tipo [TIPO]. Consulte los ejemplos en el panel de ayuda." |
| Exportacion completada | Dialogo de confirmacion | "Exportacion completada. 1.250 registros exportados a: C:\ruta\archivo.txt" |
| Exportacion cancelada | Barra de estado | "Exportacion cancelada por el usuario." |
| Error de escritura en exportacion | Dialogo de error | "No se pudo escribir el archivo de salida. Verifique los permisos y el espacio disponible en disco." |
| Archivo reciente no encontrado | Dialogo de aviso | "El archivo 'nombre.dbf' ya no se encuentra en la ruta registrada. Desea eliminarlo de la lista de recientes?" |
| Perfil eliminado | Barra de estado | "El perfil 'nombre del perfil' fue eliminado." |

---

## 5. Lineamientos de Diseno

### 5.1 Principios generales

- La accion mas frecuente (Abrir + Exportar) debe completarse en no mas de 3 clics desde el inicio de la aplicacion.
- La grilla es el elemento dominante de la pantalla; las configuraciones son secundarias y se acceden bajo demanda.
- Los errores deben ser descriptivos y orientados a la accion del usuario, no a mensajes tecnicos internos.
- Las operaciones largas (exportacion de grandes volumenes) nunca deben bloquear la interfaz.

### 5.2 Controles recomendados (WinForms / WPF)

| Elemento | Control sugerido |
|----------|-----------------|
| Grilla de datos | DataGridView (WinForms) / DataGrid (WPF) con virtualizacion |
| Lista de codificaciones | ComboBox con busqueda incremental |
| Tabla de formatos | DataGridView / DataGrid con celdas editables |
| Barra de progreso | ProgressBar con etiqueta de porcentaje |
| Selector de perfil | ComboBox en barra de herramientas |
| Panel de inicio | Panel / UserControl centrado sobre la grilla |

### 5.3 Comportamiento de ventanas

- La ventana principal debe recordar su posicion y tamano entre sesiones.
- Las ventanas de configuracion y formatos se abren como dialogos modales centrados sobre la ventana principal.
- El dialogo de progreso de exportacion no es redimensionable y no puede cerrarse con el boton X del sistema operativo durante la exportacion activa.
