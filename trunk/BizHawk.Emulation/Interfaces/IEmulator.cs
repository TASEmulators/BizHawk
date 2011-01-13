using System.IO;

namespace BizHawk
{
    public interface IEmulator
    {
        IVideoProvider VideoProvider { get; }
        ISoundProvider SoundProvider { get; }

        ControllerDefinition ControllerDefinition { get; }
        IController Controller { get; set; }

        void LoadGame(IGame game);
        void FrameAdvance(bool render);
        void HardReset();
        
        int Frame { get; }
        bool DeterministicEmulation { get; set; }

        byte[] SaveRam { get; }
        bool SaveRamModified { get; set; }

        // TODO: should IEmulator expose a way of enumerating the Options it understands?
        // (the answer is yes)

        void SaveStateText(TextWriter writer);
        void LoadStateText(TextReader reader);
        void SaveStateBinary(BinaryWriter writer);
        void LoadStateBinary(BinaryReader reader);
        byte[] SaveStateBinary();
    }

    public enum DisplayType { NTSC, PAL }
}
