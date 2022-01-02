#nullable enable

using System.Collections.Generic;
using System.IO;

#if EXE_PROJECT
namespace EXE_PROJECT.PathExtensions // Use a different namespace so the executable can still use this class' members without an implicit dependency on the BizHawk.Common library, and without resorting to code duplication.
#else
namespace BizHawk.Common.PathExtensions
#endif
{
	public static class MorePathExtensions
	{
		private const int MAX_DEPTH = 20;

		/// <summary>as <see cref="BreadthFirstSearch(System.IO.DirectoryInfo,string,int?)"/>, but files matching any of <paramref name="filenameGlobs"/> are returned</summary>
		public static IEnumerable<FileInfo> BreadthFirstSearch(this DirectoryInfo root, IReadOnlyCollection<string> filenameGlobs, int? maxDepth = null)
		{
			var realMaxDepth = maxDepth ?? MAX_DEPTH;
			Queue<(DirectoryInfo, int)> q = new(new[] { (root, 0) });
			while (q.Count is not 0)
			{
				var (di, depth) = q.Dequeue();
				if (depth < realMaxDepth) foreach (var diSub in di.GetDirectories()) q.Enqueue((diSub, depth + 1));
				foreach (var glob in filenameGlobs) //TODO maybe enumerate once and then apply filter to name? is the shell faster?
				{
					foreach (var fi in di.EnumerateFiles(glob)) yield return fi;
				}
			}
		}

		/// <summary>
		/// Enumerates files (not including directories) below <paramref name="root"/> to the given <paramref name="maxDepth"/>.<br/>
		/// Files at a lower depth are returned first. They are not lexically ordered.
		/// </summary>
		/// <remarks><paramref name="root"/> must exist</remarks>
		public static IEnumerable<FileInfo> BreadthFirstSearch(this DirectoryInfo root, string? filenameGlob = null, int? maxDepth = null)
		{
			var realMaxDepth = maxDepth ?? MAX_DEPTH;
			Queue<(DirectoryInfo, int)> q = new(new[] { (root, 0) });
			while (q.Count is not 0)
			{
				var (di, depth) = q.Dequeue();
				if (depth < realMaxDepth) foreach (var diSub in di.GetDirectories()) q.Enqueue((diSub, depth + 1));
				foreach (var fi in filenameGlob is null ? di.EnumerateFiles() : di.EnumerateFiles(filenameGlob)) yield return fi;
			}
		}
	}
}
