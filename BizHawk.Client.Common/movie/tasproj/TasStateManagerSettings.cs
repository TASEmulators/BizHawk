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
			SaveGreenzone = true;
			Capacitymb = 512;
		}

		public TasStateManagerSettings(TasStateManagerSettings settings)
		{
			SaveGreenzone = settings.SaveGreenzone;
			Capacitymb = settings.Capacitymb;
		}

		/// <summary>
		/// Whether or not to save greenzone information to disk
		/// </summary>
		[DisplayName("Save History")]
		[Description("Whether or not to use savestate history")]
		public bool SaveGreenzone { get; set; }

		/// <summary>
		/// The total amount of memory to devote to greenzone in megabytes
		/// </summary>
		[DisplayName("Capacity (in megabytes))")]
		[Description("The size limit of the state history buffer.  When this limit is reached it will start removing previous savestates")]
		public int Capacitymb { get; set; }

		[JsonIgnore]
		[Browsable(false)]
		public int Cap
		{
			get { return Capacitymb * 1024 * 1024; }
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine(SaveGreenzone.ToString());
			sb.AppendLine(Capacitymb.ToString());

			return sb.ToString();
		}

		public void PopulateFromString(string settings)
		{
			if (!string.IsNullOrWhiteSpace(settings))
			{
				var lines = settings.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
				SaveGreenzone = bool.Parse(lines[0]);
				Capacitymb = int.Parse(lines[1]);
			}
		}
	}
}
