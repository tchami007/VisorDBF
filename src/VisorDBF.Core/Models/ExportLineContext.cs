using System.Globalization;
using VisorDBF.Core.Services;

namespace VisorDBF.Core.Models;

public sealed record ExportLineContext(
    DbfRecord Record,
    IReadOnlyList<DbfField> Fields,
    ExportConfiguration Config,
    ColumnFormatConfiguration? ColumnFormats = null,
    IColumnFormatService? FormatService = null,
    CultureInfo? NumberCulture = null);
