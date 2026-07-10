# Phase 3 Research: Formatos por Columna

## 1. Format Strings Reference

### 1.1 IFormattable.ToString(string format, IFormatProvider)

Every typed value in DbfRecord.Values is a boxed CLR type. The runtime types returned by DbfDataReader map as follows:

| DBF Type   | CLR Runtime Type        | IFormattable? | Default format in TxtExportService |
|------------|------------------------|---------------|-------------------------------------|
| Date (D)   | `DateTime`             | Yes           | `"yyyy-MM-dd"` (InvariantCulture)   |
| DateTime(T)| `DateTime`             | Yes           | `"yyyy-MM-dd"` (InvariantCulture)   |
| Numeric(N) | `decimal`              | Yes           | `null` → CurrentCulture              |
| Float (F)  | `decimal` or `double`  | Yes           | `null` → CurrentCulture              |
| Integer(I) | `int`                  | Yes           | `null` → CurrentCulture              |
| Logical(L) | `bool`                 | No            | `.ToString()`                        |
| Character  | `string`               | No            | `.ToString()`                        |
| Memo (M)   | `string`               | No            | `.ToString()`                        |

### 1.2 Common Format Strings by Type

**DateTime / DateTimeOffset:**
- `"yyyy-MM-dd"` — ISO date
- `"dd/MM/yyyy"` — European date
- `"MM/dd/yyyy"` — US date
- `"yyyy-MM-dd HH:mm:ss"` — ISO datetime
- `"dd/MM/yyyy HH:mm"` — European datetime
- `"HH:mm:ss"` — time only
- `"ddd, dd MMM yyyy"` — abbreviated day/month

**TimeSpan** (not currently used, but could be relevant for time-only fields):
- `@"hh\:mm\:ss"` — literal colon escaping
- `@"hh\:mm"` — hours and minutes
- `"c"` — invariant format `[-][d.]hh:mm:ss[.fffffff]`

**Numeric (decimal, double, int):**
- `"N2"` — 123,456.78 (thousands separator, 2 decimals)
- `"N0"` — 123,457 (thousands, no decimals)
- `"F2"` — 123456.78 (fixed point, no thousands)
- `"C"` — $123,456.78 (currency, respects CurrentCulture)
- `"P0"` — 12 % (percentage, 0 decimals)
- `"#,##0.00"` — custom: 1,234.56
- `"0.000"` — custom: 123.456
- `"G"` — general (default)

### 1.3 Format String Validation

```csharp
static bool IsValidFormatString(string format, Type targetType)
{
    if (string.IsNullOrEmpty(format)) return true; // raw
    try
    {
        var sample = targetType switch
        {
            Type t when t == typeof(DateTime) => DateTime.Now,
            Type t when t == typeof(decimal)  => 1234.56m,
            Type t when t == typeof(double)   => 1234.56,
            Type t when t == typeof(int)      => 1234,
            _ => null
        };
        if (sample is IFormattable f)
        {
            _ = f.ToString(format, CultureInfo.CurrentCulture);
            return true;
        }
        return false;
    }
    catch (FormatException) { return false; }
}
```

### 1.4 CultureInfo: InvariantCulture vs CurrentCulture

- **`CultureInfo.InvariantCulture`** — used in export. Ensures consistent output regardless of user's regional settings. `DateTime` → `"yyyy-MM-dd"`, `decimal` → `"1234.56"` (dot separator).
- **`CultureInfo.CurrentCulture`** — used in UI display. Shows format that matches user's control panel settings (e.g., `"1 234,56"` in es-AR, `"1,234.56"` in en-US).

**Recommendation for Phase 3:** The UI display should use `CurrentCulture` for numeric values (matching user expectations). The export format should use the *configured* format string with `CurrentCulture` (the user explicitly chose a format, so respect their culture). For export, `InvariantCulture` can be used only when no format string is configured (legacy behavior).

---

## 2. WPF Converter Patterns

### 2.1 Options Analysis

**Option A: Single `IValueConverter` with combined parameter** (recommended)
- Pass `"formatString|TypeName"` as `ConverterParameter`.
- Converter splits the parameter, gets runtime type from the value, applies format.
- Simple, single binding per cell.

```csharp
// ConverterParameter example: "yyyy-MM-dd|Date" or "N2|Numeric"
public class ColumnFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return string.Empty;
        if (parameter is not string raw || string.IsNullOrWhiteSpace(raw))
            return value.ToString() ?? string.Empty;

        // parameter = "N2" (just format) or "N2|Numeric" (format + hint)
        var format = raw;
        try
        {
            if (value is IFormattable fmt)
                return fmt.ToString(format, CultureInfo.CurrentCulture);
            return value.ToString() ?? string.Empty;
        }
        catch (FormatException)
        {
            return value.ToString() ?? string.Empty;
        }
    }

    public object ConvertBack(...) => Binding.DoNothing;
}
```

**Option B: `IMultiValueConverter` with MultiBinding**
- Pass `{Binding Value}` and `{Binding FormatString}` as two bindings.
- Cleaner separation, but requires `MultiBinding` which is verbose in code-behind.
- Adds complexity to dynamic column generation.

**Decision: Use Option A.** The single converter with format string as `ConverterParameter` is simpler, matches existing patterns (`StringEqualsConverter` uses `ConverterParameter`), and for dynamically-generated columns in code-behind, it is trivial to set `Binding.ConverterParameter = formatString`.

### 2.2 Null and Error Handling

- **Null values:** Return `string.Empty` (consistent with `FormatValue` in TxtExportService).
- **Empty format string:** Fall back to `.ToString()` (raw display).
- **FormatException:** Catch and return `.ToString()` (degradation without crash).
- **Unknown runtime type:** Return `.ToString()`.

### 2.3 Toggle Formatted/Raw Display

Two approaches:

1. **Switch converter parameter:** Bind a `ShowFormattedValues` bool to the converter via a separate mechanism. Easiest: create two converters (`FormattedConverter`, `RawConverter`) and swap the binding in `GenerateColumns()`.

2. **Converter uses a static/shared toggle:** The converter reads a static property `ColumnFormatConfig.ShowFormatted` to decide. This avoids rebinding but couples converter to global state.

**Recommendation:** Use approach 1 — regenerate columns with different converter when `ShowRawValues` toggles. MainViewModel exposes `bool ShowRawValues`, and `GenerateColumns` reads this to pick converter. Simple, testable, no global state.

---

## 3. DataGrid Dynamic Columns with Converters

### 3.1 Current Pattern (MainWindow.xaml.cs:43–85)

```csharp
var col = new DataGridTextColumn
{
    Header = headerPanel,
    Binding = new Binding
    {
        Path = new PropertyPath($"Values[{field.Name}]"),
        Converter = ... ,              // NEW
        ConverterParameter = ... ,     // NEW
        ConverterCulture = CultureInfo.CurrentCulture  // NEW
    },
    MinWidth = 60,
    MaxWidth = field.Type == DbfFieldType.Memo ? 120 : 300,
    Width = DataGridLength.Auto
};
```

### 3.2 Changes Needed in `GenerateColumns`

1. Build a dictionary `Dictionary<string, string>` of `{ fieldName → formatString }` from the ViewModel.
2. Before creating each column, check if `field.Type.HasConfigurableFormat()`. If yes, set `Converter` and `ConverterParameter`.
3. When `ShowRawValues` is true, omit the converter (or use a pass-through converter).

### 3.3 DataGridTemplateColumn Alternative

`DataGridTextColumn` works via `Binding.StringFormat` for simple cases, but it does **not** support converter-based value transformation for complex scenarios. However, since we use a converter, `DataGridTextColumn` is sufficient — the converter returns a `string`, which `TextBlock` displays directly.

No need for `DataGridTemplateColumn`. Keep using `DataGridTextColumn` with a converter.

### 3.4 Cell Editability

Phase 3 is display-only (no editing). Ensure `IsReadOnly="True"` on the DataGrid or per-column. Currently set at DataGrid level implicitly (no `IsReadOnly` on columns, but `CanUserAddRows="False"` and no edit commands). Explicitly set `col.IsReadOnly = true` on generated columns.

---

## 4. Service Architecture

### 4.1 ColumnFormatService Design

```csharp
public interface IColumnFormatService
{
    string FormatValue(object? value, DbfFieldType fieldType, string? formatString);
    bool IsValidFormat(DbfFieldType fieldType, string formatString);
    string GetDefaultFormat(DbfFieldType fieldType);
}
```

```
┌─────────────────────────────────────┐
│         IColumnFormatService        │
├─────────────────────────────────────┤
│ FormatValue(value, type, format)    │
│ IsValidFormat(type, format)         │
│ GetDefaultFormat(type)              │
└──────────┬──────────────────────────┘
           │
    ┌──────┴──────┐
    │             │
    ▼             ▼
MainViewModel  TxtExportService
(UI display)   (export)
```

### 4.2 Shared Logic: ColumnFormatService.FormatValue

The core formatting logic lives in `ColumnFormatService`:

```csharp
public string FormatValue(object? value, DbfFieldType fieldType, string? formatString)
{
    if (value == null) return string.Empty;

    if (string.IsNullOrEmpty(formatString))
        return FallbackFormat(value, fieldType);

    try
    {
        if (value is IFormattable fmt)
            return fmt.ToString(formatString, CultureInfo.CurrentCulture);
        return value.ToString() ?? string.Empty;
    }
    catch (FormatException)
    {
        return value.ToString() ?? string.Empty;
    }
}

private static string FallbackFormat(object value, DbfFieldType fieldType)
{
    // Preserve existing export defaults when no format is configured
    return fieldType switch
    {
        DbfFieldType.Date or DbfFieldType.DateTime
            when value is DateTime dt => dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        _ when value is IFormattable fmt => fmt.ToString(null, CultureInfo.CurrentCulture),
        _ => value.ToString() ?? string.Empty
    };
}
```

This replicates the current `TxtExportService.FormatValue` behavior when no format string is configured, ensuring backward compatibility.

### 4.3 Thread Safety

`ColumnFormatService` is **stateless** (pure functions). No locks needed. Safe to call from any thread:

- **UI thread:** `MainViewModel` calls it to build `FormattedRecord` wrapper objects for the grid.
- **Background thread:** `TxtExportService` calls it during export inside `Task.Run`.

### 4.4 Integration with TxtExportService

Extract the current `FormatValue` logic into `ColumnFormatService`:

```csharp
// TxtExportService.FormatValue becomes a thin wrapper:
internal static string FormatValue(DbfRecord record, DbfField field, 
    ExportConfiguration config, IColumnFormatService formatService)
{
    var value = record.Values.GetValueOrDefault(field.Name);
    var formatString = config.ColumnFormats?.GetValueOrDefault(field.Name);
    return formatService.FormatValue(value, field.Type, formatString);
}
```

### 4.5 DI Registration

In `App.xaml.cs`:
```csharp
var formatService = new ColumnFormatService();
var mainViewModel = new MainViewModel(dbfReader, encodingDetector, exportService, formatService);
```

Alternatively, if `TxtExportService` needs `IColumnFormatService`, inject it via constructor:
```csharp
public class TxtExportService : IExportService
{
    private readonly IColumnFormatService _formatService;
    public TxtExportService(IColumnFormatService formatService)
    {
        _formatService = formatService;
    }
    // ...
}
```

---

## 5. Integration Points

### 5.1 Files That Change

| File | Change |
|------|--------|
| `src/VisorDBF.Core/Models/ExportConfiguration.cs` | Add `Dictionary<string, string> ColumnFormats` property (field name → format string) |
| `src/VisorDBF.Core/Services/IColumnFormatService.cs` | **New file** — interface |
| `src/VisorDBF.Core/Services/ColumnFormatService.cs` | **New file** — implementation |
| `src/VisorDBF.Core/Services/TxtExportService.cs` | Replace `FormatValue` to use `IColumnFormatService` |
| `src/VisorDBF.UI/Converters/ColumnFormatConverter.cs` | **New file** — IValueConverter |
| `src/VisorDBF.UI/ViewModels/MainViewModel.cs` | Add `IColumnFormatService` dependency, `ShowRawValues` property, `ColumnFormatConfig` dictionary |
| `src/VisorDBF.UI/Views/MainWindow.xaml.cs` | Update `GenerateColumns` to attach converter based on field type and format config |
| `src/VisorDBF.UI/Views/MainWindow.xaml` | Add menu item for "Formatos de columnas..." (currently disabled), keyboard shortcut |
| `src/VisorDBF.UI/App.xaml` | Register `ColumnFormatConverter` as `Application.Resources` |
| `src/VisorDBF.UI/App.xaml.cs` | Wire `IColumnFormatService` into DI |

### 5.2 New Dialog: ColumnFormatDialog

A new WPF dialog (View + ViewModel) for configuring per-column formats:

- Shows a `DataGrid` with columns: Field Name, Type, Current Format, Format Input
- User can type/edit format strings for Date/DateTime/Numeric/Float fields
- Format validation via `IColumnFormatService.IsValidFormat()` with visual feedback (green check / red X)
- "Reset to default" and "Clear all" buttons
- Returns `Dictionary<string, string>` back to `MainViewModel`

### 5.3 Toggle Raw Values in DataGrid

- Add `bool ShowRawValues` property to `MainViewModel`
- Bind to a toggle button (checkbox or toolbar toggle)
- When `ShowRawValues` changes, call `GenerateColumns` again (or rebuild bindings)
- Same mechanism used to toggle between formatted/raw display

### 5.4 Export Configuration Update

`ExportConfiguration.ColumnFormats` flows through to `TxtExportService`. The export UI dialog can optionally show a "Use display formats for export" checkbox.

---

## 6. Recommendations

### 6.1 Concrete Approach

1. **ColumnFormatService (Core):**
   - Create `IColumnFormatService` / `ColumnFormatService` in `VisorDBF.Core/Services/`
   - Pure stateless class with `FormatValue(object?, DbfFieldType, string?)` and `IsValidFormat(DbfFieldType, string)`
   - Thread-safe by design (no state)

2. **ExportConfiguration (Core):**
   - Add `Dictionary<string, string> ColumnFormats { get; init; } = new()` to the record
   - Key = field name, Value = format string (null/empty = default)

3. **TxtExportService (Core):**
   - Accept `IColumnFormatService` via DI
   - `FormatValue` becomes `FormatService.FormatValue(value, field.Type, config.ColumnFormats?.GetValueOrDefault(field.Name))`
   - Legacy default formats preserved via `FallbackFormat`

4. **ColumnFormatConverter (UI):**
   - New `IValueConverter` in `VisorDBF.UI.Converters`
   - `Convert` receives `value` and `parameter` (format string)
   - Delegates to `ColumnFormatService.FormatValue` for consistency
   - Registered in `App.xaml` as `<converters:ColumnFormatConverter x:Key="ColumnFormatConverter"/>`

5. **MainViewModel (UI):**
   - Add `IColumnFormatService` dependency
   - Add `Dictionary<string, string> ColumnFormatConfig` property
   - Add `bool ShowRawValues` property
   - When `ColumnFormatConfig` changes, notify UI to regenerate columns
   - Open new `ColumnFormatDialog` from menu command

6. **MainWindow.xaml.cs (UI):**
   - `GenerateColumns` reads `ColumnFormatConfig` and `ShowRawValues`
   - For configurable types: attach `ColumnFormatConverter` with `formatString` as parameter
   - For non-configurable types: no converter (raw string)

7. **ColumnFormatDialog (UI):**
   - New `ColumnFormatDialog` window + `ColumnFormatViewModel` (following pattern from `ExportConfigurationDialog`)
   - Editable DataGrid showing fields with configurable types
   - Real-time format validation
   - Returns updated `Dictionary<string, string>`

### 6.2 Key Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Converter parameter | Single `IValueConverter` with string parameter | Matches existing `StringEqualsConverter` style, simpler than MultiBinding |
| Format string storage | `Dictionary<string, string>` on `ExportConfiguration` | Reusable by both UI and export; no new model types needed |
| Thread safety | Stateless service, no locks | Service is pure; UI and export threads call independently |
| Raw value toggle | Regenerate columns with different converter | Clean break, easy to implement, no dynamic binding switching |
| Format string validation | Try `IFormattable.ToString` in `IsValidFormat` | Same code path as actual formatting; guarantees consistency |
| Culture for export | Use `CultureInfo.CurrentCulture` when format is set | User chose a format — respect their locale; use `InvariantCulture` only for legacy defaults |

### 6.3 Backward Compatibility

- No format configured → behavior identical to current code (Date → `"yyyy-MM-dd"` invariant, numeric → `null` format → `CurrentCulture`)
- Old `ExportConfiguration` instances (without `ColumnFormats` dict) → default to empty dict
- Existing `TxtExportService.FormatValue` moved into `ColumnFormatService.FallbackFormat`
