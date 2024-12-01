using System.IO;
using System.Linq;

using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Common.IOExtensions;

namespace BizHawk.Tests.Client.Common.Dearchive
{
	[TestClass]
	public class DearchivalTests
	{
		private const string EMBED_GROUP = "dearchive";

		private static readonly (string Filename, bool HasSharpCompressSupport)[] TestCases = {
			("m3_scy_change.7z", true),
			("m3_scy_change.gb.gz", true),
			("m3_scy_change.rar", true),
			("m3_scy_change.bsdtar.tar", true),
			("m3_scy_change.gnutar.tar", true),
			("m3_scy_change.zip", true),
		};

		private readonly Lazy<byte[]> _rom = new(() => EmbeddedData.GetStream(EMBED_GROUP, "m3_scy_change.gb").ReadAllBytes());

		private byte[] Rom => _rom.Value;

		[TestMethod]
		public void SanityCheck() => Assert.AreEqual("SHA1:70DCA8E791878BDD32426391E4233EA52B47CDD1", SHA1Checksum.ComputePrefixedHex(Rom));

		[TestMethod]
		public void TestSharpCompress()
		{
			var sc = SharpCompressDearchivalMethod.Instance;
			foreach (var filename in TestCases.Where(testCase => testCase.HasSharpCompressSupport)
				.Select(testCase => testCase.Filename))
			{
				var archive = EmbeddedData.GetStream(EMBED_GROUP, filename);
				Assert.IsTrue(sc.CheckSignature(archive, filename), $"{filename} is an archive, but wasn't detected as such"); // puts the seek pos of the Stream param back where it was (in this case at the start)
				var af = sc.Construct(archive);
				var items = af.Scan();
				Assert.IsNotNull(items, $"{filename} contains 1 file, but it couldn't be enumerated correctly");
				Assert.AreEqual(1, items!.Count, $"{filename} contains 1 file, but was detected as containing {items.Count} files");
				using MemoryStream ms = new((int) items[0].Size);
				af.ExtractFile(items[0].ArchiveIndex, ms);
				ms.Seek(0L, SeekOrigin.Begin);
				Assert.IsTrue(ms.ReadAllBytes().SequenceEqual(Rom), $"the file extracted from {filename} doesn't match the uncompressed file");
			}
		}
	}
}
