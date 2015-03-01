using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using BizHawk.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.MultiHawk
{
	/// <summary>
	/// encapsulates thread-safe concept of pending/current display surfaces, reusing buffers where matching 
	/// sizes are available and keeping them cleaned up when they dont seem like theyll need to be used anymore
	/// </summary>
	class SwappableDisplaySurfaceSet
	{
		DisplaySurface Pending, Current;
		bool IsPending;
		Queue<DisplaySurface> ReleasedSurfaces = new Queue<DisplaySurface>();

		/// <summary>
		/// retrieves a surface with the specified size, reusing an old buffer if available and clearing if requested
		/// </summary>
		public DisplaySurface AllocateSurface(int width, int height, bool needsClear = true)
		{
			for (; ; )
			{
				DisplaySurface trial;
				lock (this)
				{
					if (ReleasedSurfaces.Count == 0) break;
					trial = ReleasedSurfaces.Dequeue();
				}
				if (trial.Width == width && trial.Height == height)
				{
					if (needsClear) trial.Clear();
					return trial;
				}
				trial.Dispose();
			}
			return new DisplaySurface(width, height);
		}

		/// <summary>
		/// sets the provided buffer as pending. takes control of the supplied buffer
		/// </summary>
		public void SetPending(DisplaySurface newPending)
		{
			lock (this)
			{
				if (Pending != null) ReleasedSurfaces.Enqueue(Pending);
				Pending = newPending;
				IsPending = true;
			}
		}

		public void ReleaseSurface(DisplaySurface surface)
		{
			lock (this) ReleasedSurfaces.Enqueue(surface);
		}

		/// <summary>
		/// returns the current buffer, making the most recent pending buffer (if there is such) as the new current first.
		/// </summary>
		public DisplaySurface GetCurrent()
		{
			lock (this)
			{
				if (IsPending)
				{
					if (Current != null) ReleasedSurfaces.Enqueue(Current);
					Current = Pending;
					Pending = null;
					IsPending = false;
				}
			}
			return Current;
		}
	}

}