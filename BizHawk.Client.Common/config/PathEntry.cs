using System.Linq;

namespace BizHawk.Client.Common
{
	public class PathEntry
	{
		public string SystemDisplayName { get; set; }
		public string Type { get; set; }
		public string Path { get; set; }
		public string System { get; set; }
		public int Ordinal { get; set; }

		internal bool IsSystem(string systemID)
		{
			return systemID == System || System.Split('_').Contains(systemID);
		}
	}
}
