using System;
using System.ComponentModel;
using System.Globalization;

namespace BizHawk.Common
{
	public class BizDateTimeConverter : DateTimeConverter
	{
		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			var valueStr = value?.ToString() ?? throw new ArgumentException($"got null {nameof(value)}");
			return valueStr;
		}
	}
}
