#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Common.ReflectionExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public static class ControlExtensions
	{
		/// <exception cref="ArgumentException"><typeparamref name="T"/> does not inherit <see cref="Enum"/></exception>
		public static void PopulateFromEnum<T>(this ComboBox box, T enumVal)
			where T : Enum
		{
			box.Items.Clear();
			box.Items.AddRange(typeof(T).GetEnumDescriptions().Cast<object>().ToArray());
			box.SelectedItem = enumVal.GetDescription();
		}

		/// <summary>extension method to make <see cref="Control.Invoke(Delegate)"/> easier to use</summary>
		public static object Invoke(this Control control, Action action)
		{
			return control.Invoke(action);
		}

		/// <summary>extension method to make <see cref="Control.BeginInvoke(Delegate)"/> easier to use</summary>
		public static IAsyncResult BeginInvoke(this Control control, Action action)
		{
			return control.BeginInvoke(action);
		}

		public static ToolStripMenuItem ToColumnsMenu(this InputRoll inputRoll, Action changeCallback)
		{
			var menu = new ToolStripMenuItem
			{
				Name = "GeneratedColumnsSubMenu",
				Text = "Columns"
			};

			var columns = inputRoll.AllColumns;

			foreach (var column in columns)
			{
				var menuItem = new ToolStripMenuItem
				{
					Name = column.Name,
					Text = $"{column.Text} ({column.Name})",
					Checked = column.Visible,
					CheckOnClick = true,
					Tag = column.Name
				};

				menuItem.CheckedChanged += (o, ev) =>
				{
					var sender = (ToolStripMenuItem)o;
					columns.Find(c => c.Name == (string)sender.Tag).Visible = sender.Checked;
					columns.ColumnsChanged();
					changeCallback();
					inputRoll.Refresh();
				};

				menu.DropDownItems.Add(menuItem);
			}

			return menu;
		}

		public static Point ChildPointToScreen(this Control control, Control child)
		{
			return control.PointToScreen(new Point(child.Location.X, child.Location.Y));
		}

		public static Color Add(this Color color, int val)
		{
			var col = color.ToArgb();
			col += val;
			return Color.FromArgb(col);
		}

		/// <remarks>
		/// Due to the way this is written, using it in a foreach (as is done in SNESGraphicsDebugger)
		/// passes <c>Control</c> as the type parameter, meaning only properties on <see cref="Control"/> (and <see cref="Component"/>, etc.)
		/// will be processed. Why is there even a type param at all? I certainly don't know. --yoshi
		/// </remarks>
		public static T Clone<T>(this T controlToClone)
			where T : Control
		{
			PropertyInfo[] controlProperties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

			Type t = controlToClone.GetType();
			var instance = (T) Activator.CreateInstance(t);

			t.GetProperty("AutoSize")?.SetValue(instance, false, null);

			for (int i = 0; i < 3; i++) // why 3 passes of this? --yoshi
			{
				foreach (var propInfo in controlProperties)
				{
					if (!propInfo.CanWrite)
					{
						continue;
					}

					if (propInfo.Name != "AutoSize" && propInfo.Name != "WindowTarget")
					{
						propInfo.SetValue(instance, propInfo.GetValue(controlToClone, null), null);
					}
				}
			}

			if (controlToClone is RetainedViewportPanel rvpToClone && instance is RetainedViewportPanel rvpCloned)
			{
				rvpCloned.SetBitmap((Bitmap) rvpToClone.GetBitmap().Clone());
			}

			return instance;
		}

		/// <summary>
		/// Converts the outdated IEnumerable Controls property to an <see cref="IEnumerable{T}"/> like .NET should have done a long time ago
		/// </summary>
		public static IEnumerable<Control> Controls(this Control control)
			=> control.Controls.Cast<Control>();

		public static IEnumerable<TabPage> TabPages(this TabControl tabControl)
		{
			return tabControl.TabPages.Cast<TabPage>();
		}
	}

	public static class FormExtensions
	{
		public static void DoWithTempMute(this IDialogController dialogController, Action action)
		{
			dialogController.StopSound();
			action();
			dialogController.StartSound();
		}

		public static T DoWithTempMute<T>(this IDialogController dialogController, Func<T> action)
		{
			dialogController.StopSound();
			var ret = action();
			dialogController.StartSound();
			return ret;
		}

		/// <summary>
		/// Creates a <see cref="MessageBox"/> with the receiver (<paramref name="dialogParent"/>) as its parent, with the given <paramref name="text"/>,
		/// and with the given <paramref name="caption"/>, <paramref name="buttons"/>, and <paramref name="icon"/> if they're specified.
		/// </summary>
		public static DialogResult ModalMessageBox(
			this IDialogParent dialogParent,
			string text,
			string? caption = null,
			MessageBoxButtons? buttons = null,
			MessageBoxIcon? icon = null)
				=> dialogParent.DialogController.ShowMessageBox(
					owner: dialogParent,
					text: text,
					caption: caption,
					buttons: buttons,
					icon: icon);

		public static DialogResult ShowDialogAsChild(this IDialogParent dialogParent, CommonDialog dialog)
			=> dialog.ShowDialog(dialogParent.SelfAsHandle);

		public static DialogResult ShowDialogAsChild(this IDialogParent dialogParent, Form dialog)
			=> dialog.ShowDialog(dialogParent.SelfAsHandle);

		public static DialogResult ShowDialogWithTempMute(this IDialogParent dialogParent, CommonDialog dialog)
			=> dialogParent.DialogController.DoWithTempMute(() => dialog.ShowDialog(dialogParent.SelfAsHandle));

		public static DialogResult ShowDialogWithTempMute(this IDialogParent dialogParent, Form dialog)
			=> dialogParent.DialogController.DoWithTempMute(() => dialog.ShowDialog(dialogParent.SelfAsHandle));

		/// <summary>
		/// Creates a <see cref="MessageBox"/> without a parent, with the given <paramref name="text"/>,
		/// and with the given <paramref name="caption"/>, <paramref name="buttons"/>, and <paramref name="icon"/> if they're specified.
		/// </summary>
		public static DialogResult ShowMessageBox(
			this IDialogController dialogController,
			string text,
			string? caption = null,
			MessageBoxButtons? buttons = null,
			MessageBoxIcon? icon = null)
				=> dialogController.ShowMessageBox(
					owner: null,
					text: text,
					caption: caption,
					buttons: buttons,
					icon: icon);
	}

	public static class ListViewExtensions
	{
		/// <summary>
		/// Dumps the contents of the ListView into a tab separated list of lines
		/// </summary>
		public static string CopyItemsAsText(this ListView listViewControl)
		{
			var indexes = listViewControl.SelectedIndices;
			if (indexes.Count <= 0)
			{
				return "";
			}

			var sb = new StringBuilder();

			// walk over each selected item and subitem within it to generate a string from it
			foreach (int index in indexes)
			{
				foreach (ListViewItem.ListViewSubItem item in listViewControl.Items[index].SubItems)
				{
					if (!string.IsNullOrWhiteSpace(item.Text))
					{
						sb.Append(item.Text).Append('\t');
					}
				}

				// remove the last tab
				sb.Remove(sb.Length - 1, 1);

				sb.Append("\r\n");
			}

			// remove last newline
			sb.Length -= 2;


			return sb.ToString();
		}

		/// <exception cref="Win32Exception">unmanaged call failed</exception>
		public static void SetSortIcon(this ListView listViewControl, int columnIndex, SortOrder order)
		{
			const int LVM_GETHEADER = 4127;
			const int HDM_GETITEM = 4619;
			const int HDM_SETITEM = 4620;
			var columnHeader = Win32Imports.SendMessage(listViewControl.Handle, LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);
			for (int columnNumber = 0, l = listViewControl.Columns.Count; columnNumber < l; columnNumber++)
			{
				var columnPtr = new IntPtr(columnNumber);
				var item = new Win32Imports.HDITEM { mask = Win32Imports.HDITEM.Mask.Format };
				if (Win32Imports.SendMessage(columnHeader, HDM_GETITEM, columnPtr, ref item) == IntPtr.Zero) throw new Win32Exception();
				if (columnNumber != columnIndex || order == SortOrder.None)
				{
					item.fmt &= ~Win32Imports.HDITEM.Format.SortDown & ~Win32Imports.HDITEM.Format.SortUp;
				}
				else if (order == SortOrder.Ascending)
				{
					item.fmt &= ~Win32Imports.HDITEM.Format.SortDown;
					item.fmt |= Win32Imports.HDITEM.Format.SortUp;
				}
				else if (order == SortOrder.Descending)
				{
					item.fmt &= ~Win32Imports.HDITEM.Format.SortUp;
					item.fmt |= Win32Imports.HDITEM.Format.SortDown;
				}
				if (Win32Imports.SendMessage(columnHeader, HDM_SETITEM, columnPtr, ref item) == IntPtr.Zero) throw new Win32Exception();
			}
		}

		public static bool IsOk(this DialogResult dialogResult)
		{
			return dialogResult == DialogResult.OK;
		}

		/// <summary>
		/// Sets the desired effect if data is present, else None
		/// </summary>
		public static void Set(this DragEventArgs e, DragDropEffects effect)
		{
			e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop)
				? effect
				: DragDropEffects.None;
		}

		public static Bitmap ToBitMap(this Control control)
		{
			var b = new Bitmap(control.Width, control.Height);
			var rect = new Rectangle(new Point(0, 0), control.Size);
			control.DrawToBitmap(b, rect);
			return b;
		}

		public static void ToClipBoard(this Bitmap bitmap)
		{
			using var img = bitmap;
			Clipboard.SetImage(img);
		}

		public static void SaveAsFile(this Bitmap bitmap, IGameInfo game, string suffix, string systemId, PathEntryCollection paths, IDialogParent parent)
		{
			using var sfd = new SaveFileDialog
			{
				FileName = $"{game.FilesystemSafeName()}-{suffix}",
				InitialDirectory = paths.ScreenshotAbsolutePathFor(systemId),
				Filter = FilesystemFilterSet.Screenshots.ToString(),
				RestoreDirectory = true
			};

			if (parent.ShowDialogWithTempMute(sfd) != DialogResult.OK) return;

			var file = new FileInfo(sfd.FileName);
			string extension = file.Extension.ToUpper();
			ImageFormat i = extension switch
			{
				".BMP" => ImageFormat.Bmp,
				_ => ImageFormat.Png,
			};
			bitmap.Save(file.FullName, i);
		}

		public static void SetDistanceOrDefault(this SplitContainer splitter, int distance, int defaultDistance)
		{
			if (distance > 0)
			{
				try
				{
					splitter.SplitterDistance = distance;
				}
				catch (Exception)
				{
					splitter.SplitterDistance = defaultDistance;
				}
			}
		}

		public static bool IsPressed(this KeyEventArgs e, Keys key)
			=> !e.Alt && !e.Control && !e.Shift && e.KeyCode == key;

		public static bool IsShift(this KeyEventArgs e, Keys key)
			=> !e.Alt && !e.Control && e.Shift && e.KeyCode == key;

		public static bool IsCtrl(this KeyEventArgs e, Keys key)
			=> !e.Alt && e.Control && !e.Shift && e.KeyCode == key;

		public static bool IsAlt(this KeyEventArgs e, Keys key)
			=> e.Alt && !e.Control && !e.Shift && e.KeyCode == key;

		public static bool IsCtrlShift(this KeyEventArgs e, Keys key)
			=> !e.Alt && e.Control && e.Shift && e.KeyCode == key;

		/// <summary>
		/// Changes the description heigh area to match the rows needed for the largest description in the list
		/// </summary>
		public static void AdjustDescriptionHeightToFit(this PropertyGrid grid)
		{
			try
			{
				int maxLength = 0;
				string desc = "";

				foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(grid.SelectedObject))
				{
					var s = property.Description;
					if (s != null && s.Length > maxLength)
					{
						maxLength = s.Length;
						desc = s;
					}
				}

				foreach (Control control in grid.Controls)
				{
					if (control.GetType().Name == "DocComment")
					{
						var field = control.GetType().GetField("userSized", BindingFlags.Instance | BindingFlags.NonPublic);
						field?.SetValue(control, true);
						int height = (int)Graphics.FromHwnd(control.Handle).MeasureString(desc, control.Font, grid.Width).Height;
						control.Height = Math.Max(20, height) + 16; // magic for now
						return;
					}
				}
			}
			catch
			{
				// Eat it
			}
		}
	}
}
