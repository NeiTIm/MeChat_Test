using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using System.Xml;
using SkiaSharp;

namespace MeChat.Api.Features.Files;

public static class FileContentValidator
{
    private static readonly SemaphoreSlim ImageDecoders = new(2, 2);

    public static bool IsValid(Stream stream, string extension)
    {
        if (!stream.CanSeek || stream.Length is <= 0 or > 25L * 1024 * 1024) return false;
        stream.Position = 0;
        Span<byte> header = stackalloc byte[16];
        var count = stream.Read(header);
        extension = extension.ToLowerInvariant();
        try
        {
            if (extension is ".png" or ".jpg" or ".jpeg" or ".webp")
            {
                if (!DecodeImage(stream, extension)) return false;
                stream.Position = 0;
                count = stream.Read(header);
            }
            switch (extension)
            {
                case ".png":
                    if (count < 16 || !header[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return false;
                    stream.Position = 8;
                    var chunk = new byte[8];
                    var first = true; var data = false;
                    while (stream.Position + 12 <= stream.Length)
                    {
                        stream.ReadExactly(chunk);
                        var length = BinaryPrimitives.ReadUInt32BigEndian(chunk);
                        if (length > stream.Length - stream.Position - 4) return false;
                        var type = Encoding.ASCII.GetString(chunk, 4, 4);
                        if (first)
                        {
                            if (type != "IHDR" || length != 13) return false;
                            var dimensions = new byte[8]; stream.ReadExactly(dimensions);
                            var width = BinaryPrimitives.ReadUInt32BigEndian(dimensions);
                            var height = BinaryPrimitives.ReadUInt32BigEndian(dimensions.AsSpan(4));
                            if (width == 0 || height == 0 || (ulong)width * height > 40_000_000) return false;
                            stream.Position += length - 8;
                        }
                        else stream.Position += length;
                        stream.Position += 4; // Complete pixel decoding also runs before structural validation.
                        if (type == "IDAT" && length > 0) data = true;
                        if (type == "IEND") return length == 0 && data && stream.Position == stream.Length;
                        first = false;
                    }
                    return false;
                case ".jpg": case ".jpeg":
                    if (count < 3 || header[0] != 255 || header[1] != 216 || header[2] != 255 || stream.Length < 20) return false;
                    stream.Position = stream.Length - 2;
                    return stream.ReadByte() == 255 && stream.ReadByte() == 217;
                case ".webp":
                    return count >= 16 && header[..4].SequenceEqual("RIFF"u8) && header.Slice(8, 4).SequenceEqual("WEBP"u8)
                        && BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(4, 4)) + 8L == stream.Length
                        && (header.Slice(12, 4).SequenceEqual("VP8 "u8) || header.Slice(12, 4).SequenceEqual("VP8L"u8) || header.Slice(12, 4).SequenceEqual("VP8X"u8));
                case ".pdf": return count >= 5 && header[..5].SequenceEqual("%PDF-"u8);
                case ".txt":
                    stream.Position = 0;
                    using (var reader = new StreamReader(stream, new UTF8Encoding(false, true), false, 4096, leaveOpen: true))
                    {
                        var buffer = new char[4096]; int read;
                        while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
                            if (buffer.AsSpan(0, read).Contains('\0')) return false;
                    }
                    return true;
                case ".zip": case ".docx": case ".xlsx": return ValidArchive(stream, extension);
                default: return false;
            }
        }
        catch (Exception error) when (error is InvalidDataException or EndOfStreamException or DecoderFallbackException or XmlException or OverflowException)
        { return false; }
    }

    private static bool DecodeImage(Stream stream, string extension)
    {
        // Bound both decoded allocation and simultaneous native decoders, independent of compressed size.
        if (!ImageDecoders.Wait(0)) return false;
        try
        {
            stream.Position = 0;
            using var managed = new SKManagedStream(stream, false);
            using var codec = SKCodec.Create(managed);
            if (codec is null || codec.Info.Width <= 0 || codec.Info.Height <= 0) return false;
            var expected = extension switch { ".png" => SKEncodedImageFormat.Png, ".webp" => SKEncodedImageFormat.Webp, _ => SKEncodedImageFormat.Jpeg };
            if (codec.EncodedFormat != expected) return false;
            var pixels = (long)codec.Info.Width * codec.Info.Height;
            var frames = Math.Max(1, codec.FrameCount);
            if (pixels > 40_000_000 || frames > 100 || pixels * frames > 80_000_000) return false;
            var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
            using var bitmap = new SKBitmap();
            if (!bitmap.TryAllocPixels(info)) return false;
            for (var frame = 0; frame < frames; frame++)
                if (codec.GetPixels(info, bitmap.GetPixels(), new SKCodecOptions(frame)) != SKCodecResult.Success) return false;
            return true;
        }
        finally { ImageDecoders.Release(); }
    }

    private static bool ValidArchive(Stream stream, string extension)
    {
        stream.Position = 0;
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        if (archive.Entries.Count is 0 or > 1000) return false;
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long total = 0;
        foreach (var entry in archive.Entries)
        {
            var name = entry.FullName.Replace('\\', '/');
            if (name.StartsWith('/') || name.Contains(':') || name.Any(char.IsControl) || name.Split('/').Contains("..") || !names.Add(name)) return false;
            if (entry.Length > 25L * 1024 * 1024 || (total += entry.Length) > 100L * 1024 * 1024) return false;
            if (entry.Length > 1024 * 1024 && entry.Length / Math.Max(1, entry.CompressedLength) > 100) return false;
            if (extension != ".zip" && name.EndsWith("vbaProject.bin", StringComparison.OrdinalIgnoreCase)) return false;
            // Drain bounded entry data without extracting: reject corrupt/truncated compressed streams too.
            using var input = entry.Open();
            var buffer = new byte[32 * 1024];
            long actual = 0; int read;
            while ((read = input.Read(buffer)) > 0)
                if ((actual += read) > entry.Length) return false;
            if (actual != entry.Length) return false;
        }
        if (extension == ".zip") return true; // Never extract entries on the server.
        var main = extension == ".docx" ? "word/document.xml" : "xl/workbook.xml";
        var mainType = extension == ".docx" ? "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"
            : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml";
        var mainNamespace = extension == ".docx" ? "http://schemas.openxmlformats.org/wordprocessingml/2006/main"
            : "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var strictNamespace = extension == ".docx" ? "http://purl.oclc.org/ooxml/wordprocessingml/main" : "http://purl.oclc.org/ooxml/spreadsheetml/main";
        var mainRoot = extension == ".docx" ? "document" : "workbook";
        foreach (var name in new[] { "[Content_Types].xml", "_rels/.rels", main })
        {
            var entry = archive.GetEntry(name);
            if (entry is null || entry.Length is <= 0 or > 2 * 1024 * 1024) return false;
            using var content = entry.Open();
            using var xml = XmlReader.Create(content, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 2 * 1024 * 1024 });
            var root = false; var requiredPart = name == main;
            while (xml.Read())
            {
                if (xml.NodeType != XmlNodeType.Element) continue;
                if (!root)
                {
                    if (xml.Depth != 0) return false;
                    var expectedRoot = name == main ? mainRoot : name == "[Content_Types].xml" ? "Types" : "Relationships";
                    var expectedNamespace = name == main ? mainNamespace : name == "[Content_Types].xml"
                        ? "http://schemas.openxmlformats.org/package/2006/content-types" : "http://schemas.openxmlformats.org/package/2006/relationships";
                    if (xml.LocalName != expectedRoot || xml.NamespaceURI != expectedNamespace && !(name == main && xml.NamespaceURI == strictNamespace)) return false;
                    root = true;
                }
                if (name == "[Content_Types].xml" && xml.Depth == 1 && xml.LocalName == "Override"
                    && xml.NamespaceURI == "http://schemas.openxmlformats.org/package/2006/content-types"
                    && xml.GetAttribute("PartName") == "/" + main && xml.GetAttribute("ContentType") == mainType) requiredPart = true;
                if (name == "_rels/.rels" && xml.Depth == 1 && xml.LocalName == "Relationship"
                    && xml.NamespaceURI == "http://schemas.openxmlformats.org/package/2006/relationships"
                    && xml.GetAttribute("Type") is "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" or "http://purl.oclc.org/ooxml/officeDocument/relationships/officeDocument"
                    && xml.GetAttribute("Target")?.TrimStart('/') == main && xml.GetAttribute("TargetMode") != "External") requiredPart = true;
            }
            if (!root || !requiredPart) return false;
        }
        return true;
    }
}
