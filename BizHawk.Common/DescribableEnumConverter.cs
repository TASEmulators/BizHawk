using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

namespace BizHawk.Common
{
	public class DescribableEnumConverter : EnumConverter
	{
		private Type enumType;

		public DescribableEnumConverter(Type type) : base(type)
		{
			enumType = type;
		}

		public override bool CanConvertTo(ITypeDescriptorContext context, Type destType)
		{
			return destType == typeof(string);
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture,
			object value, Type destType)
		{
			var fi = enumType.GetField(Enum.GetName(enumType, value));
			var attr = (DisplayAttribute)fi.GetCustomAttribute(typeof(DisplayAttribute));
			if (attr != null)
				return attr.Name;
			else
				return value.ToString();
		}

		public override bool CanConvertFrom(ITypeDescriptorContext context, Type srcType)
		{
			return srcType == typeof(string);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture,
			object value)
		{
			foreach (var fi in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
			{
				var attr = (DisplayAttribute)fi.GetCustomAttribute(typeof(DisplayAttribute));
				if (attr != null && attr.Name.Equals(value))
					return Enum.Parse(enumType, fi.Name);
			}
			return Enum.Parse(enumType, (string)value);
		}

		public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
		{
			return true;
		}

		public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
		{
			return true;
		}

		public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
		{
			var ret = new List<object>();
			foreach (var fi in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
			{
				ret.Add(fi.GetValue(null));
			}
			return new StandardValuesCollection(ret);
		}
	}
}
