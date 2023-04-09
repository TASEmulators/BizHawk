namespace SharpAudio
{
    public class AudioCaptureOptions
    {
        /// <summary>
        /// The sample rate to which all sources will be resampled to (if required). 
        /// Then coverts to the maximum supported sample rate by the device.
        /// </summary>
        public int SampleRate;

        /// <summary>
        /// The number of channels that this device is sammpling
        /// </summary>
        public int SampleChannels;

        public AudioCaptureOptions() : this(44100, 2)
        {

        }

        public AudioCaptureOptions(int sampleRate, int sampleChannels)
        {
            SampleRate = sampleRate;
            SampleChannels = sampleChannels;
        }
    }
}
