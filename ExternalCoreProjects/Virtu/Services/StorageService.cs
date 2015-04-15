using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Security;
using Jellyfish.Virtu.Properties;

namespace Jellyfish.Virtu.Services
{
    public abstract class StorageService : MachineService
    {
        protected StorageService(Machine machine) : 
            base(machine)
        {
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public bool Load(string fileName, Action<Stream> reader)
        {
            try
            {
                DebugService.WriteMessage("Loading file '{0}'", fileName);
                OnLoad(fileName, reader);
            }
            catch (Exception ex)
            {
                DebugService.WriteMessage(ex.ToString());
                return false;
            }

            return true;
        }

#if !WINDOWS
        [SecuritySafeCritical]
#endif
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public static bool LoadFile(Stream stream, Action<Stream> reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            try
            {
                DebugService.Default.WriteMessage("Loading file '{0}'", "STREAM");
                {
                    reader(stream);
                }
            }
            catch (Exception ex)
            {
                DebugService.Default.WriteMessage(ex.ToString());
                return false;
            }

            return true;
        }

#if !WINDOWS
        [SecuritySafeCritical]
#endif
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public static bool LoadFile(FileInfo fileInfo, Action<Stream> reader)
        {
            if (fileInfo == null)
            {
                throw new ArgumentNullException("fileInfo");
            }
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            try
            {
                DebugService.Default.WriteMessage("Loading file '{0}'", fileInfo.Name);
                using (var stream = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    reader(stream);
                }
            }
            catch (Exception ex)
            {
                DebugService.Default.WriteMessage(ex.ToString());
                return false;
            }

            return true;
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public static bool LoadResource(string resourceName, Action<Stream> reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            try
            {
                DebugService.Default.WriteMessage("Loading resource '{0}'", resourceName);
								using (var stream = File.OpenRead(resourceName))
									reader(stream);
								//using (var stream = GetResourceStream(resourceName))
								//{
								//    reader(stream);
								//}
            }
            catch (Exception ex)
            {
                DebugService.Default.WriteMessage(ex.ToString());
                return false;
            }

            return true;
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public bool Save(string fileName, Action<Stream> writer)
        {
            try
            {
                DebugService.WriteMessage("Saving file '{0}'", fileName);
                OnSave(fileName, writer);
            }
            catch (Exception ex)
            {
                DebugService.WriteMessage(ex.ToString());
                return false;
            }

            return true;
        }

#if !WINDOWS
        [SecuritySafeCritical]
#endif
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public static bool SaveFile(string fileName, Action<Stream> writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            try
            {
                DebugService.Default.WriteMessage("Saving file '{0}'", fileName);
                using (var stream = File.Open(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    writer(stream);
                }
            }
            catch (Exception ex)
            {
                DebugService.Default.WriteMessage(ex.ToString());
                return false;
            }

            return true;
        }

#if !WINDOWS
        [SecuritySafeCritical]
#endif
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public static bool SaveFile(FileInfo fileInfo, Action<Stream> writer)
        {
            if (fileInfo == null)
            {
                throw new ArgumentNullException("fileInfo");
            }
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            try
            {
                DebugService.Default.WriteMessage("Saving file '{0}'", fileInfo.Name);
                using (var stream = fileInfo.Open(FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    writer(stream);
                }
            }
            catch (Exception ex)
            {
                DebugService.Default.WriteMessage(ex.ToString());
                return false;
            }

            return true;
        }

        protected abstract void OnLoad(string fileName, Action<Stream> reader);

        protected abstract void OnSave(string fileName, Action<Stream> writer);

        private static Stream GetResourceStream(string resourceName)
        {
            resourceName = "Jellyfish.Virtu." + resourceName.Replace('/', '.');
            var resourceStream = typeof(StorageService).Assembly.GetManifestResourceStream(resourceName);
            if (resourceStream == null)
            {
                throw new FileNotFoundException(string.Format(CultureInfo.CurrentUICulture, Strings.ResourceNotFound, resourceName));
            }

            return resourceStream;
        }
    }
}
