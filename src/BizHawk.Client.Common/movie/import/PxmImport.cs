﻿using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common.Movie.Import
{
	// PXM files are directly compatible with binary-format PJM files, with the only
	// difference being fewer flags implemented in the header, hence just calling the
	// base class methods via a subclass.
	//
	// However, the magic number/file signature is slightly different, requiring some
	// refactoring to avoid PXM-specific code in the PJMImport class.
	// ReSharper disable once UnusedMember.Global
	[ImporterFor("PCSX", ".pxm")]
	internal class PxmImport : PjmImport
	{
		protected override void RunImport()
		{
			var movie = Result.Movie;
			movie.HeaderEntries[HeaderKeys.Platform] = VSystemID.Raw.PSX;

			using var fs = SourceFile.OpenRead();
			using var br = new BinaryReader(fs);
			var info = ParseHeader(movie, "PXM ", br);

			fs.Seek(info.ControllerDataOffset, SeekOrigin.Begin);

			if (info.BinaryFormat)
			{
				ParseBinaryInputLog(br, movie, info);
			}
			else
			{
				ParseTextInputLog(br, movie, info);
			}
		}
	}
}
