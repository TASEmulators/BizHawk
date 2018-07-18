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
    }
}
