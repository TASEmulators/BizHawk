namespace BizHawk.Common
{
	/// <summary>Indicates that a string value is formatted as a path, with an extension to the format: paths followed by <c>'|'</c> and then a relative path represent a member of an archive file.</summary>
	/// <remarks>
	/// The archive's path may be absolute or relative. If the path doesn't specify a member (it's a regular path), it obviously may also be absolute or relative.<br/>
	/// The last '|' is the separator if multiple appear in the path, but the behaviour of such paths generally is undefined. Warnings may be printed on Debug builds.<br/>
	/// Paths are still OS-dependent. <c>C:\path\to\file</c> and <c>C:\path\to\archive|member</c> are valid on Windows, <c>/path/to/file</c> and <c>/path/to/archive|member</c> are valid everywhere else.<br/>
	/// This attribute is for humans.<br/>
	/// TODO how are local (<c>\\?\C:\file.txt</c>) and remote (<c>\\?\UNC\Server\Share\file.txt</c>) UNCs treated by WinForms, and are we able to handle at least the valid ones? --yoshi
	/// </remarks>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
	public sealed class HawkFilePathAttribute : Attribute {}
}
