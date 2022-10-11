using System;

namespace BizHawk.Emulation.Common
{
	[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public sealed class GenEmuServicePropAttribute : Attribute
	{
		public const string SETTER_DEPR_MSG = "only service provider should be writing to this";

		public bool IsOptional { get; set; } = false;

		public readonly string PropName;

		public readonly Type Service;

		public GenEmuServicePropAttribute(Type service, string propName)
		{
			PropName = propName;
			Service = service;
		}
	}
}
