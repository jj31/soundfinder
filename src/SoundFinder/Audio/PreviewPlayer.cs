using NAudio.Wave;

namespace SoundFinder.Audio;

public sealed class PreviewPlayer : IDisposable
{
    private const int FftSize = 2048;
    private WaveOutEvent? _output;
    private AudioFileReader? _reader;
    private TeeSampleProvider? _tee;
    private readonly object _sync = new();

    public bool IsPlaying { get; private set; }
    public bool Loop { get; set; }
    public TimeSpan Position => _reader?.CurrentTime ?? TimeSpan.Zero;
    public TimeSpan Duration => _reader?.TotalTime ?? TimeSpan.Zero;
    public int SampleRate => _reader?.WaveFormat.SampleRate ?? 44100;

    public event EventHandler? PlaybackStopped;

    public void Play(string path)
    {
        Stop();
        lock (_sync)
        {
            _reader = new AudioFileReader(path);
            _tee = new TeeSampleProvider(_reader, FftSize * 4);
            if (Loop)
            {
                _tee.Loop = true;
            }
            _output = new WaveOutEvent();
            _output.PlaybackStopped += OnPlaybackStopped;
            _output.Init(_tee);
            _output.Play();
            IsPlaying = true;
        }
    }

    public void Stop()
    {
        lock (_sync)
        {
            if (_output is not null)
            {
                _output.PlaybackStopped -= OnPlaybackStopped;
                _output.Stop();
                _output.Dispose();
                _output = null;
            }
            _reader?.Dispose();
            _reader = null;
            _tee = null;
            IsPlaying = false;
        }
    }

    public void SetLoop(bool loop)
    {
        Loop = loop;
        lock (_sync)
        {
            if (_tee is not null)
            {
                _tee.Loop = loop;
            }
        }
    }

    public float[] ReadLatestSamples(int count)
    {
        lock (_sync)
        {
            return _tee?.ReadLatest(count) ?? Array.Empty<float>();
        }
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        Stop();
        PlaybackStopped?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose() => Stop();

    private sealed class TeeSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private readonly float[] _ring;
        private int _writeIndex;
        private int _available;
        private bool _loop;

        public TeeSampleProvider(ISampleProvider source, int ringSize)
        {
            _source = source;
            _ring = new float[ringSize];
        }

        public bool Loop
        {
            get => _loop;
            set => _loop = value;
        }

        public WaveFormat WaveFormat => _source.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            var read = _source.Read(buffer, offset, count);
            if (read > 0)
            {
                for (var i = 0; i < read; i++)
                {
                    _ring[_writeIndex] = buffer[offset + i];
                    _writeIndex = (_writeIndex + 1) % _ring.Length;
                }
                _available = Math.Min(_available + read, _ring.Length);
            }
            if (read < count && _loop)
            {
                if (_source is AudioFileReader reader)
                {
                    reader.Position = 0;
                }
                var more = Read(buffer, offset + read, count - read);
                return read + more;
            }
            return read;
        }

        public float[] ReadLatest(int count)
        {
            count = Math.Min(count, _available);
            var result = new float[count];
            var start = (_writeIndex - count + _ring.Length) % _ring.Length;
            for (var i = 0; i < count; i++)
            {
                result[i] = _ring[(start + i) % _ring.Length];
            }
            return result;
        }
    }
}
