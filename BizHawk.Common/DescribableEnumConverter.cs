using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace BizHawk.Common
{
	public class DescribableEnumConverter : EnumConverter
	{
		private readonly Type enumType;

		public DescribableEnumConverter(Type type) : base(type)
		{
			enumType = type;
		}

		public override bool CanConvertFrom(ITypeDescriptorContext context, Type srcType) => srcType == typeof(string);

		public override bool CanConvertTo(ITypeDescriptorContext context, Type destType) => destType == typeof(string);

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			var valueStr = value?.ToString() ?? throw new ArgumentException($"got null {nameof(value)}");
			return Enum.Parse(
				enumType,
				enumType.GetFields(BindingFlags.Public | BindingFlags.Static)
					.FirstOrDefault(fi => valueStr.Equals((fi.GetCustomAttribute(typeof(DisplayAttribute)) as DisplayAttribute)?.Name))?.Name
					?? valueStr
			);
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destType)
		{
			var fieldName = Enum.GetName(enumType, value ?? throw new ArgumentException($"got null {nameof(value)}"));
			if (fieldName != null)
			{
				var fieldInfo = enumType.GetField(fieldName);
				if (fieldInfo != null)
				{
					var found = fieldInfo.GetCustomAttribute(typeof(DisplayAttribute));
					if (found is DisplayAttribute da) return da.Name;
				}
			}
#pragma warning disable CS8603 // value can't be null here, it would've thrown already
			return value.ToString();
#pragma warning restore CS8603
		}

		public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) => new StandardValuesCollection(
			enumType.GetFields(BindingFlags.Public | BindingFlags.Static)
				.Select(fi => fi.GetValue(null))
				.ToList()
		);

		public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true;

		public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
	}
}
