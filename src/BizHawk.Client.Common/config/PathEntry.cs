using System.Linq;
using Newtonsoft.Json;

namespace BizHawk.Client.Common
{
	public sealed class PathEntry
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

		public PathEntry(string system, int ordinal, string type, string path)
		{
			Ordinal = ordinal;
			Path = path;
			System = system;
			Type = type;
		}

		internal bool IsSystem(string systemID)
		{
			return systemID == System || System.Split('_').Contains(systemID);
		}
	}
}
