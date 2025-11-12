using System.Collections.Generic;
using System.IO;

using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Common.IOExtensions;

namespace BizHawk.Tests.Client.Common.Dearchive
{
	[TestClass]
	public class DearchivalTests
	{
		private const string EMBED_GROUP = "data.dearchive.";

		private static IEnumerable<object?[]> TestCases { get; } = new[]
		{
			new object?[] { "m3_scy_change.7z", true },
			new object?[] { "m3_scy_change.gb.gz", true },
			new object?[] { "m3_scy_change.rar", true },
			new object?[] { "m3_scy_change.bsdtar.tar", true },
			new object?[] { "m3_scy_change.gnutar.tar", true },
			new object?[] { "m3_scy_change.zip", true },
		};

		private readonly Lazy<byte[]> _rom = new(static () => ReflectionCache.EmbeddedResourceStream(EMBED_GROUP + "m3_scy_change.gb").ReadAllBytes());

		private byte[] Rom => _rom.Value;

		[TestMethod]
#pragma warning disable BHI1600 // wants message argument
		public void SanityCheck() => Assert.AreEqual("SHA1:70DCA8E791878BDD32426391E4233EA52B47CDD1", SHA1Checksum.ComputePrefixedHex(Rom));
#pragma warning restore BHI1600

		[DynamicData(nameof(TestCases))]
		[TestMethod]
		public void TestSharpCompress(string filename, bool hasSharpCompressSupport)
		{
			if (!hasSharpCompressSupport) return;
			var sc = SharpCompressDearchivalMethod.Instance;
			/*scope*/{
				var archive = ReflectionCache.EmbeddedResourceStream(EMBED_GROUP + filename);
				Assert.IsTrue(sc.CheckSignature(archive, filename), $"{filename} is an archive, but wasn't detected as such"); // puts the seek pos of the Stream param back where it was (in this case at the start)
				using var af = sc.Construct(archive);
				var items = af.Scan();
				Assert.IsNotNull(items, $"{filename} contains 1 file, but it couldn't be enumerated correctly");
				Assert.AreEqual(1, items!.Count, $"{filename} contains 1 file, but was detected as containing {items.Count} files");
				using MemoryStream ms = new((int) items[0].Size);
				af.ExtractFile(items[0].ArchiveIndex, ms);
				ms.Seek(0L, SeekOrigin.Begin);
				CollectionAssert.AreEqual(Rom, ms.ReadAllBytes(), $"the file extracted from {filename} doesn't match the uncompressed file");
			}
		}
	}
}
