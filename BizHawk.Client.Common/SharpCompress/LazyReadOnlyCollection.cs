using System;
using System.Collections;
using System.Collections.Generic;

namespace SharpCompress
{
    internal class LazyReadOnlyCollection<T> : ICollection<T>
    {
        private readonly List<T> backing = new List<T>();
        private readonly IEnumerator<T> source;
        private bool fullyLoaded;

        public LazyReadOnlyCollection(IEnumerable<T> source)
        {
            this.source = source.GetEnumerator();
        }

        private class LazyLoader : IEnumerator<T>
        {
            private readonly LazyReadOnlyCollection<T> lazyReadOnlyCollection;
            private bool disposed;
            private int index = -1;

            internal LazyLoader(LazyReadOnlyCollection<T> lazyReadOnlyCollection)
            {
                this.lazyReadOnlyCollection = lazyReadOnlyCollection;
            }

            #region IEnumerator<T> Members

            public T Current => lazyReadOnlyCollection.backing[index];

            #endregion

            #region IDisposable Members

            public void Dispose()
            {
                if (!disposed)
                {
                    disposed = true;
                }
            }

            #endregion

            #region IEnumerator Members

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (index + 1 < lazyReadOnlyCollection.backing.Count)
                {
                    index++;
                    return true;
                }
                if (!lazyReadOnlyCollection.fullyLoaded && lazyReadOnlyCollection.source.MoveNext())
                {
                    lazyReadOnlyCollection.backing.Add(lazyReadOnlyCollection.source.Current);
                    index++;
                    return true;
                }
                lazyReadOnlyCollection.fullyLoaded = true;
                return false;
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }

            #endregion
        }

        internal void EnsureFullyLoaded()
        {
            if (!fullyLoaded)
            {
                this.ForEach(x => { });
                fullyLoaded = true;
            }
        }

        internal IEnumerable<T> GetLoaded()
        {
            return backing;
        }

        #region ICollection<T> Members

        public void Add(T item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(T item)
        {
            EnsureFullyLoaded();
            return backing.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            EnsureFullyLoaded();
            backing.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get
            {
                EnsureFullyLoaded();
                return backing.Count;
            }
        }

        public bool IsReadOnly => true;

        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }

        #endregion

        #region IEnumerable<T> Members

        //TODO check for concurrent access
        public IEnumerator<T> GetEnumerator()
        {
            return new LazyLoader(this);
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}