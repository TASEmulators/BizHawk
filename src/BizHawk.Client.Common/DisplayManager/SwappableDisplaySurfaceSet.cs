using System;
using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// encapsulates thread-safe concept of pending/current display surfaces, reusing buffers where matching 
	/// sizes are available and keeping them cleaned up when they don't seem like they'll need to be used anymore
	/// </summary>
	public class SwappableDisplaySurfaceSet<T>
		where T : class, IDisplaySurface
	{
		private readonly Func<int, int, T> _createDispSurface;

		private T _pending, _current;
		private bool _isPending;
		private readonly Queue<T> _releasedSurfaces = new();

		public SwappableDisplaySurfaceSet(Func<int, int, T> createDispSurface) => _createDispSurface = createDispSurface;

		/// <summary>
		/// retrieves a surface with the specified size, reusing an old buffer if available and clearing if requested
		/// </summary>
		public T AllocateSurface(int width, int height, bool needsClear = true)
		{
			for (; ; )
			{
				T trial;
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

			return _createDispSurface(width, height);
		}

		/// <summary>
		/// sets the provided buffer as pending. takes control of the supplied buffer
		/// </summary>
		public void SetPending(T newPending)
		{
			lock (this)
			{
				if (_pending != null) _releasedSurfaces.Enqueue(_pending);
				_pending = newPending;
				_isPending = true;
			}
		}

		public void ReleaseSurface(T surface)
		{
			lock (this) _releasedSurfaces.Enqueue(surface);
		}

		/// <summary>
		/// returns the current buffer, making the most recent pending buffer (if there is such) as the new current first.
		/// </summary>
		public T GetCurrent()
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