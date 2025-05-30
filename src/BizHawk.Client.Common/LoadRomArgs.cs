namespace BizHawk.Client.Common
{
	public sealed record class LoadRomArgs(
		IOpenAdvanced OpenAdvanced,
		string/*?*/ ForcedSysID = null,
		bool? Deterministic = null);
}
