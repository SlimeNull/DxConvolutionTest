namespace DxConvolutionTest
{
    public static class GaussianBlur
    {
        public static float[] GetWeights(int size)
        {
            if (size < 1)
                throw new ArgumentOutOfRangeException(nameof(size), "Size must be at least 1");

            // 如果 size=1，直接返回 [1]（不模糊）
            if (size == 1)
                return new float[] { 1f };

            // 计算半径（如 size=5 → radius=2）
            int radius = size / 2;
            float sigma = radius / 3f; // 经验值：sigma ≈ radius / 3

            float[] weights = new float[size];
            float sum = 0f;

            // 计算高斯权重
            for (int i = 0; i < size; i++)
            {
                int x = i - radius; // x ∈ [-radius, radius]
                float g = (float)(Math.Exp(-(x * x) / (2 * sigma * sigma)) / (sigma * Math.Sqrt(2 * Math.PI)));
                weights[i] = g;
                sum += g;
            }

            // 归一化，使总和=1
            for (int i = 0; i < size; i++)
            {
                weights[i] /= sum;
            }

            return weights;
        }
    }
}
