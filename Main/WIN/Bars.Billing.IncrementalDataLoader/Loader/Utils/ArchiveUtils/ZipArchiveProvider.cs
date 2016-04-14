using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ionic.Zip;

namespace Bars.Billing.IncrementalDataLoader.Utils.ArchiveUtils
{
    internal sealed class ZipArchive : Archive
    {
        public override event EventHandler<ArchiveExceptionEventArgs> onExceptionThrowed;

        private ZipArchive()
            : base()
        {

        }

        public override sealed bool Compress(string OutputArchiveName, string[] CompressData, bool DeleteArchiveFiles = false, string Password = null)
        {
            bool result = false;
            using (var zip = new ZipFile(Encoding.GetEncoding("cp866")))
            {
                try
                {
                    zip.Strategy = Ionic.Zlib.CompressionStrategy.Default;
                    zip.TempFileFolder = Path.GetTempPath();
                    zip.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                    zip.CompressionMethod = Ionic.Zip.CompressionMethod.Deflate;
                    if (!string.IsNullOrEmpty(Password))
                    {
                        zip.Password = Password;
                        zip.Encryption = EncryptionAlgorithm.WinZipAes256;
                    }
                    string CommonFilePath = GetCommonFilePath(CompressData);
                    foreach (string path in GetPathes(CompressData))
                        zip.AddFile(path,
                            path.Replace(CommonFilePath, string.Empty).Replace(Path.GetFileName(path), string.Empty));
                    zip.Save(OutputArchiveName);
                    if (DeleteArchiveFiles) DeleteFiles(CompressData);
                    result = true;
                }
                catch (Exception ex)
                {
                    if (onExceptionThrowed != null)
                        onExceptionThrowed(this, new ArchiveExceptionEventArgs(ex));
                    else throw;
                }
                return result;
            }
        }

        public override sealed string[] Decompress(string InputArchiveName, string ExtractDirectory, bool DeleteArchive = false, string Password = null)
        {
            if (!File.Exists(InputArchiveName)) return null;
            var result = new List<string>();
            if (ExtractDirectory == null)
                ExtractDirectory = string.Format("{0}\\{1}", Environment.CurrentDirectory, Path.GetFileNameWithoutExtension(InputArchiveName));
            try
            {
                if (!Directory.Exists(ExtractDirectory)) Directory.CreateDirectory(ExtractDirectory);
                if (ExtractDirectory != null)
                {
                    using (
                        ZipFile zip = ZipFile.Read(InputArchiveName,
                            new ReadOptions {Encoding = Encoding.GetEncoding("cp866")}))
                    {
                        foreach (ZipEntry e in zip)
                        {
                            if (string.IsNullOrEmpty(Password))
                                e.Extract(ExtractDirectory, ExtractExistingFileAction.OverwriteSilently);
                            else
                                e.ExtractWithPassword(ExtractDirectory, ExtractExistingFileAction.OverwriteSilently,
                                    Password);
                            result.Add(e.FileName);
                        }
                    }
                    if (DeleteArchive) DeleteFiles(new [] {InputArchiveName});
                }
            }
            catch (BadPasswordException ex)
            {
                if (onExceptionThrowed != null)
                    onExceptionThrowed(this, new ArchiveExceptionEventArgs(ex));
                else throw;
            }
            catch (Exception ex)
            {
                if (onExceptionThrowed != null)
                    onExceptionThrowed(this, new ArchiveExceptionEventArgs(ex));
                else throw;
            }
            return result.Count > 0 ? result.ToArray() : null;
        }

        public override sealed string[] Decompress(Stream InputArchiveStream, string ExtractDirectory, string Password = null)
        {
            if (InputArchiveStream == null) return null;
            var result = new List<string>();
            if (ExtractDirectory == null) throw new ArgumentNullException();
            try
            {
                if (!Directory.Exists(ExtractDirectory)) Directory.CreateDirectory(ExtractDirectory);
                if (ExtractDirectory != null)
                {
                    using (
                        ZipFile zip = ZipFile.Read(InputArchiveStream,
                            new ReadOptions {Encoding = Encoding.GetEncoding("cp866")}))
                    {
                        foreach (ZipEntry e in zip)
                        {
                            if (string.IsNullOrEmpty(Password))
                                e.Extract(ExtractDirectory, ExtractExistingFileAction.OverwriteSilently);
                            else
                                e.ExtractWithPassword(ExtractDirectory, ExtractExistingFileAction.OverwriteSilently,
                                    Password);
                            result.Add(e.FileName);
                        }
                    }
                }
            }
            catch (BadPasswordException ex)
            {
                if (onExceptionThrowed != null)
                    onExceptionThrowed(this, new ArchiveExceptionEventArgs(ex));
                else throw;
            }
            catch (Exception ex)
            {
                if (onExceptionThrowed != null)
                    onExceptionThrowed(this, new ArchiveExceptionEventArgs(ex));
                else throw;
            }
            return result.Count > 0 ? result.ToArray() : null;
        }
    }
}
