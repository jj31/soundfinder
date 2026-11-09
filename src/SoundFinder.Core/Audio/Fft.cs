namespace SoundFinder.Core.Audio;

public static class Fft
{
    public static double[] HannWindow(int size)
    {
        var window = new double[size];
        for (var i = 0; i < size; i++)
        {
            window[i] = 0.5 * (1.0 - Math.Cos(2.0 * Math.PI * i / (size - 1)));
        }
        return window;
    }

    public static void ComputeRadix2(double[] real, double[] imaginary)
    {
        var n = real.Length;
        if (n != imaginary.Length || (n & (n - 1)) != 0 || n == 0)
        {
            throw new ArgumentException("Array length must be a power of two.");
        }

        var levels = (int)Math.Log2(n);
        for (var i = 0; i < n; i++)
        {
            var j = ReverseBits(i, levels);
            if (j > i)
            {
                (real[i], real[j]) = (real[j], real[i]);
                (imaginary[i], imaginary[j]) = (imaginary[j], imaginary[i]);
            }
        }

        for (var size = 2; size <= n; size *= 2)
        {
            var half = size / 2;
            var angle = -2.0 * Math.PI / size;
            var wReal = Math.Cos(angle);
            var wImag = Math.Sin(angle);
            for (var start = 0; start < n; start += size)
            {
                double curReal = 1.0;
                double curImag = 0.0;
                for (var k = 0; k < half; k++)
                {
                    var evenIndex = start + k;
                    var oddIndex = start + k + half;
                    var tReal = curReal * real[oddIndex] - curImag * imaginary[oddIndex];
                    var tImag = curReal * imaginary[oddIndex] + curImag * real[oddIndex];
                    real[oddIndex] = real[evenIndex] - tReal;
                    imaginary[oddIndex] = imaginary[evenIndex] - tImag;
                    real[evenIndex] += tReal;
                    imaginary[evenIndex] += tImag;
                    var nextReal = curReal * wReal - curImag * wImag;
                    curImag = curReal * wImag + curImag * wReal;
                    curReal = nextReal;
                }
            }
        }
    }

    public static double[] Magnitudes(float[] samples, int fftSize)
    {
        if (fftSize <= 0 || (fftSize & (fftSize - 1)) != 0)
        {
            throw new ArgumentException("fftSize must be a power of two.");
        }
        var real = new double[fftSize];
        var imaginary = new double[fftSize];
        var window = HannWindow(fftSize);
        var count = Math.Min(samples.Length, fftSize);
        var offset = samples.Length > fftSize ? (samples.Length - fftSize) / 2 : 0;
        for (var i = 0; i < count; i++)
        {
            real[i] = samples[offset + i] * window[i];
        }
        ComputeRadix2(real, imaginary);
        var magnitudes = new double[fftSize / 2];
        var scale = 2.0 / fftSize;
        for (var i = 0; i < magnitudes.Length; i++)
        {
            magnitudes[i] = Math.Sqrt(real[i] * real[i] + imaginary[i] * imaginary[i]) * scale;
        }
        return magnitudes;
    }

    private static int ReverseBits(int value, int levels)
    {
        var result = 0;
        for (var i = 0; i < levels; i++)
        {
            result = (result << 1) | (value & 1);
            value >>= 1;
        }
        return result;
    }
}
