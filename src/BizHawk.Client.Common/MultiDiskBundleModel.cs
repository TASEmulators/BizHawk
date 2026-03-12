using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace BizHawk.Client.Common
{
	/// <remarks>TODO merge into <see cref="XmlGame"/></remarks>
	public sealed class MultiDiskBundleModel(string bundlePath, string sysID, IReadOnlyList<string> romPaths)
	{
		public static readonly FilesystemFilterSet BundlesFSFilterSet = new(new FilesystemFilter("XML Files", [ "xml" ]));

		private string _ser = null;

		public string XMLString
			=> _ser ??= new XElement("BizHawk-XMLGame",
				new XAttribute("System", sysID),
				new XAttribute("Name", Path.GetFileNameWithoutExtension(bundlePath)),
				new XElement("LoadAssets",
					romPaths.Select(static path => new XElement("Asset", new XAttribute("FileName", path))))).ToString();

		public override string ToString()
			=> XMLString;
	}
}
