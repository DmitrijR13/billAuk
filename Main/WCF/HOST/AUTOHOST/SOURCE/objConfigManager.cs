using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace STCLINE.KP50.HostMan.SOURCE
{
    public struct ConfigFile
    {
        private string strPutDirectory;
        private string strOrigConfigFileName;
        private string strLocalConfigFileName;

        public string OutputDirectory { get { return this.strPutDirectory; } set { this.strPutDirectory = value; } } // Папка, в которой должен находиться файл для работы
        public string LocalName { get { return this.strLocalConfigFileName; } set { this.strLocalConfigFileName = value; } } // Имя сохраненного файла
        public string OriginalName { get { return this.strOrigConfigFileName; } set { this.strOrigConfigFileName = value; } } // Оригинальное имя файла
    }

    public struct GroupsList
    {
        private int intGroupID;
        private string strGroupName;
        public int GroupID { get { return this.intGroupID; } set { this.intGroupID = value; } }
        public string GroupName { get { return this.strGroupName; } set { this.strGroupName = value; } }
    }

    internal class FilesList
    {
        private int GID;
        private string strGroupName;
        private string strGroupDataFile;
        private string strGroupDirectory;
        private List<ConfigFile> lstFiles;

        public int GroupID { get { return GID; } }
        public string GroupName { get { return this.strGroupName; } }
        public List<ConfigFile> FileList { get { return this.lstFiles; } }

        private void AddLoaded(string strLocalName, string strOriginalName, string strOutputDirectory)
        {
            ConfigFile cfgNewFile = new ConfigFile();
            cfgNewFile.LocalName = strLocalName;
            cfgNewFile.OriginalName = strOriginalName;
            cfgNewFile.OutputDirectory = strOutputDirectory;
            lstFiles.Add(cfgNewFile);
        }

        private void Load()
        {
            this.lstFiles.Clear();
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(this.strGroupDataFile);
            foreach (XmlNode xnTable in xmlDoc.DocumentElement.ChildNodes)
            {
                if (Convert.ToInt32(xnTable.Attributes["GID"].Value) == GID)
                {
                    this.strGroupName = xnTable.Attributes["Name"].Value;
                    foreach (XmlNode xnFiles in xnTable.ChildNodes) this.AddLoaded(xnFiles.Attributes["LocalName"].Value, xnFiles.Attributes["OriginalName"].Value, xnFiles.Attributes["OutputDirectory"].Value);
                }
            }
        }

        public FilesList(string GroupName, string GroupDataFile)
        {
            this.strGroupDataFile = GroupDataFile;
            this.strGroupName = GroupName;
            this.lstFiles = new List<ConfigFile>();
            this.strGroupDirectory = String.Format("{0}\\ConfigManager\\", Environment.CurrentDirectory);
            while (Directory.Exists(String.Format("{0}\\{1}\\", this.strGroupDirectory, GID))) GID++;
            this.strGroupDirectory = String.Format("{0}\\{1}\\", this.strGroupDirectory, GID);
            Directory.CreateDirectory(strGroupDirectory);
        }

        public FilesList(int GroupID, string GroupDataFile)
        {
            this.strGroupDataFile = GroupDataFile;
            this.lstFiles = new List<ConfigFile>();
            GID = GroupID;
            this.strGroupDirectory = String.Format("{0}\\ConfigManager\\{1}\\", Environment.CurrentDirectory, GID);
            if (!Directory.Exists(strGroupDirectory)) Directory.CreateDirectory(strGroupDirectory);
            this.Load();
        }

        public void Add(string FileName)
        {
            FileName = Path.GetFullPath(FileName);
            ConfigFile cfgNewFile = new ConfigFile();
            cfgNewFile.OriginalName = Path.GetFileName(FileName);
            cfgNewFile.OutputDirectory = String.Format("{0}\\", Path.GetDirectoryName(FileName));
            cfgNewFile.LocalName = String.Format("{0}", DateTime.Now.ToString());
            cfgNewFile.LocalName = cfgNewFile.LocalName.Replace(":", ".");
            cfgNewFile.LocalName = cfgNewFile.LocalName.Replace(".", "");
            cfgNewFile.LocalName = cfgNewFile.LocalName.Replace(" ", "_");
            cfgNewFile.LocalName += String.Format("{0}", Path.GetExtension(String.Format("{0}{1}", cfgNewFile.OutputDirectory, cfgNewFile.OriginalName)));
            File.Copy(String.Format("{0}{1}", cfgNewFile.OutputDirectory, cfgNewFile.OriginalName), String.Format("{0}{1}", strGroupDirectory, cfgNewFile.LocalName));
            lstFiles.Add(cfgNewFile);
        }

        public bool Delete(string OriginalFileName)
        {
            foreach (ConfigFile cfgFile in lstFiles)
            {
                if (cfgFile.OriginalName == OriginalFileName)
                {
                    File.Delete(String.Format("{0}{1}", this.strGroupDirectory, cfgFile.LocalName));
                    lstFiles.Remove(cfgFile);
                    return true;
                }
            }
            return false;
        }

        public void Save(XmlWriter xwOutput)
        {
            xwOutput.WriteStartElement("Group");
            xwOutput.WriteAttributeString("GID", this.GroupID.ToString());
            xwOutput.WriteAttributeString("Name", this.strGroupName);
            for (int i = 0; i < this.lstFiles.Count; i++)
            {
                xwOutput.WriteStartElement("File");
                xwOutput.WriteAttributeString("LocalName", this.lstFiles[i].LocalName);
                xwOutput.WriteAttributeString("OriginalName", this.lstFiles[i].OriginalName);
                xwOutput.WriteAttributeString("OutputDirectory", this.lstFiles[i].OutputDirectory);
                xwOutput.WriteEndElement();
            }
            xwOutput.WriteEndElement();
        }

        public void DeleteGroup()
        {
            while (lstFiles.Count > 0)
            {
                ConfigFile cfgFile = lstFiles[0];
                try
                {
                    File.Delete(String.Format("{0}{1}", this.strGroupDirectory, cfgFile.LocalName));
                    lstFiles.Remove(cfgFile);
                    if (this.lstFiles.Count == 0) break;
                }
                catch (Exception) { }
            }
        }

        private static FileAttributes RemoveAttribute(FileAttributes attributes, FileAttributes attributesToRemove)
        {
            return attributes & ~attributesToRemove;
        }

        internal void RestoreFiles()
        {
            try
            {
                foreach (ConfigFile cfFile in this.lstFiles)
                {
                    FileAttributes attributes = File.GetAttributes(String.Format("{0}{1}", cfFile.OutputDirectory, cfFile.OriginalName));
                    if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        attributes = RemoveAttribute(attributes, FileAttributes.ReadOnly);
                        File.SetAttributes(String.Format("{0}{1}", cfFile.OutputDirectory, cfFile.OriginalName), attributes);
                    }
                    File.Delete(String.Format("{0}{1}", cfFile.OutputDirectory, cfFile.OriginalName));
                    File.Copy(String.Format("{0}{1}", this.strGroupDirectory, cfFile.LocalName), String.Format("{0}{1}", cfFile.OutputDirectory, cfFile.OriginalName));
                }
            }
            catch (Exception) { }
        }
    }

    public class ConfigManager
    {
        private List<FilesList> lstGroups;
        private List<GroupsList> lstGroupsNames;
        private string strConfFile;
        public List<GroupsList> GroupsNames { get { return this.lstGroupsNames; } }

        public void AddFile(int GID, string FileName) { foreach (FilesList flGroup in this.lstGroups) if (flGroup.GroupID == GID) flGroup.Add(FileName); }
        public void DeleteFile(int GID, string FileName) { foreach (FilesList flGroup in this.lstGroups) if (flGroup.GroupID == GID) flGroup.Delete(FileName); }
        public void RestoreFiles(int GID) { foreach (FilesList flGroups in this.lstGroups) if (flGroups.GroupID == GID) flGroups.RestoreFiles(); }

        public List<ConfigFile> GetFilesList(int GroupID)
        {
            List<ConfigFile> lstReturn = new List<ConfigFile>();
            lstReturn.Clear();
            foreach (FilesList item in this.lstGroups)
            {
                if (item.GroupID == GroupID)
                {
                    foreach (ConfigFile fl in item.FileList) lstReturn.Add(fl);
                    break;
                }
            }
            return lstReturn;
        }

        public void AddGroup(string GroupName)
        {
            FilesList flGroup = new FilesList(GroupName, strConfFile);
            this.lstGroups.Add(flGroup);
            GroupsList glGroup = new GroupsList();
            glGroup.GroupID = flGroup.GroupID;
            glGroup.GroupName = flGroup.GroupName;
            this.lstGroupsNames.Add(glGroup);
        }

        public void DeleteGroup(string GroupName)
        {
            foreach (FilesList flGroup in this.lstGroups)
            {
                if (flGroup.GroupName == GroupName)
                {
                    flGroup.DeleteGroup();
                    this.lstGroups.Remove(flGroup);
                    break;
                }
            }
            foreach (GroupsList glNames in lstGroupsNames)
            {
                if (glNames.GroupName == GroupName) this.lstGroupsNames.Remove(glNames);
                break;
            }
        }

        public ConfigManager(string ConfigFile)
        {
            this.lstGroups = new List<FilesList>();
            this.lstGroupsNames = new List<GroupsList>();
            this.strConfFile = ConfigFile;
        }

        public void CreateGroup(string GroupName)
        {
            FilesList flGroup = new FilesList(GroupName, strConfFile);
            this.lstGroups.Add(flGroup);
        }

        public void Save()
        {
            XmlWriterSettings xwsSettings = new XmlWriterSettings();
            xwsSettings.Indent = true;
            xwsSettings.IndentChars = "    ";
            xwsSettings.NewLineChars = "\n";
            xwsSettings.OmitXmlDeclaration = true;

            using (XmlWriter xwOutput = XmlWriter.Create(this.strConfFile, xwsSettings))
            {
                xwOutput.WriteStartElement("Groups");
                foreach (FilesList flGroup in this.lstGroups) flGroup.Save(xwOutput);
                xwOutput.WriteEndElement();
            }
        }

        public void Load()
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(this.strConfFile);

                foreach (XmlNode xnTable in xmlDoc.DocumentElement.ChildNodes)
                {
                    GroupsList glGroup = new GroupsList();
                    glGroup.GroupID = Convert.ToInt32(xnTable.Attributes["GID"].Value);
                    glGroup.GroupName = xnTable.Attributes["Name"].Value;
                    this.lstGroupsNames.Add(glGroup);
                }
                foreach (GroupsList glGroup in this.lstGroupsNames)
                {
                    FilesList flGroup = new FilesList(glGroup.GroupID, strConfFile);
                    this.lstGroups.Add(flGroup);
                }
            }
            catch (Exception) { }
        }
    }
}
