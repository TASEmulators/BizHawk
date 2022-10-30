using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

using static BizHawk.Emulation.Cores.Arcades.MAME.MAME;

namespace BizHawk.Emulation.Cores.Arcades.MAME
{
	public class MAMETypeDescriptorProvider : TypeDescriptionProvider
	{
		public MAMETypeDescriptorProvider(List<DriverSetting> settings)
		{
			Settings = settings;
		}

		public List<DriverSetting> Settings { get; }
		public override bool IsSupportedType(Type type) => type == typeof(List<DriverSetting>);
		public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
			=> new SyncSettingsCustomTypeDescriptor(Settings);
	}

	public class SyncSettingsCustomTypeDescriptor : CustomTypeDescriptor
	{
		public SyncSettingsCustomTypeDescriptor(List<DriverSetting> settings)
		{
			Settings = settings;
		}

		public List<DriverSetting> Settings { get; }
		public override string GetClassName() => nameof(List<DriverSetting>);
		public override string GetComponentName() => nameof(List<DriverSetting>);
		public override PropertyDescriptor GetDefaultProperty() => GetProperties()[0]; // "default" ??
		public override PropertyDescriptorCollection GetProperties(Attribute[] attributes) => GetProperties();

		public override PropertyDescriptorCollection GetProperties()
		{
			var s = Settings.Select(m => new MAMEPropertyDescriptor(m));
			return new PropertyDescriptorCollection(s.ToArray());
		}
	}

	public class MAMEPropertyDescriptor : PropertyDescriptor
	{
		public MAMEPropertyDescriptor(DriverSetting setting) : base(setting.LookupKey, new Attribute[0])
		{
			Setting = setting;
		}

		public DriverSetting Setting { get; private set; }
		protected object ConvertFromString(string s) => s;
		protected string ConvertToString(object o) => (string)o;
		public override bool CanResetValue(object component) => true;
		public override bool ShouldSerializeValue(object component)
			=> ((MAMESyncSettings)component).DriverSettings.ContainsKey(Setting.LookupKey);
		public override Type PropertyType => typeof(string);
		public override TypeConverter Converter => new MyTypeConverter { Setting = Setting };
		public override Type ComponentType => typeof(List<DriverSetting>);
		public override bool IsReadOnly => false;
		public override string Name => Setting.LookupKey;
		public override string DisplayName => Setting.Name;
		public override string Description => Setting.LookupKey;

		public override object GetValue(object component)
		{
			var ss = (MAMESyncSettings)component;
			if (!ss.DriverSettings.TryGetValue(Setting.LookupKey, out var val))
				val = Setting.DefaultValue;
			return ConvertFromString(val);
		}

		public override void ResetValue(object component)
		{
			((MAMESyncSettings)component).DriverSettings.Remove(Setting.LookupKey);
		}

		public override void SetValue(object component, object value)
		{
			var s = ConvertToString(value);
			if (s == null || s == Setting.DefaultValue)
			{
				ResetValue(component);
				return;
			}
			((MAMESyncSettings)component).DriverSettings[Setting.LookupKey] = s;
		}

		private class MyTypeConverter : TypeConverter
		{
			public DriverSetting Setting { get; set; }
			public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true;
			public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
			public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);
			public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string);
			public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
				=> new(Setting.Options.Select(e => e.Key).ToList());
			public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
				=> Setting.Options.SingleOrDefault(d => d.Value == (string)value).Key ?? Setting.DefaultValue;
			public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
				=> Setting.Options[(string)value] ?? Setting.Options[Setting.DefaultValue];
		}
	}
}