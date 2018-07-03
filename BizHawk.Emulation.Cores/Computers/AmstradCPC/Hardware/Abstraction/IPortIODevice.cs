
namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
    /// <summary>
    /// Represents a device that utilizes port IN & OUT
    /// </summary>
    public interface IPortIODevice
    {
        /// <summary>
        /// Device responds to an IN instruction
        /// </summary>
        /// <param name="port"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        bool ReadPort(ushort port, ref int result);

        /// <summary>
        /// Device responds to an OUT instruction
        /// </summary>
        /// <param name="port"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        bool WritePort(ushort port, int result);
    }
}
