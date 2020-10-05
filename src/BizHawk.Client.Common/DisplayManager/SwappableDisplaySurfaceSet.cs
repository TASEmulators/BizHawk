using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// encapsulates thread-safe concept of pending/current display surfaces, reusing buffers where matching 
	/// sizes are available and keeping them cleaned up when they don't seem like they'll need to be used anymore
	/// </summary>
	public class SwappableDisplaySurfaceSet
	{
		private DisplaySurface _pending, _current;
		private bool _isPending;
		private readonly Queue<DisplaySurface> _releasedSurfaces = new Queue<DisplaySurface>();

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
					if (_releasedSurfaces.Count == 0) break;
					trial = _releasedSurfaces.Dequeue();
				}
				if (trial.Width == width && trial.Height == height)
				{
					if (needsClear)
					{
						trial.Clear();
					}

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
				if (_pending != null) _releasedSurfaces.Enqueue(_pending);
				_pending = newPending;
				_isPending = true;
			}
		}

		public void ReleaseSurface(DisplaySurface surface)
		{
			lock (this) _releasedSurfaces.Enqueue(surface);
		}

		/// <summary>
		/// returns the current buffer, making the most recent pending buffer (if there is such) as the new current first.
		/// </summary>
		public DisplaySurface GetCurrent()
		{
			lock (this)
			{
				if (_isPending)
				{
					if (_current != null) _releasedSurfaces.Enqueue(_current);
					_current = _pending;
					_pending = null;
					_isPending = false;
				}
			}

			return _current;
		}
	}
}