using System;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Define if the property has to be persisted in config
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class ConfigPersistAttribute : Attribute
	{
	}
}
