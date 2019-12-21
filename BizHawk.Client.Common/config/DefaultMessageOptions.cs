namespace BizHawk.Client.Common
{
	public class MessageOption
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

		public MessageOption Clone()
		{
			return (MessageOption)MemberwiseClone();
		}
	}

	public static class MessageOptionExtensions
	{
		public static bool IsTop(this MessageOption.AnchorType type)
		{
			return type == MessageOption.AnchorType.TopLeft
				|| type == MessageOption.AnchorType.TopRight;
		}

		public static bool IsLeft(this MessageOption.AnchorType type)
		{
			return type == MessageOption.AnchorType.TopLeft
				|| type == MessageOption.AnchorType.BottomLeft;
		}
	}

	public static class DefaultMessageOptions
	{
		public static MessageOption Fps = new MessageOption { X = 0, Y = 0 };
		public static MessageOption FrameCounter = new MessageOption { X = 0, Y = 14 };
		public static MessageOption LagCounter = new MessageOption { X = 0, Y = 42 };
		public static MessageOption InputDisplay = new MessageOption { X = 0, Y = 28 };
		public static MessageOption ReRecordCounter = new MessageOption { X = 0, Y = 56 };
		public static MessageOption MultitrackRecorder = new MessageOption { X = 0, Y = 14, Anchor = MessageOption.AnchorType.TopRight };
		public static MessageOption Messages = new MessageOption { X = 0, Y = 0, Anchor = MessageOption.AnchorType.BottomLeft };
		public static MessageOption Autohold = new MessageOption { X = 0, Y = 0, Anchor = MessageOption.AnchorType.TopRight };
		public static MessageOption RamWatches = new MessageOption { X = 0, Y = 70 };

		public const int
			MessagesColor = -1,
			AlertMessageColor = -65536,
			LastInputColor = -23296,
			MovieInput = -8355712;
	}
}
