namespace BizHawk.Common
{
	/// <summary>Used by <see cref="HawkFile"/> to delegate archive management.</summary>
	public interface IFileDearchivalMethod<out T> where T : IHawkArchiveFile
	{
		/// <remarks>TODO could this receive a <see cref="HawkFile"/> itself? possibly handy, in very clever scenarios of mounting fake files</remarks>
		bool CheckSignature(string fileName, out int offset, out bool isExecutable);

		T Construct(string path);
	}
}
