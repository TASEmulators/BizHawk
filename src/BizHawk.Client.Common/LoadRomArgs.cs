namespace BizHawk.Client.Common
{
	public sealed class LoadRomArgs(IOpenAdvanced ioa, bool? deterministic = null)
	{
		public readonly bool? Deterministic = deterministic;

		public readonly IOpenAdvanced OpenAdvanced = ioa;
	}
}
