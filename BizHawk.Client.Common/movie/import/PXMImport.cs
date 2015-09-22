using System.IO;

namespace BizHawk.Client.Common.movie.import
{

	// PXM files are directly compatible with binary-format PJM files, with the only
	// difference being fewer flags implemented in the header, hence just calling the
	// base class methods via a subclass.
	//
	// However, the magic number/file signature is slightly different, requiring some
	// refactoring to avoid PXM-specific code in the PJMImport class.
	[ImportExtension(".pxm")]
	class PXMImport : PJMImport
	{
		protected override void RunImport()
		{
			Bk2Movie movie = Result.Movie;
			MiscHeaderInfo info;

			movie.HeaderEntries[HeaderKeys.PLATFORM] = "PSX";

			using (var fs = SourceFile.OpenRead())
			{
				using (var br = new BinaryReader(fs))
				{
					info = parseHeader(movie, "PXM ", br);

					fs.Seek(info.controllerDataOffset, SeekOrigin.Begin);

					if (info.binaryFormat)
					{
						parseBinaryInputLog(br, movie, info);
					}
					else
					{
						parseTextInputLog(br, movie, info);
					}
				}
			}

			movie.Save();
		}
	}
}
