namespace BizHawk.Client.Common
{
	/// <summary>
	/// This enum specify the size of a <see cref="Watch"/>
	/// </summary>
	public enum WatchSize
	{
		/// <summary>
		/// One byte (8 bits)
		/// Use this for <see cref="ByteWatch"/>
		/// </summary>
		Byte = 1,

		/// <summary>
		/// 2 bytes (16 bits)
		/// Use this for <see cref="WordWatch"/>
		/// </summary>
		Word = 2,

		/// <summary>
		/// 4 bytes (32 bits)
		/// Use this for <see cref="DWordWatch"/>
		/// </summary>
		DWord = 4,

		/// <summary>
		/// Special case used for a separator in ram tools
		/// Use this for <see cref="SeparatorWatch"/>
		/// </summary>
		Separator = 0
	}
}
