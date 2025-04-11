using System.Diagnostics;
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
            var weights = GaussianBlur.GetWeights(10);
            Console.WriteLine(string.Join(", ", weights));
            Console.ReadKey();

            var inputBitmapBytes = File.ReadAllBytes("test.png");
            SKBitmap bitmap = SKBitmap.Decode(inputBitmapBytes);

            var blurSize = 5;
            IImageProcessor gpuBlur = new CpuAvgBlur(bitmap.Width, bitmap.Height, blurSize);
            IImageProcessor cpuBlur = new CpuAvgBlurOptimized(bitmap.Width, bitmap.Height, blurSize);

            SKBitmap outputBitmap = new SKBitmap(gpuBlur.OutputWidth, gpuBlur.OutputHeight, SKColorType.Bgra8888, SKAlphaType.Unpremul);

            bool notFirst = false;

            Stopwatch stopwatch = new Stopwatch();
            for (int i = 0; i < 100; i++)
            {
                stopwatch.Restart();
                gpuBlur.Process(bitmap.GetPixelSpan(), outputBitmap.GetPixelSpan());
                stopwatch.Stop();

                Console.WriteLine($"GPU Elapsed: {stopwatch.ElapsedMilliseconds}ms");

                if (!notFirst)
                {
                    using var outputFile = File.Create("gpu_output.png");
                    outputBitmap.Encode(outputFile, SKEncodedImageFormat.Png, 1);
                }

                stopwatch.Restart();
                cpuBlur.Process(bitmap.GetPixelSpan(), outputBitmap.GetPixelSpan());
                stopwatch.Stop();

                Console.WriteLine($"CPU Elapsed: {stopwatch.ElapsedMilliseconds}ms");

                if (!notFirst)
                {
                    using var outputFile = File.Create("cpu_output.png");
                    outputBitmap.Encode(outputFile, SKEncodedImageFormat.Png, 1);
                }

                notFirst = true;
            }
        }
    }
}
