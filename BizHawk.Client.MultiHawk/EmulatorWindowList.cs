using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace BizHawk.Client.MultiHawk
{
	public class EmulatorWindowList : List<EmulatorWindow>
	{
		public string SessionName { get; set; }

		public EmulatorWindow Master
		{
			get
			{
				if (this.Any())
				{
					return this.First();
				}

				return null;
			}
		}

		public IEnumerable<RomSessionEntry> Session
		{
			get
			{
				return this.Select(ew => new RomSessionEntry
				{
					RomName = ew.CurrentRomPath,
					Wndx = ew.Location.X,
					Wndy = ew.Location.Y
				});
			}
		}

		public string SessionJson
		{
			get
			{
				return JsonConvert.SerializeObject(Session);
			}
		}

		public static IEnumerable<RomSessionEntry> FromJson(string json)
		{
			return JsonConvert.DeserializeObject<List<RomSessionEntry>>(json);
		}

		public new void Clear()
		{
			SessionName = string.Empty;
			base.Clear();
		}

		public class RomSessionEntry
		{
			public string RomName { get; set; }
			public int Wndx { get; set; }
			public int Wndy { get; set; }
		}
	}
}
