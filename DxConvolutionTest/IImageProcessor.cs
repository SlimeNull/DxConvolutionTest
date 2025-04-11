namespace DxConvolutionTest
{
    public interface IImageProcessor
    {
        int InputWidth { get; }
        int InputHeight { get; }
        int OutputWidth { get; }
        int OutputHeight { get; }

        void Process(ReadOnlySpan<byte> inputBgraData, Span<byte> outputBgraData);
    }
}
