 using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Collections;
using System.Diagnostics;
using STCLINE.KP50.Global;
using System.IO;

namespace STCLINE.KP50.Interfaces
{
    [ServiceContract]
    public interface I_Patch
    {
        [OperationContract]
        Dictionary<string, object> GoPatch(ArrayList sql_array, string dataBaseType, byte[] soup);

        [OperationContract]
        Stream GetMonitorLog(DateTime BeginDate, DateTime EndDate);

        [OperationContract]
        void ExecSQLList(Stream List);

        [OperationContract]
        Stream GetSelect();

        // UpdateIndex, 0-host, 1-web, 2-broker
        [OperationContract]
        void FullUpdateStr(string UpdateFile, string UpdateMD5, int UpdateIndex, string WebPath, string pass, string passMD5);

        [OperationContract]
        void FullUpdate(Stream UpdateFile);

        [OperationContract]
        void UpdateUpdater(Stream UpdaterFile);

        [OperationContract]
        void RemoveBackupFiles(string WebPath);

        [OperationContract]
        void RestoreFromBackup(string WebPath);

        [OperationContract]
        Stream GetHistoryFull();

        [OperationContract]
        Stream GetHistoryLast(int count);

        [OperationContract]
        int CheckConn();

        [OperationContract]
        bool ExecSQLFile(Stream SQLStream);

        [OperationContract]
        DateTime GetCurrentMount();

        [OperationContract]
        bool Replace7zdll(byte[] File7z);

        [OperationContract]
        bool RestartHosting();
    }

    [DataContract]
    public class DictsClass
    {
        List<UpData2> l1;
        List<UpData2> l2;

        [DataMember]
        public List<UpData2> L1
        {
            set { l1 = value; }
            get { return l1; }
        }

        [DataMember]
        public List<UpData2> L2
        {
            set { l2 = value; }
            get { return l2; }
        }
    }

    //[DataContract]
    [SerializableAttribute]
    public class UpData2 //Данные об обновлении
    {
        [DataMember]
        public double Version;
        [DataMember]
        public string status;
        [DataMember]
        public string date;
        [DataMember]
        public string typeUp;
        [DataMember]
        public string Path;
        [DataMember]
        public string key;
        [DataMember]
        public string soup;
        [DataMember]
        public string nzp;
        [DataMember]
        public string web_path;
        [DataMember]
        public string report;

        public UpData2()
        {
            this.Version = -1;
            this.typeUp = "NO DATA";
            this.status = "NO DATA";
            this.date = "NO DATA";
            this.Path = "NO DATA";
            this.key = "NO DATA";
            this.soup = "NO DATA";
            this.nzp = "NO DATA";
            this.web_path = "NO DATA";
            this.report = "NO DATA";

        }
        public UpData2(UpData upd)
        {
            this.Version = upd.Version;
            this.date = upd.date;
            this.status = upd.status;
            this.typeUp = upd.typeUp;
            this.report = upd.report;
            this.Path = upd.Path;
            this.key = upd.key;
            this.soup = upd.soup;
            this.nzp = upd.nzp;
            this.web_path = upd.web_path;
        }
    }

    [SerializableAttribute] // для доступности ToArray()
    public class UpdateClass
    {
        [DataMember]
        public byte[] File;
        [DataMember]
        public string MD5;
        [DataMember]
        public int Index;
        [DataMember]
        public string WebPath;
        [DataMember]
        public string pass;
        [DataMember]
        public string passMD5;

        public UpdateClass()
        {
            File = new byte[1];
            MD5 = "";
            Index = 0;
            WebPath = "";
            pass = "";
            passMD5 = "";
        }
    }
}
