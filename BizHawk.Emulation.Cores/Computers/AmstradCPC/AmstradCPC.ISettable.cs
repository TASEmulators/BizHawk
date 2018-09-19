using BizHawk.Common;
using BizHawk.Emulation.Common;
using System.ComponentModel;
using System.Text;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
    /// <summary>
    /// CPCHawk: Core Class
    /// * ISettable *
    /// </summary>
    public partial class AmstradCPC : ISettable<AmstradCPC.AmstradCPCSettings, AmstradCPC.AmstradCPCSyncSettings>
    {
        internal AmstradCPCSettings Settings = new AmstradCPCSettings();
        internal AmstradCPCSyncSettings SyncSettings = new AmstradCPCSyncSettings();

        public AmstradCPCSettings GetSettings()
        {
            return Settings.Clone();
        }

        public AmstradCPCSyncSettings GetSyncSettings()
        {
            return SyncSettings.Clone();
        }

        public bool PutSettings(AmstradCPCSettings o)
        {
            
            // restore user settings to devices
            if (_machine != null && _machine.AYDevice != null)
            {
                ((AY38912)_machine.AYDevice as AY38912).PanningConfiguration = o.AYPanConfig;
                _machine.AYDevice.Volume = o.AYVolume;
            }
            if (_machine != null && _machine.TapeBuzzer != null)
            {
                ((Beeper)_machine.TapeBuzzer as Beeper).Volume = o.TapeVolume;
            }
            

            Settings = o;

            return false;
        }

        public bool PutSyncSettings(AmstradCPCSyncSettings o)
        {
            bool ret = AmstradCPCSyncSettings.NeedsReboot(SyncSettings, o);
            SyncSettings = o;
            return ret;
        }

        public class AmstradCPCSettings 
        {
            [DisplayName("AY-3-8912 Panning Config")]
            [Description("Set the PSG panning configuration.\nThe chip has 3 audio channels that can be outputed in different configurations")]
            [DefaultValue(AY38912.AYPanConfig.ABC)]
            public AY38912.AYPanConfig AYPanConfig { get; set; }

            [DisplayName("AY-3-8912 Volume")]
            [Description("The AY chip volume")]
            [DefaultValue(75)]
            public int AYVolume { get; set; }

            [DisplayName("Core OSD Message Verbosity")]
            [Description("Full: Display all GUI messages\nMedium: Display only emulator/device generated messages\nNone: Show no messages")]
            [DefaultValue(OSDVerbosity.Medium)]
            public OSDVerbosity OSDMessageVerbosity { get; set; }

            [DisplayName("Tape Loading Volume")]
            [Description("The buzzer volume when the tape is playing")]
            [DefaultValue(50)]
            public int TapeVolume { get; set; }

            public AmstradCPCSettings Clone()
            {
                return (AmstradCPCSettings)MemberwiseClone();
            }

            public AmstradCPCSettings()
            {
                BizHawk.Common.SettingsUtil.SetDefaultValues(this);
            }
        }

        public class AmstradCPCSyncSettings
        {
            [DisplayName("Deterministic Emulation")]
            [Description("If true, the core agrees to behave in a completely deterministic manner")]
            [DefaultValue(true)]
            public bool DeterministicEmulation { get; set; }

            [DisplayName("CPC Model")]
            [Description("The model of Amstrad CPC machine to be emulated")]
            [DefaultValue(MachineType.CPC464)]
            public MachineType MachineType { get; set; }

            [DisplayName("Auto Start/Stop Tape")]
            [Description("If true, CPCHawk will automatically start and stop the tape when the tape motor is triggered")]
            [DefaultValue(true)]
            public bool AutoStartStopTape { get; set; }

            [DisplayName("Border type")]
            [Description("Select how to show the border area")]
            [DefaultValue(BorderType.Uniform)]
            public BorderType BorderType { get; set; }

            public AmstradCPCSyncSettings Clone()
            {
                return (AmstradCPCSyncSettings)MemberwiseClone();
            }

            public AmstradCPCSyncSettings()
            {
                BizHawk.Common.SettingsUtil.SetDefaultValues(this);
            }

            public static bool NeedsReboot(AmstradCPCSyncSettings x, AmstradCPCSyncSettings y)
            {
                return !DeepEquality.DeepEquals(x, y);
            }
        }

        /// <summary>
        /// Verbosity of the CPCHawk generated OSD messages
        /// </summary>
        public enum OSDVerbosity
        {
            /// <summary>
            /// Show all OSD messages
            /// </summary>
            Full,
            /// <summary>
            /// Only show machine/device generated messages
            /// </summary>
            Medium,
            /// <summary>
            /// No core-driven OSD messages
            /// </summary>
            None
        }

        /// <summary>
        /// Provides information on each emulated machine
        /// </summary>
        public class CPCMachineMetaData
        {
            public MachineType MachineType { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string Released { get; set; }
            public string CPU { get; set; }
            public string Memory { get; set; }
            public string Video { get; set; }
            public string Audio { get; set; }
            public string Media { get; set; }
            public string OtherMisc { get; set; }


            public static CPCMachineMetaData GetMetaObject(MachineType type)
            {
                CPCMachineMetaData m = new CPCMachineMetaData();
                m.MachineType = type;

                switch (type)
                {
                    case MachineType.CPC464:
                        m.Name = "Amstrad CPC 464";
                        m.Description = "The CPC 464 was the first personal home computer built by Amstrad in 1984. ";
                        m.Description += "The 464 was popular with consumers for various reasons. Aside from the joystick port, the computer, keyboard, and tape deck were all combined into one unit.";
                        m.Released = "1984";
                        m.CPU = "Zilog Z80A @ 4MHz";
                        m.Memory = "64KB RAM / 32KB ROM";
                        m.Video = "Amstrad Gate Array @ 16Mhz & CRCT @ 1Mhz";
                        m.Audio = "General Instruments AY-3-8912 PSG (3ch)";
                        m.Media = "Cassette Tape (via built-in Datacorder)";
                        break;
                    case MachineType.CPC6128:
                        m.Name = "Amstrad CPC 6128";
                        m.Description = "The CPC6128 features 128 KB RAM and an internal 3-inch floppy disk drive. ";
                        m.Description += "Aside from various hardware and firmware improvements, one of the CPC6128's most prominent features is the compatibility with the CP/M+ operating system that rendered it attractive for business uses.";
                        m.Released = "1985";
                        m.CPU = "Zilog Z80A @ 4MHz";
                        m.Memory = "64KB RAM / 32KB ROM";
                        m.Video = "Amstrad Gate Array @ 16Mhz & CRCT @ 1Mhz";
                        m.Audio = "General Instruments AY-3-8912 PSG (3ch)";
                        m.Media = "3\" Floppy Disk (via built-in Floppy Drive) & Cassette Tape (via external cassette player)";
                        break;
                        /*
                    case MachineType.ZXSpectrum48:
                        m.Name = "Sinclair ZX Spectrum 48K / 48K+";
                        m.Description = "The original ZX Spectrum 48K RAM version. 2 years later a 'plus' version was released that had a better keyboard. ";
                        m.Description += "Electronically both the 48K and + are identical, so ZXHawk treats them as the same emulated machine. ";
                        m.Description += "These machines dominated the UK 8-bit home computer market throughout the 1980's so most non-128k only games are compatible.";
                        m.Released = "1982 (48K) / 1984 (48K+)";
                        m.CPU = "Zilog Z80A @ 3.5MHz";
                        m.Memory = "16KB ROM / 48KB RAM";
                        m.Video = "ULA @ 7MHz - PAL (50.08Hz Interrupt)";
                        m.Audio = "Beeper (HW 1ch. / 10oct.) - Internal Speaker";
                        m.Media = "Cassette Tape (via 3rd party external tape player)";
                        break;
                    case MachineType.ZXSpectrum128:
                        m.Name = "Sinclair ZX Spectrum 128";
                        m.Description = "The first Spectrum 128K machine released in Spain in 1985 and later UK in 1986. ";
                        m.Description += "With an updated ROM and new memory paging system to work around the Z80's 16-bit address bus. ";
                        m.Description += "The 128 shipped with a copy of the 48k ROM (that is paged in when required) and a new startup menu with the option of dropping into a '48k mode'. ";
                        m.Description += "Even so, there were some compatibility issues with older Spectrum games that were written to utilise some of the previous model's intricacies. ";
                        m.Description += "Many games released after 1985 supported the new AY-3-8912 PSG chip making for far superior audio. The extra memory also enabled many games to be loaded in all at once (rather than loading each level from tape when needed).";
                        m.Released = "1985 / 1986";
                        m.CPU = "Zilog Z80A @ 3.5469 MHz";
                        m.Memory = "32KB ROM / 128KB RAM";
                        m.Video = "ULA @ 7.0938MHz - PAL (50.01Hz Interrupt)";
                        m.Audio = "Beeper (HW 1ch. / 10oct.) & General Instruments AY-3-8912 PSG (3ch) - RF Output";
                        m.Media = "Cassette Tape (via 3rd party external tape player)";
                        break;
                    case MachineType.ZXSpectrum128Plus2:
                        m.Name = "Sinclair ZX Spectrum +2";
                        m.Description = "The first Sinclair Spectrum 128K machine that was released after Amstrad purchased Sinclair in 1986. ";
                        m.Description += "Electronically it was almost identical to the 128, but with the addition of a built-in tape deck and 2 Sinclair Joystick ports.";
                        m.Released = "1986";
                        m.CPU = "Zilog Z80A @ 3.5469 MHz";
                        m.Memory = "32KB ROM / 128KB RAM";
                        m.Video = "ULA @ 7.0938MHz - PAL (50.01Hz Interrupt)";
                        m.Audio = "Beeper (HW 1ch. / 10oct.) & General Instruments AY-3-8912 PSG (3ch) - RF Output";
                        m.Media = "Cassette Tape (via built-in Datacorder)";
                        break;
                    case MachineType.ZXSpectrum128Plus2a:
                        m.Name = "Sinclair ZX Spectrum +2a";
                        m.Description = "The +2a looks almost identical to the +2 but is a variant of the +3 machine that was released the same year (except with the same built-in datacorder that the +2 had rather than a floppy drive). ";
                        m.Description += "Memory paging again changed significantly and this (along with memory contention timing changes) caused more compatibility issues with some older games. ";
                        m.Description += "Although functionally identical to the +3, it does not contain floppy disk controller.";
                        m.Released = "1987";
                        m.CPU = "Zilog Z80A @ 3.5469 MHz";
                        m.Memory = "64KB ROM / 128KB RAM";
                        m.Video = "ULA @ 7.0938MHz - PAL (50.01Hz Interrupt)";
                        m.Audio = "Beeper (HW 1ch. / 10oct.) & General Instruments AY-3-8912 PSG (3ch) - RF Output";
                        m.Media = "Cassette Tape (via built-in Datacorder)";
                        break;
                    case MachineType.ZXSpectrum128Plus3:
                        m.Name = "Sinclair ZX Spectrum +3";
                        m.Description = "Amstrad released the +3 the same year as the +2a, but it featured a built-in floppy drive rather than a datacorder. An external cassette player could still be connected though as in the older 48k models. ";
                        m.Description += "Memory paging again changed significantly and this (along with memory contention timing changes) caused more compatibility issues with some older games. ";
                        m.Description += "Currently ZXHawk does not emulate the floppy drive or floppy controller so the machine reports as a +2a on boot.";
                        m.Released = "1987";
                        m.CPU = "Zilog Z80A @ 3.5469 MHz";
                        m.Memory = "64KB ROM / 128KB RAM";
                        m.Video = "ULA @ 7.0938MHz - PAL (50.01Hz Interrupt)";
                        m.Audio = "Beeper (HW 1ch. / 10oct.) & General Instruments AY-3-8912 PSG (3ch) - RF Output";
                        m.Media = "3\" Floppy Disk (via built-in Floppy Drive)";
                        break;
                        */
                }
                return m;
            }

            public static string GetMetaString(MachineType type)
            {
                var m = GetMetaObject(type);

                StringBuilder sb = new StringBuilder();

                sb.Append(m.Name);
                sb.Append("\n");
                sb.Append("-----------------------------------------------------------------\n");
                // Release
                sb.Append("Released:");
                sb.Append(" ");
                sb.Append(m.Released);
                sb.Append("\n");
                // CPU
                sb.Append("CPU:");
                sb.Append("      ");
                sb.Append(m.CPU);
                sb.Append("\n");
                // Memory
                sb.Append("Memory:");
                sb.Append("   ");
                sb.Append(m.Memory);
                sb.Append("\n");
                // Video
                sb.Append("Video:");
                sb.Append("    ");
                sb.Append(m.Video);
                sb.Append("\n");
                // Audio
                sb.Append("Audio:");
                sb.Append("    ");
                sb.Append(m.Audio);
                sb.Append("\n");
                // Audio
                sb.Append("Media:");
                sb.Append("    ");
                sb.Append(m.Media);
                sb.Append("\n");

                sb.Append("-----------------------------------------------------------------\n");
                // description
                sb.Append(m.Description);
                if (m.OtherMisc != null)
                    sb.Append("\n" + m.OtherMisc);

                return sb.ToString();

            }
        }

        /// <summary>
        /// The size of the Spectrum border
        /// </summary>
        public enum BorderType
        {
            /// <summary>
            /// Attempts to equalise the border areas
            /// </summary>
            Uniform,

            /// <summary>
            /// Pretty much the signal the gate array is generating (looks shit)
            /// </summary>
            Uncropped,

            /// <summary>
            /// Top and bottom border removed so that the result is *almost* 16:9
            /// </summary>
            Widescreen,
        }
    }    
}
