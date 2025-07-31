namespace BizHawk.Client.Common
{
	/// <summary>Indicates that a property is to be saved to config for persistence.</summary>
	[AttributeUsage(AttributeTargets.Property)]
	[Obsolete("Use interface IConfigPersist instead.")]
	public sealed class ConfigPersistAttribute : Attribute
	{
	}
}
