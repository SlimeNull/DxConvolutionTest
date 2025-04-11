namespace DxConvolutionTest
{
    public class CpuAvgBlurOptimized : IImageBlurProcessor
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

        public CpuAvgBlurOptimized(int inputWidth, int inputHeight, int blurSize)
        {
            if (blurSize < 1)
                throw new ArgumentOutOfRangeException(nameof(blurSize), "Blur size must be at least 1");

            _inputWidth = inputWidth;
            _inputHeight = inputHeight;
            _blurSize = blurSize;
            _outputWidth = inputWidth - blurSize + 1;
            _outputHeight = inputHeight - blurSize + 1;
        }

        public void Process(ReadOnlySpan<byte> inputBgraData, Span<byte> outputBgraData)
        {
            if (inputBgraData.Length != _inputWidth * _inputHeight * 4)
                throw new ArgumentException("Input data length does not match expected size", nameof(inputBgraData));

            if (outputBgraData.Length != _outputWidth * _outputHeight * 4)
                throw new ArgumentException("Output data length does not match expected size", nameof(outputBgraData));

            // 计算积分图（前缀和）
            var integralB = new int[_inputWidth * _inputHeight];
            var integralG = new int[_inputWidth * _inputHeight];
            var integralR = new int[_inputWidth * _inputHeight];
            var integralA = new int[_inputWidth * _inputHeight];

            // 计算行前缀和
            for (int y = 0; y < _inputHeight; y++)
            {
                int rowOffset = y * _inputWidth;
                int bSum = 0, gSum = 0, rSum = 0, aSum = 0;

                for (int x = 0; x < _inputWidth; x++)
                {
                    int srcIndex = (rowOffset + x) * 4;
                    bSum += inputBgraData[srcIndex];
                    gSum += inputBgraData[srcIndex + 1];
                    rSum += inputBgraData[srcIndex + 2];
                    aSum += inputBgraData[srcIndex + 3];

                    integralB[rowOffset + x] = bSum;
                    integralG[rowOffset + x] = gSum;
                    integralR[rowOffset + x] = rSum;
                    integralA[rowOffset + x] = aSum;
                }
            }

            // 计算列前缀和（最终得到积分图）
            for (int x = 0; x < _inputWidth; x++)
            {
                for (int y = 1; y < _inputHeight; y++)
                {
                    int currentIdx = y * _inputWidth + x;
                    int prevIdx = (y - 1) * _inputWidth + x;

                    integralB[currentIdx] += integralB[prevIdx];
                    integralG[currentIdx] += integralG[prevIdx];
                    integralR[currentIdx] += integralR[prevIdx];
                    integralA[currentIdx] += integralA[prevIdx];
                }
            }

            // 计算均值模糊
            int kernelSize = _blurSize * _blurSize;
            for (int y = 0; y < _outputHeight; y++)
            {
                for (int x = 0; x < _outputWidth; x++)
                {
                    // 计算窗口的四个角
                    int x1 = x;
                    int y1 = y;
                    int x2 = x + _blurSize - 1;
                    int y2 = y + _blurSize - 1;

                    // 获取四个角的积分值
                    int bSum = GetIntegralValue(integralB, x1, y1, x2, y2);
                    int gSum = GetIntegralValue(integralG, x1, y1, x2, y2);
                    int rSum = GetIntegralValue(integralR, x1, y1, x2, y2);
                    int aSum = GetIntegralValue(integralA, x1, y1, x2, y2);

                    // 计算均值并写入输出
                    int dstIndex = (y * _outputWidth + x) * 4;
                    outputBgraData[dstIndex] = (byte)(bSum / kernelSize);
                    outputBgraData[dstIndex + 1] = (byte)(gSum / kernelSize);
                    outputBgraData[dstIndex + 2] = (byte)(rSum / kernelSize);
                    outputBgraData[dstIndex + 3] = (byte)(aSum / kernelSize);
                }
            }
        }

        private int GetIntegralValue(int[] integral, int x1, int y1, int x2, int y2)
        {
            int a = (y1 > 0 && x1 > 0) ? integral[(y1 - 1) * _inputWidth + (x1 - 1)] : 0;
            int b = (y1 > 0) ? integral[(y1 - 1) * _inputWidth + x2] : 0;
            int c = (x1 > 0) ? integral[y2 * _inputWidth + (x1 - 1)] : 0;
            int d = integral[y2 * _inputWidth + x2];

            return d - b - c + a;
        }
    }
}
