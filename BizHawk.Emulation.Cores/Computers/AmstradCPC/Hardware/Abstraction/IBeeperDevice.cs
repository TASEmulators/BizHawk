using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
    /// <summary>
    /// Represents a beeper/buzzer device
    /// </summary>
    public interface IBeeperDevice
    {
        /// <summary>
        /// Initialisation
        /// </summary>
        /// <param name="sampleRate"></param>
        /// <param name="tStatesPerFrame"></param>
        void Init(int sampleRate, int tStatesPerFrame);

        /// <summary>
        /// Processes an incoming pulse value and adds it to the blipbuffer
        /// </summary>
        /// <param name="pulse"></param>
        void ProcessPulseValue(bool pulse);

        /// <summary>
        /// State serialization
        /// </summary>
        /// <param name="ser"></param>
        void SyncState(Serializer ser);
    }
}
