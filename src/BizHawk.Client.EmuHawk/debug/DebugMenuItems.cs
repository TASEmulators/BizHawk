#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk.ForDebugging
{
	internal sealed class DebugVSystemChildItem : ToolStripMenuItemEx
	{
		public string? RequiresCore = null;

		public bool RequiresLoadedRom = true;

		public DebugVSystemChildItem(string labelText, Action onClick)
		{
			Text = labelText;
			Click += (_, _) => onClick();
		}
	}

	internal sealed class DebugVSystemMenuItem : ToolStripMenuItemEx
	{
		public readonly IReadOnlyCollection<string> SysIDs;

		public DebugVSystemMenuItem(string sysID, params string[] extraSysIDs)
		{
			SysIDs = extraSysIDs.Prepend(sysID).ToHashSet();
			Text = sysID;
		}
	}

	internal static class GenControl
	{
		public static Button Button(string text, int width, Action onClick)
		{
			SzButtonEx btn = new() { Size = new(width, 23), Text = text };
			btn.Click += (_, _) => onClick();
			return btn;
		}
	}
}
