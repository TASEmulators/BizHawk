using System;
using System.Collections.Generic;
using BizHawk.Bizware.BizwareGL;


namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// encapsulates thread-safe concept of pending/current BitmapBuffer, reusing buffers where matching 
	/// sizes are available and keeping them cleaned up when they dont seem like theyll need to be used anymore
	/// This isnt in the csproj right now, but I'm keeping it, in case its handy.
	/// </summary>
	class SwappableBitmapBufferSet
	{
		BitmapBuffer Pending, Current;
		Queue<BitmapBuffer> ReleasedSurfaces = new Queue<BitmapBuffer>();

		/// <summary>
		/// retrieves a surface with the specified size, reusing an old buffer if available and clearing if requested
		/// </summary>
		public BitmapBuffer AllocateSurface(int width, int height, bool needsClear = true)
		{
			for (; ; )
			{
				BitmapBuffer trial;
				lock (this)
				{
					if (ReleasedSurfaces.Count == 0) break;
					trial = ReleasedSurfaces.Dequeue();
				}
				if (trial.Width == width && trial.Height == height)
				{
					if (needsClear) trial.ClearWithoutAlloc();
					return trial;
				}
				trial.Dispose();
			}
			return new BitmapBuffer(width, height);
		}

		/// <summary>
		/// sets the provided buffer as pending. takes control of the supplied buffer
		/// </summary>
		public void SetPending(BitmapBuffer newPending)
		{
			lock (this)
			{
				if (Pending != null) ReleasedSurfaces.Enqueue(Pending);
				Pending = newPending;
			}
		}

		public void ReleaseSurface(BitmapBuffer surface)
		{
			lock (this) ReleasedSurfaces.Enqueue(surface);
		}

		/// <summary>
		/// returns the current buffer, making the most recent pending buffer (if there is such) as the new current first.
		/// </summary>
		public BitmapBuffer GetCurrent()
		{
			lock (this)
			{
				if (Pending != null)
				{
					if (Current != null) ReleasedSurfaces.Enqueue(Current);
					Current = Pending;
					Pending = null;
				}
			}
			return Current;
		}
	}

}