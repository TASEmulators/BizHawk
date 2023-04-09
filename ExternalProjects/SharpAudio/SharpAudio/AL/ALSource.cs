using SharpAudio.ALBinding;

namespace SharpAudio.AL
{
    internal sealed class ALSource : AudioSource
    {
        internal uint _source;

        public override int BuffersQueued
        {
            get
            {
                RemoveProcessed();

                AlNative.alGetSourcei(_source, AlNative.AL_BUFFERS_QUEUED, out int bufs);
                ALEngine.checkAlError();
                return bufs;
            }
        }

        public override int SamplesPlayed
        {
	        get
	        {
		        RemoveProcessed();

		        AlNative.alGetSourcei(_source, AlNative.AL_SAMPLE_OFFSET, out int bufs);
		        ALEngine.checkAlError();
		        return bufs;
	        }
        }

        public override float Volume
        {
            get { return _volume; }
            set
            {
                _volume = value; AlNative.alSourcef(_source, AlNative.AL_GAIN, value);
                ALEngine.checkAlError();
            }
        }

        public override bool Looping
        {
            get { return _looping; }
            set
            {
                _looping = value; AlNative.alSourcei(_source, AlNative.AL_LOOPING, value ? 1 : 0);
                ALEngine.checkAlError();
            }
        }

        public ALSource()
        {
            var sources = new uint[1];
            AlNative.alGenSources(1, sources);
            ALEngine.checkAlError();
            _source = sources[0];
        }

        public override void Dispose()
        {
            AlNative.alDeleteSources(1, new uint[] { _source });
            ALEngine.checkAlError();
        }

        public override void Flush()
        {
            //TODO: not sure if we should unquery buffers here. Investigate
        }

        public override bool IsPlaying()
        {
            AlNative.alGetSourcei(_source, AlNative.AL_SOURCE_STATE, out int state);
            ALEngine.checkAlError();
            bool playing = state == AlNative.AL_PLAYING;

            return playing;
        }

        public override void Play()
        {
            AlNative.alSourcePlay(_source);
            ALEngine.checkAlError();
        }

        private void RemoveProcessed()
        {
            //before querying new data check if sth was processed already:
            AlNative.alGetSourcei(_source, AlNative.AL_BUFFERS_PROCESSED, out int processed);
            ALEngine.checkAlError();

            while (processed > 0)
            {
                var bufs = new uint[] { 1 };
                AlNative.alSourceUnqueueBuffers(_source, 1, bufs);
                ALEngine.checkAlError();
                processed--;
            }
        }

        public override void QueueBuffer(AudioBuffer buffer)
        {
            RemoveProcessed();

            var alBuffer = (ALBuffer) buffer;
            AlNative.alSourceQueueBuffers(_source, 1, new uint[] { alBuffer.Buffer });
            ALEngine.checkAlError();
        }

        public override void Stop()
        {
            AlNative.alSourceStop(_source);
            ALEngine.checkAlError();
        }
    }
}
