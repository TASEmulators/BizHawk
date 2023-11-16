namespace NLua.Native
{
	/// <summary>
	/// Operation value used by Arith method
	/// </summary>
	public enum LuaOperation
	{
		/// <summary>
		/// adition(+)
		/// </summary>
		Add = 0,
		/// <summary>
		///  substraction (-)
		/// </summary>
		Sub = 1,
		/// <summary>
		/// Multiplication (*)
		/// </summary>
		Mul = 2,

		/// <summary>
		/// Modulo (%)
		/// </summary>
		Mod = 3,

		/// <summary>
		/// Exponentiation (^)
		/// </summary>
		Pow = 4,
		/// <summary>
		///  performs float division (/)
		/// </summary>
		Div = 5,
		/// <summary>
		///  performs floor division (//)
		/// </summary>
		Idiv = 6,
		/// <summary>
		/// performs bitwise AND
		/// </summary>
		Band = 7,
		/// <summary>
		/// performs bitwise OR (|)
		/// </summary>
		Bor = 8,
		/// <summary>
		/// performs bitwise exclusive OR (~)
		/// </summary>
		Bxor = 9,
		/// <summary>
		/// performs left shift 
		/// </summary>
		Shl = 10,
		/// <summary>
		///  performs right shift
		/// </summary>
		Shr = 11,
		/// <summary>
		///  performs mathematical negation (unary -)
		/// </summary>
		Unm = 12,
		/// <summary>
		/// performs bitwise NOT (~)
		/// </summary>
		Bnot = 13,
	}
}
