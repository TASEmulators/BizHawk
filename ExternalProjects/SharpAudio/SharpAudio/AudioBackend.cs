namespace SharpAudio
{
    /// <summary>
    /// The specific graphics API used by the <see cref="AudioEngine"/> or <see cref="AudioCapture"/>.
    /// </summary>
    public enum AudioBackend
    {
        /// <summary>
        /// XAudio2
        /// </summary>
        XAudio2,
        /// <summary>
        /// MediaFoundation
        /// </summary>
        MediaFoundation,
        /// <summary>
        /// OpenAL
        /// </summary>
        OpenAL,
    }
}
