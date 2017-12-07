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
            if (SoundMixer != null)
                SoundMixer.Stereo = o.StereoSound;

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
            [DisplayName("Stereo Sound")]
            [Description("Turn stereo sound on or off")]
            [DefaultValue(true)]
            public bool StereoSound { get; set; }


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

            [DisplayName("Auto-load tape")]
            [Description("Auto or manual tape operation")]
            [DefaultValue(true)]
            public bool AutoLoadTape { get; set; }

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

        /// <summary>
        /// The size of the Spectrum border
        /// </summary>
        public enum BorderType
        {
            Full,
            Widescreen,
        }

        /// <summary>
        /// The speed at which the tape is loaded
        /// </summary>
        public enum TapeLoadSpeed
        {
            Accurate,
            Fast,
            Fastest
        }
    }
}
