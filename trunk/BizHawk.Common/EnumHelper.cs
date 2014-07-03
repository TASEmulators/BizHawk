using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Reflection;

using BizHawk.Common.ReflectionExtensions;

namespace BizHawk.Common
{
	public static class EnumHelper
	{
		public static IEnumerable<string> GetDescriptions<T>()
		{
			var vals = Enum.GetValues(typeof(T));

			foreach (var v in vals)
			{
				yield return v.GetDescription();
			}
		}
	}
}