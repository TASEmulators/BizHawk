using System;
using System.ComponentModel;
using System.Globalization;

namespace BizHawk.Common
{
	public class BizDateTimeConverter : DateTimeConverter
	{
		public override object ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
		{
			if (value is not DateTime d || destinationType != typeof(string)) throw new NotSupportedException("can only do DateTime --> string");
#pragma warning disable CA1305
			// this function is explicitly supposed to be locale dependent
			return d.ToString();
#pragma warning restore CA1305
		}
	}
}
