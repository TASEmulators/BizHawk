using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using NymaTypes;
using static BizHawk.Emulation.Cores.Waterbox.NymaCore;
using static BizHawk.Emulation.Cores.Waterbox.NymaCore.NymaSettingsInfo;

namespace BizHawk.Emulation.Cores.Waterbox
{
	public class NymaTypeDescriptorProvider : TypeDescriptionProvider
	{
		public NymaSettingsInfo SettingsInfo { get; }
		public NymaTypeDescriptorProvider(NymaSettingsInfo info)
		{
			SettingsInfo = info;
		}
		public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
		{
			if (objectType == typeof(NymaSyncSettings))
				return new SyncSettingsCustomTypeDescriptor(SettingsInfo);
			else if (objectType == typeof(NymaSettings))
				return new SettingsCustomTypeDescriptor(SettingsInfo);
			else
				return null; //?
		}

		public override bool IsSupportedType(Type type)
		{
			return type == typeof(NymaSyncSettings) || type == typeof(NymaSettings);
		}
	}

	public class SyncSettingsCustomTypeDescriptor : CustomTypeDescriptor
	{
		public NymaSettingsInfo SettingsInfo { get; }
		public SyncSettingsCustomTypeDescriptor(NymaSettingsInfo info)
		{
			SettingsInfo = info;
		}
		public override string GetClassName() => nameof(NymaSyncSettings);
		public override string GetComponentName() => nameof(NymaSyncSettings);
		public override PropertyDescriptor GetDefaultProperty() => GetProperties()[0]; // "default" ??
		public override PropertyDescriptorCollection GetProperties()
		{
			var s1 = SettingsInfo.Ports
				.Select((p, i) => new PortPropertyDescriptor(p, i))
				.Cast<PropertyDescriptor>();
			var s2 = SettingsInfo.Settings
				.Where(s => !SettingsInfo.HiddenSettings.Contains(s.SettingsKey))
				.Select(m => MednaPropertyDescriptor.Create(m));
			return new PropertyDescriptorCollection(s1.Concat(s2).ToArray());
		}
		public override PropertyDescriptorCollection GetProperties(Attribute[] attributes) => GetProperties();
	}

	public class SettingsCustomTypeDescriptor : CustomTypeDescriptor
	{
		public NymaSettingsInfo SettingsInfo { get; }
		public SettingsCustomTypeDescriptor(NymaSettingsInfo info)
		{
			SettingsInfo = info;
		}
		public override string GetClassName() => nameof(NymaSettings);
		public override string GetComponentName() => nameof(NymaSettings);
		public override PropertyDescriptor GetDefaultProperty() => GetProperties()[0]; // "default" ??
		public override PropertyDescriptorCollection GetProperties()
		{
			return new PropertyDescriptorCollection(
				SettingsInfo.LayerNames.Select(l => new LayerPropertyDescriptor(l)).ToArray());
		}
		public override PropertyDescriptorCollection GetProperties(Attribute[] attributes) => GetProperties();
	}

	public abstract class MednaPropertyDescriptor : PropertyDescriptor
	{
		public SettingT Setting { get; private set; }
		public MednaPropertyDescriptor(SettingT setting)
			: base(setting.SettingsKey, new Attribute[0])
		{
			Setting = setting;
		}

		public override Type ComponentType => typeof(NymaSyncSettings);
		public override bool IsReadOnly => false;
		// public override Type PropertyType => typeof(string);
		public override bool CanResetValue(object component) => true;

		public override string Name => Setting.SettingsKey;
		public override string DisplayName => Setting.Name;
		public override string Description => $"{Setting.Description}\n[{Setting.SettingsKey}]";
		public override string Category => "Settings";

		protected abstract object ConvertFromString(string s);
		protected abstract string ConvertToString(object o);

		public override object GetValue(object component)
		{
			var ss = (NymaSyncSettings)component;
			if (!ss.MednafenValues.TryGetValue(Setting.SettingsKey, out var val))
				val = Setting.DefaultValue;
			var ret = ConvertFromString(val);
			return ret;
		}

		public override void ResetValue(object component)
		{
			((NymaSyncSettings)component).MednafenValues.Remove(Setting.SettingsKey);
		}

		public override void SetValue(object component, object value)
		{
			var s = ConvertToString(value);
			if (s == null || s == Setting.DefaultValue)
			{
				ResetValue(component);
				return;
			}
			((NymaSyncSettings)component).MednafenValues[Setting.SettingsKey] = s;
		}

		public override bool ShouldSerializeValue(object component)
		{
			return ((NymaSyncSettings)component).MednafenValues.ContainsKey(Setting.SettingsKey);
		}

		public static MednaPropertyDescriptor Create(SettingT s)
		{
			switch (s.Type)
			{
				case SettingType.Int:
					return new MednaLongDescriptor(s);
				case SettingType.Uint:
					return new MednaUlongDescriptor(s);
				case SettingType.Bool:
					return new MednaBoolDescriptor(s);
				case SettingType.Float:
					return new MednaDoubleDescriptor(s);
				case SettingType.String:
					return new MednaStringDescriptor(s);
				case SettingType.Enum:
					return new MednaEnumDescriptor(s);
				default:
					throw new NotImplementedException($"Unexpected SettingType {s.Type}");
			}
		}
	}

	public class MednaEnumDescriptor : MednaPropertyDescriptor
	{
		public MednaEnumDescriptor(SettingT s) : base(s) {}
		public override Type PropertyType => typeof(string);
		protected override object ConvertFromString(string s)
		{
			return s;
		}
		protected override string ConvertToString(object o)
		{
			return (string)o;
		}
		public override TypeConverter Converter => new MyTypeConverter { Setting = Setting };

		private class MyTypeConverter : TypeConverter
		{
			public SettingT Setting { get; set; }

			public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);
			public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string);
			public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
			{
				return Setting.SettingEnums
					.SingleOrDefault(d => d.Name == (string)value)
					?.Value
					?? Setting.DefaultValue;

			}
			public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
			{
				return Setting.SettingEnums
					.SingleOrDefault(d => d.Value == (string)value)
					?.Name
					?? Setting.SettingEnums
					.Single(d => d.Value == Setting.DefaultValue)
					.Name;
			}

			public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) => new StandardValuesCollection(
				Setting.SettingEnums.Select(e => e.Value).ToList()
			);

			public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true;

			public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
		}
	}
	public class MednaStringDescriptor : MednaPropertyDescriptor
	{
		public MednaStringDescriptor(SettingT s) : base(s) {}
		public override Type PropertyType => typeof(string);
		protected override object ConvertFromString(string s)
		{
			return s;
		}
		protected override string ConvertToString(object o)
		{
			return (string)o;
		}
	}
	public class MednaBoolDescriptor : MednaPropertyDescriptor
	{
		public MednaBoolDescriptor(SettingT s) : base(s) {}
		public override Type PropertyType => typeof(bool);
		protected override object ConvertFromString(string s)
		{
			return int.Parse(s) != 0;
		}
		protected override string ConvertToString(object o)
		{
			return (bool)o ? "1" : "0";
		}
	}
	public class MednaLongDescriptor : MednaPropertyDescriptor
	{
		public MednaLongDescriptor(SettingT s) : base(s) {}
		public override Type PropertyType => typeof(long);
		protected override object ConvertFromString(string s)
		{
			var ret = long.Parse(s);
			if (Setting.Min != null && ret < long.Parse(Setting.Min) || Setting.Max != null && ret > long.Parse(Setting.Max))
				ret = long.Parse(Setting.DefaultValue);
			return ret;
		}
		protected override string ConvertToString(object o)
		{
			return o.ToString();
		}
	}
	public class MednaUlongDescriptor : MednaPropertyDescriptor
	{
		public MednaUlongDescriptor(SettingT s) : base(s) {}
		public override Type PropertyType => typeof(ulong);
		protected override object ConvertFromString(string s)
		{
			var ret = Parse(s);
			if (Setting.Min != null && ret < Parse(Setting.Min) || Setting.Max != null && ret > Parse(Setting.Max))
				ret = Parse(Setting.DefaultValue);
			return ret;
		}
		protected override string ConvertToString(object o)
		{
			return o.ToString();
		}
		private static ulong Parse(string s)
		{
			if (s.StartsWith("0x"))
			{
				return ulong.Parse(s.Substring(2), NumberStyles.HexNumber);
			}
			else
			{
				return ulong.Parse(s);
			}
		}
	}
	public class MednaDoubleDescriptor : MednaPropertyDescriptor
	{
		public MednaDoubleDescriptor(SettingT s) : base(s) {}
		public override Type PropertyType => typeof(double);
		protected override object ConvertFromString(string s)
		{
			var ret = double.Parse(s);
			if (Setting.Min != null && ret < double.Parse(Setting.Min) || Setting.Max != null && ret > double.Parse(Setting.Max))
				ret = double.Parse(Setting.DefaultValue);
			return ret;
		}
		protected override string ConvertToString(object o)
		{
			return o.ToString();
		}
	}

	public class PortPropertyDescriptor : PropertyDescriptor
	{
		public Port Port { get; private set; }
		public int PortIndex { get; private set; }
		public PortPropertyDescriptor(Port port, int index)
			: base(port.Name, new Attribute[0])
		{
			Port = port;
			PortIndex = index;
		}

		public override string Name => Port.Name;
		public override string DisplayName => Port.Name;
		public override string Description => $"Change the device plugged into {Port.Name}";
		public override string Category => "Ports";

		public override Type ComponentType => typeof(NymaSyncSettings);
		public override bool IsReadOnly => false;
		public override Type PropertyType => typeof(string);
		public override bool CanResetValue(object component) => true;

		public override object GetValue(object component)
		{
			var ss = (NymaSyncSettings)component;
			if (!ss.PortDevices.TryGetValue(PortIndex, out var val))
				val = Port.DefaultSettingsValue;
			return val;
		}

		public override void ResetValue(object component)
		{
			((NymaSyncSettings)component).PortDevices.Remove(PortIndex);
		}

		public override void SetValue(object component, object value)
		{
			if ((string)value == Port.DefaultSettingsValue)
			{
				ResetValue(component);
			}
			else if (!Port.AllowedDevices.Any(d => d.SettingValue == (string)value))
			{
				// does not validate
			}
			else
			{
				((NymaSyncSettings)component).PortDevices[PortIndex] = (string)value;
			}
		}

		public override bool ShouldSerializeValue(object component)
		{
			return ((NymaSyncSettings)component).PortDevices.ContainsKey(PortIndex);
		}

		public override TypeConverter Converter => new MyTypeConverter { Port = Port };

		private class MyTypeConverter : TypeConverter
		{
			public Port Port { get; set; }

			public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);
			public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string);
			public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
			{
				return Port.AllowedDevices
					.SingleOrDefault(d => d.Name == (string)value)
					?.SettingValue
					?? Port.DefaultSettingsValue;

			}
			public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
			{
				return Port.AllowedDevices
					.SingleOrDefault(d => d.SettingValue == (string)value)
					?.Name
					?? Port.AllowedDevices
					.Single(d => d.SettingValue == Port.DefaultSettingsValue)
					.Name;
			}

			public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) => new StandardValuesCollection(
				Port.AllowedDevices.Select(d => d.SettingValue).ToList()
			);

			public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true;

			public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
		}
	}

	public class LayerPropertyDescriptor : PropertyDescriptor
	{
		public string LayerName { get; private set; }
		public LayerPropertyDescriptor(string layerName)
			: base(layerName, new Attribute[0])
		{
			LayerName = layerName;
		}

		public override string Name => LayerName;
		public override string DisplayName => $"Show {LayerName}";
		public override string Description => null;
		public override string Category => "Layers";

		public override Type ComponentType => typeof(NymaSettings);
		public override bool IsReadOnly => false;
		public override Type PropertyType => typeof(bool);
		public override bool CanResetValue(object component) => true;

		public override object GetValue(object component)
		{
			return !((NymaSettings)component).DisabledLayers.Contains(LayerName);
		}

		public override void ResetValue(object component)
		{
			((NymaSettings)component).DisabledLayers.Remove(LayerName);
		}

		public override void SetValue(object component, object value)
		{
			if ((bool)value)
				((NymaSettings)component).DisabledLayers.Remove(LayerName);
			else
				((NymaSettings)component).DisabledLayers.Add(LayerName);
		}

		public override bool ShouldSerializeValue(object component)
		{
			return ((NymaSettings)component).DisabledLayers.Contains(LayerName);
		}
	}
}
