using System.Collections.Generic;

namespace BizHawk.Common
{
	public class UndoHistory<T>
	{
		private readonly T _blankState;

		/// <remarks>
		/// <c>1</c>-based, so the "current timeline" includes all of <see cref="_history"/> up to and not including <c>_history[_curPos]</c>
		/// (that is, all of <see cref="_history"/> has been redo'd when <c>_curPos == _history.Count</c>)
		/// </remarks>
		private int _curPos;

		private readonly IList<T> _history = new List<T>();

		public bool CanRedo => Enabled && _curPos < _history.Count;

		public bool CanUndo => Enabled && _curPos > 1;

		public bool Enabled { get; }

		public bool HasHistory => Enabled && _history.Count != 0;

		/// <remarks>
		/// <see cref="_history"/> can actually grow to this + 1<br/>
		/// TODO fix that by moving the <c>.RemoveAt(0)</c> loop in <see cref="AddState"/> to AFTER the insertion<br/>
		/// TODO old code assumed the setter was public, so pruning multiple states from start may have been required if this changed between insertions
		/// </remarks>
		public int MaxUndoLevels { get; } = 5;

		/// <param name="blankState">
		/// returned from calls to <see cref="Undo"/>/<see cref="Redo"/> when there is nothing to undo/redo, or
		/// when either method is called while disabled
		/// </param>
		public UndoHistory(bool enabled, T blankState)
		{
			_blankState = blankState;
			Enabled = enabled;
		}

		public void AddState(T newState)
		{
			if (!Enabled) return;
			while (_history.Count > _curPos) _history.RemoveAt(_history.Count - 1); // prune "alternate future timeline" that we're about to overwrite
			while (_history.Count > MaxUndoLevels) _history.RemoveAt(0); // prune from start when at user-defined limit
			_history.Add(newState);
			_curPos = _history.Count;
		}

		public void Clear()
		{
			_history.Clear();
			_curPos = 0;
		}

		public T Redo() => CanRedo && Enabled ? _history[++_curPos - 1] : _blankState;

		public T Undo() => CanUndo && Enabled ? _history[--_curPos - 1] : _blankState;
	}
}
