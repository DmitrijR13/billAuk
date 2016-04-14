using System;
using System.Data;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Globals.SOURCE.Container;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using Globals.SOURCE.Config;

namespace STCLINE.KP50.DataBase
{


    /// <summary>
    /// Класс определяющий работу системы по банкам данных
    /// </summary>
    public class DbLoadPoints : DataBaseHead
    {
        /// <summary>
        /// Шаг процесса загрузки
        /// </summary>
        private string _stepLoad = "0";

        /// <summary>
        /// Локальное подключение к БД
        /// </summary>
        private IDbConnection _connDB;

        /// <summary>
        /// Работать только с центральным банком
        /// </summary>
        private bool _workOnlyWithCentralBank;


        private bool PrepareData(bool workOnlyWithCentralBank)
        {

            _workOnlyWithCentralBank = workOnlyWithCentralBank;

            _connDB = GetConnection(Constants.cons_Kernel);

            _stepLoad = "3";

            return OpenDb(_connDB, true).result;
        }

        /// <summary>
        /// Облегченная версия загрузки параметра
        /// </summary>
        /// <param name="tableName">Имя таблицы параметров</param>
        /// <param name="nzpPrm">код параметра</param>
        /// <returns></returns>
        private string GetValPrm(string tableName, int nzpPrm)
        {
            MyDataReader reader;
            string result = "";
            string sql = " Select val_prm " +
                         " From " + tableName + " p " +
                         " Where p.nzp_prm =  " + nzpPrm +
                         "   and p.is_actual = 1 " +
                         "   and p.dat_s  <= " + DBManager.sCurDate +
                         "   and p.dat_po >= " + DBManager.sCurDate;
            Returns ret = ExecRead(_connDB, out reader, sql, true);
            if (ret.result)
            {
                if (reader.Read())
                {
                    result = reader["val_prm"].ToString().Trim();
                }
            }
            reader.Close();
            return result;

        }


        /// <summary>
        /// Процедура загрузки системных параметров
        /// </summary>
        /// <param name="workOnlyWithCentralBank">Работать только с центральным банком</param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public bool PointLoad(bool workOnlyWithCentralBank, out Returns ret, out List<string> warnPoint)
        //проверить наличие s_point и заполнить список Points
        //----------------------------------------------------------------------
        {
            warnPoint = new List<string>();
            ret = Utils.InitReturns();
            if (!PrepareData(workOnlyWithCentralBank)) return false;
            try
            {

                Points.IsPoint = false;
                _stepLoad = "1";

                ClearPointList();
                _stepLoad = "2";


                if (!GetCurCalcMonth()) return false;
                _stepLoad = "3";

                GetCurOperDay();
                _stepLoad = "4";

                if (!GetDateBeginWork()) return false;
                _stepLoad = "5";

                Points.Region = SetRegions();
                _stepLoad = "6";

                if (!SetPoints(out warnPoint)) return false;
                _stepLoad = "7";

                CorrectPointsCalcMonths();
                _stepLoad = "8";

                Points.IsSmr = GetSamaraBanks();
                _stepLoad = "9";

                Points.packDistributionParameters = SetParametersForDistribMoney();
                _stepLoad = "10";

                Points.SaveCounterReadingsToRealBank = SetSaveCountersPlace();

                Points.IsDemo = CheckDemoBank();
                _stepLoad = "11";

                Points.Is50 = Check50System();
                _stepLoad = "13";

                Points.isClone = CheckCloneComputer();
                _stepLoad = "14";

                Points.IsCalcSubsidy = CheckSubsidyCalc();
                _stepLoad = "15";

                Points.RecalcMode = GetRecalcMode();
                _stepLoad = "16";

                Points.IsIpuHasNgpCnt = CheckFiledNzpCnt();
                _stepLoad = "17";

                Points.isUseSeries = CheckUseSeries();
                _stepLoad = "18";

                Points.functionTypeGeneratePkod = SetTypeFunctionGeneratePkod();

                _stepLoad = "19";

                Points.FullLogging = GetFullLoggingState();

                DelTempAnalizTable();
                _stepLoad = "20";

                Points.DefaultPkodType = GetDefaultTypePkod();
                _stepLoad = "21";

                Points.isNewNorms = GetParamIsNewNorms();
                _stepLoad = "22";

                Points.isUchetContrPu = GetParamIsUchetContrPu();
                _stepLoad = "23";

                return true;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, _stepLoad + ": " + ex.Message);
                MonitorLog.WriteLog("Ошибка в функции DbLoadPoints " + _stepLoad + ": " + ex.Message,
                    MonitorLog.typelog.Error, 30, 1, true);
                return false;
            }
            finally
            {
                if (_connDB != null) _connDB.Close();
            }
        }

        private bool GetFullLoggingState()
        {
            var ic = IocContainer.Current.Resolve<IConfigProvider>();
            return ic != null && ic.GetConfig().FullLogging;
        }


        /// <summary>
        /// Функция обновления информации о текущем расчетном месяце
        /// </summary>
        public void CorrectPointsCalcMonths()
        {

            if (Points.PointList == null) return;
            if (Points.PointList.Count == 0) return;
            if (Points.pointWebData.calcMonths == null) return;
            if (Points.pointWebData.calcMonths.Count == 0) return;

            RecordMonth recordMonthPointZero = Points.GetCalcMonth(new CalcMonthParams(Points.PointList[0].pref));
            var dt = new DateTime(recordMonthPointZero.year_, recordMonthPointZero.month_, 1);
            for (int i = 1; i < Points.PointList.Count; i++)
            {
                RecordMonth recordMonthSecond = Points.GetCalcMonth(new CalcMonthParams(Points.PointList[i].pref));
                var dt2 = new DateTime(recordMonthSecond.year_, recordMonthSecond.month_, 1);

                if (dt2 > dt)
                {
                    dt = dt2;
                    recordMonthPointZero = recordMonthSecond;
                }
            }
            var r = new RecordMonth();
            for (int y = recordMonthPointZero.year_; y > Points.pointWebData.calcMonths[0].year_; y--)
            {
                int mm = 12;
                if (y == recordMonthPointZero.year_) mm = recordMonthPointZero.month_;

                for (int m = mm; m >= 1; m--)
                {
                    r.year_ = y;
                    r.month_ = m;

                    Points.pointWebData.calcMonths.Insert(0, r);
                }

                r.year_ = y;
                r.month_ = 0;
                Points.pointWebData.calcMonths.Insert(0, r);
            }
        }


        /// <summary>
        /// Определение типа функции генерации платежного кода
        /// </summary>
        /// <returns></returns>
        private FunctionsTypesGeneratePkod SetTypeFunctionGeneratePkod()
        {
            string valPrm = GetValPrm(Points.Pref + "_data" + tableDelimiter + "prm_10",
                ParamIds.SpravParams.TypePkod.GetHashCode());

            if (valPrm == "") return FunctionsTypesGeneratePkod.standart;

            return (FunctionsTypesGeneratePkod)Enum.Parse(typeof(FunctionsTypesGeneratePkod),
                valPrm);
        }


        /// <summary>
        /// Проверка работы с Series
        /// </summary>
        /// <returns></returns>
        private bool CheckUseSeries()
        {
            string valPrm = GetValPrm(Points.Pref + "_data" + tableDelimiter + "prm_10", 1266);
            return valPrm == "1";
        }

        /// <summary>
        ///  Получить тип платежного кода по умолчанию
        /// </summary>
        /// <returns></returns>
        private int GetDefaultTypePkod()
        {
            MyDataReader reader;
            int result = 1;
            Returns ret = ExecRead(_connDB, out reader,
                " Select nzp_pkod_type From " + Points.Pref + DBManager.sKernelAliasRest + "pkod_types " +
                " Where is_default = 1", true);

            if (ret.result)
                if (reader.Read())
                {
                    result = reader["nzp_pkod_type"] != DBNull.Value ? (int)reader["nzp_pkod_type"] : 1;
                }
            reader.Close();
            return result;
        }



        /// <summary>
        ///  Получить параметр действия нового режима нормативов
        /// </summary>
        /// <returns></returns>
        private bool GetParamIsNewNorms()
        {
            string s = GetValPrm(Points.Pref + sDataAliasRest + "prm_5", 1983);
            return s.PrmValToBool();
        }


        /// <summary>
        ///  Получить параметр действия нового режима учета контрольных показаний
        /// </summary>
        /// <returns></returns>
        private bool GetParamIsUchetContrPu()
        {
            string s = GetValPrm(Points.Pref + sDataAliasRest + "prm_10", 1368);
            return s.PrmValToBool();
        }
        
        /// <summary>
        /// проверить, что в таблице counters всех банков есть поле ngp_cnt
        /// </summary>
        /// <returns></returns>
        private bool CheckFiledNzpCnt()
        {

            bool result = true;
            if (_workOnlyWithCentralBank)
            {
                Points.IsIpuHasNgpCnt = false;
            }
            else
            {
                Points.IsIpuHasNgpCnt = true;

                foreach (_Point p in Points.PointList)
                {
                    Returns ret = ExecSQL(_connDB, " Select c.ngp_cnt " +
                                                   " From " + p.pref + DBManager.sDataAliasRest + "counters c " +
                                                   " Where c.nzp_cr = 0 ", false);
                    if (!ret.result)
                    {
                        result = false;
                        break;
                    }
                }
            }
            return result;


        }



        /// <summary>
        /// Установка режима перерасчетов
        /// </summary>
        /// <returns></returns>
        private RecalcModes GetRecalcMode()
        {
            var result = RecalcModes.AutomaticWithCancelAbility;
            string valPrm = GetValPrm(Points.Pref + "_data" + tableDelimiter + "prm_5", 1990);
            if (!String.IsNullOrEmpty(valPrm))
            {
                if (valPrm == RecalcModes.Automatic.GetHashCode().ToString(CultureInfo.InvariantCulture))
                    result = RecalcModes.Automatic;
                else if (valPrm ==
                         RecalcModes.AutomaticWithCancelAbility.GetHashCode().ToString(CultureInfo.InvariantCulture))
                    result = RecalcModes.AutomaticWithCancelAbility;
                else if (valPrm == RecalcModes.Manual.GetHashCode().ToString(CultureInfo.InvariantCulture))
                    result = RecalcModes.Manual;

                if (Constants.Trace)
                    Utility.ClassLog.WriteLog("Задан режим перерасчета: " + Points.RecalcMode + "(код " +
                                              Points.RecalcMode.GetHashCode() + ")");
            }
            return result;
        }


        /// <summary>
        /// Проверка должен ли осуществляться расчет субсидий
        /// </summary>
        /// <returns></returns>
        private bool CheckSubsidyCalc()
        {
            string valPrm = GetValPrm(Points.Pref + "_data" + tableDelimiter + "prm_5", 1992);
            return valPrm == "1";
        }


        /// <summary>
        /// Признак компьютера - клона основоной базы
        /// </summary>
        /// <returns></returns>
        private bool CheckCloneComputer()
        {
            MyDataReader reader;
            bool result = false;
            Returns ret = ExecRead(_connDB, out reader,
                " Select dbname From " + Points.Pref + DBManager.sKernelAliasRest + "s_baselist " +
                " Where idtype = " + BaselistTypes.PrimaryBank.GetHashCode(), true);

            if (ret.result)
                if (reader.Read())
                {
                    result = (reader["dbname"] != DBNull.Value && ((string)reader["dbname"]).Trim() != "");
                }
            reader.Close();
            return result;
        }


        /// <summary>
        /// удаляет временные образы __anlXX
        /// </summary>
        private void DelTempAnalizTable()
        {
            IDbConnection connWeb = GetConnection(Constants.cons_Webdata);

            if (!OpenDb(connWeb, true).result) return;
            try
            {

                var dba = new DbAnalizClient();
                dba.Drop__AnlTables(connWeb);
                dba.Close();
            }
            finally
            {
                connWeb.Close();
            }
        }


        /// <summary>
        /// Проверка признака работы со структурами КП5.0
        /// </summary>
        /// <returns></returns>
        private bool Check50System()
        {
            string valPrm = GetValPrm(Points.Pref + "_data" + tableDelimiter + "prm_5", 1997);
            return valPrm == "1";
        }


        /// <summary>
        /// Установка признака Демо Банка 
        /// </summary>
        /// <returns></returns>
        private bool CheckDemoBank()
        {
            string valPrm = GetValPrm(Points.Pref + "_data" + tableDelimiter + "prm_5", 1999);
            return valPrm == "1";
        }

        /// <summary>
        ///Установка признак, сохранять ли показания ПУ прямо в основной банк
        /// </summary>
        private bool SetSaveCountersPlace()
        {
            string valPrm = GetValPrm(Points.Pref + "_data" + tableDelimiter + "prm_5", 1993);
            bool result = valPrm == "1";
            if (Constants.Trace)
                Utility.ClassLog.WriteLog("Сохранять показания приборов учета в основной банк данных: " +
                                          (result ? "Да" : "Нет"));
            return result;
        }


        /// <summary>
        /// Параметры распределения оплат
        /// </summary>
        /// <returns></returns>
        private PackDistributionParameters SetParametersForDistribMoney()
        {

            var result = new PackDistributionParameters();

            //Определение стратегии распределения оплат
            result.strategy = GetDistribStrategy();

            //Определение стратегии распределения оплат
            result.repayPeni = GetOrderRepayPeni();

            //Распределять пачки сразу после загрузки
            result.DistributePackImmediately = GetDistribPackTime();

            //Выполнять протоколирование процесса распределения оплат
            result.EnableLog = GetDistribLogging();

            //Первоначальное распределение по полю
            result.chargeMethod = GetDistribChargeMethod();

            //Рассчитывать суммы к перечислению автоматически при распределении/откате оплат
            result.CalcDistributionAutomatically = GetDistribCalcAutomatically();

            //Первоначальное распределение по полю
            result.distributionMethod = GetDistributionMetod();

            //Плательщик заполняет оплату по услугам
            result.AllowSelectServicesWhilePaying = GetDistribAllowServiceWhilePaying();

            //Список приоритетных услуг
            result.PriorityServices = GetDistribPriorityServices();

            return result;
        }

        public void SetSetups(bool workOnlyWithCentralBank, int nzp_prm)
        {
            if (PrepareData(workOnlyWithCentralBank))
            {
                switch (nzp_prm)
                {
                    case 1274: Points.packDistributionParameters.CalcDistributionAutomatically = GetDistribCalcAutomatically();
                        break;
                    case 1135: Points.packDistributionParameters.AllowSelectServicesWhilePaying = GetDistribAllowServiceWhilePaying();
                        break;
                    case 1131: Points.packDistributionParameters.strategy = GetDistribStrategy();
                        break;
                    case 1273: Points.packDistributionParameters.distributionMethod = GetDistributionMetod();
                        break;
                    case 1132: Points.packDistributionParameters.DistributePackImmediately = GetDistribPackTime();
                        break;
                    case 1134: Points.packDistributionParameters.chargeMethod = GetDistribChargeMethod();
                        break;
                    case 1133: Points.packDistributionParameters.EnableLog = GetDistribLogging();
                        break;
                    case 2464: Points.packDistributionParameters.repayPeni = GetOrderRepayPeni();
                        break;
                    case 1368: Points.isUchetContrPu = GetParamIsUchetContrPu();
                        break;
                }
            }
        }

        /// <summary>
        /// Плательщик заполняет оплату по услугам
        /// </summary>
        /// <returns></returns>
        private bool GetDistribAllowServiceWhilePaying()
        {
            string valPrm = GetValPrm(Points.Pref + "_data" + tableDelimiter + "prm_10", 1135);
            return valPrm == "1";
        }

        /// <summary>
        /// Рассчитывать суммы к перечислению автоматически при распределении/откате оплат
        /// </summary>
        /// <returns></returns>
        private bool GetDistribCalcAutomatically()
        {
            string valPrm = GetValPrm(Points.Pref + "_data" + tableDelimiter + "prm_10",
                (int)ParamIds.SystemParams.CalcDistributionAutomatically);
            return valPrm == "1";
        }

        private List<Service> GetDistribPriorityServices()
        {
            var result = new List<Service>();

            string servpriority = DBManager.GetFullBaseName(_connDB, Points.Pref + "_kernel", "servpriority");
            string services = DBManager.GetFullBaseName(_connDB, Points.Pref + "_kernel", "services");

            string sql = " select p.nzp_serv, s.service, s.ordering" +
                         " from " + servpriority + " p, " + services + " s" +
                         " where s.nzp_serv = p.nzp_serv " +
                         "      and " + DBManager.sCurDateTime + " between p.dat_s and p.dat_po " +
                         " order by ordering";
            MyDataReader reader;
            Returns ret = ExecRead(_connDB, out reader, sql, true);

            if (ret.result)
            {
                while (reader.Read())
                {
                    var zap = new Service();
                    if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = (int)reader["nzp_serv"];
                    if (reader["service"] != DBNull.Value) zap.service = ((string)reader["service"]).Trim();
                    if (reader["ordering"] != DBNull.Value) zap.ordering = (int)reader["ordering"];

                    result.Add(zap);
                }
                reader.Close();
            }

            return result;
        }


        /// <summary>
        /// Определение стратегии распределения
        /// </summary>
        /// <returns></returns>
        private PackDistributionParameters.Strategies GetDistribStrategy()
        {
          //  if (Points.IsSmr) return PackDistributionParameters.Strategies.Samara;


            var result = PackDistributionParameters.Strategies.InactiveServicesFirst;
            string valPrm = GetValPrm(Points.Pref + "_data" + tableDelimiter + "prm_10", 1131);

            int id;
            if (Int32.TryParse(valPrm, out id))
            {
                switch (id)
                {
                    case 1:
                        result = PackDistributionParameters.Strategies.InactiveServicesFirst;
                        break;
                    case 2:
                        result = PackDistributionParameters.Strategies.ActiveServicesFirst;
                        break;
                    case 3:
                        result = PackDistributionParameters.Strategies.NoPriority;
                        break;
                }
            }


            return result;
        }


        /// <summary>
        /// Определение порядка гашения пени
        /// </summary>
        /// <returns></returns>
        private PackDistributionParameters.OrderRepayPeni GetOrderRepayPeni()
        {
            var result = PackDistributionParameters.OrderRepayPeni.Last;
            string valPrm = GetValPrm(Points.Pref + "_data" + tableDelimiter + "prm_10", 2464);

            int id;
            if (Int32.TryParse(valPrm, out id))
            {
                switch (id)
                {
                    case 1:
                        result = PackDistributionParameters.OrderRepayPeni.First;
                        break;
                    case 2:
                        result = PackDistributionParameters.OrderRepayPeni.Equally;
                        break;
                    case 3:
                        result = PackDistributionParameters.OrderRepayPeni.Last;
                        break;
                }
            }


            return result;
        }


        /// <summary>
        /// Определение метода начисления
        /// </summary>
        /// <returns></returns>
        private PackDistributionParameters.PaymentDistributionMethods GetDistributionMetod()
        {
            var result = PackDistributionParameters.PaymentDistributionMethods.LastMonthPositiveSumCharge;

            string valPrm = GetValPrm(Points.Pref + "_data" + tableDelimiter + "prm_10",
                (int)ParamIds.SystemParams.PaymentDistributionMethod);
            int id;

            if (Int32.TryParse(valPrm, out id))
            {
                switch (id)
                {
                    case 1:
                        result =
                            PackDistributionParameters.PaymentDistributionMethods.LastMonthPositiveSumCharge;
                        break;
                    case 2:
                        result =
                            PackDistributionParameters.PaymentDistributionMethods.LastMonthSumCharge;
                        break;
                    case 3:
                        result =
                            PackDistributionParameters.PaymentDistributionMethods.CurrentMonthPositiveSumInsaldo;
                        break;
                    case 4:
                        result =
                            PackDistributionParameters.PaymentDistributionMethods.CurrentMonthSumInsaldo;
                        break;
                    case 5:
                        result =
                            PackDistributionParameters.PaymentDistributionMethods.LastMonthPositiveSumOutsaldo;
                        break;
                }
            }

            return result;
        }


        /// <summary>
        /// Определение метода начисления
        /// </summary>
        /// <returns></returns>
        private PackDistributionParameters.ChargeMethods GetDistribChargeMethod()
        {
            var result = PackDistributionParameters.ChargeMethods.PositiveOutsaldo;
            string valPrm = GetValPrm(Points.Pref + "_data" + tableDelimiter + "prm_10", 1134);
            int id;

            if (Int32.TryParse(valPrm, out id))
            {
                switch (id)
                {
                    case 1:
                        result = PackDistributionParameters.ChargeMethods.Outsaldo;
                        break;
                    case 2:
                        result = PackDistributionParameters.ChargeMethods.PositiveOutsaldo;
                        break;
                    case 3:
                        result = PackDistributionParameters.ChargeMethods.MonthlyCalculationWithChangesAndOverpayment;
                        break;
                    case 4:
                        result =
                            PackDistributionParameters.ChargeMethods.PositiveMonthlyCalculationWithChangesAndOverpayment;
                        break;
                    case 5:
                        result = PackDistributionParameters.ChargeMethods.MonthlyCalculationWithChanges;
                        break;
                    case 6:
                        result = PackDistributionParameters.ChargeMethods.PositiveMonthlyCalculationWithChanges;
                        break;
                }
            }

            return result;
        }

        private bool GetDistribLogging()
        {
            string valPrm = GetValPrm(Points.Pref + "_data" + tableDelimiter + "prm_10", 1133);
            return valPrm == "1";
        }


        /// <summary>
        /// Определяет распределять ли загруженные пачки немедленно 
        /// </summary>
        /// <returns></returns>
        private bool GetDistribPackTime()
        {
            string valPrm = GetValPrm(Points.Pref + "_data" + tableDelimiter + "prm_10", 1132);
            return valPrm == "1";
        }

        /// <summary>
        /// Определение относится ли банк к банкам Самары
        /// </summary>
        /// <returns></returns>
        private bool GetSamaraBanks()
        {
            string valPrm = GetValPrm(Points.Pref + "_data" + tableDelimiter + "prm_5", 2000);
            return valPrm == "1";
        }


        /// <summary>
        /// Определение региона
        /// </summary>
        /// <returns></returns>
        private Regions.Region SetRegions()
        {
            var result = Regions.Region.Tatarstan;
            Points.mainPageName = "Биллинговый центр";
            if (!TempTableInWebCashe(_connDB, Points.Pref + sKernelAliasRest + "s_point"))
            {
                //заполнить по-умолчанию point = pref (для одиночных баз)
                var zap = new _Point
                {
                    nzp_wp = Constants.DefaultZap,
                    point = "Локальный банк",
                    pref = Points.Pref,
                    nzp_server = -1
                };



                zap.BeginWork = SetdateBeginWorkSystem(zap.pref);
                zap.BeginCalc = SetDateBeginCalcSystem(zap.pref, zap.BeginWork);
                zap.CalcMonths = GetCalcMonthsByPref(zap.pref, Points.CalcMonth, zap.BeginWork);
                Points.isFinances = false; //финансы не подключены
                Points.PointList.Add(zap);
            }
            else
            {


                string sql = " select substr(bank_number" + DBManager.sConvToVarChar + ",1,2) region_code, point " +
                             "from " + Points.Pref + DBManager.sKernelAliasRest + "s_point " +
                             "where nzp_wp = 1 ";
                MyDataReader reader;
                Returns retRegion = ExecRead(_connDB, out reader, sql, true);
                if (retRegion.result)
                {
                    try
                    {
                        if (reader.Read())
                        {
                            if (reader["region_code"] != DBNull.Value)
                            {
                                result = Regions.GetById(Convert.ToInt32((string)reader["region_code"]));
                            }
                            if (reader["point"] != DBNull.Value)
                                Points.mainPageName = reader["point"].ToString().Trim();
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Ошибка при определении кода региона " + ex.Message);
                    }
                }
                reader.Close();
            }
            return result;
        }


        /// <summary>
        /// Загрузка списка серверов и локальных банков данных 
        /// </summary>
        /// <returns></returns>
        private bool SetPoints(out List<string> warnPoint)
        {
            warnPoint = null;
            if (!TempTableInWebCashe(_connDB, Points.Pref + sKernelAliasRest + "s_point")) return true;

            bool bYesServer = SetServerList();

            return SetSpointList(bYesServer, out warnPoint);
        }


        /// <summary>
        /// Заполнение списка локальных банков данных
        /// </summary>
        /// <param name="bYesServer">Наличие распределнной по серверам БД</param>
        /// <returns></returns>
        private bool SetSpointList(bool bYesServer, out List<string> warnPoint)
        {
            MyDataReader reader;
            warnPoint = new List<string>();
            if (!ExecRead(_connDB, out reader,
                " Select * From " + Points.Pref + DBManager.sKernelAliasRest + "s_point " +
                " Order by point", false).result) return false;
            try
            {
                while (reader.Read())
                {
                    var point = new _Point
                    {
                        nzp_wp = (int)reader["nzp_wp"],
                        flag = (int)reader["flag"],
                        point = reader["point"].ToString().Trim(),
                        pref = reader["bd_kernel"].ToString().Trim(),
                        n = reader["n"] != DBNull.Value ? Convert.ToInt32(reader["n"]) : 0,
                        ol_server = ""
                    };
                    //if (!PointExists(point)) continue;

                    Returns ret;
                    point.CalcMonth = GetCalcMonth(point.pref, out ret);
                    if (ret.result)
                    {
                        if (ret.tag == -222)
                        {
                            warnPoint.Add(ret.text);
                        }
                    }

                    if (bYesServer)
                    {
                        point.nzp_server = (int)reader["nzp_server"];
                        if (reader["bd_old"] != DBNull.Value)
                            point.ol_server = reader["bd_old"].ToString().Trim();
                    }
                    else
                    {
                        point.nzp_server = -1;
                    }

                    if (point.nzp_wp == 1)
                    {
                        //фин.банк
                        Points.Point = point;
                    }
                    else
                    {
                        //MonitorLog.WriteLog("4", MonitorLog.typelog.Info, true);
                        //список расчетных дат

                        if (!_workOnlyWithCentralBank)
                        {
                            //MonitorLog.WriteLog("5", MonitorLog.typelog.Info, true);
                            point.BeginWork = SetdateBeginWorkSystem(point.pref);
                            point.BeginCalc = SetDateBeginCalcSystem(point.pref, point.BeginWork);
                            point.CalcMonths = GetCalcMonthsByPref(point.pref, point.CalcMonth, point.BeginWork);

                        }
                        else
                        {
                            //MonitorLog.WriteLog("6", MonitorLog.typelog.Info, true);
                            // при запрете работе с локальными банками используем параметры центрального банка
                            point.BeginWork = Points.BeginWork;
                            point.BeginCalc = Points.BeginCalc;
                            point.CalcMonths = Points.CalcMonths;

                        }


                        Points.PointList.Add(point);
                    }
                }

                Points.IsPoint = true;
            }
            catch (Exception ex)
            {
                Points.IsPoint = false;
                throw new Exception("Ошибка заполнения s_point " + ex.Message);
            }
            reader.Close();
            return true;
        }


        /// <summary>
        /// Функция проверяет наличие банка данных на текущем сервере
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private bool PointExists(_Point point)
        {
            MyDataReader reader;
            bool result = false;
            string sql = " Select * " +
                         " From " + point.pref + DBManager.sDataAliasRest + "s_area  ";
            if (ExecRead(_connDB, out reader, sql, true).result) result = reader.Read();

            reader.Close();

            return result;
        }

        /// <summary>
        /// Получить список серверов
        /// </summary>
        private bool SetServerList()
        {
            bool bYesServer = false; //isTableHasColumn(_connDB, Points.Pref + sKernelAliasRest + "s_point", "nzp_server");

            if (!bYesServer || !TempTableInWebCashe(_connDB, Points.Pref + sKernelAliasRest + "servers")) return false;
            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
            //подключен механизм фабрики серверов, считаем список серверов
            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++
            MyDataReader reader;
            if (!ExecRead(_connDB, out reader,
                " Select * From " + Points.Pref + DBManager.sKernelAliasRest + "servers " +
                " Order by nzp_server ", false).result) return false;
            try
            {
                while (reader.Read())
                {
                    var zap = new _Server
                    {
                        is_valid = true,
                        conn = "",
                        ip_adr = "",
                        login = "",
                        pwd = "",
                        nserver = "",
                        ol_server = "",
                        nzp_server = (int)reader["nzp_server"]
                    };

                    if (reader["conn"] != DBNull.Value)
                        zap.conn = reader["conn"].ToString().Trim();
                    if (reader["ip_adr"] != DBNull.Value)
                        zap.ip_adr = reader["ip_adr"].ToString().Trim();
                    if (reader["login"] != DBNull.Value)
                        zap.login = reader["login"].ToString().Trim();
                    if (reader["pwd"] != DBNull.Value)
                        zap.pwd = reader["pwd"].ToString().Trim();
                    if (reader["nserver"] != DBNull.Value)
                        zap.nserver = reader["nserver"].ToString().Trim();
                    if (reader["ol_server"] != DBNull.Value)
                        zap.ol_server = reader["ol_server"].ToString().Trim();

                    Points.Servers.Add(zap);
                }
                Points.IsFabric = true;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка заполнения servers " + ex.Message, MonitorLog.typelog.Error, 30, 1, true);
                Points.IsFabric = false;
            }
            finally
            {
                reader.Close();
            }
            return true;
        }

        /// <summary>
        /// Выставляет дату началала работы системы и дату начала расчетов
        /// </summary>
        /// <returns>В случае успеха возвращает true</returns>
        private bool GetDateBeginWork()
        {
            Points.pointWebData.beginWork = SetdateBeginWorkSystem(Points.Pref);
            Points.pointWebData.beginCalc = SetDateBeginCalcSystem(Points.Pref, Points.pointWebData.beginWork);
            Points.pointWebData.calcMonths = GetCalcMonthsByPref(Points.Pref, Points.CalcMonth,
                Points.pointWebData.beginWork);
            return true;
        }


        /// <summary>
        /// Получение текущего расчетного месяца
        /// </summary>
        /// <returns>В случае успеха возвращает true</returns>
        private bool GetCurCalcMonth()
        {
            Points.pointWebData.calcMonth = GetCalcMonth(Points.Pref);
            return Points.pointWebData.calcMonth.month_ > 0 && Points.pointWebData.calcMonth.year_ > 0;
        }


        /// <summary>
        /// Получение текущего операционного дня
        /// </summary>
        private void GetCurOperDay()
        {

            if (Points.Pref == null) _stepLoad = "6. Points.Pref is null ";
            else _stepLoad = "6. Points.Pref = " + Points.Pref;

            MyDataReader reader;

            if (ExecRead(_connDB, out reader,
                "Select dat_oper " +
                "From " + Points.Pref + DBManager.sDataAliasRest + "fn_curoperday", true).result)
            {
                _stepLoad = "6.1";

                Points.isFinances = true;

                if (reader.Read() && reader["dat_oper"] != DBNull.Value)
                {
                    _stepLoad = "6.2";

                    // Points.DateOper = Convert.ToDateTime(reader["dat_oper"]);
                    //определение версии "Финансы УК"
                    //int yy = Points.DateOper.Year;
                    //if (ExecRead(conn_db, out reader,
                    //    "Select * From " + Points.Pref + "_fin_" + (yy - 2000).ToString("00") + ":fn_distrib_01", false).result)
                    //{
                    //    Points.financesType = FinancesTypes.Uk;
                    //}
                }
                reader.Close();
            }

        }


        /// <summary>
        /// Очистка листа банков данных
        /// </summary>
        private static void ClearPointList()
        {
            if (Points.PointList == null) Points.PointList = new List<_Point>();
            else Points.PointList.Clear();

            if (Points.Servers == null) Points.Servers = new List<_Server>();
            else Points.Servers.Clear();
        }


        /// <summary>
        /// Получение списка рассчитанных месяцев для банка данных
        /// </summary>
        /// <param name="pref">Банк данных</param>
        /// <param name="calcMonth">Текущий расчетный месяц</param>
        /// <param name="beginWork">Дата начала работы системы</param>
        /// <returns></returns>
        private List<RecordMonth> GetCalcMonthsByPref(string pref, RecordMonth calcMonth,
            RecordMonth beginWork)
        {
            var result = new List<RecordMonth>();

            for (int y = calcMonth.year_; y >= beginWork.year_; y--)
            {
                RecordMonth r;
                r.year_ = y;
                r.month_ = 0;
                result.Add(r);

                int mm = 12;
                if (y == calcMonth.year_) mm = calcMonth.month_;

                for (int m = mm; m >= 1; m--)
                {
                    r.year_ = y;
                    r.month_ = m;

                    result.Add(r);
                }
            }
            if (result.Count == 0)
            {
                MonitorLog.WriteLog("Список расчетных месяцев пуст для банка " + pref, MonitorLog.typelog.Warn, true);
            }
            if (Constants.Trace)
                Utility.ClassLog.WriteLog("Список расчетных месяцев для банка " + pref + " - " + result.Count);
            return result;
        }


        /// <summary>
        /// Определение даты начала расччета в системе
        /// </summary>
        /// <param name="pref">Банк данных</param>
        /// <param name="beginWork">Дата начала работы системы</param>
        /// <returns></returns>
        private RecordMonth SetDateBeginCalcSystem(string pref, RecordMonth beginWork)
        {
            var result = new RecordMonth();
            string dateBeginCalcSystem = GetValPrm(pref + "_data" + tableDelimiter + "prm_10", 771);

            if (String.IsNullOrEmpty(dateBeginCalcSystem))
            {
                MonitorLog.WriteLog("не указана дата пересчетов " + pref, MonitorLog.typelog.Warn, true);
                result.year_ = beginWork.year_;
                result.month_ = beginWork.month_;
            }
            else
            {
                DateTime d = Convert.ToDateTime(dateBeginCalcSystem);
                result.month_ = d.Month;
                result.year_ = d.Year;
            }
            return result;
        }


        /// <summary>
        /// Определение даты начала работы системы
        /// </summary>
        /// <param name="pref">Банк данных</param>
        /// <returns></returns>
        private RecordMonth SetdateBeginWorkSystem(string pref)
        {
            var result = new RecordMonth();

            string dateBeginWorkSystem = GetValPrm(pref + "_data" + tableDelimiter + "prm_10", 82);
            var defaultStartDate = new DateTime(2005, 1, 1); //дата начала работы по умолчанию

            if (String.IsNullOrEmpty(dateBeginWorkSystem))
            {
                MonitorLog.WriteLog("Не указана дата начала работы системы " + pref, MonitorLog.typelog.Warn, true);
                result.year_ = defaultStartDate.Year;
                result.month_ = defaultStartDate.Month;
            }
            else
            {
                DateTime d = Convert.ToDateTime(dateBeginWorkSystem);
                result.month_ = d.Month;
                result.year_ = d.Year;
            }

            return result;
        }

        public RecordMonth GetCalcMonth(string pref)
        {
            Returns ret;
            return GetCalcMonth(pref, out ret);
        }

        /// <summary>
        /// Получить текущий расчетный месяц
        /// </summary>
        public RecordMonth GetCalcMonth(string pref, out Returns ret)
        {
            MyDataReader reader;
            var rm = new RecordMonth { year_ = 0, month_ = 0 };
            ret = Utils.InitReturns();
            InsertCentralCalcMonths(_connDB, out ret);
            var sql = new StringBuilder("Select count(*) From " + pref +
                                                DBManager.sDataAliasRest + "saldo_date "
                                                + " Where iscurrent = 0 ");
            var obj = ExecScalar(_connDB, sql.ToString(), out ret, true);
            var count = 0;
            try
            {
                count = Convert.ToInt32(obj);
            }
            catch (Exception)
            {
                ret = new Returns(false, "Не определен текущий расчетный месяц", -1);
                return rm;
            }

            if (count == 0)
            {
                ret = ExecSQL(_connDB, "insert into " + pref + DBManager.sDataAliasRest +
                                       "saldo_date (month_, yearr, iscurrent, dat_saldo,prev_date)" +
                                       "select month_, yearr, iscurrent, dat_saldo, prev_date from " + Points.Pref +
                                       DBManager.sDataAliasRest + "saldo_date Where iscurrent = 0", true);
                if (!ret.result) return rm;


            }

            ret = ExecRead(_connDB, out reader,
                " Select month_,yearr From " + pref + DBManager.sDataAliasRest + "saldo_date " +
                " Where iscurrent = 0 ", true);
            if (!ret.result) return rm;

            if (!reader.Read())
            {
                ret.text = "Не определен текущий расчетный месяц";
                ret.result = false;
                ret.tag = -1;
                return rm;
            }

            if (reader["month_"] == DBNull.Value || reader["yearr"] == DBNull.Value)
            {
                ret.text = "Ошибка определения текущего расчетного месяца";
                ret.result = false;
                return rm;
            }
            rm.month_ = (int)reader["month_"];
            rm.year_ = (int)reader["yearr"];
            reader.Close();

            if (count == 0)
            {
                ret = ExecRead(_connDB, out reader, "select point from " + Points.Pref + DBManager.sKernelAliasRest +
                                  "s_point where trim(bd_kernel)='" + pref.Trim() + "'", true);
                if (!ret.result) return rm;
                if (!reader.Read())
                {
                    ret = new Returns(false, "Не определен текущий расчетный месяц", -1);
                    return rm;
                }
                var point = "";
                if (reader["point"] != DBNull.Value) point = Convert.ToString(reader["point"]);

                ret = ExecRead(_connDB, out reader, "select dat_saldo from " + Points.Pref + DBManager.sDataAliasRest +
                                                    "saldo_date Where iscurrent = 0", true);
                if (!ret.result) return rm;
                if (!reader.Read())
                {
                    ret = new Returns(false, "Не определен расчетный месяц центрального банка", -1);
                    return rm;
                }
                string datSaldo = "";
                if (reader["dat_saldo"] != DBNull.Value)
                    datSaldo = Convert.ToDateTime(reader["dat_saldo"]).ToShortDateString();
                reader.Close();
                ret = new Returns(true,
                    "Не указан расчетный месяц банка данных " + point.Trim() + " (" + pref.Trim() +
                    "), для него был установлен расчетный месяц " + datSaldo + " центрального банка", -222);
                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Warn, true);
            }
            return rm;
        }

        public void InsertCentralCalcMonths(IDbConnection connection, out Returns ret)
        {
            var count = Convert.ToInt32(ExecScalar(connection, "select count(*) from " + Points.Pref + sDataAliasRest + "saldo_date", out ret, true));
            if (!ret.result) return;
            var insDate = new DateTime(2000, 01, 01);
            var prevDate = insDate.AddMonths(-1);
            if (count == 0)
            {
                ret = ExecSQL(connection, string.Format("insert into {0}saldo_date(month_,yearr,iscurrent,dat_saldo,prev_date) values ({1},{2},0,'{3}','{4}')",
                    Points.Pref + sDataAliasRest, insDate.Month, insDate.Year, insDate.Date.ToString("dd.MM.yyyy"), prevDate.Date.ToString("dd.MM.yyyy")));
                if (!ret.result) return;
                Console.WriteLine(@"Расчетный месяц установлен автоматически:" + insDate.Date.ToString("MM.yyyy"));
            }
            count = Convert.ToInt32(ExecScalar(connection, "select count(*) from " + Points.Pref + sDataAliasRest + "fn_curoperday", out ret, true));
            if (!ret.result) return;
            if (count == 0)
            {
                ret = ExecSQL(connection, string.Format("insert into {0}fn_curoperday(dat_oper,dat_inkas,flag) values ('{1}','{1}',1)",
                    Points.Pref + sDataAliasRest, insDate.Date.ToString("dd.MM.yyyy")));
                Console.WriteLine(@"Операционный день установлен автоматически:" + insDate.Date.ToString("dd.MM.yyyy"));
            }
        }

    }


}