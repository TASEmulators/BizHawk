using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Represents an input adapter, that can take in a source, manipulate it as needed
	/// and then represent the resulting state as an <see cref="IController"/>
	/// </summary>
	public interface IInputAdapter : IController
	{
		IController Source { get; set; }
	}

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
		/// Creates a new IController that is in a state of a bitwise Xor of the source and target controllers
		/// </summary>
		public static IController Xor(this IController source, IController target)
		{
			return new XorAdapter
			{
				Source = source,
				SourceXor = target
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
