namespace BizHawk.Client.Common
{
	/// <summary>Indicates which method of an <see cref="IToolFormAutoConfig"/> is to be called when the generated <c>Restore Defaults</c> menu item is clicked.</summary>
	/// <remarks>If not present on any instance method, the menu item will do nothing. If present on multiple, the first will be called.</remarks>
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class RestoreDefaultsAttribute : Attribute
	{
	}
}
