using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using BizHawk.Common.PathExtensions;

namespace BizHawk.Emulation.DiscSystem
{
	/// <remarks>
	/// cue format preferences notes
	///
	/// PCEjin -
	/// does not like session commands
	/// it can handle binpercue
	/// it seems not to be able to handle binpertrack, or maybe i am doing something wrong (still haven't ruled it out)
	/// </remarks>
	public static class DiscoHawkLogic
	{
		private static bool CompareFile(string infile, DiscInterface loadDiscInterface, DiscInterface cmpif, bool verbose, CancellationTokenSource cancelToken, StringWriter sw)
		{
			Disc srcDisc = null, dstDisc = null;

			try
			{
				bool success = false;

				sw.WriteLine("BEGIN COMPARE: {0}\nSRC {1} vs DST {2}", infile, loadDiscInterface, cmpif);

				//reload the original disc, with new policies as needed
				var dmj = new DiscMountJob(
					fromPath: infile,
					discMountPolicy: new DiscMountPolicy { CUE_PregapContradictionModeA = cmpif != DiscInterface.MednaDisc },
					discInterface: loadDiscInterface);
				dmj.Run();
				srcDisc = dmj.OUT_Disc;

				var dstDmj = new DiscMountJob(fromPath: infile, discInterface: cmpif);
				dstDmj.Run();
				dstDisc = dstDmj.OUT_Disc;

				var srcDsr = new DiscSectorReader(srcDisc);
				var dstDsr = new DiscSectorReader(dstDisc);

				var srcToc = srcDisc.TOC;
				var dstToc = dstDisc.TOC;

				var srcDataBuf = new byte[2448];
				var dstDataBuf = new byte[2448];

				void SwDumpTocOne(DiscTOC.TOCItem item)
				{
					if (!item.Exists)
					{
						sw.Write("(---missing---)");
					}
					else
					{
						sw.Write("({0:X2} - {1})", (byte) item.Control, item.LBA);
					}
				}

				void SwDumpToc(int index)
				{
					sw.Write("SRC TOC#{0,3} ", index);
					SwDumpTocOne(srcToc.TOCItems[index]);
					sw.WriteLine();
					sw.Write("DST TOC#{0,3} ", index);
					SwDumpTocOne(dstToc.TOCItems[index]);
					sw.WriteLine();
				}

				//verify sector count
				if (srcDisc.Session1.LeadoutLBA != dstDisc.Session1.LeadoutLBA)
				{
					sw.Write("LeadoutTrack.LBA {0} vs {1}\n", srcDisc.Session1.LeadoutTrack.LBA, dstDisc.Session1.LeadoutTrack.LBA);
					goto SKIPPO;
				}

				//verify TOC match
				if (srcDisc.TOC.FirstRecordedTrackNumber != dstDisc.TOC.FirstRecordedTrackNumber
					|| srcDisc.TOC.LastRecordedTrackNumber != dstDisc.TOC.LastRecordedTrackNumber)
				{
					sw.WriteLine("Mismatch of RecordedTrackNumbers: {0}-{1} vs {2}-{3}",
						srcDisc.TOC.FirstRecordedTrackNumber, srcDisc.TOC.LastRecordedTrackNumber,
						dstDisc.TOC.FirstRecordedTrackNumber, dstDisc.TOC.LastRecordedTrackNumber
						);
					goto SKIPPO;
				}

				bool badToc = false;
				for (int t = 0; t < 101; t++)
				{
					if (srcToc.TOCItems[t].Exists != dstToc.TOCItems[t].Exists
						|| srcToc.TOCItems[t].Control != dstToc.TOCItems[t].Control
						|| srcToc.TOCItems[t].LBA != dstToc.TOCItems[t].LBA
						)
					{
						sw.WriteLine("Mismatch in TOCItem");
						SwDumpToc(t);
						badToc = true;
					}
				}
				if (badToc)
					goto SKIPPO;

				void SwDumpChunkOne(string comment, int lba, byte[] buf, int addr, int count)
				{
					sw.Write("{0} -  ", comment);
					for (int i = 0; i < count; i++)
					{
						if (i + addr >= buf.Length) continue;
						sw.Write("{0:X2}{1}", buf[addr + i], (i == count - 1) ? " " : "  ");
					}

					sw.WriteLine();
				}

				int[] offenders = new int[12];

				void SwDumpChunk(int lba, int dispAddr, int addr, int count, int numOffenders)
				{
					var hashedOffenders = new HashSet<int>();
					for (int i = 0; i < numOffenders; i++)
					{
						hashedOffenders.Add(offenders[i]);
					}

					sw.Write("                          ");
					for (int i = 0; i < count; i++)
					{
						sw.Write((hashedOffenders.Contains(dispAddr + i)) ? "vvv " : "    ");
					}

					sw.WriteLine();
					sw.Write("                          ");
					for (int i = 0; i < count; i++)
					{
						sw.Write("{0:X3} ", dispAddr + i, (i == count - 1) ? " " : "  ");
					}

					sw.WriteLine();
					sw.Write("                          ");
					sw.Write(new string('-', count * 4));
					sw.WriteLine();
					SwDumpChunkOne($"SRC #{lba,6} ({new Timestamp(lba)})", lba, srcDataBuf, addr, count);
					SwDumpChunkOne($"DST #{lba,6} ({new Timestamp(lba)})", lba, dstDataBuf, addr, count);
				}

				//verify each sector contents
				int nSectors = srcDisc.Session1.LeadoutLBA;
				for (int lba = -150; lba < nSectors; lba++)
				{
					if (verbose)
						if (lba % 1000 == 0)
							Console.WriteLine("LBA {0} of {1}", lba, nSectors);

					if (cancelToken != null)
						if (cancelToken.Token.IsCancellationRequested)
							return false;

					srcDsr.ReadLBA_2448(lba, srcDataBuf, 0);
					dstDsr.ReadLBA_2448(lba, dstDataBuf, 0);

					//check the header
					for (int b = 0; b < 16; b++)
					{
						if (srcDataBuf[b] != dstDataBuf[b])
						{
							sw.WriteLine("Mismatch in sector header at byte {0}", b);
							offenders[0] = b;
							SwDumpChunk(lba, 0, 0, 16, 1);
							goto SKIPPO;
						}
					}

					// check userData
					for (int b = 16; b < 2352; b++)
					{
						if (srcDataBuf[b] != dstDataBuf[b])
						{
							sw.Write("LBA {0} mismatch at userdata byte {1}; terminating sector cmp\n", lba, b);
							goto SKIPPO;
						}
					}

					// check subChannels
					for (int c = 0, b = 2352; c < 8; c++)
					{
						int numOffenders = 0;
						for (int e = 0; e < 12; e++, b++)
						{
							if (srcDataBuf[b] != dstDataBuf[b])
							{
								offenders[numOffenders++] = e;
							}
						}

						if (numOffenders != 0)
						{
							sw.Write("LBA {0} mismatch(es) at subchannel {1}; terminating sector cmp\n", lba, (char)('P' + c));
							SwDumpChunk(lba, 0, 2352 + c * 12, 12, numOffenders);
							goto SKIPPO;
						}
					}
				}

				success = true;

			SKIPPO:
				sw.WriteLine("END COMPARE");
				sw.WriteLine("-----------------------------");

				return success;
			}
			finally
			{
				srcDisc?.Dispose();
				dstDisc?.Dispose();
			}
		}

		private static List<string> FindCuesRecurse(string dir)
		{
			var ret = new List<string>();
			var dpTodo = new Queue<string>();
			dpTodo.Enqueue(dir);
			for (; ; )
			{
				if (dpTodo.Count == 0)
					break;
				var dpCurr = dpTodo.Dequeue();
				foreach(var fi in new DirectoryInfo(dpCurr).GetFiles("*.cue"))
				{
					ret.Add(fi.FullName);
				}
				Parallel.ForEach(new DirectoryInfo(dpCurr).GetDirectories(), di =>
				{
					lock(dpTodo)
						dpTodo.Enqueue(di.FullName);
				});
			}

			return ret;
		}

		/// <summary>
		/// Formats supported with HawkAndWriteFile
		/// </summary>
		public enum HawkedFormats
		{
			CCD,
			CHD,
		}

		public static bool HawkAndWriteFile(string inputPath, Action<string> errorCallback, DiscInterface discInterface = DiscInterface.BizHawk, HawkedFormats hawkedFormat = HawkedFormats.CCD)
		{
			DiscMountJob job = new(inputPath, discInterface);
			job.Run();
			if (job.OUT_ErrorLevel)
			{
				errorCallback(job.OUT_Log);
				return false;
			}
			using var disc = job.OUT_Disc;
			var (dir, baseName, _) = inputPath.SplitPathToDirFileAndExt();
			var ext = hawkedFormat switch
			{
				HawkedFormats.CCD => ".ccd",
				HawkedFormats.CHD => ".chd",
				_ => throw new InvalidOperationException(),
			};
			var outfile = Path.Combine(dir!, $"{baseName}_hawked{ext}");
			switch (hawkedFormat)
			{
				case HawkedFormats.CCD:
					CCD_Format.Dump(disc, outfile);
					break;
				case HawkedFormats.CHD:
					CHD_Format.Dump(disc, outfile);
					break;
				default:
					throw new InvalidOperationException();
			}

			return true;
		}

		public static void RunWithArgs(string[] args, Action<string> showComparisonResultsCallback)
		{
			bool scanCues = false;
			string dirArg = null;
			string infile = null;
			var loadDiscInterface = DiscInterface.BizHawk;
			var outputFormat = HawkedFormats.CCD;
			var compareDiscInterfaces = new List<DiscInterface>();
			bool hawk = false;
			bool music = false;
			bool overwrite = false;
			int idx = 0;
			while (idx < args.Length)
			{
				string a = args[idx++];
				string au = a.ToUpperInvariant();
				if (au == "LOAD")
					loadDiscInterface = (DiscInterface)Enum.Parse(typeof(DiscInterface), args[idx++], true);
				else if (au == "COMPARE")
					compareDiscInterfaces.Add((DiscInterface)Enum.Parse(typeof(DiscInterface), args[idx++], true));
				else if (au == "HAWK")
					hawk = true;
				else if (au == "CUEDIR")
				{
					dirArg = args[idx++];
					scanCues = true;
				}
				else if (au is "MUSIC")
				{
					music = true;
				}
				else if (au is "OVERWRITE")
				{
					overwrite = true;
				}
				else if (au is "OUTPUT")
				{
					outputFormat = (HawkedFormats)Enum.Parse(typeof(HawkedFormats), args[idx++], true);
				}
				else infile = a;
			}

			if (hawk)
			{
				if (infile == null) return;
				HawkAndWriteFile(
					inputPath: infile,
					errorCallback: err => Console.WriteLine($"failed to convert {infile}:\n{err}"),
					discInterface: loadDiscInterface,
					hawkedFormat: outputFormat);
			}

			if (music)
			{
				if (infile is null) return;
				using var disc = Disc.LoadAutomagic(infile);
				var (path, filename, _) = infile.SplitPathToDirFileAndExt();
				bool? CheckOverwrite(string mp3Path)
				{
					if (overwrite) return true; // overwrite
					Console.WriteLine($"{mp3Path} already exists. Remove existing output files, or retry with the extra argument \"OVERWRITE\".");
					return null; // cancel
				}
				AudioExtractor.Extract(disc, path, filename, CheckOverwrite);
			}

			bool verbose = true;

			if (scanCues)
			{
				verbose = false;
				var todo = FindCuesRecurse(dirArg);
				var po = new ParallelOptions();
				var cts = new CancellationTokenSource();
				po.CancellationToken = cts.Token;
				po.MaxDegreeOfParallelism = 1;
				if(po.MaxDegreeOfParallelism < 0) po.MaxDegreeOfParallelism = 1;
				object olock = new object();
				int ctr=0;
				bool blocked = false;
				try
				{
					Parallel.ForEach(todo, po, (fp) =>
					{
						lock (olock)
						{
							ctr++;
							int strlen = todo.Count.ToString().Length;
							string fmt = string.Format("{{0,{0}}}/{{1,{0}}} {{2}}", strlen);
							Console.WriteLine(fmt, ctr, todo.Count, Path.GetFileNameWithoutExtension(fp));
						}

						if(!blocked)
							foreach (var cmpif in compareDiscInterfaces)
							{
								var sw = new StringWriter();
								bool success = CompareFile(fp, loadDiscInterface, cmpif, verbose, cts, sw);
								if (!success)
								{
									lock (Console.Out)
										Console.Out.Write(sw.ToString());

									cts.Cancel();
									return;
								}
							}
					});
				}
				catch (AggregateException ae) {
					Console.WriteLine(ae.ToString());
				}
				catch (OperationCanceledException oce)
				{
					Console.WriteLine(oce.ToString());
				}
				Console.WriteLine("--TERMINATED--");
				return;
			}

			if (compareDiscInterfaces.Count != 0)
			{
				var sw = new StringWriter();
				foreach (var cmpif in compareDiscInterfaces)
				{
					CompareFile(infile, loadDiscInterface, cmpif, verbose, null, sw);
				}

				sw.Flush();
				string results = sw.ToString();
				showComparisonResultsCallback(results);
			}
		}
	}
}
