using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common.InputAdapterExtensions
{
	public static class InputAdapterExtensions
	{
		/// <summary>
		/// Creates a new IController that is in a state of a bitwise And of the source and target controllers
		/// </summary>
		public static IController And(this IController source, IController target)
		{
			return new AndAdapter
			{
				Source = source,
				SourceAnd = target
			};
		}

		/// <summary>
		/// Creates a new IController that is in a state of a bitwise Or of the source and target controllers
		/// </summary>
		public static IController Or(this IController source, IController target)
		{
			return new ORAdapter
			{
				Source = source,
				SourceOr = target
			};
		}
	}
}
