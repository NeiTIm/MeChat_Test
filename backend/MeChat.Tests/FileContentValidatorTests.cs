using System.IO.Compression;
using System.Text;
using SkiaSharp;
using MeChat.Api.Features.Files;

namespace MeChat.Tests;

public sealed class FileContentValidatorTests
{
    private static bool Valid(byte[] bytes, string extension) => FileContentValidator.IsValid(new MemoryStream(bytes), extension);
    private static byte[] Archive(params (string Name, string Content)[] entries)
    {
        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, true))
            foreach (var (name, content) in entries)
            { using var writer = new StreamWriter(zip.CreateEntry(name).Open()); writer.Write(content); }
        return output.ToArray();
    }

    [Fact]
    public void Archives_reject_bombs_traversal_duplicate_names_and_invalid_office_xml()
    {
        Assert.True(Valid(Archive(("proof.txt", "safe fixture")), ".zip"));
        Assert.False(Valid(Archive(("../../outside", "deny")), ".zip"));
        Assert.False(Valid(Archive(("C:/outside", "deny")), ".zip"));
        Assert.False(Valid(Archive(("same.txt", "a"), ("SAME.txt", "b")), ".zip"));
        Assert.False(Valid(Archive(("bomb.txt", new string('a', 2 * 1024 * 1024))), ".zip"));
        Assert.False(Valid(Archive(("word/document.xml", "<document/>")), ".docx"));
        Assert.False(Valid(Archive(("[Content_Types].xml", "<Types/>"), ("word/document.xml", "<document/>")), ".docx"));
        foreach (var (extension, main, root, ns, mime) in new[] {
            (".docx", "word/document.xml", "document", "wordprocessingml", "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"),
            (".xlsx", "xl/workbook.xml", "workbook", "spreadsheetml", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml") })
        {
            var types = $"<Types xmlns='http://schemas.openxmlformats.org/package/2006/content-types'><Override PartName='/{main}' ContentType='{mime}'/></Types>";
            var relationships = $"<Relationships xmlns='http://schemas.openxmlformats.org/package/2006/relationships'><Relationship Id='rId1' Type='http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument' Target='{main}'/></Relationships>";
            var content = $"<{root} xmlns='http://schemas.openxmlformats.org/{ns}/2006/main'/>";
            Assert.True(Valid(Archive(("[Content_Types].xml", types), ("_rels/.rels", relationships), (main, content)), extension));
            Assert.False(Valid(Archive(("[Content_Types].xml", types), ("_rels/.rels", relationships), (main, "<wrong/>")), extension));
        }
        Assert.False(Valid(Archive(("[Content_Types].xml", "<Types/>"), ("word/document.xml", "<!DOCTYPE d [<!ENTITY x SYSTEM 'file:///outside'>]><d>&x;</d>")), ".docx"));
        Assert.False(Valid([80, 75, 3, 4, 0], ".zip"));
    }

    [Fact]
    public void Image_structure_and_full_text_are_checked_instead_of_only_a_short_signature()
    {
        Assert.False(Valid([137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 0], ".png"));
        Assert.True(Valid(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+ip1sAAAAASUVORK5CYII="), ".png"));
        Assert.True(Valid(Encoding.UTF8.GetBytes(new string('a', 4095) + "ế"), ".txt"));
        Assert.False(Valid(Encoding.UTF8.GetBytes(new string('a', 5000) + "\0"), ".txt"));
        Assert.False(Valid([0xff, 0xff], ".txt"));
        Assert.False(Valid([255, 216, 255], ".jpg"));
    }
    [Fact]
    public void Real_image_decoders_check_pixels_format_and_corruption()
    {
        using var bitmap = new SKBitmap(8, 8);
        bitmap.Erase(SKColors.CornflowerBlue);
        using var image = SKImage.FromBitmap(bitmap);
        foreach (var (format, extension) in new[] { (SKEncodedImageFormat.Png, ".png"), (SKEncodedImageFormat.Jpeg, ".jpg"), (SKEncodedImageFormat.Webp, ".webp") })
        {
            using var encoded = image.Encode(format, 90);
            var bytes = encoded.ToArray();
            Assert.True(Valid(bytes, extension));
            Assert.False(Valid(bytes, extension == ".png" ? ".jpg" : ".png"));
            Assert.False(Valid(bytes[..(bytes.Length / 2)], extension));
        }
        // This formerly accepted signature fixture has a bad IDAT CRC.
        Assert.False(Valid(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+a3foAAAAASUVORK5CYII="), ".png"));
    }

}
