using System;

namespace BizHawk.Client.EmuHawk
{
	[AttributeUsage(AttributeTargets.Class)]
	public class SchemaAttributes : Attribute
	{
		/// <summary>
		/// Gets the system id associated with this schema
		/// </summary>
		public string SystemId { get; private set; }

		public SchemaAttributes(string systemId)
		{
			SystemId = systemId;
		}
	}
}
