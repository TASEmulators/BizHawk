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
			DiskCapacitymb = 512;
			StateGap = 4;
			BranchStatesInTasproj = false;
			EraseBranchStatesFirst = true;
		}

		public TasStateManagerSettings(TasStateManagerSettings settings)
		{
			DiskSaveCapacitymb = settings.DiskSaveCapacitymb;
			Capacitymb = settings.Capacitymb;
			DiskCapacitymb = settings.DiskCapacitymb;
			StateGap = settings.StateGap;
			BranchStatesInTasproj = settings.BranchStatesInTasproj;
			EraseBranchStatesFirst = settings.EraseBranchStatesFirst;
		}

		/// <summary>
		/// Whether or not to save state history information to disk
		/// </summary>
		[DisplayName("Save History")]
		[Description("Whether or not to use savestate history")]
		public bool SaveStateHistory { get { return DiskSaveCapacitymb != 0; } }

		/// <summary>
		/// The size limit to use when saving the tas project to disk.
		/// </summary>
		[DisplayName("Save Capacity (in megabytes)")]
		[Description("The size limit to use when saving the tas project to disk.")]
		public int DiskSaveCapacitymb { get; set; }

		/// <summary>
		/// The total amount of memory to devote to state history in megabytes
		/// </summary>
		[DisplayName("Capacity (in megabytes)")]
		[Description("The size limit of the state history buffer.  When this limit is reached it will start moving to disk.")]
		public int Capacitymb { get; set; }

		/// <summary>
		/// The total amount of disk space to devote to state history in megabytes
		/// </summary>
		[DisplayName("Disk Capacity (in megabytes)")]
		[Description("The size limit of the state history buffer on the disk.  When this limit is reached it will start removing previous savestates")]
		public int DiskCapacitymb { get; set; }

		/// <summary>
		/// The amount of states to skip during project saving
		/// </summary>
		[DisplayName("State interval for .tasproj")]
		[Description("The actual state gap in frames is calculated as Nth power on 2")]
		public int StateGap { get; set; }

		/// <summary>
		/// Put branch states to .tasproj
		/// </summary>
		[DisplayName("Put branch states to .tasproj")]
		[Description("Put branch states to .tasproj")]
		public bool BranchStatesInTasproj { get; set; }

		/// <summary>
		/// Erase branch states before greenzone states when capacity is met
		/// </summary>
		[DisplayName("Erase branch states first")]
		[Description("Erase branch states before greenzone states when capacity is met")]
		public bool EraseBranchStatesFirst { get; set; }

		/// <summary>
		/// The total state capacity in bytes.
		/// </summary>
		[JsonIgnore]
		[Browsable(false)]
		public ulong CapTotal
		{
			get { return (ulong)(Capacitymb + DiskCapacitymb) * 1024UL * 1024UL; }
		}

		/// <summary>
		/// The memory state capacity in bytes.
		/// </summary>
		[JsonIgnore]
		[Browsable(false)]
		public ulong Cap
		{
			get { return (ulong)Capacitymb * 1024UL * 1024UL; }
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine(DiskSaveCapacitymb.ToString());
			sb.AppendLine(Capacitymb.ToString());
			sb.AppendLine(DiskCapacitymb.ToString());
			sb.AppendLine(BranchStatesInTasproj.ToString());
			sb.AppendLine(EraseBranchStatesFirst.ToString());
			sb.AppendLine(StateGap.ToString());

			return sb.ToString();
		}

		public void PopulateFromString(string settings)
		{
			if (!string.IsNullOrWhiteSpace(settings))
			{
				try
				{
					string[] lines = settings.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
					Capacitymb = int.Parse(lines[1]);
					int refCapacity;

					if (!int.TryParse(lines[0], out refCapacity))
					{
						if (bool.Parse(lines[0]))
							DiskSaveCapacitymb = Capacitymb;
						else
							DiskSaveCapacitymb = 0;
					}
					else
						DiskSaveCapacitymb = refCapacity;

					if (lines.Length > 2)
						DiskCapacitymb = int.Parse(lines[2]);
					else
						DiskCapacitymb = 512;

					if (lines.Length > 3)
						BranchStatesInTasproj = bool.Parse(lines[3]);
					else
						BranchStatesInTasproj = false;

					if (lines.Length > 4)
						EraseBranchStatesFirst = bool.Parse(lines[4]);
					else
						EraseBranchStatesFirst = true;

					if (lines.Length > 5)
						StateGap = int.Parse(lines[5]);
					else
						StateGap = 4;
				}
				catch (Exception)
				{
					// "GreenZoneSettings inconsistent, ignoring"
					// if we don't catch it, the project won't load
					// but dialog boxes aren't supposed to exist here?
				}
			}
		}
	}
}
