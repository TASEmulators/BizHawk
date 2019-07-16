
namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// Represents the possible commands that can be raised from each tape block
    /// </summary>
    public enum TapeCommand
    {
        NONE,
        STOP_THE_TAPE,
        STOP_THE_TAPE_48K,
        BEGIN_GROUP,
        END_GROUP,
        SHOW_MESSAGE,
    }
}
