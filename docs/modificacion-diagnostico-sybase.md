# Modificación: Migración Sybase a ODBC

## Objetivo
Reemplazar `AdoNetCore.AseClient` por `System.Data.Odbc` para compatibilidad con Sybase ASE 15.7.

## Archivos modificados (4)

### 1. `src/VisorDBF.Core/VisorDBF.Core.csproj`
- Eliminar `<PackageReference Include="AdoNetCore.AseClient" />`
- Agregar `<PackageReference Include="System.Data.Odbc" Version="8.*" />`

### 2. `src/VisorDBF.Core/Services/SybaseExportService.cs`
- `AseConnection` → `OdbcConnection`
- `AseCommand` → `OdbcCommand`
- `AseParameter` → `OdbcParameter`
- Eliminar método `GetAseDbType()`
- Eliminar `command.Prepare()`
- Connection string ODBC:
  ```
  DRIVER={Sybase ASE ODBC Driver};Server=host:port;Database=db;UID=user;PWD=pass;
  ```

### 3. `src/VisorDBF.UI/ViewModels/SybaseConnectionViewModel.cs`
- `AseConnection` → `OdbcConnection`
- Connection string ODBC (mismo formato)
- Preview `ConnectionString` actualizado a formato ODBC

### 4. `src/VisorDBF.UI/Views/SybaseConnectionDialog.xaml`
- Sin cambios (ya adaptado con Expander + Copiar detalle)

## Build & Publish
```powershell
dotnet publish src\VisorDBF.UI -c Release -r win-x64 --self-contained true
```

Output: `src\VisorDBF.UI\bin\Release\net8.0-windows\win-x64\publish\`

## Notas
- ODBC requiere driver Sybase ASE ODBC instalado en el equipo destino
- Si el ODBC es 32 bits, publicar como `win-x86` en vez de `win-x64`
