using System;
using System.Text;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BizHawk
{
	public static class Extensions
	{
		//extension method to make Control.Invoke easier to use
		public static void Invoke(this Control control, Action action)
		{
			control.Invoke((Delegate)action);
		}
	}


	public static class ListViewExtensions
	{
		[StructLayout(LayoutKind.Sequential)]
		public struct HDITEM
		{
			public Mask mask;
			public int cxy;
			[MarshalAs(UnmanagedType.LPTStr)]
			public string pszText;
			public IntPtr hbm;
			public int cchTextMax;
			public Format fmt;
			public IntPtr lParam;
			// _WIN32_IE >= 0x0300 
			public int iImage;
			public int iOrder;
			// _WIN32_IE >= 0x0500
			public uint type;
			public IntPtr pvFilter;
			// _WIN32_WINNT >= 0x0600
			public uint state;

			[Flags]
			public enum Mask
			{
				Format = 0x4,       // HDI_FORMAT
			};

			[Flags]
			public enum Format
			{
				SortDown = 0x200,   // HDF_SORTDOWN
				SortUp = 0x400,     // HDF_SORTUP
			};
		};

		public const int LVM_FIRST = 0x1000;
		public const int LVM_GETHEADER = LVM_FIRST + 31;

		public const int HDM_FIRST = 0x1200;
		public const int HDM_GETITEM = HDM_FIRST + 11;
		public const int HDM_SETITEM = HDM_FIRST + 12;

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		static extern IntPtr SendMessage(IntPtr hWnd, UInt32 msg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		static extern IntPtr SendMessage(IntPtr hWnd, UInt32 msg, IntPtr wParam, ref HDITEM lParam);

		public static string CopyItemsAsText(this ListView listViewControl)
		{
			ListView.SelectedIndexCollection indexes = listViewControl.SelectedIndices;
			if (indexes.Count <= 0)
				return "";

			StringBuilder sb = new StringBuilder();
			
			//walk over each selected item and subitem within it to generate a string from it
			foreach (int index in indexes)
			{
				foreach (ListViewItem.ListViewSubItem item in listViewControl.Items[index].SubItems)
				{
					if (!String.IsNullOrWhiteSpace(item.Text))
						sb.Append(item.Text).Append('\t');
				}
				//remove the last tab
				sb.Remove(sb.Length - 1, 1);

				sb.Append("\r\n");
			}

			//remove last newline
			sb.Length -= 2;

	
			return sb.ToString();
		}

		public static void SetSortIcon(this ListView listViewControl, int columnIndex, SortOrder order)
		{
			IntPtr columnHeader = SendMessage(listViewControl.Handle, LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);
			for (int columnNumber = 0; columnNumber <= listViewControl.Columns.Count - 1; columnNumber++)
			{
				var columnPtr = new IntPtr(columnNumber);
				var item = new HDITEM
				{
					mask = HDITEM.Mask.Format
				};

				if (SendMessage(columnHeader, HDM_GETITEM, columnPtr, ref item) == IntPtr.Zero)
				{
					throw new Win32Exception();
				}

				if (order != SortOrder.None && columnNumber == columnIndex)
				{
					switch (order)
					{
						case SortOrder.Ascending:
							item.fmt &= ~HDITEM.Format.SortDown;
							item.fmt |= HDITEM.Format.SortUp;
							break;
						case SortOrder.Descending:
							item.fmt &= ~HDITEM.Format.SortUp;
							item.fmt |= HDITEM.Format.SortDown;
							break;
					}
				}
				else
				{
					item.fmt &= ~HDITEM.Format.SortDown & ~HDITEM.Format.SortUp;
				}

				if (SendMessage(columnHeader, HDM_SETITEM, columnPtr, ref item) == IntPtr.Zero)
				{
					throw new Win32Exception();
				}
			}
		}
	}
}