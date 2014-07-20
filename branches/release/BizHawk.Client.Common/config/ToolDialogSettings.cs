using System.Drawing;
using Newtonsoft.Json;

namespace BizHawk.Client.Common
{
	public class ToolDialogSettings
	{
		private int? _wndx = null;
		private int? _wndy = null;

		public ToolDialogSettings()
		{
			SaveWindowPosition = true;
			FloatingWindow = true;
		}

		[JsonIgnore]
		public int? Wndx
		{
			get { return _wndx; }
			set
			{
				if (value != -32000)
				{
					_wndx = value;
				}
				
			}
		}

		[JsonIgnore]
		public int? Wndy
		{
			get { return _wndy; }
			set
			{
				if (value != -32000)
				{
					_wndy = value;
				}

			}
		}

		public int? Width { get; set; }
		public int? Height { get; set; }

		public bool SaveWindowPosition { get; set; }
		public bool TopMost { get; set; }
		public bool FloatingWindow { get; set; }

		[JsonIgnore]
		public bool UseWindowPosition
		{
			get
			{
				return SaveWindowPosition && Wndx.HasValue && Wndy.HasValue
					&& Wndx != -32000 && Wndy != -32000; // Windows OS annoyance, this is saved if the tool was minimized when closing
			}
		}

		[JsonIgnore]
		public bool UseWindowSize
		{
			get
			{
				return SaveWindowPosition && Width.HasValue && Height.HasValue;
			}
		}

		[JsonIgnore]
		public Point WindowPosition
		{
			get
			{
				return new Point(Wndx ?? 0, Wndy ?? 0);
			}
		}

		[JsonIgnore]
		public Size WindowSize
		{
			get
			{
				return new Size(Width ?? 0, Height ?? 0);
			}
		}
	}
}
