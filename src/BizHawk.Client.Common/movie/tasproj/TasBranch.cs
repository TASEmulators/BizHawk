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
		public BitmapBuffer CoreFrameBuffer { get; set; }
		public BitmapBuffer OSDFrameBuffer { get; set; }
		public TasMovieChangeLog ChangeLog { get; set; }
		public DateTime TimeStamp { get; set; }
		public TasMovieMarkerList Markers { get; set; }
		public Guid Uuid { get; set; }
		public string UserText { get; set; }

		public TasBranch Clone() => (TasBranch)MemberwiseClone();
	}

	public interface ITasBranchCollection : IList<TasBranch>
	{
		int Current { get; set; }
		string NewBranchText { get; set; }

		void Swap(int b1, int b2);
		void Replace(TasBranch old, TasBranch newBranch);

		void Save(ZipStateSaver bs);
		void Load(ZipStateLoader bl, ITasMovie movie);
	}

	public class TasBranchCollection : List<TasBranch>, ITasBranchCollection
	{
		private readonly ITasMovie _movie;

		private readonly IQuickBmpFile _quickBmpFile;

		public TasBranchCollection(ITasMovie movie, IQuickBmpFile quickBmpFile)
		{
			_movie = movie;
			_quickBmpFile = quickBmpFile;
		}

		public int Current { get; set; } = -1;
		public string NewBranchText { get; set; } = "";

		public void Swap(int b1, int b2)
		{
			var branch = this[b1];

			if (b2 >= Count)
			{
				b2 = Count - 1;
			}

			Remove(branch);
			Insert(b2, branch);
			_movie.FlagChanges();
		}

		public void Replace(TasBranch old, TasBranch newBranch)
		{
			int index = IndexOf(old);
			newBranch.Uuid = old.Uuid;
			if (newBranch.UserText == "")
			{
				newBranch.UserText = old.UserText;
			}

			this[index] = newBranch;
			_movie.FlagChanges();
		}

		public new TasBranch this[int index]
		{
			get => index >= Count || index < 0
				? null
				: base [index];
			set => base[index] = value;
		}

		public new void Add(TasBranch item)
		{
			if (item == null)
			{
				throw new ArgumentNullException($"{nameof(item)} cannot be null");
			}

			if (item.Uuid == Guid.Empty)
			{
				item.Uuid = Guid.NewGuid();
			}

			base.Add(item);
			_movie.FlagChanges();
		}

		public new bool Remove(TasBranch item)
		{
			var result = base.Remove(item);
			if (result)
			{
				_movie.FlagChanges();
			}

			return result;
		}

		public void Save(ZipStateSaver bs)
		{
			var nheader = new IndexedStateLump(BinaryStateLump.BranchHeader);
			var ncore = new IndexedStateLump(BinaryStateLump.BranchCoreData);
			var ninput = new IndexedStateLump(BinaryStateLump.BranchInputLog);
			var nframebuffer = new IndexedStateLump(BinaryStateLump.BranchFrameBuffer);
			var ncoreframebuffer = new IndexedStateLump(BinaryStateLump.BranchCoreFrameBuffer);
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
						UniqueIdentifier = b.Uuid
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
					_quickBmpFile.Save(vp, s, b.OSDFrameBuffer.Width, b.OSDFrameBuffer.Height);
				});

				bs.PutLump(ncoreframebuffer, delegate(Stream s)
				{
					var vp = new BitmapBufferVideoProvider(b.CoreFrameBuffer);
					_quickBmpFile.Save(vp, s, b.CoreFrameBuffer.Width, b.CoreFrameBuffer.Height);
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
				ncoreframebuffer.Increment();
				nmarkers.Increment();
				nusertext.Increment();
			}
		}

		public void Load(ZipStateLoader bl, ITasMovie movie)
		{
			var nheader = new IndexedStateLump(BinaryStateLump.BranchHeader);
			var ncore = new IndexedStateLump(BinaryStateLump.BranchCoreData);
			var ninput = new IndexedStateLump(BinaryStateLump.BranchInputLog);
			var nframebuffer = new IndexedStateLump(BinaryStateLump.BranchFrameBuffer);
			var ncoreframebuffer = new IndexedStateLump(BinaryStateLump.BranchCoreFrameBuffer);
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
						b.Uuid = (Guid)identifier;
					}
					else
					{
						b.Uuid = Guid.NewGuid();
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
					_quickBmpFile.LoadAuto(s, out var vp);
					b.OSDFrameBuffer = new BitmapBuffer(vp.BufferWidth, vp.BufferHeight, vp.GetVideoBuffer());
				});

				bl.GetLump(ncoreframebuffer, false, delegate(Stream s, long length)
				{
					_quickBmpFile.LoadAuto(s, out var vp);
					b.CoreFrameBuffer = new BitmapBuffer(vp.BufferWidth, vp.BufferHeight, vp.GetVideoBuffer());
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
				ncoreframebuffer.Increment();
				nmarkers.Increment();
				nusertext.Increment();
			}
		}
	}

	public static class TasBranchExtensions
	{
		public static int IndexOfFrame(this IList<TasBranch> list, int frame)
		{
			var branch = list
				.Where(b => b.Frame == frame)
				.OrderByDescending(b => b.TimeStamp)
				.FirstOrDefault();

			return branch == null
				? -1
				: list.IndexOf(branch);
		}

		// TODO: stop relying on the index value of a branch
		public static int IndexOfHash(this IList<TasBranch> list, Guid uuid)
		{
			var branch = list.SingleOrDefault(b => b.Uuid == uuid);
			return branch == null
				? -1
				: list.IndexOf(branch);
		}
	}
}
