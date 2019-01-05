using BizHawk.Common;
using BizHawk.Emulation.Common;
using System.Collections.Generic;
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

			Dictionary<string, string> Data = new Dictionary<string, string>();

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
                }

				m.Data.Add(AmstradCPC.GetMemberName((CPCMachineMetaData c) => c.Name), m.Name.Trim());
				m.Data.Add(AmstradCPC.GetMemberName((CPCMachineMetaData c) => c.Description), m.Description.Trim());
				m.Data.Add(AmstradCPC.GetMemberName((CPCMachineMetaData c) => c.Released), m.Released.Trim());
				m.Data.Add(AmstradCPC.GetMemberName((CPCMachineMetaData c) => c.CPU), m.CPU.Trim());
				m.Data.Add(AmstradCPC.GetMemberName((CPCMachineMetaData c) => c.Memory), m.Memory.Trim());
				m.Data.Add(AmstradCPC.GetMemberName((CPCMachineMetaData c) => c.Video), m.Video.Trim());
				m.Data.Add(AmstradCPC.GetMemberName((CPCMachineMetaData c) => c.Audio), m.Audio.Trim());
				m.Data.Add(AmstradCPC.GetMemberName((CPCMachineMetaData c) => c.Media), m.Media.Trim());

				return m;
            }

			public static string GetMetaString(MachineType type)
			{
				var m = GetMetaObject(type);

				StringBuilder sb = new StringBuilder();

				// get longest title
				int titleLen = 0;
				foreach (var d in m.Data)
				{
					if (d.Key.Length > titleLen)
						titleLen = d.Key.Length;
				}

				var maxDataLineLen = 40;

				// generate layout
				foreach (var d in m.Data)
				{
					var tLen = d.Key.Length;
					var makeup = (titleLen - tLen) / 4;
					sb.Append(d.Key + ":\t");
					for (int i = 0; i < makeup; i++)
					{
						if (tLen > 4)
							sb.Append("\t");
						else
						{
							makeup--;
							sb.Append("\t");
						}
					}

					// output the data splitting and tabbing as neccessary
					var arr = d.Value.Split(' ');
					int cnt = 0;

					List<string> builder = new List<string>();
					string working = "";
					foreach (var s in arr)
					{
						var len = s.Length;
						if (working.Length + 1 + len > maxDataLineLen)
						{
							// new line needed
							builder.Add(working.Trim(' '));
							working = "";
						}
						working += s + " ";
					}

					builder.Add(working.Trim(' '));

					// output the data
					for (int i = 0; i < builder.Count; i++)
					{
						if (i != 0)
						{
							sb.Append("\t");
							sb.Append("\t");
						}

						sb.Append(builder[i]);
						sb.Append("\r\n");
					}

					//sb.Append("\r\n");
				}

				return sb.ToString();
			}

			public static string GetMetaStringOld(MachineType type)
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
