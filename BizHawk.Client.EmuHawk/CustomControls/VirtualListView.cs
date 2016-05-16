using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	#region win32interop

	[StructLayout(LayoutKind.Sequential)]
	internal struct LvDispInfo 
	{
		public NmHdr Hdr;
		public LvItem Item;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct NmHdr 
	{
		public IntPtr HwndFrom;
		public IntPtr IdFrom;
		public int Code;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct NmItemActivate 
	{
		public NmHdr Hdr;
		public int Item;
		public int SubItem;
		public uint NewState;
		public uint OldState;
		public uint uChanged;
		public POINT Action;
		public uint lParam;
		public uint KeyFlags;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct RECT
	{
		public int Top;
		public int Left;
		public int Bottom;
		public int Right;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct NmCustomDrawInfo 
	{
		public NmHdr Hdr;
		public uint dwDrawStage;
		public IntPtr Hdc;
		public RECT Rect;
		public int dwItemSpec;
		public uint ItemState;
		public int lItemlParam;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct NmLvCustomDraw 
	{
		public NmCustomDrawInfo Nmcd;
		public int ClearText;
		public int ClearTextBackground;
		public int SubItem;
	}



	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	internal struct LvItem 
	{
		public uint Mask;
		public int Item;
		public int SubItem;
		public uint State;
		public uint StateMask;
		public IntPtr PszText;
		public int cchTextMax;
		public int Image;
		public IntPtr lParam;
		public int Indent;
	}

	[FlagsAttribute]
	internal enum CustomDrawReturnFlags 
	{
		CDRF_DODEFAULT = 0x00000000,
		CDRF_NEWFONT = 0x00000002,
		CDRF_SKIPDEFAULT = 0x00000004,
		CDRF_NOTIFYPOSTPAINT = 0x00000010,
		CDRF_NOTIFYITEMDRAW = 0x00000020,
		CDRF_NOTIFYSUBITEMDRAW = 0x00000020,
		CDRF_NOTIFYPOSTERASE = 0x00000040,
	}

	[FlagsAttribute]
	internal enum CustomDrawDrawStageFlags 
	{
		CDDS_PREPAINT = 0x00000001,
		CDDS_POSTPAINT = 0x00000002,
		CDDS_PREERASE = 0x00000003,
		CDDS_POSTERASE = 0x00000004,

		// the 0x000010000 bit means it's individual item specific
		CDDS_ITEM = 0x00010000,
		CDDS_ITEMPREPAINT = (CDDS_ITEM | CDDS_PREPAINT),
		CDDS_ITEMPOSTPAINT = (CDDS_ITEM | CDDS_POSTPAINT),
		CDDS_ITEMPREERASE = (CDDS_ITEM | CDDS_PREERASE),
		CDDS_ITEMPOSTERASE = (CDDS_ITEM | CDDS_POSTERASE),
		CDDS_SUBITEM = 0x00020000,
		CDDS_SUBITEMPREPAINT = (CDDS_SUBITEM | CDDS_ITEMPREPAINT),
		CDDS_SUBITEMPOSTPAINT = (CDDS_SUBITEM | CDDS_ITEMPOSTPAINT),
		CDDS_SUBITEMPREERASE = (CDDS_SUBITEM | CDDS_ITEMPREERASE),
		CDDS_SUBITEMPOSTERASE = (CDDS_SUBITEM | CDDS_ITEMPOSTERASE),
	}

	[FlagsAttribute]
	internal enum LvHitTestFlags 
	{
		LVHT_NOWHERE = 0x0001,
		LVHT_ONITEMICON = 0x0002,
		LVHT_ONITEMLABEL = 0x0004,
		LVHT_ONITEMSTATEICON = 0x0008,
		LVHT_ONITEM = (LVHT_ONITEMICON | LVHT_ONITEMLABEL | LVHT_ONITEMSTATEICON),

		LVHT_ABOVE = 0x0008,
		LVHT_BELOW = 0x0010,
		LVHT_TORIGHT = 0x0020,
		LVHT_TOLEFT = 0x0040
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct POINT 
	{
		public int X;
		public int Y;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal class LvHitTestInfo 
	{
		public POINT Point;
		public uint Flags;
		public int Item;
		public int SubItem;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct NMLISTVIEW 
	{
		public NmHdr hdr;
		public int iItem;
		public int iSubItem;
		public uint uNewState;
		public uint uOldState;
		public uint uChanged;
		public POINT ptAction;
		public IntPtr lParam;
	}


	internal enum ListViewItemMask : short 
	{
		LVIF_TEXT = 0x0001,
		LVIF_IMAGE = 0x0002,
		LVIF_PARAM = 0x0004,
		LVIF_STATE = 0x0008,

		LVIF_INDENT = 0x0010,
		LVIF_NORECOMPUTE = 0x0800,
		LVIF_GROUPID = 0x0100,
		LVIF_COLUMNS = 0x0200
	}

	internal enum LvNi 
	{
		ALL = 0x0000,
		FOCUSED = 0x0001,
		SELECTED = 0x0002,
		CUT = 0x0004,
		DROPHILITED = 0x0008,

		ABOVE = 0x0100,
		BELOW = 0x0200,
		TOLEFT = 0x0400,
		TORIGHT = 0x0800
	}

	internal enum ListViewMessages 
	{
		LVM_FIRST = 0x1000,
		LVM_GETITEMCOUNT = (LVM_FIRST + 4),
		LVM_SETCALLBACKMASK = (LVM_FIRST + 11),
		LVM_GETNEXTITEM = (LVM_FIRST + 12),
		LVM_HITTEST = (LVM_FIRST + 18),
		LVM_ENSUREVISIBLE = (LVM_FIRST + 19),
		LVM_SETITEMSTATE = (LVM_FIRST + 43),
		LVM_GETITEMSTATE = (LVM_FIRST + 44),
		LVM_SETITEMCOUNT = (LVM_FIRST + 47),
		LVM_GETSUBITEMRECT = (LVM_FIRST + 56)
	}

	internal enum ListViewStyles : short 
	{
		LVS_OWNERDATA = 0x1000,
		LVS_SORTASCENDING = 0x0010,
		LVS_SORTDESCENDING = 0x0020,
		LVS_SHAREIMAGELISTS = 0x0040,
		LVS_NOLABELWRAP = 0x0080,
		LVS_AUTOARRANGE = 0x0100
	}

	internal enum ListViewStylesICF : uint 
	{
		LVSICF_NOINVALIDATEALL = 0x00000001,
		LVSICF_NOSCROLL = 0x00000002
	}

	internal enum WindowsMessage : uint 
	{
		WM_ERASEBKGND = 0x0014,
		WM_SCROLL = 0x115,
		WM_LBUTTONDOWN = 0x0201,
		WM_LBUTTONUP = 0x0202,
		WM_LBUTTONDBLCLK = 0x0203,
		WM_RBUTTONDOWN = 0x0204,
		WM_RBUTTONUP = 0x0205,
		WM_RBUTTONDBLCLK = 0x0206,
		WM_SETFOCUS = 0x0007,
		WM_NOTIFY = 0x004E,
		WM_USER = 0x0400,
		WM_REFLECT = WM_USER + 0x1c00
	}

	internal enum Notices
	{
		NM_FIRST = 0,
		NM_CUSTOMDRAW = NM_FIRST - 12,
		NM_CLICK = NM_FIRST - 2,
		NM_DBLCLICK = NM_FIRST - 3,
	}

	internal enum ListViewNotices
	{
		LVN_FIRST = (0 - 100),
		LVN_LAST = (0 - 199),
		LVN_BEGINDRAG = LVN_FIRST - 9,
		LVN_BEGINRDRAG = LVN_FIRST - 11,
		LVN_GETDISPINFOA = LVN_FIRST - 50,
		LVN_GETDISPINFOW = LVN_FIRST - 77,
		LVN_SETDISPINFOA = LVN_FIRST - 51,
		LVN_SETDISPINFOW = LVN_FIRST - 78,
		LVN_ODCACHEHINT = LVN_FIRST - 13,
		LVN_ODFINDITEMW = LVN_FIRST - 79
	}

	[Flags]
	internal enum ListViewCallBackMask : uint 
	{
		LVIS_FOCUSED = 0x0001,
		LVIS_SELECTED = 0x0002,
		LVIS_CUT = 0x0004,
		LVIS_DROPHILITED = 0x0008,
		LVIS_GLOW = 0x0010,
		LVIS_ACTIVATING = 0x0020,

		LVIS_OVERLAYMASK = 0x0F00,
		LVIS_STATEIMAGEMASK = 0xF000,
	}

	#endregion

	#region VirtualListView Delegates

	/// <summary>
	/// Retrieve the background color for a Listview cell (item and subitem).
	/// </summary>
	/// <param name="item">Listview item (row).</param>
	/// <param name="subItem">Listview subitem (column).</param>
	/// <param name="color">Background color to use</param>
	public delegate void QueryItemBkColorHandler(int item, int subItem, ref Color color);

	/// <summary>
	/// Retrieve the text for a Listview cell (item and subitem).
	/// </summary>
	/// <param name="item">Listview item (row).</param>
	/// <param name="subItem">Listview subitem (column).</param>
	/// <param name="text">Text to display.</param>
	public delegate void QueryItemTextHandler(int item, int subItem, out string text);

	/// <summary>
	/// Retrieve the image index for a Listview item.
	/// </summary>
	/// <param name="item">Listview item (row).</param>
	/// <param name="subItem">Listview subitem (column) - should always be zero.</param>
	/// <param name="imageIndex">Index of associated ImageList.</param>
	public delegate void QueryItemImageHandler(int item, int subItem, out int imageIndex);

	/// <summary>
	/// Retrieve the indent for a Listview item.  The indent is always width of an image.
	/// For example, 1 indents the Listview item one image width.
	/// </summary>
	/// <param name="item">Listview item (row).</param>
	/// <param name="itemIndent">The amount to indent the Listview item.</param>
	public delegate void QueryItemIndentHandler(int item, out int itemIndent);

	public delegate void QueryItemHandler(int idx, out ListViewItem item);

	#endregion

	/// <summary>
	/// VirtualListView is a virtual Listview which allows for a large number of items(rows)
	/// to be displayed.  The virtual Listview contains very little actual information -
	/// mainly item selection and focus information.
	/// </summary>
	public class VirtualListView : ListView 
	{
		// store the item count to prevent the call to SendMessage(LVM_GETITEMCOUNT)
		private int _itemCount;

		#region Display query callbacks

		/// <summary>
		/// Fire the QueryItemBkColor event which requests the background color for the passed Listview cell
		/// </summary>
		public event QueryItemBkColorHandler QueryItemBkColor;

		/// <summary>
		/// Fire the QueryItemText event which requests the text for the passed Listview cell.
		/// </summary>
		[Category("Data")]
		public event QueryItemTextHandler QueryItemText;
		
		/// <summary>
		/// Fire the QueryItemImage event which requests the ImageIndex for the passed Listview item.
		/// </summary>
		[Category("Data")]
		public event QueryItemImageHandler QueryItemImage;
		
		/// <summary>
		/// Fire the QueryItemIndent event which requests the indent for the passed Listview item.
		/// </summary>
		[Category("Data")]
		public event QueryItemIndentHandler QueryItemIndent;
		
		[Category("Data")]
		public event QueryItemHandler QueryItem;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the sets the virtual number of items to be displayed.
		/// </summary>
		[Category("Behavior")]
		public int ItemCount 
		{
			get 
			{
				return _itemCount;
			}

			set 
			{
				_itemCount = value;

				// If the virtual item count is set before the handle is created
				// then the image lists don't get loaded properly
				if (!IsHandleCreated)
				{
					return;
				}

				SetVirtualItemCount();
			}
		}

		/// <summary>
		/// Gets or sets how list items are to be displayed.
		/// Hide the ListView.View property.
		/// Virtual Listviews only allow Details or List.
		/// </summary>
		public new View View 
		{
			get 
			{
				return base.View;
			}

			set 
			{
				if (value == View.LargeIcon ||
					value == View.SmallIcon) 
				{
					throw new ArgumentException("Icon views are invalid for virtual ListViews", "View");
				}

				base.View = value;
			}
		}

		/// <summary>
		/// Gets the required creation parameters when the control handle is created.
		/// Use LVS_OWNERDATA to set this as a virtual Listview.
		/// </summary>
		protected override CreateParams CreateParams 
		{
			get 
			{
				var cp = base.CreateParams;
				
				// LVS_OWNERDATA style must be set when the control is created
				cp.Style |= (int)ListViewStyles.LVS_OWNERDATA;
				return cp;
			}
		}

		#endregion

		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public int LineHeight { get; private set; }

		[Browsable(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public int NumberOfVisibleRows
		{
			get
			{
				return Height / LineHeight;
			}
		}

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="VirtualListView"/> class. 
		/// Create a new instance of this control.
		/// </summary>
		public VirtualListView() 
		{
			// virtual listviews must be Details or List view with no sorting
			View = View.Details;
			Sorting = SortOrder.None;

			UseCustomBackground = true;

			ptrlvhti = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(LvHitTestInfo)));

			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			SetStyle(ControlStyles.Opaque, true);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);

			LineHeight = this.Font.Height + 5;
		}

		~VirtualListView() 
		{
			Marshal.FreeHGlobal(ptrlvhti);
		}

		#endregion

		#region Methods
		/// <summary>
		/// Set the state of the passed Listview item's index.
		/// </summary>
		/// <param name="index">Listview item's index.</param>
		/// <param name="selected">Select the passed item?</param>
		public void SelectItem(int index, bool selected) 
		{
			var ptrItem = IntPtr.Zero;

			try 
			{
				// Determine whether selecting or unselecting.
				uint select = selected ? (uint)ListViewCallBackMask.LVIS_SELECTED : 0;

				// Fill in the LVITEM structure with state fields.
				var stateItem = new LvItem
				{
					Mask = (uint)ListViewItemMask.LVIF_STATE,
					Item = index,
					SubItem = 0,
					State = @select,
					StateMask = (uint)ListViewCallBackMask.LVIS_SELECTED
				};

				// Copy the structure to unmanaged memory.
				ptrItem = Marshal.AllocHGlobal(Marshal.SizeOf(stateItem.GetType()));
				Marshal.StructureToPtr(stateItem, ptrItem, true);

				// Send the message to the control window.
				Win32.SendMessage(
					this.Handle,
					(int)ListViewMessages.LVM_SETITEMSTATE,
					index,
					ptrItem.ToInt32());
			} 
			catch (Exception ex) 
			{
				System.Diagnostics.Trace.WriteLine("VirtualListView.SetItemState error=" + ex.Message);
				
				// TODO: should this eat any exceptions?
				throw;
			} 
			finally 
			{
				// Always release the unmanaged memory.
				if (ptrItem != IntPtr.Zero) 
				{
					Marshal.FreeHGlobal(ptrItem);
				}
			}
		}

		private void SetVirtualItemCount() 
		{
			Win32.SendMessage(
				this.Handle,
				(int)ListViewMessages.LVM_SETITEMCOUNT,
				this._itemCount,
				0);
		}

		protected void OnDispInfoNotice(ref Message m, bool useAnsi)
		{
			var info = (LvDispInfo)m.GetLParam(typeof(LvDispInfo));

			if ((info.Item.Mask & (uint)ListViewItemMask.LVIF_TEXT) > 0)
			{
				if (QueryItemText != null)
				{
					string lvtext;
					QueryItemText(info.Item.Item, info.Item.SubItem, out lvtext);
					if (lvtext != null)
					{
						try
						{
							int maxIndex = Math.Min(info.Item.cchTextMax - 1, lvtext.Length);
							var data = new char[maxIndex + 1];
							lvtext.CopyTo(0, data, 0, lvtext.Length);
							data[maxIndex] = '\0';
							Marshal.Copy(data, 0, info.Item.PszText, data.Length);
						}
						catch (Exception e)
						{
							Debug.WriteLine("Failed to copy text name from client: " + e, "VirtualListView.OnDispInfoNotice");
						}
					}
				}
			}

			if ((info.Item.Mask & (uint)ListViewItemMask.LVIF_IMAGE) > 0)
			{
				int imageIndex = 0;
				if (QueryItemImage != null)
				{
					QueryItemImage(info.Item.Item, info.Item.SubItem, out imageIndex);
				}

				info.Item.Image = imageIndex;
				Marshal.StructureToPtr(info, m.LParam, false);
			}

			if ((info.Item.Mask & (uint)ListViewItemMask.LVIF_INDENT) > 0)
			{
				int itemIndent = 0;
				if (QueryItemIndent != null)
				{
					QueryItemIndent(info.Item.Item, out itemIndent);
				}

				info.Item.Indent = itemIndent;
				Marshal.StructureToPtr(info, m.LParam, false);
			}

			m.Result = new IntPtr(0);
		}

		protected void OnCustomDrawNotice(ref Message m) 
		{
			var cd = (NmLvCustomDraw)m.GetLParam(typeof(NmLvCustomDraw));
			switch (cd.Nmcd.dwDrawStage) 
			{
				case (int)CustomDrawDrawStageFlags.CDDS_ITEMPREPAINT:
				case (int)CustomDrawDrawStageFlags.CDDS_PREPAINT:
					m.Result = new IntPtr((int)CustomDrawReturnFlags.CDRF_NOTIFYSUBITEMDRAW);
					break;
				case (int)CustomDrawDrawStageFlags.CDDS_SUBITEMPREPAINT:
					if (QueryItemBkColor != null)
					{
						var color = Color.FromArgb(cd.ClearTextBackground & 0xFF, (cd.ClearTextBackground >> 8) & 0xFF, (cd.ClearTextBackground >> 16) & 0xFF);
						QueryItemBkColor(cd.Nmcd.dwItemSpec, cd.SubItem, ref color);
						cd.ClearTextBackground = (color.B << 16) | (color.G << 8) | color.R;
						Marshal.StructureToPtr(cd, m.LParam, false);
					}

					m.Result = new IntPtr((int)CustomDrawReturnFlags.CDRF_DODEFAULT);
					break;
			}
		}
		
		/// <summary>
		/// Event to be fired whenever the control scrolls
		/// </summary>
		public event ScrollEventHandler Scroll;
		protected virtual void OnScroll(ScrollEventArgs e)
		{
			var handler = this.Scroll;
			if (handler != null)
			{
				handler(this, e);
			}
		}

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern int GetScrollPos(IntPtr hWnd, Orientation nBar);

		/// <summary>
		/// Gets the Vertical Scroll position of the control.
		/// </summary>
		public int VScrollPos
		{
			get { return GetScrollPos(this.Handle, Orientation.Vertical); }
		}
		
		protected override void WndProc(ref Message m) 
		{
			var messageProcessed = false;
			switch (m.Msg)
			{
				case (int)WindowsMessage.WM_REFLECT + (int)WindowsMessage.WM_NOTIFY:
					var nm1 = (NmHdr)m.GetLParam(typeof(NmHdr));
					switch (nm1.Code)
					{
						case (int)Notices.NM_CUSTOMDRAW:
							OnCustomDrawNotice(ref m);
							messageProcessed = true;

							if (QueryItemBkColor == null || !UseCustomBackground)
							{
								m.Result = (IntPtr)0;
							}

							break;
						case (int)ListViewNotices.LVN_GETDISPINFOW:
							OnDispInfoNotice(ref m, false);
							messageProcessed = true;
							break;
						case (int)ListViewNotices.LVN_BEGINDRAG:
							OnBeginItemDrag(MouseButtons.Left, ref m);
							messageProcessed = true;
							break;
						case (int)ListViewNotices.LVN_BEGINRDRAG:
							OnBeginItemDrag(MouseButtons.Right, ref m);
							messageProcessed = true;
							break;
					}

					break;
				case (int)WindowsMessage.WM_SCROLL:
					// http://stackoverflow.com/questions/1851620/handling-scroll-event-on-listview-in-c-sharp
					OnScroll(new ScrollEventArgs((ScrollEventType)(m.WParam.ToInt32() & 0xffff), m.WParam.ToInt32()));
					break;
				case (int)WindowsMessage.WM_ERASEBKGND:
					if (BlazingFast)
					{
						messageProcessed = true;
						m.Result = new IntPtr(1);
					}

					break;
			}
			
			if (!messageProcessed) 
			{
				try 
				{
					base.WndProc(ref m);
				}
				catch (Exception ex)
				{
					Trace.WriteLine(string.Format("Message {0} caused an exception: {1}", m, ex.Message));
				}
			}
		}

		public bool BlazingFast { get; set; }
		public bool UseCustomBackground { get; set; }

		protected ListViewItem GetItem(int idx) 
		{
			ListViewItem item = null;
			if (QueryItem != null) 
			{
				QueryItem(idx, out item);
			}

			if (item == null) 
			{
				throw new ArgumentException("cannot find item " + idx + " via QueryItem event");
			}

			return item;
		}

		protected void OnBeginItemDrag(MouseButtons mouseButton, ref Message m) 
		{
			var info = (NMLISTVIEW)m.GetLParam(typeof(NMLISTVIEW));
			ListViewItem item = null;
			if (QueryItem != null) 
			{
				QueryItem(info.iItem, out item);
			}

			OnItemDrag(new ItemDragEventArgs(mouseButton, item));
		}

		protected override void OnHandleCreated(EventArgs e) 
		{
			base.OnHandleCreated(e);

			// ensure the value for ItemCount is sent to the control properly if the user set it 
			// before the handle was created
			SetVirtualItemCount();
		}

		protected override void OnHandleDestroyed(EventArgs e) 
		{
			// the ListView OnHandleDestroyed accesses the Items list for all selected items
			ItemCount = 0;
			base.OnHandleDestroyed(e);
		}

		#endregion

		LvHitTestInfo lvhti = new LvHitTestInfo();
		IntPtr ptrlvhti;
		int selection = -1;

		public int hitTest(int x, int y) 
		{
			lvhti.Point.X = x;
			lvhti.Point.Y = y;
			Marshal.StructureToPtr(lvhti, ptrlvhti, true);
			int z = Win32.SendMessage(this.Handle, (int)ListViewMessages.LVM_HITTEST, 0, ptrlvhti.ToInt32());
			Marshal.PtrToStructure(ptrlvhti, lvhti);
			return z;
		}

		public void ensureVisible(int index) 
		{
			Win32.SendMessage(Handle, (int)ListViewMessages.LVM_ENSUREVISIBLE, index, 1);
		}

		public void ensureVisible() 
		{
			ensureVisible(selectedItem);
		}

		public void setSelection(int index) 
		{
			clearSelection();
			selection = index;
			SelectItem(selection, true);
		}

		public int selectedItem
		{
			get
			{
				if (SelectedIndices.Count == 0)
				{
					return -1;
				}
				else
				{
					return SelectedIndices[0];
				}
			} 
			
			set
			{
				setSelection(value);
			}
		}

		public void clearSelection() 
		{
			if (selection != -1)
			{
				SelectItem(selection, false);
			}

			selection = -1;
		}

		// Informs user that a select all event is in place, can be used in change events to wait until this is false
		public bool SelectAllInProgress { get; set; }

		public void SelectAll()
		{
			this.BeginUpdate();
			SelectAllInProgress = true;

			for (var i = 0; i < _itemCount; i++)
			{
				if (i == _itemCount - 1)
				{
					SelectAllInProgress = false;
				}

				this.SelectItem(i, true);
			}

			this.EndUpdate();
		}

		public void DeselectAll()
		{
			this.BeginUpdate();
			SelectAllInProgress = true;

			for (var i = 0; i < _itemCount; i++)
			{
				if (i == _itemCount - 1)
				{
					SelectAllInProgress = false;
				}

				this.SelectItem(i, false);
			}

			this.EndUpdate();
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.A && e.Control && !e.Alt && !e.Shift) // Select All
			{
				SelectAll();
			}

			base.OnKeyDown(e);
		}
	}
}
