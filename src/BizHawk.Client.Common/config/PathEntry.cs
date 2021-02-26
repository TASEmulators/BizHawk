using System.Linq;
using Newtonsoft.Json;

namespace BizHawk.Client.Common
{
	public class PathEntry
	{
		public string Type { get; set; }
		[JsonIgnore]
		private string _path;
		public string Path
		{
			get => _path;
			set => _path = value.Replace('\\', '/');
		}
		public string System { get; set; }
		public int Ordinal { get; set; }

		internal bool IsSystem(string systemID)
		{
			return systemID == System || System.Split('_').Contains(systemID);
		}
	}
}
