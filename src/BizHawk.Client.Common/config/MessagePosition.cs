namespace BizHawk.Client.Common
{
	public class MessagePosition
	{
		public int X { get; set; }
		public int Y { get; set; }
		public AnchorType Anchor { get; set; }

		public enum AnchorType
		{
			TopLeft = 0,
			TopRight = 1,
			BottomLeft = 2,
			BottomRight = 3
		}

		public MessagePosition Clone() => (MessagePosition)MemberwiseClone();
	}

	public static class MessagePositionExtensions
	{
		public static bool IsTop(this MessagePosition.AnchorType type)
		{
			return type == MessagePosition.AnchorType.TopLeft
				|| type == MessagePosition.AnchorType.TopRight;
		}

		public static bool IsLeft(this MessagePosition.AnchorType type)
		{
			return type == MessagePosition.AnchorType.TopLeft
				|| type == MessagePosition.AnchorType.BottomLeft;
		}

		public static string ToCoordinateStr(this MessagePosition position) => $"{position.X}, {position.Y}";
	}

	public static class DefaultMessagePositions
	{
		public static readonly MessagePosition Fps = new MessagePosition { X = 0, Y = 0 };
		public static readonly MessagePosition FrameCounter = new MessagePosition { X = 0, Y = 14 };
		public static readonly MessagePosition LagCounter = new MessagePosition { X = 0, Y = 42 };
		public static readonly MessagePosition InputDisplay = new MessagePosition { X = 0, Y = 28 };
		public static readonly MessagePosition ReRecordCounter = new MessagePosition { X = 0, Y = 56 };
		public static readonly MessagePosition Messages = new MessagePosition { X = 0, Y = 0, Anchor = MessagePosition.AnchorType.BottomLeft };
		public static readonly MessagePosition Autohold = new MessagePosition { X = 0, Y = 0, Anchor = MessagePosition.AnchorType.TopRight };
		public static readonly MessagePosition RamWatches = new MessagePosition { X = 0, Y = 70 };

		public const int
			MessagesColor = unchecked((int)0x_FF_FF_FF_FF),
			AlertMessageColor = unchecked((int)0x_FF_FF_00_00),
			LastInputColor = unchecked((int)0x_FF_FF_A5_00),
			MovieInputColor = unchecked((int)0x_FF_80_80_80);
	}
}
