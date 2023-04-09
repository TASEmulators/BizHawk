using System;
using System.Threading;

namespace SharpAudio.Codec
{
    public sealed class SoundSink : IDisposable
    {
        private static readonly TimeSpan SampleQuantum = TimeSpan.FromSeconds(0.05);
        private readonly BufferChain _chain;
        private readonly CircularBuffer _circBuffer;
        private readonly AudioFormat _format;
        private readonly byte[] _silenceData;
        private readonly ISoundSinkReceiver _receiver;
        private readonly byte[] _tempBuf;
        private volatile bool _isDisposed;
        private Thread _sinkThread;
        private Submixer _submixer;

        public SoundSink(AudioEngine audioEngine, Submixer submixer = null, ISoundSinkReceiver receiver = null)
        {
            _format = new AudioFormat { SampleRate = 44_100, Channels = 2, BitsPerSample = 16 };

            var silenceDataCount = (int) (_format.Channels * _format.SampleRate * sizeof(ushort) * SampleQuantum.TotalSeconds);

            _silenceData = new byte[silenceDataCount];

            Engine = audioEngine;
            _receiver = receiver;
            _chain = new BufferChain(Engine);
            _circBuffer = new CircularBuffer(_silenceData.Length);
            _tempBuf = new byte[_silenceData.Length];
            _submixer = submixer;
            _sinkThread = new Thread(MainLoop);
            _sinkThread.Name = "SoundSink";
            _sinkThread.Start();
        }

        public AudioEngine Engine { get; }

        public AudioSource Source { get; private set; }

        public bool NeedsNewSample => _circBuffer.Length < _silenceData.Length;

        public void Dispose()
        {
            _isDisposed = true;
            _sinkThread.Join();
        }

        private void InitializeSource()
        {
            Source?.Dispose();
            Source = Engine.CreateSource(_submixer);
            _chain.QueueData(Source, _silenceData, _format);
            Source.Play();
        }

        private void MainLoop()
        {
            InitializeSource();

            while (!_isDisposed)
            {
                if (Source is null || !Source.IsPlaying())
                {
                    Thread.Sleep(TimeSpan.FromSeconds(0.5));
                    InitializeSource();
                    continue;
                }

                if (Source.BuffersQueued >= 3)
                {
                    continue;
                }

                var cL = _circBuffer.Length;
                var tL = _tempBuf.Length;

                if (cL >= tL)
                {
                    _circBuffer.Read(_tempBuf, 0, _tempBuf.Length);
                    _chain.QueueData(Source, _tempBuf, _format);
                    _receiver?.Receive(_tempBuf);
                }
                else if ((cL < tL) & (cL > 0))
                {
                    var remainingSamples = new byte[cL];
                    _circBuffer.Read(remainingSamples, 0, remainingSamples.Length);

                    Buffer.BlockCopy(remainingSamples, 0, _tempBuf, 0, remainingSamples.Length);
                    _chain.QueueData(Source, _tempBuf, _format);
                    _receiver?.Receive(_tempBuf);
                }
                else
                {
                    _chain.QueueData(Source, _silenceData, _format);
                }
            }

            Source?.Stop();
            Source?.Dispose();
            _receiver?.Dispose();
        }

        public void Send(byte[] data)
        {
            _circBuffer.Write(data, 0, data.Length);
        }

        internal void ClearBuffers()
        {
            _circBuffer.Clear();
        }
    }
}
