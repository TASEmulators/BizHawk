namespace BizHawk.Common
{
	/// <summary>Used by <see cref="IHawkArchiveFile"/> to represent archive members.</summary>
	public readonly struct HawkArchiveFileItem
	{
		/// <value>the index of the member within the archive, not to be confused with <see cref="Index"/></value>
		/// <remarks>this is for <see cref="IFileDearchivalMethod{T}"/> implementations to use internally</remarks>
		public readonly int ArchiveIndex;

		/// <value>the index of this archive item</value>
		public readonly int Index;

		/// <value>the member name</value>
		public readonly string Name;

		/// <value>the size of member file</value>
		public readonly long Size;

		public HawkArchiveFileItem(string name, long size, int index, int archiveIndex)
		{
			Name = name;
			Size = size;
			Index = index;
			ArchiveIndex = archiveIndex;
		}
	}
}
