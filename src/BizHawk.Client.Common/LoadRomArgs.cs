namespace BizHawk.Client.Common
{
	public sealed record class LoadRomArgs(
		IOpenAdvanced OpenAdvanced,
		bool? Deterministic = null);
}
