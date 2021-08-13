#nullable enable // for when this file is embedded

using System;

namespace BizHawk.SrcGen.PeripheralOption
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public sealed class PeripheralOptionConsumerAttribute : Attribute
	{
		public object DefaultOption { get; }

		public Type EnumType { get; }

		public Type ImplSupertype { get; }

		public PeripheralOptionConsumerAttribute(Type enumType, Type implSupertype, object defaultOption)
		{
			DefaultOption = defaultOption;
			EnumType = enumType;
			ImplSupertype = implSupertype;
		}
	}

	[AttributeUsage(AttributeTargets.Enum)]
	public sealed class PeripheralOptionEnumAttribute : Attribute {}

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public sealed class PeripheralOptionImplAttribute : Attribute
	{
		public Type EnumType { get; }

		public object Option { get; }

		public PeripheralOptionImplAttribute(Type enumType, object option)
		{
			EnumType = enumType;
			Option = option;
		}
	}
}
