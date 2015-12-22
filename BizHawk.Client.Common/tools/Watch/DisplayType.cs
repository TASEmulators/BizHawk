namespace BizHawk.Client.Common
{
	/// <summary>
	/// This enum is used to specify how you want your <see cref="Watch"/> to be displayed
	/// </summary>
	public enum DisplayType
	{
		/// <summary>
		/// Separator, only used by <see cref="SeparatorWatch"/>
		/// </summary>
		Separator,
		/// <summary>
		/// Display the value as a signed integer
		/// Used by <see cref="ByteWatch"/>, <see cref="WordWatch"/> and <see cref="DWordWatch"/>
		/// </summary>
		Signed,
		/// <summary>
		/// Display the value as an unsigned integer
		/// Used by <see cref="ByteWatch"/>, <see cref="WordWatch"/> and <see cref="DWordWatch"/>
		/// </summary>
		Unsigned,
		/// <summary>
		/// Raw hexadecimal display
		/// Used by <see cref="ByteWatch"/>, <see cref="WordWatch"/> and <see cref="DWordWatch"/>
		/// </summary>
		Hex,
		/// <summary>
		/// Raw binary display
		/// Used by <see cref="ByteWatch"/>, <see cref="WordWatch"/> and <see cref="DWordWatch"/>
		/// If you can read it easily, you're probably a computer
		/// </summary>
		Binary,
		/// <summary>
		/// Display the value as fractionnal number. 12 before coma and 4 after
		/// Used only by <see cref="WordWatch"/> as it is 16 bits length
		/// </summary>
		FixedPoint_12_4,
		/// <summary>
		/// Display the value as fractionnal number. 20 before coma and 12 after
		/// Used only by <see cref="DWordWatch"/> as it is 32 bits length
		/// </summary>
		FixedPoint_20_12,
		/// <summary>
		/// Display the value as fractionnal number. 16 before coma and 16 after
		/// Used only by <see cref="DWordWatch"/> as it is 32 bits length
		/// </summary>
		FixedPoint_16_16,
		/// <summary>
		/// The traditionnal float type as in C++ <seealso cref="float"/>
		/// Used only by <see cref="DWordWatch"/> as it is 32 bits length
		/// </summary>
		Float
	}
}
