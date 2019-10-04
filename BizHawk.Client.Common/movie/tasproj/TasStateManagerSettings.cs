using System;
using System.ComponentModel;
using System.Text;
using Newtonsoft.Json;

namespace BizHawk.Client.Common
{
	public class TasStateManagerSettings
	{
		public TasStateManagerSettings()
		{
			DiskSaveCapacitymb = 512;
			Capacitymb = 512;
			DiskCapacitymb = 1; // not working yet
			MemStateGapDivider = 64;
			FileStateGap = 4;
			BranchStatesInTasproj = false;
			EraseBranchStatesFirst = true;
		}

		public TasStateManagerSettings(TasStateManagerSettings settings)
		{
			DiskSaveCapacitymb = settings.DiskSaveCapacitymb;
			Capacitymb = settings.Capacitymb;
			DiskCapacitymb = settings.DiskCapacitymb;
			MemStateGapDivider = settings.MemStateGapDivider;
			FileStateGap = settings.FileStateGap;
			BranchStatesInTasproj = settings.BranchStatesInTasproj;
			EraseBranchStatesFirst = settings.EraseBranchStatesFirst;
		}

		/// <summary>
		/// Whether or not to save state history information to disk
		/// </summary>
		[DisplayName("Save History")]
		[Description("Whether or not to use savestate history")]
		public bool SaveStateHistory => DiskSaveCapacitymb != 0;

		/// <summary>
		/// Gets or sets the size limit to use when saving the TAS project to disk.
		/// </summary>
		[DisplayName("Save Capacity (in megabytes)")]
		[Description("The size limit to use when saving the tas project to disk.")]
		public int DiskSaveCapacitymb { get; set; }

		/// <summary>
		/// Gets or sets the total amount of memory to devote to state history in megabytes
		/// </summary>
		[DisplayName("Capacity (in megabytes)")]
		[Description("The size limit of the state history buffer.  When this limit is reached it will start moving to disk.")]
		public int Capacitymb { get; set; }

		/// <summary>
		/// Gets or sets the total amount of disk space to devote to state history in megabytes
		/// </summary>
		[DisplayName("Disk Capacity (in megabytes)")]
		[Description("The size limit of the state history buffer on the disk.  When this limit is reached it will start removing previous savestates")]
		public int DiskCapacitymb { get; set; }

		/// <summary>
		/// Gets or sets the divider that determines memory state gap
		/// </summary>
		[DisplayName("Divider for memory state interval")]
		[Description("The actual state gap in frames is calculated as ExpectedStateSize / div / 1024")]
		public int MemStateGapDivider { get; set; }

		/// <summary>
		/// Gets or sets the amount of states to skip during project saving
		/// </summary>
		[DisplayName("State interval for .tasproj")]
		[Description("The actual state gap in frames is calculated as Nth power on 2")]
		public int FileStateGap { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not to save branch states into the movie file
		/// </summary>
		[DisplayName("Put branch states to .tasproj")]
		[Description("Put branch states to .tasproj")]
		public bool BranchStatesInTasproj { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not to erase branch states before greenzone states when capacity is met
		/// </summary>
		[DisplayName("Erase branch states first")]
		[Description("Erase branch states before greenzone states when capacity is met")]
		public bool EraseBranchStatesFirst { get; set; }

		/// <summary>
		/// The total state capacity in bytes.
		/// </summary>
		[JsonIgnore]
		[Browsable(false)]
		public ulong CapTotal => (ulong)(Capacitymb + DiskCapacitymb) * 1024UL * 1024UL;

		/// <summary>
		/// The memory state capacity in bytes.
		/// </summary>
		[JsonIgnore]
		[Browsable(false)]
		public ulong Cap => (ulong)Capacitymb * 1024UL * 1024UL;

		public override string ToString()
		{
			var sb = new StringBuilder();

			sb.AppendLine(DiskSaveCapacitymb.ToString());
			sb.AppendLine(Capacitymb.ToString());
			sb.AppendLine(DiskCapacitymb.ToString());
			sb.AppendLine(BranchStatesInTasproj.ToString());
			sb.AppendLine(EraseBranchStatesFirst.ToString());
			sb.AppendLine(FileStateGap.ToString());
			sb.AppendLine(MemStateGapDivider.ToString());

			return sb.ToString();
		}

		public void PopulateFromString(string settings)
		{
			if (!string.IsNullOrWhiteSpace(settings))
			{
				try
				{
					string[] lines = settings.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
					Capacitymb = int.Parse(lines[1]);
					int refCapacity;

					if (!int.TryParse(lines[0], out refCapacity))
					{
						DiskSaveCapacitymb = bool.Parse(lines[0]) ? Capacitymb : 0;
					}
					else
					{
						DiskSaveCapacitymb = refCapacity;
					}

					int i = 2;

					DiskCapacitymb = lines.Length > i ? int.Parse(lines[i++]) : 1;
					BranchStatesInTasproj = lines.Length > i && bool.Parse(lines[i++]);
					EraseBranchStatesFirst = lines.Length <= i || bool.Parse(lines[i++]);
					FileStateGap = lines.Length > i ? int.Parse(lines[i++]) : 4;
					FileStateGap = lines.Length > i ? int.Parse(lines[i++]) : 64;
				}
				catch (Exception) // TODO: this is bad
				{
					// "GreenZoneSettings inconsistent, ignoring"
					// if we don't catch it, the project won't load
					// but dialog boxes aren't supposed to exist here?
				}
			}
		}
	}
}
