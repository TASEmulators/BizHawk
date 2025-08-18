namespace BizHawk.Emulation.Common
{
#pragma warning disable CA1715 // breaks IInterface convention
	public interface AxisConstraint
#pragma warning restore CA1715
	{
		public string? Class { get; }

		public string? PairedAxis { get; }
	}
}
