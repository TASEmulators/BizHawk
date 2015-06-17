using System;

namespace Jellyfish.Library
{
    public abstract class DisposableBase : IDisposable
    {
        protected DisposableBase()
        {
        }

        ~DisposableBase()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
