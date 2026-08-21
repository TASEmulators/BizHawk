using System.Windows.Forms;
using System.Windows.Forms.Automation;

using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	public static class WinFormsUIAutomation
	{
		// Throttle screen reader announcements to prevent overwhelming NVDA/other readers
		private static DateTime _lastAnnouncementTime = DateTime.MinValue;
		private static string _pendingMessage = null;
		private static readonly object _announceLock = new object();

		// Minimum time between announcements (milliseconds)
		private const int ANNOUNCEMENT_THROTTLE_MS = 150;

		/// <summary>
		/// Whether screen reader announcements are enabled.
		/// Can be toggled off for users who experience performance issues.
		/// </summary>
		public static bool AnnouncementsEnabled { get; set; } = true;

		public static bool ScreenReaderAnnounce(string message, Form form)
		{
			if (!AnnouncementsEnabled || string.IsNullOrEmpty(message))
				return true;

			lock (_announceLock)
			{
				var now = DateTime.UtcNow;
				var elapsed = (now - _lastAnnouncementTime).TotalMilliseconds;

				if (elapsed < ANNOUNCEMENT_THROTTLE_MS)
				{
					// Queue this message - it will be announced on next non-throttled call
					_pendingMessage = message;
					return true;
				}

				// Use pending message if we have one, otherwise use current message
				var messageToAnnounce = _pendingMessage ?? message;
				_pendingMessage = null;
				_lastAnnouncementTime = now;

				try
				{
					// Use CurrentThenMostRecent to avoid flooding the screen reader
					// This processes the current notification and queues only the most recent
					return form.AccessibilityObject.RaiseAutomationNotification(
						AutomationNotificationKind.ActionCompleted,
						AutomationNotificationProcessing.CurrentThenMostRecent,
						messageToAnnounce);
				}
				catch
				{
					// Silently fail if screen reader is not available
					return true;
				}
			}
		}

		/// <summary>
		/// Forces an immediate announcement, bypassing throttle.
		/// Use sparingly for critical messages only.
		/// </summary>
		public static bool ScreenReaderAnnounceImmediate(string message, Form form)
		{
			if (!AnnouncementsEnabled || string.IsNullOrEmpty(message))
				return true;

			try
			{
				return form.AccessibilityObject.RaiseAutomationNotification(
					AutomationNotificationKind.ActionCompleted,
					AutomationNotificationProcessing.ImportantMostRecent,
					message);
			}
			catch
			{
				return true;
			}
		}
	}

	public static class WinFormsScreenReaderExtensions
	{
		public static bool SafeScreenReaderAnnounce(this Form form, string message)
			=> OSTailoredCode.HostWindowsVersion?.Version >= OSTailoredCode.WindowsVersion.XP
				? WinFormsUIAutomation.ScreenReaderAnnounce(message, form)
				: true; // under Mono (NixOS): `TypeLoadException: Could not resolve type with token 01000434 from typeref (expected class '[...].AutomationNotificationKind' in assembly 'System.Windows.Forms, Version=4.0.0.0 [...]')`

		public static bool SafeScreenReaderAnnounceImmediate(this Form form, string message)
			=> OSTailoredCode.HostWindowsVersion?.Version >= OSTailoredCode.WindowsVersion.XP
				? WinFormsUIAutomation.ScreenReaderAnnounceImmediate(message, form)
				: true;
	}
}
