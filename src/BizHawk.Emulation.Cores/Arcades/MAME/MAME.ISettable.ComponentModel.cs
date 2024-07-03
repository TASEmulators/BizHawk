using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;

using BizHawk.Common;

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

		public override bool IsSupportedType(Type type) => type == typeof(MAMESyncSettings);

		public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
		{
			if (objectType == typeof(MAMESyncSettings))
			{
				return new SyncSettingsCustomTypeDescriptor(Settings);
			}
			else
			{
				return null; // ???
			}
		}
	}

	public class SyncSettingsCustomTypeDescriptor : CustomTypeDescriptor
	{
		public SyncSettingsCustomTypeDescriptor(List<DriverSetting> settings)
		{
			Settings = settings;
		}

		public List<DriverSetting> Settings { get; }
		public override string GetClassName() => nameof(MAMESyncSettings);
		public override string GetComponentName() => nameof(MAMESyncSettings);
		public override PropertyDescriptor GetDefaultProperty() => GetProperties()[0]; // "default" ??
		public override PropertyDescriptorCollection GetProperties(Attribute[] attributes) => GetProperties();

		public override PropertyDescriptorCollection GetProperties()
		{
			var rtc = typeof(MAMERTCSettings).GetProperties()
				.Select(t => new RTCPropertyDescriptor(t))
				.Cast<PropertyDescriptor>();
			var s = Settings.Select(m => new MAMEPropertyDescriptor(m));
			return new PropertyDescriptorCollection(rtc.Concat(s).ToArray());
		}
	}

	public class RTCPropertyDescriptor : PropertyDescriptor
	{
		public RTCPropertyDescriptor(PropertyInfo settingInfo)
			: base(settingInfo.Name, settingInfo.GetCustomAttributes(false).Cast<Attribute>().ToArray())
		{
			SettingInfo = settingInfo;
		}

		private PropertyInfo SettingInfo { get; }
		protected object ConvertFromString(string s) => Converter.ConvertFromString(s);
		protected string ConvertToString(object o) => Converter.ConvertToString(o);
		public override bool CanResetValue(object component) => true;

		public override bool ShouldSerializeValue(object component)
			=> !DeepEquality.DeepEquals(GetValue(component), SettingInfo.GetCustomAttribute<DefaultValueAttribute>().Value);

		public override Type PropertyType => SettingInfo.PropertyType;
		public override Type ComponentType => typeof(MAMERTCSettings);
		public override bool IsReadOnly => false;

		public override object GetValue(object component)
			=> SettingInfo.GetValue(((MAMESyncSettings)component).RTCSettings);

		public override void ResetValue(object component)
			=> SetValue(component, SettingInfo.GetCustomAttribute<DefaultValueAttribute>().Value);

		public override void SetValue(object component, object value)
			=> SettingInfo.SetValue(((MAMESyncSettings)component).RTCSettings, value);
	}

	public class MAMEPropertyDescriptor : PropertyDescriptor
	{
		public MAMEPropertyDescriptor(DriverSetting setting)
			: base(setting.LookupKey, new Attribute[0])
		{
			Setting = setting;
			Converter = new MyTypeConverter(Setting);
		}

		private DriverSetting Setting { get; }
		protected object ConvertFromString(string s) => s;
		protected string ConvertToString(object o) => (string)o;
		public override bool CanResetValue(object component) => true;
		public override bool ShouldSerializeValue(object component)
			=> ((MAMESyncSettings)component).DriverSettings.ContainsKey(Setting.LookupKey);
		public override Type PropertyType => typeof(string);
		public override TypeConverter Converter { get; }
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
			public MyTypeConverter(DriverSetting setting)
				=> Setting = setting;
			private DriverSetting Setting { get; }
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