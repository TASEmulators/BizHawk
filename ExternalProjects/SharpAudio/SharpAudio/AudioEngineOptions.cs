namespace SharpAudio
{
    public class AudioEngineOptions
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

        /// <summary>
        /// The device name to use. null == default
        /// </summary>
        public string DeviceName;

        public AudioEngineOptions() : this(44100, 2, null)
        {

        }

        public AudioEngineOptions(int sampleRate, int sampleChannels, string deviceName)
        {
            SampleRate = sampleRate;
            SampleChannels = sampleChannels;
            DeviceName = deviceName;
        }
    }
}
