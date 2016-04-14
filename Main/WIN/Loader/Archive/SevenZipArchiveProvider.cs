using System.ServiceModel;
using SevenZip;
using System;
using System.IO;
using System.Linq;


namespace FormatLibrary
{
    internal class OSVersionInfo
    {
        public enum SoftwareArchitecture
        {
            Unknown = 0,
            Bit32 = 1,
            Bit64 = 2
        }

        static public SoftwareArchitecture ProgramBits
        {
            get
            {
                var pbits = SoftwareArchitecture.Unknown;

                System.Collections.IDictionary test = Environment.GetEnvironmentVariables();

                switch (IntPtr.Size * 8)
                {
                    case 64:
                        pbits = SoftwareArchitecture.Bit64;
                        break;

                    case 32:
                        pbits = SoftwareArchitecture.Bit32;
                        break;

                    default:
                        pbits = SoftwareArchitecture.Unknown;
                        break;
                }

                return pbits;
            }
        }
    }

    internal abstract class SevenZipArchiveProvider : Archive
    {
        public override sealed event EventHandler<ArchiveExceptionEventArgs> onExceptionThrowed;

        protected abstract OutArchiveFormat OutFormat { get; }

        protected abstract InArchiveFormat InFormat { get; }

        protected SevenZipArchiveProvider()
            : base()
        {
            string path = (File.Exists("7z.dll") || File.Exists("7z.dll")) ?
                Path.GetFullPath((OSVersionInfo.ProgramBits == OSVersionInfo.SoftwareArchitecture.Bit64) ? "7z64.dll" : "7z.dll") :
                (OSVersionInfo.ProgramBits == OSVersionInfo.SoftwareArchitecture.Bit64) ?
                    Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory + "\\7z\\7z64.dll") :
                    Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory + "\\7z\\7z.dll");
            SevenZipCompressor.SetLibraryPath(path);
        }

        public override bool Compress(string OutputArchiveName, string[] CompressData, bool DeleteArchiveFiles = false, string Password = null)
        {
            for (int i = 0; i < CompressData.Count(); i++) CompressData[i] = Path.GetFullPath(CompressData[i]);
            try
            {
                var Compressor = new SevenZipCompressor();
                Compressor.ArchiveFormat = OutFormat;
                Compressor.CompressionMode = CompressionMode.Create;
                Compressor.TempFolderPath = Path.GetTempPath();
                Compressor.CompressionMethod = CompressionMethod.Lzma;
                Compressor.CompressionLevel = CompressionLevel.Normal;
                Compressor.ZipEncryptionMethod = ZipEncryptionMethod.Aes256;
                var CommonFilePath = GetCommonFilePath(CompressData);
                var Files = GetPathes(CompressData).ToDictionary(path => path.Replace(CommonFilePath, string.Empty));
                if (string.IsNullOrEmpty(Password)) Compressor.CompressFileDictionary(Files, OutputArchiveName);
                else Compressor.CompressFileDictionary(Files, OutputArchiveName, Password);
                Compressor = null;
                if (DeleteArchiveFiles) DeleteFiles(CompressData);
                return true;
            }
            catch (Exception ex)
            {
                if (onExceptionThrowed != null)
                    onExceptionThrowed(this, new ArchiveExceptionEventArgs(ex));
                else throw;
                return false;
            }
        }

        public override sealed string[] Decompress(string InputArchiveName, string ExtractDirectory, bool DeleteArchive = false, string Password = null)
        {
            if (!File.Exists(InputArchiveName)) return null;
            string[] result = null;
            if (ExtractDirectory == null)
                ExtractDirectory = string.Format("{0}\\{1}", Environment.CurrentDirectory, Path.GetFileNameWithoutExtension(InputArchiveName));
            InputArchiveName = Path.GetFullPath(InputArchiveName);
            if (!Directory.Exists(ExtractDirectory)) Directory.CreateDirectory(ExtractDirectory);
            using (SevenZipExtractor Extractor = (Password == null) ?
                new SevenZipExtractor(Path.GetFullPath(InputArchiveName)) :
                new SevenZipExtractor(Path.GetFullPath(InputArchiveName), Password))
            {
                try
                {
                    Extractor.ExtractArchive(Path.GetFullPath(ExtractDirectory));
                    result = Extractor.ArchiveFileNames.ToArray();
                }
                catch (Exception ex)
                {
                    if (onExceptionThrowed != null)
                        onExceptionThrowed(this, new ArchiveExceptionEventArgs(ex));
                    else throw;
                }
            }
            if (DeleteArchive) DeleteFiles(new[] { InputArchiveName });
            return result;
        }

        public override sealed string[] Decompress(Stream InputArchiveStream, string ExtractDirectory, string Password = null)
        {
            if (InputArchiveStream == null) return null;
            string[] result = null;
            if (ExtractDirectory == null) throw new ArgumentNullException();

            if (!Directory.Exists(ExtractDirectory)) Directory.CreateDirectory(ExtractDirectory);
            using (SevenZipExtractor Extractor = (Password == null) ?
                new SevenZipExtractor(InputArchiveStream, InFormat) :
                new SevenZipExtractor(InputArchiveStream, Password, InFormat))
            {
                try
                {
                    Extractor.ExtractArchive(Path.GetFullPath(ExtractDirectory));
                    result = Extractor.ArchiveFileNames.ToArray();
                }
                catch (Exception ex)
                {
                    if (onExceptionThrowed != null)
                        onExceptionThrowed(this, new ArchiveExceptionEventArgs(ex));
                    else throw;
                }
            }
            return result;
        }
    }

    internal sealed class SevenZipArchive : SevenZipArchiveProvider
    {
        private SevenZipArchive()
            : base()
        {

        }

        protected override OutArchiveFormat OutFormat
        {
            get { return OutArchiveFormat.SevenZip; }
        }

        protected override InArchiveFormat InFormat
        {
            get { return InArchiveFormat.SevenZip; }
        }
    }

    internal sealed class RarArchive : SevenZipArchiveProvider
    {
        private RarArchive()
            : base()
        {

        }

        protected override OutArchiveFormat OutFormat
        {
            get { throw new ActionNotSupportedException(); }
        }

        protected override InArchiveFormat InFormat
        {
            get { return InArchiveFormat.Rar; }
        }
    }
}
