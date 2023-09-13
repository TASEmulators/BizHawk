using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Bizware.BizwareGL
{
	/// <summary>
	/// Load resources through here, and they can be grouped together, for purposes of batching and whatnot.
	/// You can't use any of the returned Art resources until calling Close on the ArtManager
	/// </summary>
	public class ArtManager : IDisposable
	{
		public ArtManager(IGL owner)
		{
			Owner = owner;
			Open();
		}

		public void Dispose()
		{
			// todo
		}

		/// <summary>
		/// Reopens this instance for further resource loading. Fails if it has been closed forever.
		/// </summary>
		public void Open()
		{
			AssertIsOpen(false);
			if (IsClosedForever)
			{
				throw new InvalidOperationException($"{nameof(ArtManager)} instance has been closed forever!");
			}

			IsOpened = true;
		}

		/// <summary>
		/// Loads the given stream as an Art instance
		/// </summary>
		public Art LoadArt(Stream stream)
		{
			return LoadArtInternal(new BitmapBuffer(stream, new BitmapLoadOptions()));
		}

		/// <summary>
		/// Loads the given path as an Art instance.
		/// </summary>
		public Art LoadArt(string path)
		{
			using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
			return LoadArtInternal(new BitmapBuffer(path, new BitmapLoadOptions()));
		}

		private Art LoadArtInternal(BitmapBuffer tex)
		{
			AssertIsOpen(true);

			var a = new Art(this);
			ArtLooseTextureAssociation.Add((a, tex));
			ManagedArts.Add(a);

			return a;
		}

		/// <summary>
		/// Closes this instance for for further resource loading. Will result in a texture atlasing operation.
		/// If the close operation is forever, then internal backup copies of resources will be freed, but it can never be reopened.
		/// This function may take some time to run, as it is
		/// </summary>
		public void Close(bool forever = true)
		{
			AssertIsOpen(true);
			IsOpened = false;
			IsClosedForever = forever;

			// first, cleanup old stuff
			foreach (var tex in ManagedTextures)
			{
				tex.Dispose();
			}
			ManagedTextures.Clear();

			// prepare input for atlas process and perform atlas
			// add 2 extra pixels for padding on all sides
			var atlasItems = ArtLooseTextureAssociation
				.Select(kvp => new TexAtlas.RectItem(kvp.Bitmap.Width + 2, kvp.Bitmap.Height + 2, kvp))
				.ToList();
			var results = TexAtlas.PackAtlas(atlasItems);

			// this isn't supported yet:
			if (results.Count > 1)
				throw new InvalidOperationException("Art files too big for atlas");

			// prepare the output buffer
			var bmpResult = new BitmapBuffer(results[0].Size);

			//for each item, copy it into the output buffer and set the tex parameters on them
			for (var i = 0; i < atlasItems.Count; i++)
			{
				var item = results[0].Items[i];
				var (art, bitmap) = ((Art, BitmapBuffer)) item.Item;
				var w = bitmap.Width;
				var h = bitmap.Height;
				var dx = item.X + 1;
				var dy = item.Y + 1;
				for (var y = 0; y < h; y++)
				{
					for (var x = 0; x < w; x++)
					{
						var pixel = bitmap.GetPixel(x, y);
						bmpResult.SetPixel(x+dx,y+dy,pixel);
					}
				}

				var myDestWidth = (float)bmpResult.Width;
				var myDestHeight = (float)bmpResult.Height;

				art.u0 = dx / myDestWidth;
				art.v0 = dy / myDestHeight;
				art.u1 = (dx + w) / myDestWidth;
				art.v1 = (dy + h) / myDestHeight;
				art.Width = w;
				art.Height = h;
			}

			//if we're closed forever, then forget all the original bitmaps
			if (forever)
			{
				foreach (var tuple in ArtLooseTextureAssociation) tuple.Bitmap.Dispose();
				ArtLooseTextureAssociation.Clear();
			}

			//create a physical texture
			var texture = Owner.LoadTexture(bmpResult);
			ManagedTextures.Add(texture);

			//oops, we couldn't do this earlier.
			foreach (var art in ManagedArts)
				art.BaseTexture = texture;
		}

		/// <summary>
		/// Throws an exception if the instance is not open
		/// </summary>
		private void AssertIsOpen(bool state)
		{
			if (IsOpened != state)
			{
				throw new InvalidOperationException($"{nameof(ArtManager)} instance is not open!");
			}
		}

		public IGL Owner { get; }

		public bool IsOpened { get; private set; }
		public bool IsClosedForever { get; private set; }

		/// <summary>
		/// This is used to remember the original bitmap sources for art files. Once the ArtManager is closed forever, this will be purged
		/// </summary>
		private readonly List<(Art Art, BitmapBuffer Bitmap)> ArtLooseTextureAssociation = new();

		/// <summary>
		/// Physical texture resources, which exist after this ArtManager has been closed
		/// </summary>
		private readonly List<Texture2d> ManagedTextures = new();

		/// <summary>
		/// All the Arts managed by this instance
		/// </summary>
		private readonly List<Art> ManagedArts = new();
	}
}