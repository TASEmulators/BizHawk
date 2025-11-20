namespace BizHawk.Emulation.Common
{
#pragma warning disable CA1715 // breaks IInterface convention
	public interface AxisConstraint
#pragma warning restore CA1715
	{
		string? Class { get; }

		string? PairedAxis { get; }
	}
}
