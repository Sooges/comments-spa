using System.Security.Cryptography;
using CommentsApi.Dtos;
using Microsoft.Extensions.Caching.Memory;
using SkiaSharp;

namespace CommentsApi.Services;

public class CaptchaService : ICaptchaService
{
    // latin letters and digits, without the ones that are easy to confuse (0/O, 1/I)
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 5;
    private const int Width = 170;
    private const int Height = 60;
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    private readonly IMemoryCache _cache;

    public CaptchaService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public CaptchaResponse Create()
    {
        var code = GenerateCode();
        var id = Guid.NewGuid().ToString("N");

        _cache.Set(GetCacheKey(id), code, Lifetime);

        var png = DrawImage(code);
        return new CaptchaResponse
        {
            Id = id,
            Image = "data:image/png;base64," + Convert.ToBase64String(png)
        };
    }

    public bool Validate(string id, string code)
    {
        var key = GetCacheKey(id);
        if (!_cache.TryGetValue(key, out string? expectedCode))
        {
            return false;
        }

        // a captcha can not be used twice, even if the code was wrong
        _cache.Remove(key);

        return string.Equals(expectedCode, code.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string GetCacheKey(string id)
    {
        return "captcha:" + id;
    }

    private static string GenerateCode()
    {
        var chars = new char[CodeLength];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new string(chars);
    }

    private static byte[] DrawImage(string code)
    {
        var random = Random.Shared;

        using var bitmap = new SKBitmap(Width, Height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        // noise lines
        using (var linePaint = new SKPaint { StrokeWidth = 1.5f, IsAntialias = true })
        {
            for (var i = 0; i < 6; i++)
            {
                linePaint.Color = RandomColor(random, 140, 220);
                canvas.DrawLine(
                    random.Next(Width), random.Next(Height),
                    random.Next(Width), random.Next(Height),
                    linePaint);
            }
        }

        // the code, every letter has its own color, position and angle
        using var typeface = LoadTypeface();
        using var font = new SKFont(typeface, 36);
        using var textPaint = new SKPaint { IsAntialias = true };

        var step = Width / (code.Length + 1);
        for (var i = 0; i < code.Length; i++)
        {
            var x = step * (i + 0.6f);
            var y = Height / 2f + 12 + random.Next(-6, 7);

            textPaint.Color = RandomColor(random, 20, 120);
            canvas.Save();
            canvas.RotateDegrees(random.Next(-18, 19), x, y);
            canvas.DrawText(code[i].ToString(), x, y, SKTextAlign.Left, font, textPaint);
            canvas.Restore();
        }

        // noise dots
        using (var dotPaint = new SKPaint { IsAntialias = true })
        {
            for (var i = 0; i < 80; i++)
            {
                dotPaint.Color = RandomColor(random, 100, 200);
                canvas.DrawCircle(random.Next(Width), random.Next(Height), 1f, dotPaint);
            }
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static SKColor RandomColor(Random random, int min, int max)
    {
        return new SKColor(
            (byte)random.Next(min, max),
            (byte)random.Next(min, max),
            (byte)random.Next(min, max));
    }

    private static SKTypeface LoadTypeface()
    {
        // DejaVu is installed in the Docker image, Arial is used on Windows
        string[] fontFiles =
        {
            "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
            @"C:\Windows\Fonts\arialbd.ttf"
        };

        foreach (var file in fontFiles)
        {
            if (File.Exists(file))
            {
                return SKTypeface.FromFile(file);
            }
        }

        return SKTypeface.FromFamilyName("sans-serif", SKFontStyle.Bold) ?? SKTypeface.Default;
    }
}
