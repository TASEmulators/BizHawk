#nullable enable // for when this file is embedded

using System;

namespace BizHawk.Common
{
	/// <summary>
	/// Allows <see langword="abstract"/> methods in interfaces to be treated like <see langword="virtual"/> methods, similar to how they behave in classes.
	/// And the same for <see langword="abstract"/> property accessors and <see langword="abstract"/> events (accessors in interface events are an error without DIM, apply to event).
	/// </summary>
	/// <remarks>
	/// The base implementation can't be written into the interface, so it needs to be in a separate (usually inner) static class. A Source Generator will then add the necessary delegating method implementations at compile-time.<br/>
	/// These faux-<see langword="virtual"/> methods support the same <see langword="override"/>/<see langword="sealed"/> mechanisms that you'd expect of classes: just apply the keyword as usual.
	/// </remarks>
	/// <seealso cref="BaseImplMethodName"/>
	/// <seealso cref="ImplsClassFullName"/>
	[AttributeUsage(AttributeTargets.Event | AttributeTargets.Property | AttributeTargets.Method, Inherited = false)]
	public sealed class VirtualMethodAttribute : Attribute
	{
		/// <remarks>if unset, uses annotated method's name (with <c>_get</c>/<c>_set</c>/<c>_add</c>/<c>_remove</c> suffix for props/events)</remarks>
		public string? BaseImplMethodName { get; set; } = null;

		/// <remarks>if unset, uses <c>$"{interfaceFullName}.MethodDefaultImpls"</c></remarks>
		public string? ImplsClassFullName { get; set; } = null;
	}
}
