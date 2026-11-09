namespace SoundFinder.Core.Audio;

public static class WaveformPeaks
{
    public static float[] ToMono(float[] interleaved, int channels)
    {
        if (channels <= 1)
        {
            return interleaved;
        }
        var mono = new float[interleaved.Length / channels];
        for (var i = 0; i < mono.Length; i++)
        {
            float sum = 0f;
            for (var c = 0; c < channels; c++)
            {
                sum += interleaved[i * channels + c];
            }
            mono[i] = sum / channels;
        }
        return mono;
    }

    public static float[] ComputeMinMax(float[] mono, int peakCount)
    {
        if (peakCount <= 0)
        {
            return Array.Empty<float>();
        }
        var result = new float[peakCount * 2];
        if (mono.Length == 0)
        {
            return result;
        }
        var bucketSize = Math.Max(1, mono.Length / peakCount);
        for (var i = 0; i < peakCount; i++)
        {
            var start = i * bucketSize;
            var end = Math.Min(start + bucketSize, mono.Length);
            if (start >= mono.Length)
            {
                result[i * 2] = 0f;
                result[i * 2 + 1] = 0f;
                continue;
            }
            var min = float.MaxValue;
            var max = float.MinValue;
            for (var j = start; j < end; j++)
            {
                var value = mono[j];
                if (value < min)
                {
                    min = value;
                }
                if (value > max)
                {
                    max = value;
                }
            }
            result[i * 2] = min;
            result[i * 2 + 1] = max;
        }
        return result;
    }

    public static float[] Downsample(float[] mono, int targetCount)
    {
        if (targetCount <= 0 || mono.Length == 0)
        {
            return Array.Empty<float>();
        }
        if (mono.Length <= targetCount)
        {
            return mono;
        }
        var result = new float[targetCount];
        var bucketSize = (double)mono.Length / targetCount;
        for (var i = 0; i < targetCount; i++)
        {
            var start = (int)(i * bucketSize);
            var end = Math.Min((int)((i + 1) * bucketSize), mono.Length);
            float sum = 0f;
            var count = 0;
            for (var j = start; j < end; j++)
            {
                sum += mono[j];
                count++;
            }
            result[i] = count > 0 ? sum / count : 0f;
        }
        return result;
    }
}
