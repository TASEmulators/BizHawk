using System;
using Newtonsoft.Json;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using System.ComponentModel;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    public partial class ZXSpectrum : ISettable<ZXSpectrum.ZXSpectrumSettings, ZXSpectrum.ZXSpectrumSyncSettings>
    {
        internal ZXSpectrumSettings Settings = new ZXSpectrumSettings();
        internal ZXSpectrumSyncSettings SyncSettings = new ZXSpectrumSyncSettings();

        public ZXSpectrumSettings GetSettings()
        {
            return Settings.Clone();
        }

        public ZXSpectrumSyncSettings GetSyncSettings()
        {
            return SyncSettings.Clone();
        }

        public bool PutSettings(ZXSpectrumSettings o)
        {
            //if (SoundMixer != null)
                //SoundMixer.Stereo = o.StereoSound;

            if (_machine != null && _machine.AYDevice != null && _machine.AYDevice.GetType() == typeof(AYChip))
            {
                ((AYChip)_machine.AYDevice as AYChip).PanningConfiguration = o.AYPanConfig;
            }

            Settings = o;

            return false;
        }

        public bool PutSyncSettings(ZXSpectrumSyncSettings o)
        {
            bool ret = ZXSpectrumSyncSettings.NeedsReboot(SyncSettings, o);
            SyncSettings = o;
            return ret;
        }

        

        public class ZXSpectrumSettings
        {
            [DisplayName("Auto-load/stop tape")]
            [Description("Auto or manual tape operation. Auto will attempt to detect CPU tape traps and automatically Stop/Start the tape")]
            [DefaultValue(true)]
            public bool AutoLoadTape { get; set; }

            /*
            [DisplayName("Stereo Sound")]
            [Description("Turn stereo sound on or off")]
            [DefaultValue(true)]
            public bool StereoSound { get; set; }
            */

            [DisplayName("AY-3-8912 Panning Config")]
            [Description("Set the PSG panning configuration.\nThe chip has 3 audio channels that can be outputed in different configurations")]
            [DefaultValue(AYChip.AYPanConfig.ABC)]
            public AYChip.AYPanConfig AYPanConfig { get; set; }

            [DisplayName("Core OSD Message Verbosity")]
            [Description("Full: Display all GUI messages\nMedium: Display only emulator/device generated messages\nNone: Show no messages")]
            [DefaultValue(OSDVerbosity.Medium)]
            public OSDVerbosity OSDMessageVerbosity { get; set; }


            public ZXSpectrumSettings Clone()
            {
                return (ZXSpectrumSettings)MemberwiseClone();
            }

            public ZXSpectrumSettings()
            {
                BizHawk.Common.SettingsUtil.SetDefaultValues(this);
            }
        }

        public class ZXSpectrumSyncSettings
        {
            [DisplayName("Deterministic Emulation")]
            [Description("If true, the core agrees to behave in a completely deterministic manner")]
            [DefaultValue(true)]
            public bool DeterministicEmulation { get; set; }

            [DisplayName("Spectrum model")]
            [Description("The model of spectrum to be emulated")]
            [DefaultValue(MachineType.ZXSpectrum48)]
            public MachineType MachineType { get; set; }

            [DisplayName("Border type")]
            [Description("Select how to show the border area")]
            [DefaultValue(BorderType.Full)]
            public BorderType BorderType { get; set; }

            [DisplayName("Tape Load Speed")]
            [Description("Select how fast the spectrum loads the game from tape")]
            [DefaultValue(TapeLoadSpeed.Accurate)]
            public TapeLoadSpeed TapeLoadSpeed { get; set; }

            [DisplayName("Joystick 1")]
            [Description("The emulated joystick assigned to P1 (SHOULD BE UNIQUE TYPE!)")]
            [DefaultValue(JoystickType.Kempston)]
            public JoystickType JoystickType1 { get; set; }

            [DisplayName("Joystick 2")]
            [Description("The emulated joystick assigned to P2 (SHOULD BE UNIQUE TYPE!)")]
            [DefaultValue(JoystickType.SinclairLEFT)]
            public JoystickType JoystickType2 { get; set; }

            [DisplayName("Joystick 3")]
            [Description("The emulated joystick assigned to P3 (SHOULD BE UNIQUE TYPE!)")]
            [DefaultValue(JoystickType.SinclairRIGHT)]
            public JoystickType JoystickType3 { get; set; }


            public ZXSpectrumSyncSettings Clone()
            {
                return (ZXSpectrumSyncSettings)MemberwiseClone();
            }

            public ZXSpectrumSyncSettings()
            {
                SettingsUtil.SetDefaultValues(this);
            }

            public static bool NeedsReboot(ZXSpectrumSyncSettings x, ZXSpectrumSyncSettings y)
            {
                return !DeepEquality.DeepEquals(x, y);
            }
        }

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
        /// The size of the Spectrum border
        /// </summary>
        public enum BorderType
        {
            /// <summary>
            /// How it was originally back in the day
            /// </summary>
            Full,

            /// <summary>
            /// All borders 24px
            /// </summary>
            Medium,

            /// <summary>
            /// All borders 10px
            /// </summary>
            Small,

            /// <summary>
            /// No border at all
            /// </summary>
            None,

            /// <summary>
            /// Top and bottom border removed so that the result is *almost* 16:9
            /// </summary>
            Widescreen,
        }

        /// <summary>
        /// The speed at which the tape is loaded
        /// NOT IN USE YET
        /// </summary>
        public enum TapeLoadSpeed
        {
            Accurate,
            //Fast,
            //Fastest
        }
    }
}
