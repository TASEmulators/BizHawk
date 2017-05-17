using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using BizHawk.Bizware.BizwareGL;

namespace BizHawk.Client.Common
{
	public class TasBranch
	{
		public int Frame { get; set; }
		public byte[] CoreData { get; set; }
		public IStringLog InputLog { get; set; }
		public BitmapBuffer OSDFrameBuffer { get; set; }
		public TasLagLog LagLog { get; set; }
		public TasMovieChangeLog ChangeLog { get; set; }
		public DateTime TimeStamp { get; set; }
		public TasMovieMarkerList Markers { get; set; }
		public Guid UniqueIdentifier { get; set; }
		public string UserText { get; set; }

		public TasBranch Clone()
		{
			return (TasBranch)MemberwiseClone();
		}
	}

	public class TasBranchCollection : List<TasBranch>
	{
		public new void Add(TasBranch item)
		{
			if (item.UniqueIdentifier == Guid.Empty)
			{
				var currentHashes = this.Select(b => b.UniqueIdentifier.GetHashCode()).ToList();

				do
				{
					item.UniqueIdentifier = Guid.NewGuid();
				}
				while (currentHashes.Contains(item.UniqueIdentifier.GetHashCode()));
			}

			base.Add(item);
		}

		public void Save(BinaryStateSaver bs)
		{
			var nheader = new IndexedStateLump(BinaryStateLump.BranchHeader);
			var ncore = new IndexedStateLump(BinaryStateLump.BranchCoreData);
			var ninput = new IndexedStateLump(BinaryStateLump.BranchInputLog);
			var nframebuffer = new IndexedStateLump(BinaryStateLump.BranchFrameBuffer);
			var nlaglog = new IndexedStateLump(BinaryStateLump.BranchLagLog);
			var nmarkers = new IndexedStateLump(BinaryStateLump.BranchMarkers);
			var nusertext = new IndexedStateLump(BinaryStateLump.BranchUserText);
			foreach (var b in this)
			{
				bs.PutLump(nheader, delegate(TextWriter tw)
				{
					// if this header needs more stuff in it, handle it sensibly
					tw.WriteLine(JsonConvert.SerializeObject(new
					{
						b.Frame,
						b.TimeStamp,
						b.UniqueIdentifier
					}));
				});

				bs.PutLump(ncore, delegate(Stream s)
				{
					s.Write(b.CoreData, 0, b.CoreData.Length);
				});

				bs.PutLump(ninput, delegate(TextWriter tw)
				{
					int todo = b.InputLog.Count;
					for (int i = 0; i < todo; i++)
					{
						tw.WriteLine(b.InputLog[i]);
					}
				});

				bs.PutLump(nframebuffer, delegate(Stream s)
				{
					var vp = new BitmapBufferVideoProvider(b.OSDFrameBuffer);
					QuickBmpFile.Save(vp, s, b.OSDFrameBuffer.Width, b.OSDFrameBuffer.Height);
				});

				bs.PutLump(nlaglog, delegate(BinaryWriter bw)
				{
					b.LagLog.Save(bw);
				});

				bs.PutLump(nmarkers, delegate(TextWriter tw)
				{
					tw.WriteLine(b.Markers.ToString());
				});

				bs.PutLump(nusertext, delegate(TextWriter tw)
				{
					tw.WriteLine(b.UserText);
				});

				nheader.Increment();
				ncore.Increment();
				ninput.Increment();
				nframebuffer.Increment();
				nlaglog.Increment();
				nmarkers.Increment();
				nusertext.Increment();
			}
		}

		public void Load(BinaryStateLoader bl, TasMovie movie)
		{
			var nheader = new IndexedStateLump(BinaryStateLump.BranchHeader);
			var ncore = new IndexedStateLump(BinaryStateLump.BranchCoreData);
			var ninput = new IndexedStateLump(BinaryStateLump.BranchInputLog);
			var nframebuffer = new IndexedStateLump(BinaryStateLump.BranchFrameBuffer);
			var nlaglog = new IndexedStateLump(BinaryStateLump.BranchLagLog);
			var nmarkers = new IndexedStateLump(BinaryStateLump.BranchMarkers);
			var nusertext = new IndexedStateLump(BinaryStateLump.BranchUserText);

			Clear();

			while (true)
			{
				var b = new TasBranch();

				if (!bl.GetLump(nheader, false, delegate(TextReader tr)
				{
					var header = (dynamic)JsonConvert.DeserializeObject(tr.ReadLine());
					b.Frame = (int)header.Frame;

					var timestamp = header.TimeStamp;

					if (timestamp != null)
					{
						b.TimeStamp = (DateTime)timestamp;
					}
					else
					{
						b.TimeStamp = DateTime.Now;
					}

					var identifier = header.UniqueIdentifier;
					if (identifier != null)
					{
						b.UniqueIdentifier = (Guid)identifier;
					}
					else
					{
						b.UniqueIdentifier = Guid.NewGuid();
					}
				}))
				{
					return;
				}

				bl.GetLump(ncore, true, delegate(Stream s, long length)
				{
					b.CoreData = new byte[length];
					s.Read(b.CoreData, 0, b.CoreData.Length);
				});

				bl.GetLump(ninput, true, delegate(TextReader tr)
				{
					b.InputLog = StringLogUtil.MakeStringLog();
					string line;
					while ((line = tr.ReadLine()) != null)
					{
						b.InputLog.Add(line);
					}
				});

				bl.GetLump(nframebuffer, true, delegate(Stream s, long length)
				{
					var vp = new QuickBmpFile.LoadedBMP();
					QuickBmpFile.Load(vp, s);
					b.OSDFrameBuffer = new BitmapBuffer(vp.BufferWidth, vp.BufferHeight, vp.VideoBuffer);
				});

				bl.GetLump(nlaglog, true, delegate(BinaryReader br)
				{
					b.LagLog = new TasLagLog();
					b.LagLog.Load(br);
				});

				b.Markers = new TasMovieMarkerList(movie);
				bl.GetLump(nmarkers, false, delegate(TextReader tr)
				{
					string line;
					while ((line = tr.ReadLine()) != null)
					{
						if (!string.IsNullOrWhiteSpace(line))
						{
							b.Markers.Add(new TasMovieMarker(line));
						}
					}
				});

				bl.GetLump(nusertext, false, delegate(TextReader tr)
				{
					string line;
					if ((line = tr.ReadLine()) != null)
					{
						if (!string.IsNullOrWhiteSpace(line))
						{
							b.UserText = line;
						}
					}
				});

				Add(b);

				nheader.Increment();
				ncore.Increment();
				ninput.Increment();
				nframebuffer.Increment();
				nlaglog.Increment();
				nmarkers.Increment();
				nusertext.Increment();
			}
		}
	}
}
