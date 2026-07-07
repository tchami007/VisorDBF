namespace VisorDBF.Core.Tests.Helpers;

/// <summary>
/// Helper para generar archivos DBF minimos validos para tests de integracion.
/// Genera un binario DBF desde cero sin depender de herramientas externas.
/// </summary>
public static class DbfTestHelper
{
    /// <summary>
    /// Genera un archivo DBF minimo valido en el directorio indicado.
    /// Estructura: 1 campo CHARACTER "NOMBRE" 10 chars, 2 registros (1 activo, 1 deleted).
    /// Language Driver ID: 0x57 (CP1252).
    /// </summary>
    /// <param name="directory">Directorio donde se creara el archivo.</param>
    /// <param name="fileName">Nombre del archivo (default: test_minimal.dbf).</param>
    /// <returns>Ruta completa del archivo creado.</returns>
    public static string CreateMinimalDbf(string directory, string fileName = "test_minimal.dbf")
    {
        // Parametros del archivo
        const byte version = 0x03;        // dBASE III
        const int numRecords = 2;         // 1 activo + 1 deleted
        const int fieldLength = 10;       // NOMBRE: C(10)
        const byte languageDriverId = 0x57; // CP1252

        // Nombre del campo: null-padded to 11 bytes
        var fieldNameBytes = new byte[11];
        var nameSrc = System.Text.Encoding.ASCII.GetBytes("NOMBRE");
        Array.Copy(nameSrc, fieldNameBytes, Math.Min(nameSrc.Length, 11));

        // Tamanos
        int headerSize = 32 + 32 + 1; // header + 1 field descriptor + terminator
        int recordSize = 1 + fieldLength; // delete flag + NOMBRE

        using var ms = new System.IO.MemoryStream();
        using var writer = new System.IO.BinaryWriter(ms);

        // === DBF Header (32 bytes) ===
        writer.Write(version);                     // [0] Version
        writer.Write((byte)124);                   // [1] Year (2024 - 1900)
        writer.Write((byte)1);                     // [2] Month
        writer.Write((byte)1);                     // [3] Day
        writer.Write((uint)numRecords);            // [4-7] Record count (LE)
        writer.Write((ushort)headerSize);          // [8-9] Header size (LE)
        writer.Write((ushort)recordSize);          // [10-11] Record size (LE)
        writer.Write(new byte[16]);                // [12-27] Reserved
        writer.Write((byte)0);                     // [28] Table flags
        writer.Write(languageDriverId);            // [29] Language Driver ID
        writer.Write(new byte[2]);                 // [30-31] Reserved

        // === Field Descriptor (32 bytes) ===
        writer.Write(fieldNameBytes);              // [0-10] Field name
        writer.Write((byte)'C');                   // [11] Field type
        writer.Write(new byte[4]);                 // [12-15] Reserved (field address)
        writer.Write((byte)fieldLength);           // [16] Field length
        writer.Write((byte)0);                     // [17] Decimal count
        writer.Write(new byte[14]);                // [18-31] Reserved

        // === Header terminator ===
        writer.Write((byte)0x0D);

        // === Record 1 (active) ===
        writer.Write((byte)0x20); // Not deleted
        var alice = PadRight("Alice", fieldLength);
        writer.Write(System.Text.Encoding.ASCII.GetBytes(alice));

        // === Record 2 (deleted) ===
        writer.Write((byte)0x2A); // Deleted (*)
        var bob = PadRight("Bob", fieldLength);
        writer.Write(System.Text.Encoding.ASCII.GetBytes(bob));

        // === EOF marker ===
        writer.Write((byte)0x1A);

        // Escribir al disco
        var path = System.IO.Path.Combine(directory, fileName);
        System.IO.File.WriteAllBytes(path, ms.ToArray());
        return path;
    }

    private static string PadRight(string value, int length)
        => value.Length >= length ? value[..length] : value.PadRight(length);
}
