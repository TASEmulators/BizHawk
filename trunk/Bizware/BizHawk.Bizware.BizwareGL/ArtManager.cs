using System;
using System.IO;
using System.Collections.Generic;

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
			//todo
		}

		/// <summary>
		/// Reopens this instance for further resource loading. Fails if it has been closed forever.
		/// </summary>
		public void Open()
		{
			AssertIsOpen(false);
			if (IsClosedForever) throw new InvalidOperationException("ArtManager instance has been closed forever!");
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
			using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
				return LoadArtInternal(new BitmapBuffer(path, new BitmapLoadOptions()));
		}

		Art LoadArtInternal(BitmapBuffer tex)
		{
			AssertIsOpen(true);

			Art a = new Art(this);
			ArtLooseTextureAssociation[a] = tex;
			ManagedArts.Add(a);

			return a;
		}

		/// <summary>
		/// Closes this instance for for further resource loading. Will result in a texture atlasing operation.
		/// If the close operation is forever, then internal backup copies of resources will be freed, but it can never be reopened.
		/// This function may take some time to run, as it is 
		/// </summary>
		public unsafe void Close(bool forever = true)
		{
			AssertIsOpen(true);
			IsOpened = false;
			IsClosedForever = forever;

			//first, cleanup old stuff
			foreach (var tex in ManagedTextures)
				tex.Dispose();
			ManagedTextures.Clear();

			//prepare input for atlas process and perform atlas
			//add 2 extra pixels for padding on all sides
			List<TexAtlas.RectItem> atlasItems = new List<TexAtlas.RectItem>();
			foreach (var kvp in ArtLooseTextureAssociation)
				atlasItems.Add(new TexAtlas.RectItem(kvp.Value.Width+2, kvp.Value.Height+2, kvp));
			var results = TexAtlas.PackAtlas(atlasItems);

			//this isnt supported yet:
			if (results.Atlases.Count > 1)
				throw new InvalidOperationException("Art files too big for atlas");

			//prepare the output buffer
			BitmapBuffer bmpResult = new BitmapBuffer(results.Atlases[0].Size);

			//for each item, copy it into the output buffer and set the tex parameters on them
			for (int i = 0; i < atlasItems.Count; i++)
			{
				var item = results.Atlases[0].Items[i];
				var artAndBitmap = (KeyValuePair<Art, BitmapBuffer>)item.Item;
				var art = artAndBitmap.Key;
				var bitmap = artAndBitmap.Value;

				int w = bitmap.Width;
				int h = bitmap.Height;
				int dx = item.X + 1;
				int dy = item.Y + 1;
				for (int y = 0; y < h; y++)
					for (int x = 0; x < w; x++)
					{
						int pixel = bitmap.GetPixel(x, y);
						bmpResult.SetPixel(x+dx,y+dy,pixel);
					}

				var myDestBitmap = bmpResult;
				float myDestWidth = (float)myDestBitmap.Width;
				float myDestHeight = (float)myDestBitmap.Height;

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
				foreach (var kvp in ArtLooseTextureAssociation)
					kvp.Value.Dispose();
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
		private void AssertIsOpen(bool state) { if (IsOpened != state) throw new InvalidOperationException("ArtManager instance is not open!"); }

		public IGL Owner { get; private set; }

		public bool IsOpened { get; private set; }
		public bool IsClosedForever { get; private set; }

		/// <summary>
		/// This is used to remember the original bitmap sources for art files. Once the ArtManager is closed forever, this will be purged
		/// </summary>
		Dictionary<Art, BitmapBuffer> ArtLooseTextureAssociation = new Dictionary<Art, BitmapBuffer>();

		/// <summary>
		/// Physical texture resources, which exist after this ArtManager has been closed
		/// </summary>
		List<Texture2d> ManagedTextures = new List<Texture2d>();

		/// <summary>
		/// All the Arts managed by this instance
		/// </summary>
		List<Art> ManagedArts = new List<Art>();
	}
}