namespace DxConvolutionTest
{
    public class CpuAvgBlur : IImageBlurProcessor
    {
        private readonly int _inputWidth;
        private readonly int _inputHeight;
        private readonly int _blurSize;
        private readonly int _outputWidth;
        private readonly int _outputHeight;

        public int InputWidth => _inputWidth;
        public int InputHeight => _inputHeight;
        public int OutputWidth => _outputWidth;
        public int OutputHeight => _outputHeight;
        public int BlurSize => _blurSize;

        public CpuAvgBlur(int inputWidth, int inputHeight, int blurSize)
        {
            if (blurSize < 1)
                throw new ArgumentOutOfRangeException(nameof(blurSize), "Blur size must be at least 1");

            _inputWidth = inputWidth;
            _inputHeight = inputHeight;
            _outputWidth = inputWidth - blurSize + 1;
            _outputHeight = inputHeight - blurSize + 1;
            _blurSize = blurSize;
        }

        public void Process(ReadOnlySpan<byte> inputBgraData, Span<byte> outputBgraData)
        {
            // 验证输入输出数据长度
            if (inputBgraData.Length != _inputWidth * _inputHeight * 4)
                throw new ArgumentException("Input data length does not match expected size", nameof(inputBgraData));

            if (outputBgraData.Length != _outputWidth * _outputHeight * 4)
                throw new ArgumentException("Output data length does not match expected size", nameof(outputBgraData));

            int kernelSize = _blurSize * _blurSize;
            int halfBlur = _blurSize / 2;

            for (int y = 0; y < _outputHeight; y++)
            {
                for (int x = 0; x < _outputWidth; x++)
                {
                    int sumB = 0, sumG = 0, sumR = 0, sumA = 0;

                    // 遍历模糊区域
                    for (int ky = 0; ky < _blurSize; ky++)
                    {
                        for (int kx = 0; kx < _blurSize; kx++)
                        {
                            int srcX = x + kx;
                            int srcY = y + ky;
                            int srcIndex = (srcY * _inputWidth + srcX) * 4;

                            sumB += inputBgraData[srcIndex];
                            sumG += inputBgraData[srcIndex + 1];
                            sumR += inputBgraData[srcIndex + 2];
                            sumA += inputBgraData[srcIndex + 3];
                        }
                    }

                    // 计算平均值
                    int dstIndex = (y * _outputWidth + x) * 4;
                    outputBgraData[dstIndex] = (byte)(sumB / kernelSize);
                    outputBgraData[dstIndex + 1] = (byte)(sumG / kernelSize);
                    outputBgraData[dstIndex + 2] = (byte)(sumR / kernelSize);
                    outputBgraData[dstIndex + 3] = (byte)(sumA / kernelSize);
                }
            }
        }
    }
}
