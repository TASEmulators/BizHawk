namespace SharpAudio
{
    /// <summary>
    /// Describes the format of a set of samples
    /// </summary>
    public struct AudioFormat
    {
        /// <summary>
        /// The sample rate that is used for most input sounds. Will downsample to the actual supported rate. 
        /// </summary>
        public int SampleRate;

        /// <summary>
        /// The number of channels of the input
        /// </summary>
        public int Channels;

        /// <summary>
        /// Bits per sample. Can either be 8 or 16
        /// </summary>
        public int BitsPerSample;

        /// <summary>
        /// Gives the number of bytes per sample
        /// </summary>
        public int BytesPerSample => BitsPerSample / 8;

        /// <summary>
        /// Gives the number of bytes that is processed per second
        /// </summary>
        public int BytesPerSecond => BytesPerSample * SampleRate * Channels;
    }
}
