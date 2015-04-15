using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.IsolatedStorage;

namespace Jellyfish.Virtu.Services
{
    public class IsolatedStorageService : StorageService
    {
        public IsolatedStorageService(Machine machine) : 
            base(machine)
        {
        }

        protected override void OnLoad(string fileName, Action<Stream> reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            using (var store = GetStore())
            {
                using (var stream = store.OpenFile(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    reader(stream);
                }
            }
        }

        protected override void OnSave(string fileName, Action<Stream> writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            using (var store = GetStore())
            {
                using (var stream = store.OpenFile(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    writer(stream);
                }
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        protected virtual IsolatedStorageFile GetStore()
        {
            return IsolatedStorageFile.GetUserStoreForApplication();
        }
    }
}
