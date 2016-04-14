using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using STCLINE.KP50.Global;
using System.Net.Mail;
using System.Runtime.Serialization;
using System.Collections;
using System.IO;

namespace STCLINE.KP50.Interfaces
{
    [ServiceContract]
    public interface I_DataImport
    {
       

        [OperationContract]
        ReturnsObjectType<List<KLADRData>> LoadDataFromKLADR(KLADRFinder finder);

        [OperationContract]
        Returns RefreshKLADRFile(FilesImported finder);


        [OperationContract]
        ReturnsObjectType<List<ComparedAreas>> GetComparedArea(FilesImported finder);

        [OperationContract]
        ReturnsObjectType<List<ComparedSupps>> GetComparedSupp(FilesImported finder);

        [OperationContract]
        ReturnsObjectType<List<ComparedPayer>> GetComparedPayer(FilesImported finder);

        [OperationContract]
        ReturnsObjectType<List<ComparedVills>> GetComparedMO(FilesImported finder);

        [OperationContract]
        ReturnsObjectType<List<ComparedServs>> GetComparedServ(FilesImported finder);

        [OperationContract]
        ReturnsObjectType<List<ComparedMeasures>> GetComparedMeasure(FilesImported finder);

        [OperationContract]
        ReturnsObjectType<List<ComparedParTypes>> GetComparedParType(FilesImported finder);

        [OperationContract]
        ReturnsObjectType<List<ComparedParTypes>> GetComparedParBlag(FilesImported finder);

      

        [OperationContract]
        ReturnsObjectType<List<ComparedParTypes>> GetComparedParGas(FilesImported finder);

        [OperationContract]
        ReturnsObjectType<List<ComparedParTypes>> GetComparedParWater(FilesImported finder);

        [OperationContract]
        ReturnsObjectType<List<ComparedTowns>> GetComparedTown(FilesImported finder);

        [OperationContract]
        ReturnsObjectType<List<ComparedRajons>> GetComparedRajon(FilesImported finder);

        [OperationContract]
        ReturnsObjectType<List<ComparedStreets>> GetComparedStreets(FilesImported finder);

        [OperationContract]
        ReturnsObjectType<List<ComparedHouses>> GetComparedHouse(FilesImported finder);

        [OperationContract]
        ReturnsObjectType<List<ComparedLS>> GetComparedLS(FilesImported finder);

      


        [OperationContract]
        ReturnsObjectType<List<UncomparedAreas>> GetUncomparedArea(FilesImported finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedSupps>> GetUncomparedSupp(FilesImported finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedPayer>> GetUncomparedPayer(FilesImported finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedVills>> GetUncomparedMO(FilesImported finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedServs>> GetUncomparedServ(FilesImported finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedParTypes>> GetUncomparedParType(FilesImported finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedParTypes>> GetUncomparedParBlag(FilesImported finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedParTypes>> GetUncomparedParGas(FilesImported finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedParTypes>> GetUncomparedParWater(FilesImported finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedMeasures>> GetUncomparedMeasure(FilesImported finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedTowns>> GetUncomparedTown(FilesImported finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedRajons>> GetUncomparedRajon(FilesImported finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedStreets>> GetUncomparedStreets(FilesImported finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedHouses>> GetUncomparedHouse(FilesImported finder);
        [OperationContract]
        ReturnsObjectType<List<UncomparedLS>> GetUncomparedLS(FilesImported finder);

        [OperationContract]
        ReturnsType UnlinkArea(ComparedAreas finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType UnlinkSupp(ComparedSupps finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType UnlinkPayer(ComparedPayer finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType UnlinkMO(ComparedVills finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType UnlinkServ(ComparedServs finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType UnlinkParType(ComparedParTypes finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType UnlinkParBlag(ComparedParTypes finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType UnlinkParGas(ComparedParTypes finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType UnlinkParWater(ComparedParTypes finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType UnlinkMeasure(ComparedMeasures finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType UnlinkTown(ComparedTowns finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType UnlinkRajon(ComparedRajons finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType UnlinkStreet(ComparedStreets finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType UnlinkHouse(ComparedHouses finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType UnlinkLS(ComparedLS finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsObjectType<List<UncomparedAreas>> GetAreaByFilter(UncomparedAreas finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedSupps>> GetSuppByFilter(UncomparedSupps finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedPayer>> GetPayerByFilter(UncomparedPayer finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedVills>> GetMOByFilter(UncomparedVills finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedServs>> GetServByFilter(UncomparedServs finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedParTypes>> GetParTypeByFilter(UncomparedParTypes finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedParTypes>> GetParBlagByFilter(UncomparedParTypes finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedParTypes>> GetParGasByFilter(UncomparedParTypes finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedParTypes>> GetParWaterByFilter(UncomparedParTypes finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedMeasures>> GetMeasureByFilter(UncomparedMeasures finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedTowns>> GetTownByFilter(UncomparedTowns finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedRajons>> GetRajonByFilter(UncomparedRajons finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedStreets>> GetStreetsByFilter(UncomparedStreets finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedHouses>> GetHouseByFilter(UncomparedHouses finder);

        [OperationContract]
        ReturnsObjectType<List<UncomparedLS>> GetLsByFilter(UncomparedLS finder);

        [OperationContract]
        ReturnsType ChangeTownForRajon(UncomparedRajons finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType LinkArea(UncomparedAreas finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType LinkSupp(UncomparedSupps finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType LinkPayer(UncomparedPayer finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType LinkMO(UncomparedVills finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType LinkServ(UncomparedServs finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType LinkParType(UncomparedParTypes finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType LinkParBlag(UncomparedParTypes finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType LinkParGas(UncomparedParTypes finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType LinkParWater(UncomparedParTypes finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType LinkMeasure(UncomparedMeasures finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType LinkTown(UncomparedTowns finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType LinkRajon(UncomparedRajons finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType LinkNzpStreet(UncomparedStreets finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType LinkNzpDom(UncomparedHouses finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType LinkLS(UncomparedLS finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType AddNewArea(UncomparedAreas finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType AddNewSupp(UncomparedSupps finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType AddNewPayer(UncomparedPayer finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType AddNewMO(UncomparedVills finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType AddNewServ(UncomparedServs finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType AddNewParType(UncomparedParTypes finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType AddNewParBlag(UncomparedParTypes finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType AddNewParGas(UncomparedParTypes finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType AddNewParWater(UncomparedParTypes finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType AddNewMeasure(UncomparedMeasures finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType AddNewRajon(UncomparedRajons finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType AddNewStreet(UncomparedStreets finder, List<int> selectedFiles);

        [OperationContract]
        ReturnsType AddNewHouse(UncomparedHouses finder, List<int> selectedFiles);

        /// <summary>
        /// Выбор отмеченнвх файлов для разбора
        /// </summary>
        /// <param name="nzp_file"></param>
        /// <param name="nzp_user"></param>
        /// <returns></returns>
        [OperationContract]

        ReturnsType DbSaveFileToDisassembly(FilesImported finder);
        /// <summary>
        /// Добавить все дома 
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        [OperationContract]
        Returns AddAllHouse(FilesImported finder);

        /// <summary>
        /// Сопоставить ЛС автоматически 
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        [OperationContract]
        Returns LinkLSAutom(FilesImported finder);


        /// <summary>
        /// Автоматическое сопоставление улиц с названием '-'
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        [OperationContract]
        Returns LinkEmptyStreetAutom(FilesImported finder);

        /// <summary>
        /// Остановить обновление адресного пространства
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        [OperationContract]
        Returns StopRefresh();
        
        /// <summary>
        /// Автоматическое сопоставление улиц
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        [OperationContract]
        Returns LinkStreetAutom(FilesImported finder);

        /// <summary>
        /// Автоматическое сопоставление юридических лиц с добавлением
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        [OperationContract]
        Returns LinkPayerWithAdd(FilesImported finder);

        /// <summary>
        /// Автоматическое сопоставление юридических лиц
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        [OperationContract]
        Returns LinkPayerAutom(FilesImported finder);

        /// <summary>
        /// Автоматическое сопоставление параметров
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        [OperationContract]
        Returns LinkParamsAutom(FilesImported finder);

        /// <summary>
        /// Автоматическое сопоставление параметров с добавлением
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        [OperationContract]
        Returns LinkParAutomWithAdd(FilesImported finder);

        /// <summary>
        /// Автоматическое сопоставление населенных пунктов
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        [OperationContract]
        Returns LinkRajonAutom(FilesImported finder);

        /// <summary>
        /// Использования предыдущих сопоставлений
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        Returns MakePacks(FilesImported finder);

        /// <summary>
        /// Сопоставить дома без добавления 
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        [OperationContract]
        Returns LinkHouseOnly(FilesImported finder);

        /// <summary>
        /// Использования предыдущих сопоставлений
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        Returns UsePreviousLinks(FilesImported finder);

        /// <summary>
        /// Формирование отчетов по загруженному
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        Returns MakeReportOfLoad(FilesImported finder);

        /// <summary>
        /// Получить список файлов с несормированными актами
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        List<int> GetFilesIsNotExistAct(FilesImported finder, out Returns ret);

        /// <summary>
        /// Операции с загруженными файлами
        /// </summary>
        /// <param name="finder">файл</param>
        /// <param name="operation">операция</param>
        /// <returns>результат</returns>
        [OperationContract]
        Returns OperateWithFileImported(FilesDisassemble finder, FilesImportedOperations operation);

        [OperationContract]
        ReturnsObjectType<List<UploadingData>> GetUploadingProgress(UploadingData finder);

        [OperationContract]
        Returns GetNzpFileLoad(FilesImported finder);

        /// <summary>
        /// Отображение информации о загруженных файлах
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        [OperationContract]
        ReturnsObjectType<List<FilesImported>> GetFiles(FilesImported finder);

        [OperationContract]
        Returns MakeUniquePayer(List<int> selectedFiles);

        [OperationContract]
        Returns MakeNonUniquePayer(List<int> selectedFiles);

        /// <summary>
        /// Автоматическое сопоставление населенных пунктов
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        [OperationContract]
        Returns LinkServiceAuto(FilesImported finder);
    }

    public interface IDataImportRemoteObject : I_DataImport, IDisposable { }

    public enum FilesImportedOperations
    {
        Delete
    }


    [DataContract]
    public class ComparedAreas : Finder
    {
        [DataMember]
        public string area_file { get; set; }
        [DataMember]
        public string area_base { get; set; }
        [DataMember]
        public string nzp_area { get; set; }
    }

    [DataContract]
    public class ComparedSupps : Finder
    {
        [DataMember]
        public string supp_file { get; set; }
        [DataMember]
        public string supp_base { get; set; }
        [DataMember]
        public string nzp_supp { get; set; }
    }

    [DataContract]
    public class ComparedPayer : Finder
    {
        [DataMember]
        public string payer_file { get; set; }
        [DataMember]
        public string payer_base { get; set; }
        [DataMember]
        public string nzp_payer { get; set; }
    }

    [DataContract]
    public class ComparedVills : Finder
    {
        [DataMember]
        public string vill_file { get; set; }
        [DataMember]
        public string vill_base { get; set; }
        [DataMember]
        public string nzp_vill { get; set; }
    }

    [DataContract]
    public class ComparedServs : Finder
    {
        [DataMember]
        public string serv_file { get; set; }
        [DataMember]
        public string serv_base { get; set; }
        [DataMember]
        public string nzp_serv { get; set; }
    }

    [DataContract]
    public class ComparedParTypes : Finder
    {
        [DataMember]
        public string name_prm_file { get; set; }
        [DataMember]
        public string name_prm_base { get; set; }
        [DataMember]
        public string nzp_prm { get; set; }
        [DataMember]
        public string name_prm { get; set; }
    }

    [DataContract]
    public class ComparedMeasures : Finder
    {
        [DataMember]
        public string measure_file { get; set; }
        [DataMember]
        public string measure_base { get; set; }
        [DataMember]
        public string nzp_measure { get; set; }
    }

    [DataContract]
    public class ComparedTowns : Finder
    {
        [DataMember]
        public string town_file { get; set; }
        [DataMember]
        public string town_base { get; set; }
        [DataMember]
        public string nzp_town { get; set; }
    }

    [DataContract]
    public class ComparedRajons : Finder
    {
        [DataMember]
        public string rajon_file { get; set; }
        [DataMember]
        public string rajon_base { get; set; }
        [DataMember]
        public string nzp_raj { get; set; }
        [DataMember]
        public bool change_raj { get; set; }
    }

    [DataContract]
    public class ComparedStreets : Finder
    {
        [DataMember]
        public string ulica_file { get; set; }
        [DataMember]
        public string ulica_base { get; set; }
        [DataMember]
        public string nzp_ul { get; set; }
    }

    [DataContract]
    public class ComparedHouses : Finder
    {
        [DataMember]
        public string dom_file { get; set; }
        [DataMember]
        public string dom_base { get; set; }
        [DataMember]
        public string nzp_dom { get; set; }
    }

    [DataContract]
    public class ComparedLS : Finder
    {
        [DataMember]
        public string kvar_file { get; set; }
        [DataMember]
        public string kvar_base { get; set; }
        [DataMember]
        public string nzp_kvar { get; set; }
    }

  

    [DataContract]
    public class UncomparedAreas : Finder
    {
        [DataMember]
        public string area { get; set; }
        [DataMember]
        public string nzp_area { get; set; }
        [DataMember]
        public int nzp_file { get; set; }
    }

    [DataContract]
    public class UncomparedSupps : Finder
    {
        [DataMember]
        public string supp { get; set; }
        [DataMember]
        public string nzp_supp { get; set; }
        [DataMember]
        public int nzp_file { get; set; }
    }

    [DataContract]
    public class UncomparedPayer : Finder
    {
        [DataMember]
        public string payer { get; set; }
        [DataMember]
        public string nzp_payer { get; set; }
        [DataMember]
        public int nzp_file { get; set; }
    }

    [DataContract]
    public class UncomparedVills : Finder
    {
        [DataMember]
        public string vill { get; set; }
        [DataMember]
        public string nzp_vill { get; set; }
        [DataMember]
        public int nzp_file { get; set; }
    }

    [DataContract]
    public class UncomparedServs : Finder
    {
        [DataMember]
        public string serv { get; set; }
        [DataMember]
        public string nzp_serv { get; set; }
        [DataMember]
        public int nzp_file { get; set; }
    }

    [DataContract]
    public class UncomparedParTypes : Finder
    {
        [DataMember]
        public string name_prm { get; set; }
        [DataMember]
        public string nzp_prm { get; set; }
        [DataMember]
        public string type_prm { get; set; }
        [DataMember]
        public int nzp_file { get; set; }
        [DataMember]
        public int prm_num { get; set; }
    }

    [DataContract]
    public class UncomparedMeasures : Finder
    {
        [DataMember]
        public string measure { get; set; }
        [DataMember]
        public string nzp_measure { get; set; }
        [DataMember]
        public int nzp_file { get; set; }
    }

    [DataContract]
    public class UncomparedTowns : Finder
    {
        [DataMember]
        public string town { get; set; }
        [DataMember]
        public string nzp_town { get; set; }
        [DataMember]
        public int nzp_file { get; set; }
    }

    [DataContract]
    public class UncomparedRajons : Finder
    {
        [DataMember]
        public string show_data { get; set; }
        [DataMember]
        public string rajon { get; set; }
        [DataMember]
        public string nzp_town { get; set; }
        [DataMember]
        public string nzp_raj { get; set; }
        [DataMember]
        public int nzp_file { get; set; }
    }

    [DataContract]
    public class UncomparedStreets : Finder
    {
        [DataMember]
        public string show_data { get; set; }
        [DataMember]
        public string ulica { get; set; }
        [DataMember]
        public string nzp_town { get; set; }
        [DataMember]
        public string nzp_raj { get; set; }
        [DataMember]
        public string nzp_ul { get; set; }
        [DataMember]
        public string id { get; set; }
        [DataMember]
        public int nzp_file { get; set; }
    }

    [DataContract]
    public class UncomparedHouses : Finder
    {
        [DataMember]
        public string show_data { get; set; }
        [DataMember]
        public string dom { get; set; }
        [DataMember]
        public string nzp_town { get; set; }
        [DataMember]
        public string nzp_raj { get; set; }
        [DataMember]
        public string nzp_ul { get; set; }
        [DataMember]
        public string nzp_dom { get; set; }
        [DataMember]
        public string id { get; set; }
    }

    [DataContract]
    public class UncomparedLS : Finder
    {
        [DataMember]
        public string show_data { get; set; }
        [DataMember]
        public string dom { get; set; }
        [DataMember]
        public string nzp_town { get; set; }
        [DataMember]
        public string nzp_raj { get; set; }
        [DataMember]
        public string nzp_ul { get; set; }
        [DataMember]
        public string nzp_dom { get; set; }
        [DataMember]
        public string nzp_kvar { get; set; }
        [DataMember]
        public string id { get; set; }
        [DataMember]
        public string kvar { get; set; }
        [DataMember]
        public int nzp_file { get; set; }
    }

    [DataContract]
    public class DownloadedData
    {
        [DataMember]
        public int kol { get; set; }
        [DataMember]
        public int kol_prib { get; set; }
        [DataMember]
        public int kol_ub { get; set; }
        [DataMember]
        public decimal sum { get; set; }
    }

    [DataContract]
    public class UploadingData : Finder
    {
        [DataMember]
        public int id { get; set; }
        [DataMember]
        public string num { get; set; }
        [DataMember]
        public DateTime date_upload { get; set; }
        [DataMember]
        public string progress { get; set; }
        [DataMember]
        public string status { get; set; }
        [DataMember]
        public int upload_type { get; set; }
    }

    /// <summary>
    /// для передачи параметров для разбора
    /// </summary>
    [DataContract]
    public class FilesDisassemble
    {
        [DataMember]
        public bool delete_all_data { get; set; }

        [DataMember]
        public bool prev_month_saldo { get; set; }

        [DataMember]
        public bool use_previous_links { get; set; }

        [DataMember]
        public bool use_local_num { get; set; }

        [DataMember]
        public bool is_last_month { get; set; }

        [DataMember]
        public bool repair { get; set; }

        [DataMember]
        public bool reloaded_file { get; set; }

        /// <summary>Заморозка расчета начислений</summary>
        [DataMember]
        public Boolean FrozenCharge { get; set; }

        [DataMember]
        public int nzp_user { get; set; }
        [DataMember]
        public int nzp_file { get; set; }

        [DataMember]
        public string bank { get; set; }

        [DataMember]
        public string pref { get; set; }

        [DataMember]
        public string versionFull { get; set; }

        [DataMember]
        public int month { get; set; }

        [DataMember]
        public int year { get; set; }

        [DataMember]
        public string dat_po { get; set; }

        [DataMember]
        public List<int> selectedFiles { get; set; }

        public FilesDisassemble()
        {
            delete_all_data = false;
            prev_month_saldo = false;
            use_local_num = false;
            use_previous_links = false;
            is_last_month = false;
            repair = false;
            reloaded_file = false;
            FrozenCharge = false;
            nzp_user = -1;
            nzp_file = -1;
            bank = "";
            pref = "";
            versionFull = "";
            selectedFiles = new List<int>();
        }

    }

}
