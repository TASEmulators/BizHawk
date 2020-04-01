/***********************************************************************
Copyright 2016 Stefan Hausotte

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

*************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using PeNet.ImpHash;
using PeNet.Structures;
using PeNet.Utilities;

namespace PeNet
{
    /// <summary>
    ///     This class represents a Portable Executable (PE) file and makes the different
    ///     header and properties accessible.
    /// </summary>
    public partial class PeFile
    {
        private readonly DataDirectoryParsers _dataDirectoryParsers;
        private readonly NativeStructureParsers _nativeStructureParsers;
        private readonly DotNetStructureParsers _dotNetStructureParsers;

        /// <summary>
        ///     The PE binary as a byte array.
        /// </summary>
        public readonly byte[] Buff;

        private string _impHash;
        private string _md5;
        private string _sha1;
        private string _sha256;

        /// <summary>
        ///     Create a new PeFile object.
        /// </summary>
        /// <param name="buff">A PE file a byte array.</param>
        public PeFile(byte[] buff)
        {
            Buff = buff;
            _nativeStructureParsers = new NativeStructureParsers(Buff);

            _dataDirectoryParsers = new DataDirectoryParsers(
                Buff,
                ImageNtHeaders?.OptionalHeader?.DataDirectory,
                ImageSectionHeaders,
                Is32Bit
                );

            _dotNetStructureParsers = new DotNetStructureParsers(
                Buff,
                ImageComDescriptor,
                ImageSectionHeaders
                );
        }

        /// <summary>
        ///     Create a new PeFile object.
        /// </summary>
        /// <param name="peFile">Path to a PE file.</param>
        public PeFile(string peFile)
            : this(File.ReadAllBytes(peFile))
        {
            FileLocation = peFile;
        }

        /// <summary>
        ///     List with all exceptions that have occurred during the PE header parsing.
        /// </summary>
        public List<Exception> Exceptions { get; } = new List<Exception>();

        /// <summary>
        ///     Returns true if the Exception Dir, Export Dir, Import Dir,
        ///     Resource Dir and Security Dir are valid and the MZ header is set.
        /// </summary>
        public bool IsValidPeFile => HasValidExceptionDir
                                     && HasValidExportDir
                                     && HasValidImportDir
                                     && HasValidResourceDir
                                     && HasValidSecurityDir
                                     && (ImageDosHeader.e_magic == 0x5a4d);

        /// <summary>
        ///     Returns true if the Export directory is valid.
        /// </summary>
        public bool HasValidExportDir => ImageExportDirectory != null;

        /// <summary>
        ///     Returns true if the Import directory is valid.
        /// </summary>
        public bool HasValidImportDir => ImageImportDescriptors != null;

        /// <summary>
        ///     Returns true if the Resource directory is valid.
        /// </summary>
        public bool HasValidResourceDir => ImageResourceDirectory != null;

        /// <summary>
        ///     Returns true if the Exception directory is valid.
        /// </summary>
        public bool HasValidExceptionDir => Exceptions != null;

        /// <summary>
        ///     Returns true if the Security directory is valid.
        /// </summary>
        public bool HasValidSecurityDir => WinCertificate != null;

        /// <summary>
        ///     Returns true if the Relocation Directory is valid.
        /// </summary>
        public bool HasValidRelocDir => ImageRelocationDirectory != null;

        /// <summary>
        ///     Returns true if the COM+ 2 (CLI) directory is valid.
        /// </summary>
        public bool HasValidComDescriptor => ImageComDescriptor != null;

        /// <summary>
        ///     Returns true if the DLL flag in the
        ///     File Header is set.
        /// </summary>
        public bool IsDLL
            =>
                (ImageNtHeaders.FileHeader.Characteristics & (ushort) Constants.FileHeaderCharacteristics.IMAGE_FILE_DLL) >
                0;

        /// <summary>
        ///     Returns true if the Executable flag in the
        ///     File Header is set.
        /// </summary>
        public bool IsEXE
            =>
                (ImageNtHeaders.FileHeader.Characteristics &
                 (ushort) Constants.FileHeaderCharacteristics.IMAGE_FILE_EXECUTABLE_IMAGE) > 0;

        /// <summary>
        ///     Returns true if the PE file is signed. It
        ///     does not check if the signature is valid!
        /// </summary>
        public bool IsSigned => PKCS7 != null;

        /// <summary>
        ///     Returns true if the PE file is x64.
        /// </summary>
        public bool Is64Bit => Buff.BytesToUInt16(ImageDosHeader.e_lfanew + 0x4) ==
                               (ushort) Constants.FileHeaderMachine.IMAGE_FILE_MACHINE_AMD64;

        /// <summary>
        ///     Returns true if the PE file is x32.
        /// </summary>
        public bool Is32Bit => !Is64Bit;

        /// <summary>
        ///     Access the IMAGE_DOS_HEADER of the PE file.
        /// </summary>
        public IMAGE_DOS_HEADER ImageDosHeader => _nativeStructureParsers.ImageDosHeader;

        /// <summary>
        ///     Access the IMAGE_NT_HEADERS of the PE file.
        /// </summary>
        public IMAGE_NT_HEADERS ImageNtHeaders => _nativeStructureParsers.ImageNtHeaders;

        /// <summary>
        ///     Access the IMAGE_SECTION_HEADERS of the PE file.
        /// </summary>
        public IMAGE_SECTION_HEADER[] ImageSectionHeaders => _nativeStructureParsers.ImageSectionHeaders;

        /// <summary>
        ///     Access the IMAGE_EXPORT_DIRECTORY of the PE file.
        /// </summary>
        public IMAGE_EXPORT_DIRECTORY ImageExportDirectory => _dataDirectoryParsers.ImageExportDirectories;

        /// <summary>
        ///     Access the IMAGE_IMPORT_DESCRIPTOR array of the PE file.
        /// </summary>
        public IMAGE_IMPORT_DESCRIPTOR[] ImageImportDescriptors => _dataDirectoryParsers.ImageImportDescriptors;

        /// <summary>
        ///     Access the IMAGE_BASE_RELOCATION array of the PE file.
        /// </summary>
        public IMAGE_BASE_RELOCATION[] ImageRelocationDirectory => _dataDirectoryParsers.ImageBaseRelocations;

        /// <summary>
        ///     Access the IMAGE_DEBUG_DIRECTORY of the PE file.
        /// </summary>
        public IMAGE_DEBUG_DIRECTORY ImageDebugDirectory => _dataDirectoryParsers.ImageDebugDirectory;

        /// <summary>
        ///     Access the exported functions as an array of parsed objects.
        /// </summary>
        public ExportFunction[] ExportedFunctions => _dataDirectoryParsers.ExportFunctions;

        /// <summary>
        ///     Access the imported functions as an array of parsed objects.
        /// </summary>
        public ImportFunction[] ImportedFunctions => _dataDirectoryParsers.ImportFunctions;

        /// <summary>
        ///     Access the IMAGE_RESOURCE_DIRECTORY of the PE file.
        /// </summary>
        public IMAGE_RESOURCE_DIRECTORY ImageResourceDirectory => _dataDirectoryParsers.ImageResourceDirectory;

        /// <summary>
        ///     Access the array of RUNTIME_FUNCTION from the Exception header.
        /// </summary>
        public RUNTIME_FUNCTION[] RuntimeFunctions => _dataDirectoryParsers.RuntimeFunctions;

        /// <summary>
        ///     Access the WIN_CERTIFICATE from the Security header.
        /// </summary>
        public WIN_CERTIFICATE WinCertificate => _dataDirectoryParsers.WinCertificate;

        /// <summary>
        /// Access the IMAGE_BOUND_IMPORT_DESCRIPTOR form the data directory.
        /// </summary>
        public IMAGE_BOUND_IMPORT_DESCRIPTOR ImageBoundImportDescriptor => _dataDirectoryParsers.ImageBoundImportDescriptor;

        /// <summary>
        /// Access the IMAGE_TLS_DIRECTORY from the data directory.
        /// </summary>
        public IMAGE_TLS_DIRECTORY ImageTlsDirectory => _dataDirectoryParsers.ImageTlsDirectory;

        /// <summary>
        /// Access the IMAGE_DELAY_IMPORT_DESCRIPTOR from the data directory.
        /// </summary>
        public IMAGE_DELAY_IMPORT_DESCRIPTOR ImageDelayImportDescriptor => _dataDirectoryParsers.ImageDelayImportDescriptor;

        /// <summary>
        /// Access the IMAGE_LOAD_CONFIG_DIRECTORY from the data directory.
        /// </summary>
        public IMAGE_LOAD_CONFIG_DIRECTORY ImageLoadConfigDirectory => _dataDirectoryParsers.ImageLoadConfigDirectory;
    
        /// <summary>
        /// Access the IMAGE_COR20_HEADER (COM Descriptor/CLI) from the data directory.
        /// </summary>
        public IMAGE_COR20_HEADER ImageComDescriptor => _dataDirectoryParsers.ImageComDescriptor;

        /// <summary>
        ///     A X509 PKCS7 signature if the PE file was digitally signed with such
        ///     a signature.
        /// </summary>
        public X509Certificate2 PKCS7 => _dataDirectoryParsers.PKCS7;

        /// <summary>
        ///     Access the METADATAHDR from the COM/CLI header.
        /// </summary>
        public METADATAHDR MetaDataHdr => _dotNetStructureParsers.MetaDataHdr;

        /// <summary>
        /// Meta Data Stream #String as a parsed list of strings.
        /// </summary>
        public List<string> MetaDataStreamString => _dotNetStructureParsers.MetaDataStreamString;

        /// <summary>
        /// Meta Data Stream #US as a parsed list of strings.
        /// </summary>
        public List<string> MetaDataStreamUS => _dotNetStructureParsers.MedaDataStreamUS;

        /// <summary>
        ///     Access the Meta Data Stream Tables Header from the list of
        ///     Meta Data Streams of the .Net header.
        /// </summary>
        public METADATATABLESHDR MetaDataStreamTablesHeader => _dotNetStructureParsers.MetaDataStreamTablesHeader;

        /// <summary>
        ///     The SHA-256 hash sum of the binary.
        /// </summary>
        public string SHA256 => _sha256 ?? (_sha256 = Hashes.Sha256(Buff));

        /// <summary>
        ///     The SHA-1 hash sum of the binary.
        /// </summary>
        public string SHA1 => _sha1 ?? (_sha1 = Hashes.Sha1(Buff));

        /// <summary>
        ///     The MD5 of hash sum of the binary.
        /// </summary>
        public string MD5 => _md5 ?? (_md5 = Hashes.MD5(Buff));

        /// <summary>
        ///     The Import Hash of the binary if any imports are
        ///     given else null;
        /// </summary>
        public string ImpHash => _impHash ?? (_impHash = new ImportHash(ImportedFunctions).ImpHash);

        /// <summary>
        ///     Returns the file size in bytes.
        /// </summary>
        public int FileSize => Buff.Length;

        /// <summary>
        ///     FileLocation of the PE file if it was opened by location.
        /// </summary>
        public string FileLocation { get; private set; }

        /// <summary>
        ///     Checks if cert is from a trusted CA with a valid certificate chain.
        /// </summary>
        /// <param name="online">Check certificate chain online or off-line.</param>
        /// <returns>True of cert chain is valid and from a trusted CA.</returns>
        public bool IsValidCertChain(bool online)
        {
            if (!IsSigned)
                return false;

            return SignatureInformation.IsValidCertChain(PKCS7, online);
        }

        /// <summary>
        ///     Get an object which holds information about
        ///     the Certificate Revocation Lists of the signing
        ///     certificate if any is present.
        /// </summary>
        /// <returns>Certificate Revocation List information or null if binary is not signed.</returns>
        public CrlUrlList GetCrlUrlList()
        {
            if (PKCS7 == null)
                return null;

            CrlUrlList list = null;
            try
            {
                list = new CrlUrlList(PKCS7);
            }
            catch (Exception exception)
            {
                Exceptions.Add(exception);
            }

            return list;
        }

        /// <summary>
        ///     Tries to parse the PE file and checks all directories.
        /// </summary>
        /// <param name="file">Path to a possible PE file.</param>
        /// <returns>
        ///     True if the file could be parsed as a PE file and
        ///     all directories are valid.
        /// </returns>
        public static bool IsValidPEFile(string file)
        {
            PeFile pe;
            try
            {
                pe = new PeFile(file);
            }
            catch
            {
                return false;
            }
            return pe.IsValidPeFile;
        }

        /// <summary>
        ///     Tests is a file is a PE file based on the MZ
        ///     header. It is not checked if the PE file is correct
        ///     in all other parts.
        /// </summary>
        /// <param name="file">Path to a possible PE file.</param>
        /// <returns>True if the MZ header is set.</returns>
        public static bool IsPEFile(string file)
        {
            var buff = File.ReadAllBytes(file);
            return PeValidator.IsPeValidPeFile(buff);
        }

        /// <summary>
        ///     Returns if the file is a PE file and 64 Bit.
        /// </summary>
        /// <param name="file">Path to a possible PE file.</param>
        /// <returns>True if file is PE and x64.</returns>
        public static bool Is64BitPeFile(string file)
        {
            var buff = File.ReadAllBytes(file);
            IMAGE_DOS_HEADER dosHeader;
            bool is64;
            try
            {
                dosHeader = new IMAGE_DOS_HEADER(buff, 0);
                is64 = buff.BytesToUInt16(dosHeader.e_lfanew + 0x4) ==
                       (ushort) Constants.FileHeaderMachine.IMAGE_FILE_MACHINE_AMD64;
            }
            catch (Exception)
            {
                return false;
            }

            return (dosHeader.e_magic == 0x5a4d) && is64;
        }

        /// <summary>
        ///     Returns if the file is a PE file and 32 Bit.
        /// </summary>
        /// <param name="file">Path to a possible PE file.</param>
        /// <returns>True if file is PE and x32.</returns>
        public static bool Is32BitPeFile(string file)
        {
            var buff = File.ReadAllBytes(file);
            IMAGE_DOS_HEADER dosHeader;
            bool is32;
            try
            {
                dosHeader = new IMAGE_DOS_HEADER(buff, 0);
                is32 = buff.BytesToUInt16(dosHeader.e_lfanew + 0x4) ==
                       (ushort) Constants.FileHeaderMachine.IMAGE_FILE_MACHINE_I386;
            }
            catch (Exception)
            {
                return false;
            }

            return (dosHeader.e_magic == 0x5a4d) && is32;
        }

        /// <summary>
        ///     Returns if the PE file is a EXE, DLL and which architecture
        ///     is used (32/64).
        ///     Architectures: "I386", "AMD64", "UNKNOWN"
        ///     DllOrExe: "DLL", "EXE", "UNKNOWN"
        /// </summary>
        /// <returns>
        ///     A string "architecture_dllOrExe".
        ///     E.g. "AMD64_DLL"
        /// </returns>
        public string GetFileType()
        {
            string fileType;

            switch (ImageNtHeaders.FileHeader.Machine)
            {
                case (ushort) Constants.FileHeaderMachine.IMAGE_FILE_MACHINE_I386:
                    fileType = "I386";
                    break;
                case (ushort) Constants.FileHeaderMachine.IMAGE_FILE_MACHINE_AMD64:
                    fileType = "AMD64";
                    break;
                default:
                    fileType = "UNKNOWN";
                    break;
            }

            if ((ImageNtHeaders.FileHeader.Characteristics & (ushort) Constants.FileHeaderCharacteristics.IMAGE_FILE_DLL) !=
                0)
                fileType += "_DLL";
            else if ((ImageNtHeaders.FileHeader.Characteristics &
                      (ushort) Constants.FileHeaderCharacteristics.IMAGE_FILE_EXECUTABLE_IMAGE) != 0)
                fileType += "_EXE";
            else
                fileType += "_UNKNOWN";


            return fileType;
        }


        /// <summary>
        ///     Creates a string representation of the objects
        ///     properties.
        /// </summary>
        /// <returns>PE Header properties as a string.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder("PE HEADER:\n");
            sb.Append(this.PropertiesToString("{0,-15}:\t{1,10:X}\n"));
            return sb.ToString();
        }
    }
}