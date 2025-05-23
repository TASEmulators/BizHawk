﻿#nullable enable

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
		public string Name { get; private set; }

		/// <remarks>TODO rename to <c>Label</c>?</remarks>
		public string Text { get; private set; }

		public ColumnType Type { get; private set; }

		public bool Visible { get; set; } = true;

		/// <summary>
		/// Column will be drawn with an emphasized look, if true
		/// </summary>
		public bool Emphasis { get; set; }

		/// <summary>
		/// Column text will be drawn rotated if true
		/// </summary>
		public bool Rotatable { get; set; }

//		[JsonConstructor]
		private RollColumn()
		{
			Name = default!;
			Text = default!;
		}

		public RollColumn(string name, int widthUnscaled, ColumnType type, string text)
			: this(name, widthUnscaled, widthUnscaled, type, text) { }

		public RollColumn(string name, int verticalWidth, int horizontalHeight, ColumnType type, string text)
		{
			Name = name;
			Text = text;
			Type = type;
			VerticalWidth = UIHelper.ScaleX(verticalWidth);
			HorizontalHeight = UIHelper.ScaleX(horizontalHeight);
			Width = VerticalWidth;
		}

		public RollColumn(string name, int widthUnscaled, string text)
			: this(name: name, widthUnscaled: widthUnscaled, type: ColumnType.Text, text: text) {}
	}
}
