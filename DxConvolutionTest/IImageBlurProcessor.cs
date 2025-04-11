namespace DxConvolutionTest
{
    public interface IImageBlurProcessor : IImageProcessor
    {
        int BlurSize { get; }
    }
}
