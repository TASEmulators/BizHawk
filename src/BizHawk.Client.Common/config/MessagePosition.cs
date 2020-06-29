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
	}

	public static class DefaultMessagePositions
	{
		public static MessagePosition Fps = new MessagePosition { X = 0, Y = 0 };
		public static MessagePosition FrameCounter = new MessagePosition { X = 0, Y = 14 };
		public static MessagePosition LagCounter = new MessagePosition { X = 0, Y = 42 };
		public static MessagePosition InputDisplay = new MessagePosition { X = 0, Y = 28 };
		public static MessagePosition ReRecordCounter = new MessagePosition { X = 0, Y = 56 };
		public static MessagePosition Messages = new MessagePosition { X = 0, Y = 0, Anchor = MessagePosition.AnchorType.BottomLeft };
		public static MessagePosition Autohold = new MessagePosition { X = 0, Y = 0, Anchor = MessagePosition.AnchorType.TopRight };
		public static MessagePosition RamWatches = new MessagePosition { X = 0, Y = 70 };

		public const int
			MessagesColor = -1,
			AlertMessageColor = -65536,
			LastInputColor = -23296,
			MovieInput = -8355712;
	}
}
