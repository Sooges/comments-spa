using System.Text;
using CommentsApi.Models;
using SkiaSharp;

namespace CommentsApi.Services;

public class AttachmentProcessor : IAttachmentProcessor
{
    private const int MaxImageWidth = 320;
    private const int MaxImageHeight = 240;
    private const int JpegQuality = 85;
    private const long MaxTextFileSize = 100 * 1024;

    // limits that protect the server from huge files, they are not a part of the task
    private const long MaxImageFileSize = 5 * 1024 * 1024;
    private const long MaxImagePixels = 25_000_000;

    private const int MaxFileNameLength = 100;

    public async Task<AttachmentResult> ProcessAsync(IFormFile file)
    {
        if (file.Length == 0)
        {
            return AttachmentResult.Fail("Файл пустой.");
        }

        // the extension is used only to decide how to check the file, the real type is found by the content
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var isText = extension == ".txt";
        var isImage = extension is ".jpg" or ".jpeg" or ".png" or ".gif";

        if (!isText && !isImage)
        {
            return AttachmentResult.Fail("Можно прикрепить картинку (JPG, GIF, PNG) или текстовый файл (TXT).");
        }

        if (isText && file.Length > MaxTextFileSize)
        {
            return AttachmentResult.Fail("Текстовый файл не должен быть больше 100 КБ.");
        }

        if (isImage && file.Length > MaxImageFileSize)
        {
            return AttachmentResult.Fail("Файл с картинкой не должен быть больше 5 МБ.");
        }

        byte[] content;
        using (var stream = file.OpenReadStream())
        using (var memory = new MemoryStream())
        {
            await stream.CopyToAsync(memory);
            content = memory.ToArray();
        }

        var name = GetSafeFileName(file.FileName);
        return isText ? ProcessText(content, name) : ProcessImage(content, name);
    }

    private static AttachmentResult ProcessText(byte[] content, string name)
    {
        // it must be a real text: valid UTF-8 and without binary data
        try
        {
            new UTF8Encoding(false, true).GetString(content);
        }
        catch (DecoderFallbackException)
        {
            return AttachmentResult.Fail("Текстовый файл должен быть в кодировке UTF-8.");
        }

        if (Array.IndexOf(content, (byte)0) >= 0)
        {
            return AttachmentResult.Fail("Файл не похож на текстовый.");
        }

        return AttachmentResult.Success(new PreparedAttachment
        {
            Kind = AttachmentKind.Text,
            OriginalName = name,
            ContentType = "text/plain; charset=utf-8",
            Extension = ".txt",
            Content = content
        });
    }

    private static AttachmentResult ProcessImage(byte[] content, string name)
    {
        using var data = SKData.CreateCopy(content);
        using var codec = SKCodec.Create(data);
        if (codec == null)
        {
            return AttachmentResult.Fail("Файл не является картинкой.");
        }

        var format = codec.EncodedFormat;
        if (format != SKEncodedImageFormat.Jpeg
            && format != SKEncodedImageFormat.Png
            && format != SKEncodedImageFormat.Gif)
        {
            return AttachmentResult.Fail("Допустимые форматы картинок: JPG, GIF, PNG.");
        }

        // A photo can be stored turned, the camera only writes how to turn it (EXIF orientation).
        // Width and height below are the size the user sees, after turning.
        var origin = codec.EncodedOrigin;
        var isTurned = origin >= SKEncodedOrigin.LeftTop;
        var width = isTurned ? codec.Info.Height : codec.Info.Width;
        var height = isTurned ? codec.Info.Width : codec.Info.Height;
        if ((long)width * height > MaxImagePixels)
        {
            return AttachmentResult.Fail("Картинка слишком большая.");
        }

        var needResize = width > MaxImageWidth || height > MaxImageHeight;

        // a small GIF is saved as it is, so it keeps its animation
        if (format == SKEncodedImageFormat.Gif && !needResize)
        {
            return AttachmentResult.Success(new PreparedAttachment
            {
                Kind = AttachmentKind.Image,
                OriginalName = name,
                ContentType = "image/gif",
                Extension = ".gif",
                Content = content,
                Width = width,
                Height = height
            });
        }

        using var bitmap = SKBitmap.Decode(codec);
        if (bitmap == null)
        {
            return AttachmentResult.Fail("Не удалось прочитать картинку.");
        }

        // big pictures are made smaller, the proportions are kept
        SKBitmap? resized = null;
        if (needResize)
        {
            var scale = Math.Min((double)MaxImageWidth / width, (double)MaxImageHeight / height);
            var newWidth = Math.Max(1, (int)Math.Round(width * scale));
            var newHeight = Math.Max(1, (int)Math.Round(height * scale));

            // the picture is made smaller before it is turned, so for a turned one the sides are swapped back
            resized = bitmap.Resize(
                isTurned ? new SKImageInfo(newHeight, newWidth) : new SKImageInfo(newWidth, newHeight),
                new SKSamplingOptions(SKCubicResampler.Mitchell));

            if (resized == null)
            {
                return AttachmentResult.Fail("Не удалось уменьшить картинку.");
            }
        }

        using (resized)
        using (var turned = ApplyOrientation(resized ?? bitmap, origin))
        {
            var result = turned ?? resized ?? bitmap;

            // JPEG stays JPEG, PNG and big GIF are saved as PNG (the library can not write GIF)
            var isJpeg = format == SKEncodedImageFormat.Jpeg;
            using var image = SKImage.FromBitmap(result);
            using var encoded = image.Encode(isJpeg ? SKEncodedImageFormat.Jpeg : SKEncodedImageFormat.Png, JpegQuality);

            return AttachmentResult.Success(new PreparedAttachment
            {
                Kind = AttachmentKind.Image,
                OriginalName = name,
                ContentType = isJpeg ? "image/jpeg" : "image/png",
                Extension = isJpeg ? ".jpg" : ".png",
                Content = encoded.ToArray(),
                Width = result.Width,
                Height = result.Height
            });
        }
    }

    // Turns the picture the way the user sees it. Returns null if there is nothing to turn.
    private static SKBitmap? ApplyOrientation(SKBitmap bitmap, SKEncodedOrigin origin)
    {
        if (origin == SKEncodedOrigin.TopLeft || origin == SKEncodedOrigin.Default)
        {
            return null;
        }

        var w = bitmap.Width;
        var h = bitmap.Height;
        var isTurned = origin >= SKEncodedOrigin.LeftTop;

        var result = new SKBitmap(isTurned ? h : w, isTurned ? w : h);
        using var canvas = new SKCanvas(result);

        switch (origin)
        {
            case SKEncodedOrigin.TopRight:      // mirrored left to right
                canvas.Translate(w, 0);
                canvas.Scale(-1, 1);
                break;
            case SKEncodedOrigin.BottomRight:   // turned by 180 degrees
                canvas.Translate(w, h);
                canvas.RotateDegrees(180);
                break;
            case SKEncodedOrigin.BottomLeft:    // mirrored top to bottom
                canvas.Translate(0, h);
                canvas.Scale(1, -1);
                break;
            case SKEncodedOrigin.LeftTop:       // x and y swapped
                canvas.SetMatrix(new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1));
                break;
            case SKEncodedOrigin.RightTop:      // turned by 90 degrees clockwise (a usual phone photo)
                canvas.Translate(h, 0);
                canvas.RotateDegrees(90);
                break;
            case SKEncodedOrigin.RightBottom:   // x and y swapped and turned by 180 degrees
                canvas.SetMatrix(new SKMatrix(0, -1, h, -1, 0, w, 0, 0, 1));
                break;
            case SKEncodedOrigin.LeftBottom:    // turned by 90 degrees counterclockwise
                canvas.Translate(0, w);
                canvas.RotateDegrees(-90);
                break;
        }

        // only turning and mirroring, so the pixels are copied as they are
        canvas.DrawBitmap(bitmap, 0, 0, new SKSamplingOptions(SKFilterMode.Nearest));
        return result;
    }

    private static string GetSafeFileName(string fileName)
    {
        // the name is only shown to the users, it is never used as a path
        var name = Path.GetFileName(fileName);
        var cleaned = new string(name.Where(c => !char.IsControl(c)).ToArray()).Trim();

        if (cleaned.Length > MaxFileNameLength)
        {
            cleaned = cleaned[..MaxFileNameLength];
        }

        return cleaned.Length == 0 ? "file" : cleaned;
    }
}
