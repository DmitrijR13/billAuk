using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SevenZip;

namespace STCLINE.KP50.Utility
{
    public class Archive
    {
        private List<string> lstArchivePathes;
        private string strLibPath;
        private static FileAttributes RemoveAttribute(FileAttributes attributes, FileAttributes attributesToRemove) { return attributes & ~attributesToRemove; }

        public Archive()
        {
            this.lstArchivePathes = new List<string>();
            this.strLibPath = String.Format("{0}\\{1}.dll", Environment.CurrentDirectory, Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") == "AMD64" ? "7z64" : "7z");
        }

        public bool AddFile(string FileName)
        {
            if (File.Exists(FileName))
            {
                this.lstArchivePathes.Add(Path.GetFullPath(FileName));
                return true;
            }
            return false;
        }

        public bool CompressDirectory(string InputDirectory, string OutputArchiveName, bool DeleteArchivedFiles)
        {
            if (!Directory.Exists(InputDirectory)) return false;
            SevenZipCompressor.SetLibraryPath(this.strLibPath);
            SevenZipCompressor szcComperssor = new SevenZipCompressor();

            switch (Path.GetExtension(OutputArchiveName))
            {
                case ".7z":
                    szcComperssor.ArchiveFormat = OutArchiveFormat.SevenZip;
                    break;
                case ".7zip":
                    szcComperssor.ArchiveFormat = OutArchiveFormat.SevenZip;
                    break;
                case ".bz":
                    szcComperssor.ArchiveFormat = OutArchiveFormat.BZip2;
                    break;
                case ".gzip":
                    szcComperssor.ArchiveFormat = OutArchiveFormat.GZip;
                    break;
                case ".tar":
                    szcComperssor.ArchiveFormat = OutArchiveFormat.Tar;
                    break;
                case ".xz":
                    szcComperssor.ArchiveFormat = OutArchiveFormat.XZ;
                    break;
                case ".zip":
                    szcComperssor.ArchiveFormat = OutArchiveFormat.Zip;
                    break;
                case ".rar":
                    szcComperssor.ArchiveFormat = OutArchiveFormat.Zip;
                    break;
                default: return false;
            }

            szcComperssor.CompressionMode = CompressionMode.Create;
            szcComperssor.CompressionMethod = CompressionMethod.Lzma;
            szcComperssor.CompressionLevel = CompressionLevel.Normal;
            szcComperssor.TempFolderPath = Path.GetTempPath();
            string[] strPatches = new string[this.lstArchivePathes.Count];
            szcComperssor.CompressDirectory(InputDirectory, OutputArchiveName);
            if (DeleteArchivedFiles) Directory.Delete(InputDirectory);
            return true;
        }

        public bool CompressDirectory(string InputDirectory, string OutputArchiveName, bool DeleteArchivedFiles, string Password)
        {
            if (!Directory.Exists(InputDirectory)) return false;
            SevenZipCompressor.SetLibraryPath(this.strLibPath);
            SevenZipCompressor szcComperssor = new SevenZipCompressor();

            switch (Path.GetExtension(OutputArchiveName))
            {
                case ".7z":
                    szcComperssor.ArchiveFormat = OutArchiveFormat.SevenZip;
                    break;
                case ".7zip":
                    szcComperssor.ArchiveFormat = OutArchiveFormat.SevenZip;
                    break;
                case ".bz":
                    szcComperssor.ArchiveFormat = OutArchiveFormat.BZip2;
                    break;
                case ".gzip":
                    szcComperssor.ArchiveFormat = OutArchiveFormat.GZip;
                    break;
                case ".tar":
                    szcComperssor.ArchiveFormat = OutArchiveFormat.Tar;
                    break;
                case ".xz":
                    szcComperssor.ArchiveFormat = OutArchiveFormat.XZ;
                    break;
                case ".zip":
                    szcComperssor.ArchiveFormat = OutArchiveFormat.Zip;
                    break;
                case ".rar":
                    szcComperssor.ArchiveFormat = OutArchiveFormat.Zip;
                    break;
                default: return false;
            }

            szcComperssor.CompressionMode = CompressionMode.Create;
            szcComperssor.TempFolderPath = Path.GetTempPath();
            szcComperssor.CompressionMethod = CompressionMethod.Lzma;
            szcComperssor.CompressionLevel = CompressionLevel.Normal;
            szcComperssor.CustomParameters.Add("pass", Password);
            szcComperssor.ZipEncryptionMethod = ZipEncryptionMethod.Aes256;
            string[] strPatches = new string[this.lstArchivePathes.Count];
            szcComperssor.CompressDirectory(InputDirectory, OutputArchiveName);
            if (DeleteArchivedFiles) Directory.Delete(InputDirectory);
            return true;
        }

        public bool CompressFiles(string OutputArchiveName, bool DeleteArchivedFiles)
        {
            if (this.lstArchivePathes.Count == 0) return false;
            SevenZipCompressor.SetLibraryPath(this.strLibPath);
            SevenZipCompressor szcComperssor = new SevenZipCompressor();

            switch (Path.GetExtension(OutputArchiveName))
            {
                case ".7z":
                    szcComperssor.ArchiveFormat = OutArchiveFormat.SevenZip;
                    break;
                case ".7zip":
                    szcComperssor.ArchiveFormat = OutArchiveFormat.SevenZip;
                    break;
                case ".bz":
                    szcComperssor.ArchiveFormat = OutArchiveFormat.BZip2;
                    break;
                case ".gzip":
                    szcComperssor.ArchiveFormat = OutArchiveFormat.GZip;
                    break;
                case ".tar":
                    szcComperssor.ArchiveFormat = OutArchiveFormat.Tar;
                    break;
                case ".xz":
                    szcComperssor.ArchiveFormat = OutArchiveFormat.XZ;
                    break;
                case ".zip":
                    szcComperssor.ArchiveFormat = OutArchiveFormat.Zip;
                    break;
                case ".rar":
                    szcComperssor.ArchiveFormat = OutArchiveFormat.Zip;
                    break;
                default: return false;
            }

            szcComperssor.CompressionMode = CompressionMode.Create;
            szcComperssor.TempFolderPath = Path.GetTempPath();
            szcComperssor.CompressionMethod = CompressionMethod.Lzma;
            szcComperssor.CompressionLevel = CompressionLevel.Normal;
            string[] strPatches = new string[this.lstArchivePathes.Count];
            for (int i = 0; i < this.lstArchivePathes.Count; i++) strPatches[i] = this.lstArchivePathes[i];
            szcComperssor.CompressFiles(OutputArchiveName, strPatches);

            if (DeleteArchivedFiles)
            {
                foreach (string strPath in this.lstArchivePathes)
                {
                    FileAttributes attributes = File.GetAttributes(strPath);
                    if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        attributes = RemoveAttribute(attributes, FileAttributes.ReadOnly);
                        File.SetAttributes(strPath, attributes);
                    }
                    File.Delete(strPath);
                }
            }
            this.lstArchivePathes.Clear();
            return true;
        }

        public bool CompressFiles(string OutputArchiveName, bool DeleteArchivedFiles, string Password)
        {
            if (this.lstArchivePathes.Count == 0) return false;
            SevenZipCompressor.SetLibraryPath(this.strLibPath);
            SevenZipCompressor szcComperssor = new SevenZipCompressor();

            switch (Path.GetExtension(OutputArchiveName))
            {
                case ".7z":
                    szcComperssor.ArchiveFormat = OutArchiveFormat.SevenZip;
                    break;
                case ".7zip":
                    szcComperssor.ArchiveFormat = OutArchiveFormat.SevenZip;
                    break;
                case ".bz":
                    szcComperssor.ArchiveFormat = OutArchiveFormat.BZip2;
                    break;
                case ".gzip":
                    szcComperssor.ArchiveFormat = OutArchiveFormat.GZip;
                    break;
                case ".tar":
                    szcComperssor.ArchiveFormat = OutArchiveFormat.Tar;
                    break;
                case ".xz":
                    szcComperssor.ArchiveFormat = OutArchiveFormat.XZ;
                    break;
                case ".zip":
                    szcComperssor.ArchiveFormat = OutArchiveFormat.Zip;
                    break;
                case ".rar":
                    szcComperssor.ArchiveFormat = OutArchiveFormat.Zip;
                    break;
                default: return false;
            }

            szcComperssor.CompressionMode = CompressionMode.Create;
            szcComperssor.TempFolderPath = Path.GetTempPath();
            szcComperssor.CompressionMethod = CompressionMethod.Lzma;
            szcComperssor.CompressionLevel = CompressionLevel.Normal;
            szcComperssor.CustomParameters.Add("pass", Password);
            szcComperssor.ZipEncryptionMethod = ZipEncryptionMethod.Aes256;
            string[] strPatches = new string[this.lstArchivePathes.Count];
            for (int i = 0; i < this.lstArchivePathes.Count; i++) strPatches[i] = this.lstArchivePathes[i];
            szcComperssor.CompressFiles(OutputArchiveName, strPatches);

            if (DeleteArchivedFiles)
            {
                foreach (string strPath in this.lstArchivePathes)
                {
                    FileAttributes attributes = File.GetAttributes(strPath);
                    if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        attributes = RemoveAttribute(attributes, FileAttributes.ReadOnly);
                        File.SetAttributes(strPath, attributes);
                    }
                    File.Delete(strPath);
                }
            }
            this.lstArchivePathes.Clear();
            return true;
        }

        public string Decompress(string InputArchiveName, bool DeleteArchive)
        {
            try
            {
                SevenZipExtractor.SetLibraryPath(this.strLibPath);
                DirectoryInfo exDirectorey = Directory.CreateDirectory(Path.GetFileNameWithoutExtension(InputArchiveName));
                using (SevenZipExtractor szeExtractor = new SevenZipExtractor(Path.GetFullPath(InputArchiveName))) szeExtractor.ExtractArchive(exDirectorey.FullName);
                if (DeleteArchive)
                {
                    FileAttributes attributes = File.GetAttributes(Path.GetFullPath(InputArchiveName));
                    if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        attributes = RemoveAttribute(attributes, FileAttributes.ReadOnly);
                        File.SetAttributes(InputArchiveName, attributes);
                    }
                    File.Delete(InputArchiveName);
                }
                return exDirectorey.FullName;
            }
            catch (Exception) { }
            return null;
        }

        public bool Decompress(string InputArchiveName, string ExtractDirectory, bool DeleteArchive)
        {
            try
            {
                SevenZipExtractor.SetLibraryPath(this.strLibPath);
                if (!Directory.Exists(ExtractDirectory)) Directory.CreateDirectory(ExtractDirectory);
                using (SevenZipExtractor szeExtractor = new SevenZipExtractor(Path.GetFullPath(InputArchiveName))) szeExtractor.ExtractArchive(ExtractDirectory);
                if (DeleteArchive)
                {
                    FileAttributes attributes = File.GetAttributes(Path.GetFullPath(InputArchiveName));
                    if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        attributes = RemoveAttribute(attributes, FileAttributes.ReadOnly);
                        File.SetAttributes(InputArchiveName, attributes);
                    }
                    File.Delete(InputArchiveName);
                }
                return true;
            }
            catch (Exception) { }
            return false;
        }
    }
}
