namespace BizHawk.Emulation.Common
{
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class SchemaAttribute : Attribute
	{
		public string SystemId { get; }

		public SchemaAttribute(string systemId)
		{
			SystemId = systemId;
		}
	}
}
