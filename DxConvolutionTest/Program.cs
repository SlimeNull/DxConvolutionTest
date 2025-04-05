using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using DxConvolutionTest.Properties;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using SkiaSharp;

namespace DxConvolutionTest
{
    internal class Program
    {
        static unsafe void Main(string[] args)
        {
            var inputBitmapBytes = File.ReadAllBytes("test.png");
            SKBitmap bitmap = SKBitmap.Decode(inputBitmapBytes);

            DxBlur blur = new DxBlur(bitmap.Width, bitmap.Height, 20, 0.5f, 0.5f, 0.5f);
            SKBitmap outputBitmap = new SKBitmap(blur.OutputWidth, blur.OutputHeight, SKColorType.Bgra8888, SKAlphaType.Unpremul);
            blur.Process(bitmap.GetPixelSpan(), outputBitmap.GetPixelSpan());

            using var outputFile = File.Create("output.png");
            outputBitmap.Encode(outputFile, SKEncodedImageFormat.Png, 1);
        }
    }
}
