using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
