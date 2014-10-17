using System;
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

		/// <summary>
		/// Whether or not to save greenzone information to disk
		/// </summary>
		public bool SaveGreenzone { get; set; }

		/// <summary>
		/// The total amount of memory to devote to greenzone in megabytes
		/// </summary>
		public int Capacitymb { get; set; }

		[JsonIgnore]
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
			var lines = settings.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
			SaveGreenzone = bool.Parse(lines[0]);
			Capacitymb = int.Parse(lines[1]);
		}
	}
}
