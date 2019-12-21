namespace BizHawk.Client.Common
{
	public class MessageOption
	{
		public int X { get; set; }
		public int Y { get; set; }
		public int Anchor { get; set; } // TODO: make an enum 0 = UL, 1 = UR, 2 = DL, 3 = DR

		public MessageOption Clone()
		{
			return (MessageOption)MemberwiseClone();
		}
	}

	public static class DefaultMessageOptions
	{
		public static MessageOption Fps = new MessageOption { X = 0, Y = 0 };
		public static MessageOption FrameCounter = new MessageOption { X = 0, Y = 14 };
		public static MessageOption LagCounter = new MessageOption { X = 0, Y = 42 };
		public static MessageOption InputDisplay = new MessageOption { X = 0, Y = 28 };
		public static MessageOption ReRecordCounter = new MessageOption { X = 0, Y = 56 };
		public static MessageOption MultitrackRecorder = new MessageOption { X = 0, Y = 14, Anchor = 1 };
		public static MessageOption Messages = new MessageOption { X = 0, Y = 0, Anchor = 2 };
		public static MessageOption Autohold = new MessageOption { X = 0, Y = 0, Anchor = 1 };
		public static MessageOption RamWatches = new MessageOption { X = 0, Y = 70 };

		public const int
			MessagesColor = -1,
			AlertMessageColor = -65536,
			LastInputColor = -23296,
			MovieInput = -8355712;
	}
}
