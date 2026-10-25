using System;
using System.ComponentModel;

using Microsoft.CodeAnalysis;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// Apply this marker attribute to settings / sync settings types which use <see cref="DefaultValueAttribute">[DefaultValueAttribute]</see>.
	/// Then the Source Generator will make an overload of <c>BizHawk.Emulation.Cores.SettingsUtil.SetDefaultValues</c> for you to call.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	[Embedded]
	public sealed class CoreSettingsAttribute : Attribute {}
}
