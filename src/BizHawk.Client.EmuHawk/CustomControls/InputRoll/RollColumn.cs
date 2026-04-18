#nullable enable

using Newtonsoft.Json;

namespace BizHawk.Client.EmuHawk
{
	public class RollColumn
	{
		public int VerticalWidth { get; }
		public int HorizontalHeight { get; }
		public int Width { get; set; }
		public int Left { get; set; }
		public int Right { get; set; }

		/// <remarks>TODO rename to <c>Key</c>?</remarks>
		public string Name { get; }

		/// <remarks>TODO rename to <c>Label</c>?</remarks>
		public string Text { get; }

		public bool Visible { get; set; } = true;

		/// <summary>
		/// Column will be drawn with an emphasized look, if true
		/// </summary>
		public bool Emphasis { get; set; }

		/// <summary>
		/// Column text will be drawn rotated if true
		/// </summary>
		public bool Rotatable { get; set; }

		[JsonConstructor]
		private RollColumn(string name, string text, int verticalWidth, int horizontalHeight)
		{
			Name = name;
			Text = text;
			VerticalWidth = verticalWidth;
			HorizontalHeight = horizontalHeight;
		}

		public RollColumn(string name, int widthUnscaled, string text)
			: this(name, widthUnscaled, widthUnscaled, text) { }

		public RollColumn(string name, int verticalWidth, int horizontalHeight, string text)
		{
			Name = name;
			Text = text;
			VerticalWidth = UIHelper.ScaleX(verticalWidth);
			HorizontalHeight = UIHelper.ScaleX(horizontalHeight);
			Width = VerticalWidth;
		}
	}
}
