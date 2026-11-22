using PdfSharp.Fonts;
using System.IO;

namespace CoffeeShop.Helpers
{
    public class FontResolverHelper : IFontResolver
    {
        private readonly string _fontsFolder;

        public FontResolverHelper(string fontsFolder)
        {
            _fontsFolder = fontsFolder;
        }

        public byte[] GetFont(string faceName)
        {
            var filePath = Path.Combine(_fontsFolder, faceName);
            if (File.Exists(filePath))
            {
                return File.ReadAllBytes(filePath);
            }
            throw new FileNotFoundException($"Шрифт не найден: {filePath}");
        }

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            if (familyName.Equals("Arial", StringComparison.OrdinalIgnoreCase))
            {
                if (isBold && isItalic)
                    return new FontResolverInfo("arialbi.ttf");
                if (isBold)
                    return new FontResolverInfo("arialbd.ttf");
                if (isItalic)
                    return new FontResolverInfo("ariali.ttf");

                return new FontResolverInfo("arial.ttf");
            }

            return new FontResolverInfo("arial.ttf");
        }
    }
}
