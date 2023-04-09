using System.Numerics;

namespace SharpAudio
{
    /// <summary>
    /// Represents an abstract 3d audio engine
    /// </summary>
    public abstract class Audio3DEngine
    {
        /// <summary>
        /// Set the position of the listener (usually the camera/player)
        /// </summary>
        /// <param name="position">The position in worldspace</param>
        public abstract void SetListenerPosition(Vector3 position);
        /// <summary>
        /// Set the orientation of the listener (usually the camera/player)
        /// </summary>
        /// <param name="top">The up vector of the camera/player</param>
        /// <param name="front">The front vector of the camera/player</param>
        public abstract void SetListenerOrientation(Vector3 top, Vector3 front);
        /// <summary>
        /// Set the position of a specific sound emitter/source
        /// </summary>
        /// <param name="source">The source we're setting the position for</param>
        /// <param name="position">The position of the source</param>
        public abstract void SetSourcePosition(AudioSource source, Vector3 position);
    }
}
