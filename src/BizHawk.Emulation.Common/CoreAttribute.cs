namespace BizHawk.Emulation.Common
{
	[AttributeUsage(AttributeTargets.Class)]
	public class CoreAttribute : Attribute
	{
		public readonly string Author;

		public readonly string CoreName;

		public readonly bool Released;

		public readonly bool SingleInstance;

		public CoreAttribute(string name, string author, bool singleInstance = false, bool isReleased = true)
		{
			Author = author;
			CoreName = name;
			Released = isReleased;
			SingleInstance = singleInstance;
		}
	}

	[AttributeUsage(AttributeTargets.Class)]
	public sealed class PortedCoreAttribute : CoreAttribute
	{
		public readonly string PortedUrl;

		public readonly string PortedVersion;

		public PortedCoreAttribute(
			string name,
			string author,
			string portedVersion = "",
			string portedUrl = "",
			bool singleInstance = false,
			bool isReleased = true)
				: base(name, author, singleInstance, isReleased)
		{
			PortedUrl = portedUrl;
			PortedVersion = portedVersion;
		}
	}
}
