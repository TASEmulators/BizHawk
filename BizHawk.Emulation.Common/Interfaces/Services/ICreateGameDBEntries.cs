namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// This service provides a means for the current core to generate a
	/// game database entry and return the result
	/// If available, the client will expose functionality for the user to add the current rom
	/// to the user game database if it is currently unknown.  This UI should expose a feature
	/// that allow them to set to override the unknown status and set it to something they feel
	/// is more accurate. The intent of the feature is to easily allow users
	/// to mark unknown ROMs themselves (in their local database for their personal use)
	/// </summary>
	public interface ICreateGameDBEntries : ISpecializedEmulatorService
	{
		CompactGameInfo GenerateGameDbEntry();
	}
}
