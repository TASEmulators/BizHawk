using System.Windows.Forms;
using System.Windows.Forms.Automation;

using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	public static class WinFormsUIAutomation
	{
		public static bool ScreenReaderAnnounce(string message, Form form)
			=> form.AccessibilityObject.RaiseAutomationNotification(
				AutomationNotificationKind.Other,
				AutomationNotificationProcessing.All,
				message);
	}

	public static class WinFormsScreenReaderExtensions
	{
		public static bool SafeScreenReaderAnnounce(this Form form, string message)
			=> OSTailoredCode.HostWindowsVersion?.Version >= OSTailoredCode.WindowsVersion.XP
				? WinFormsUIAutomation.ScreenReaderAnnounce(message, form)
				: true; // under Mono (NixOS): `TypeLoadException: Could not resolve type with token 01000434 from typeref (expected class '[...].AutomationNotificationKind' in assembly 'System.Windows.Forms, Version=4.0.0.0 [...]')`
	}
}
