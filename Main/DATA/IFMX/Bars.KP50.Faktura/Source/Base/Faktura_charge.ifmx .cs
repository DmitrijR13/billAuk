



using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.Faktura.Source.Base
{
    public class FakturaPayer : Payer, ICloneable
    {
        public string Rcount;
        public string BankName;

        public FakturaPayer()
        {
            Clear();
        }

        public void Clear()
        {
            Rcount = "";
            BankName = "";
        }
        public object Clone()
        {
            var newPayer = (FakturaPayer)MemberwiseClone();
            return newPayer;
        }
    }

    public class FakturaService
    {
        public int NzpServ;
        public int Ordering;
        public int NzpMeasure;
        public string Measure;
        public string NameServ;

        public enum ServiceType
        {
            None = 0,

            /// <summary>
            /// Коммунальные услуги
            /// </summary>
            Communal = 1,

            /// <summary>
            /// Жилищные услуги
            /// </summary>
            Housing = 2
        }

        public ServiceType ServType;
        public FakturaService()
        {
            NzpServ = 0;
            NzpMeasure = 0;
            Ordering = 0;
            Measure = "";
            NameServ = "";
            ServType = ServiceType.None;
        }
    }


    /// <summary>
    /// Набор свойст услуги
    /// </summary>
    public class SumServ2 : ICloneable
    {
        public string NameServ;
        public FakturaPayer Payer;
        public string SuppRekv;
        public int NzpServ;
        public int NzpOdnMaster;//Мастер услуга для объединенной
        public int NzpSupp;
        public int NzpFrm;
        public int IsDevice;
        public int NzpMeasure;
        public int OldMeasure;
        public int Ordering;
        public string Measure;
        public decimal SumInsaldo;
        public decimal SumOutsaldo;
        public decimal Tarif;
        public decimal TarifF;
        public decimal RsumTarif;
        public decimal SumTarif;
        public decimal SumLgota;
        public decimal SumNedop;
        public decimal SumReal;
        public decimal SumSn;
        public decimal Reval;
        public decimal RevalGil;
        public decimal SumPere;
        public decimal RealCharge;
        public decimal SumCharge;
        public decimal SumMoney;
        public decimal CCalc;
        public decimal Norma;
        public decimal CReval;
        public bool UnionServ;
        public bool CanAddTarif;
        public bool CanAddVolume;
        public bool IsOdn;
        public int COkaz;

        public SumServ2()
        {
            Payer = new FakturaPayer();
            Clear();
        }

        public void Clear()
        {
            NameServ = "";
            Payer.Clear();
            SuppRekv = "";
            Measure = "";
            NzpServ = 0;
            NzpOdnMaster = 0;
            NzpSupp = 0;
            NzpFrm = 0;
            IsDevice = 0;
            NzpMeasure = 0;
            OldMeasure = 0;
            Ordering = 0;
            SumInsaldo = 0;
            SumOutsaldo = 0;
            Tarif = 0;
            TarifF = 0;
            RsumTarif = 0;
            SumTarif = 0;
            SumLgota = 0;
            SumNedop = 0;
            SumReal = 0;
            SumSn = 0;
            Reval = 0;
            RevalGil = 0;
            Norma = 0;
            SumPere = 0;
            RealCharge = 0;
            SumCharge = 0;
            SumMoney = 0;
            CCalc = 0;
            CReval = 0;
            UnionServ = false;
            CanAddTarif = true;
            CanAddVolume = true;
            IsOdn = false;
            COkaz = 0;
        }

        public void AddSum(SumServ2 aServ)
        {
            SumInsaldo += aServ.SumInsaldo;
            SumOutsaldo += aServ.SumOutsaldo;
            RsumTarif += aServ.RsumTarif;
            SumTarif += aServ.SumTarif;
            SumLgota += aServ.SumLgota;
            SumNedop += aServ.SumNedop;
            SumReal += aServ.SumReal;
            SumSn += aServ.SumSn;
            Reval += aServ.Reval;
            SumPere += aServ.SumPere;
            RealCharge += aServ.RealCharge;
            SumCharge += aServ.SumCharge;
            SumMoney += aServ.SumMoney;
            Norma += aServ.Norma;
            NzpSupp = aServ.NzpSupp;

            //if ((nzpServ == 9) & ((aServ.nzpServ == 14) || (aServ.nzpServ == 9)))
            //{
            //    tarif += aServ.tarif;
            //}
            //else if (tarif == 0)
            //{
            //    tarif = aServ.tarif;
            //}

            if (aServ.CanAddTarif)
            {
                if (NzpMeasure == aServ.NzpMeasure)
                {
                    Tarif += aServ.Tarif;
                }
                //else
                //{
                //    //Оставить всё как есть,Сделать ничего если разные единицы измерения услуги
                //}

            }
            else if (Tarif == 0)
            {
                Tarif = aServ.Tarif;
            }


            if (aServ.Tarif > 0)
            {
                if (NzpMeasure == aServ.NzpMeasure)
                {
                    if (aServ.CanAddVolume)
                    {
                        CCalc += aServ.CCalc;
                        CReval += aServ.CReval;
                    }
                    else
                    {
                        CCalc = Math.Max(CCalc, aServ.CCalc);
                        CReval = Math.Max(CReval, aServ.CReval);

                    }
                    NzpMeasure = aServ.NzpMeasure;
                    IsDevice = Math.Max(IsDevice, aServ.IsDevice);
                    NzpFrm = aServ.NzpFrm;

                }
                //else
                //{
                //   // cCalc = aServ.cCalc;
                //   // cReval = aServ.cReval;
                //   //Оставить всё как есть, если разные единицы измерения услуги
                //}

                COkaz = aServ.COkaz;
                NzpSupp = aServ.NzpSupp;
            }

        }

        public object Clone()
        {
            var newServ = (SumServ2)MemberwiseClone();
            newServ.Payer = (FakturaPayer)Payer.Clone();

            return newServ;
        }

    }

    /// <summary>
    /// Базовый класс услуги для вывода в счете на оплату ЖКУ
    /// </summary>
    public class BaseServ2 : ICloneable, IComparable<BaseServ2>
    {
        //Услуга Коммунальная или нет
        public bool KommServ;

        /// <summary>
        /// Основаня сумма по услуге
        /// </summary>
        public SumServ2 Serv;
        /// <summary>
        /// в т.ч. сумма ОДН по услуге
        /// </summary>
        public SumServ2 ServOdn;

        public List<SumServ2> SlaveServ;

        public BaseServ2()
        {
            Serv = new SumServ2();
            ServOdn = new SumServ2();
            KommServ = false;
            SlaveServ = new List<SumServ2>();
        }

        public void Clear()
        {
            Serv.Clear();
            ServOdn.Clear();
            SlaveServ.Clear();
        }

        /// <summary>
        /// Добавить к услуге некоторую сумму
        /// </summary>
        /// <param name="aServ"></param>
        public void AddSum(SumServ2 aServ)
        {
            Serv.AddSum(aServ);
            if (aServ.IsOdn)
            {
                ServOdn.AddSum(aServ);
            }
        }

        public void AddSlave(BaseServ2 aServ)
        {
            bool findServ = false;
            foreach (SumServ2 ss in SlaveServ)
            {
                if ((ss.NzpServ == aServ.Serv.NzpServ) || (aServ.Serv.NzpOdnMaster == ss.NzpServ))
                {
                    ss.AddSum(aServ.Serv);
                    findServ = true;
                }
            }
            if (!findServ) SlaveServ.Add(aServ.Serv);
        }

        public bool Empty()
        {
            if (Math.Abs(Serv.Tarif) +
                Math.Abs(Serv.RsumTarif) +
                Math.Abs(Serv.SumMoney) +
                Math.Abs(Serv.SumInsaldo) +
                Math.Abs(Serv.SumOutsaldo) +
                Math.Abs(Serv.RealCharge) +
                Math.Abs(Serv.Reval) < 0.001m)
            {
                return true;
            }
            return false;
        }


        public decimal GetTarif()
        {
            if (SlaveServ.Count == 0) return Serv.Tarif;
            return SlaveServ.Where(ss => (Serv.NzpServ == ss.NzpServ) || (ss.CanAddTarif)).Sum(ss => ss.Tarif);
        }
        public decimal GetVolume()
        {
            if (SlaveServ.Count == 0) return Serv.Tarif;
            return SlaveServ.Where(ss => (Serv.NzpServ == ss.NzpServ) || (ss.CanAddVolume)).Sum(ss => ss.CCalc);
        }
        public decimal GetCokaz()
        {
            if (SlaveServ.Count == 0) return Serv.COkaz;
            decimal dayOkaz = SlaveServ.Where(ss => !ss.IsOdn).Aggregate<SumServ2, decimal>(32, (current, ss) => Math.Min(current, ss.COkaz));

            return dayOkaz == 32 ? 0 : dayOkaz;
        }


        public void CopyToOdn()
        {
            if (Serv.IsOdn)
            {
                ServOdn.AddSum(Serv);
                ServOdn.CCalc = Serv.CCalc;
                ServOdn.CReval = Serv.CReval;
            }
        }

        public object Clone()
        {
            var newServ = new BaseServ2
            {
                Serv = (SumServ2)Serv.Clone(),
                ServOdn = (SumServ2)ServOdn.Clone(),
                KommServ = KommServ,
                SlaveServ = new List<SumServ2>()
            };
            foreach (SumServ2 ss in SlaveServ)
            {
                newServ.SlaveServ.Add((SumServ2)ss.Clone());
            }

            return newServ;
        }

        public int CompareTo(BaseServ2 other)
        {

            if (other == null) return 1;

            return Serv.Ordering.CompareTo(other.Serv.Ordering);
        }
    }


    /// <summary>
    /// Класс объединенной услуги
    /// </summary>
    public class MasterServ2
    {
        /// <summary>
        /// Услуга, к которой присоединяют остальные услуги
        /// </summary>
        public BaseServ2 MainServ;
        /// <summary>
        /// Список услуг, присоединяемых к основной услуге 
        /// </summary>
        public List<BaseServ2> SlaveListServ;

        public MasterServ2()
        {
            MainServ = new BaseServ2();
            SlaveListServ = new List<BaseServ2>();
        }

    }
    public class CUnionServ2
    {
        public List<MasterServ2> MasterList;

        public CUnionServ2()
        {
            MasterList = new List<MasterServ2>();
        }

        /// <summary>
        /// Получить услугу, в которую объединяют, по коду подчиненной услуги
        /// </summary>
        /// <param name="nzpServ">код подчиненной услуги</param>
        /// <returns>Базовая услуга</returns>
        public BaseServ2 GetMainServBySlave(int nzpServ)
        {
            return (from t in MasterList
                    where t.SlaveListServ.Any(t1 => t1.Serv.NzpServ == nzpServ)
                    select t.MainServ).FirstOrDefault();
        }

        /// <summary>
        /// Получить мастер услугу(со списком подчиненных услуг) по коду подчиненной услуги
        /// </summary>
        /// <param name="nzpServ">Код подчиненной услуги</param>
        /// <returns>Мастер услуга</returns>
        public MasterServ2 GetMasterBySlave(int nzpServ)
        {
            return MasterList.FirstOrDefault(t => t.SlaveListServ.Any(t1 => t1.Serv.NzpServ == nzpServ));
        }

        /// <summary>
        /// Добавить услугу в список объединенных услуг
        /// </summary>
        /// <param name="mainServ">Мастер услуга</param>
        /// <param name="slaveServ">Подчиненная услуга</param>
        public void AddServ(BaseServ2 mainServ, BaseServ2 slaveServ)
        {
            bool hasMaster = false;
            foreach (MasterServ2 t in MasterList)
            {
                if (t.MainServ.Serv.NzpServ == mainServ.Serv.NzpServ)
                {
                    bool hasSlave = false;
                    hasMaster = true;

                    foreach (BaseServ2 t1 in t.SlaveListServ)
                    {
                        if (t1.Serv.NzpServ == slaveServ.Serv.NzpServ)
                        {
                            hasSlave = true;
                        }
                    }
                    if (!hasSlave) //Если подчиненная услуга не была ранее добавлена, то добавляем её
                    {
                        t.SlaveListServ.Add(slaveServ);
                    }
                }
            }
            if (hasMaster) return;
            var masterServ = new MasterServ2 { MainServ = mainServ };
            masterServ.SlaveListServ.Add(slaveServ);
            MasterList.Add(masterServ);
        }
    }


    /// <summary>
    /// Начисления
    /// </summary>
    public class DbFakturaCharge
    {

        /// <summary>
        /// Текущее подключение к баще данных
        /// </summary>
        private readonly IDbConnection _connection;

        private int _nzpArea;

        /// <summary>
        /// Список объединяемых услуг
        /// </summary>
        private readonly CUnionServ2 _unionServ;

        /// <summary>
        /// Список формул расчтеа 
        /// </summary>
        private Dictionary<int, Measure> _listFormuls;

        /// <summary>
        /// Список поставщиков
        /// </summary>
        private Dictionary<long, FakturaPayer> _listPayer;

        /// <summary>
        /// Список услуг ОДН
        /// </summary>
        private Dictionary<int, int> _listOdn;

        /// <summary>
        /// Список услуг, справочник
        /// </summary>
        private Dictionary<int, FakturaService> _listService;

        //Список услуг счета
        public Dictionary<int, BaseServ2> ListServ;

        //Список поставщиков счета
        public Dictionary<long, BaseServ2> ListSupp;

        /// <summary>
        /// Суммарно Итого по счету
        /// </summary>
        public BaseServ2 SummaryServ;


        public DbFakturaCharge(IDbConnection connDb, int month, int year, bool isODNneeded = true)
        {
            _connection = connDb;
            Month = month;
            Year = year;
            Avans = false;
            _nzpArea = 0;
            if (isODNneeded) _unionServ = new CUnionServ2(); 
            SummaryServ = new BaseServ2();
            ListServ = new Dictionary<int, BaseServ2>();
            ListSupp = new Dictionary<long, BaseServ2>();
            LoadPayerList();
            LoadService();
            if (isODNneeded) LoadOdnServ(); else _listOdn = new Dictionary<int, int>();
            LoadFormulList();
            SetServiceType();
        }

        /// <summary>
        /// Расчетный год для которого выбираются счетчики
        /// </summary>
        public int Year { get; set; }

        /// <summary>
        /// Рассчетный месяц, для которого выбираются счетчики
        /// </summary>
        public int Month { get; set; }

        /// <summary>
        /// Признак авансового платежного документа
        /// </summary>
        public bool Avans { get; set; }

        /// <summary>
        /// Загрузка формул расчета
        /// </summary>
        private void LoadFormulList()
        {
            IDataReader reader = null;
            if (_listFormuls == null) _listFormuls = new Dictionary<int, Measure>();
            else _listFormuls.Clear();
            string s = " SELECT nzp_frm, a.nzp_measure, measure  " +
                       " FROM " + Points.Pref + DBManager.sKernelAliasRest + "formuls a, " +
                                  Points.Pref + DBManager.sKernelAliasRest + "s_measure b" +
                       " WHERE a.nzp_measure = b.nzp_measure ";

            IDbCommand cmd = DBManager.newDbCommand(s, _connection);

            try
            {
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (reader["nzp_frm"] != DBNull.Value)
                    {
                        _listFormuls.Add(Convert.ToInt32(reader["nzp_frm"]),
                            new Measure
                            {
                                nzp_measure = Int32.Parse(reader["nzp_measure"].ToString().Trim()),
                                measure = reader["measure"].ToString().Trim()
                            });
                    }
                }
            }
            catch (Exception e)
            {
                MonitorLog.WriteLog("Счет-квитанция : Ошибка при выборке формул расчета " + e.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
                if (cmd != null)
                {
                    cmd.Dispose();
                }
            }


        }

        /// <summary>
        /// Загрузка списка объединяемых услаг
        /// </summary>
        private bool LoadUnionServ()
        {
            if (_unionServ == null) return false;
            _unionServ.MasterList.Clear();
            string sql = " SELECT s1.ordering as ord_base, s1.service_name as serv_base, " +
                       "        s1.ed_izmer as ed_izmer_base, a.nzp_serv_base, " +
                       "        s2.ordering as ord_uni, s2.service_name as serv_uni, " +
                       "        s2.ed_izmer as ed_izmer_uni, a.nzp_serv_uni " +
                       " FROM  " + Points.Pref + DBManager.sKernelAliasRest + "service_union a, " +
                       Points.Pref + DBManager.sKernelAliasRest + "services s1, " +
                       Points.Pref + DBManager.sKernelAliasRest + "services s2 " +
                       " WHERE a.nzp_serv_base=s1.nzp_serv " +
                       "        AND a.nzp_serv_uni=s2.nzp_serv ";
            IDbCommand cmd = DBManager.newDbCommand(sql, _connection);
            try
            {
                IDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var servBase = new BaseServ2
                    {
                        Serv =
                        {
                            NzpServ = Int32.Parse(reader["nzp_serv_base"].ToString()),
                            NameServ = reader["serv_base"].ToString(),
                            Measure = reader["ed_izmer_base"].ToString(),
                            Ordering = Int32.Parse(reader["ord_base"].ToString())
                        }
                    };
                    var servUni = new BaseServ2
                    {
                        Serv =
                        {
                            NzpServ = Int32.Parse(reader["nzp_serv_uni"].ToString()),
                            NameServ = reader["serv_uni"].ToString(),
                            Measure = reader["ed_izmer_uni"].ToString(),
                            Ordering = Int32.Parse(reader["ord_uni"].ToString())
                        }
                    };
                    _unionServ.AddServ(servBase, servUni);
                }
                reader.Close();
                reader.Dispose();
                cmd.Dispose();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql + " " + ex.Message,
                    MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                cmd.Dispose();
            }
            return true;
        }

        /// <summary>
        /// зашрузка списка услуг ОДН
        /// </summary>
        private void LoadOdnServ()
        {
            if (_listOdn != null) return;
            _listOdn = new Dictionary<int, int>();

            IDataReader reader = null;
            string sql = " SELECT  * " +
                         " FROM  " + Points.Pref + DBManager.sKernelAliasRest + "serv_odn ";
            IDbCommand cmd = DBManager.newDbCommand(sql, _connection);

            try
            {
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int odnServ = Int32.Parse(reader["nzp_serv"].ToString().Trim());
                    int normalServ = Int32.Parse(reader["nzp_serv_link"].ToString().Trim());
                    _listOdn.Add(odnServ, normalServ);
                }
            }
            catch (Exception e)
            {
                MonitorLog.WriteLog("Счет-квитанция : Ошибка при выборка услуг ОДН " + e.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
                if (cmd != null)
                {
                    cmd.Dispose();
                }
            }



        }


        /// <summary>
        /// Загрузка списка поставщиков КУ
        /// </summary>
        private void LoadPayerList()
        {
            if (_listPayer != null) return;
            _listPayer = new Dictionary<long, FakturaPayer>();

            IDataReader reader = null;
            string sql = " SELECT distinct s.nzp_payer, su.nzp_supp, s.payer, b.rcount, s.inn, s.kpp, b.bank_name " +
                         " FROM  " + Points.Pref + DBManager.sKernelAliasRest + "supplier su, "+
                         " " + Points.Pref + DBManager.sKernelAliasRest + "s_payer s " +
                         " left join " + Points.Pref + DBManager.sDataAliasRest + "fn_bank b " +
                         " on s.nzp_payer=b.nzp_payer " +
                         " WHERE su.nzp_payer_supp = s.nzp_payer ";

            IDbCommand cmd = DBManager.newDbCommand(sql, _connection);

            try
            {
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var payer = new FakturaPayer
                    {
                        nzp_supp = Int64.Parse(reader["nzp_supp"].ToString().Trim()),
                        payer =
                            reader["payer"] != DBNull.Value
                                ? reader["payer"].ToString().Trim()
                                : "Поставщик не определен",
                        inn = reader["inn"] != DBNull.Value ? reader["inn"].ToString().Trim() : "",
                        kpp = reader["kpp"] != DBNull.Value ? reader["kpp"].ToString().Trim() : "",
                        Rcount = reader["rcount"] != DBNull.Value ? reader["rcount"].ToString().Trim() : "",
                        BankName = reader["bank_name"] != DBNull.Value ? reader["bank_name"].ToString().Trim() : ""
                    };
                    if (!_listPayer.ContainsKey(payer.nzp_supp))
                        _listPayer.Add(payer.nzp_supp, payer);
                }
            }
            catch (Exception e)
            {
                MonitorLog.WriteLog("Счет-квитанция : Ошибка при выборке списка поставщиков" + e.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
                if (cmd != null)
                {
                    cmd.Dispose();
                }
            }


        }

        /// <summary>
        /// Загрузка справочника услуг
        /// </summary>
        private void LoadService()
        {
            if (_listService != null) return;
            _listService = new Dictionary<int, FakturaService>();

            IDataReader reader = null;
            string sql = " SELECT  service_name, s.nzp_measure, measure, ed_izmer, s.nzp_serv, s.ordering " +
                         " FROM  " + Points.Pref + DBManager.sKernelAliasRest + "services s " +
                         " left join " + Points.Pref + DBManager.sKernelAliasRest + "s_measure b " +
                         " on s.nzp_measure=b.nzp_measure " +
                         " WHERE nzp_serv>1";

            IDbCommand cmd = DBManager.newDbCommand(sql, _connection);

            try
            {
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var service = new FakturaService
                    {
                        NzpServ = Int32.Parse(reader["nzp_serv"].ToString().Trim()),
                        NzpMeasure =
                            reader["nzp_measure"] != null ? Int32.Parse(reader["nzp_measure"].ToString().Trim()) : 0,
                        Measure =
                            reader["measure"] != DBNull.Value ? reader["measure"].ToString().Trim() : " не определен",
                        NameServ =
                            reader["service_name"] != DBNull.Value
                                ? reader["service_name"].ToString().Trim()
                                : " не определена",
                        Ordering = reader["ordering"] != DBNull.Value ? Int32.Parse(reader["ordering"].ToString()) : 0
                    };
                    if (service.NzpMeasure == 0)
                        service.Measure = reader["ed_izmer"].ToString().Trim();

                    _listService.Add(service.NzpServ, service);
                }
            }
            catch (Exception e)
            {
                MonitorLog.WriteLog("Счет-квитанция : Ошибка при выборке услуг " + e.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
                if (cmd != null)
                {
                    cmd.Dispose();
                }
            }


        }

        /// <summary>
        /// Установка типа услуг: коммунальные или жилищные
        /// </summary>
        private void SetServiceType()
        {
            if (_listService == null) return;

            IDataReader reader = null;
            string sql = " SELECT  * " +
                         " FROM  " + Points.Pref + DBManager.sKernelAliasRest + "grpserv_schet " +
                         " WHERE nzp_serv>1";

            IDbCommand cmd = DBManager.newDbCommand(sql, _connection);

            try
            {
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (reader["nzp_serv"] != null)
                    {
                        int nzpServ = Int32.Parse(reader["nzp_serv"].ToString().Trim());
                        int typeSserv = Int32.Parse(reader["nzp_grpserv"].ToString().Trim());
                        if (_listService.ContainsKey(nzpServ))
                            _listService[nzpServ].ServType = typeSserv == 2
                                ? FakturaService.ServiceType.Communal
                                : FakturaService.ServiceType.None;

                    }
                }
            }
            catch (Exception e)
            {
                MonitorLog.WriteLog("Счет-квитанция : Ошибка при выборке коммунальных услуг" + e.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
                if (cmd != null)
                {
                    cmd.Dispose();
                }
            }


        }


        /// <summary>
        /// Добавление обычной услуги 
        /// </summary>
        /// <param name="aServ"></param>
        private void AddSimpleServ(BaseServ2 aServ)
        {
            if (ListServ.ContainsKey(aServ.Serv.NzpServ)) //если такая услуга уже есть то добавляем к ней
            {
                ListServ[aServ.Serv.NzpServ].AddSum(aServ.Serv);
            }
            else
            {
                ListServ.Add(aServ.Serv.NzpServ, aServ);//иначе добавляем новую
            }

        }


        /// <summary>
        /// Добавление Объединенной услуги
        /// </summary>
        /// <param name="mainServ"></param>
        /// <param name="aServ"></param>
        private void AddUnionServ(BaseServ2 mainServ, BaseServ2 aServ)
        {

            if (ListServ.ContainsKey(mainServ.Serv.NzpServ))//если мастер услуга уже присутствует, то добавляем к ней
            {
                ListServ[mainServ.Serv.NzpServ].AddSum(aServ.Serv);
                ListServ[mainServ.Serv.NzpServ].AddSlave(aServ);

            }
            else //Не найдена объединенная услуга, добавляем её
            {
                var newMainServ = (BaseServ2)mainServ.Clone();
                newMainServ.Serv.Measure = aServ.Serv.Measure;
                newMainServ.Serv.NzpMeasure = aServ.Serv.NzpMeasure;
                newMainServ.KommServ = aServ.KommServ;
                newMainServ.AddSum(aServ.Serv);
                newMainServ.AddSlave(aServ);
                newMainServ.Serv.UnionServ = true;
                ListServ.Add(newMainServ.Serv.NzpServ, newMainServ);
            }

        }

        /// <summary>
        /// Добавление услуги в список
        /// </summary>
        /// <param name="aServ"></param>
        private void AddServ(BaseServ2 aServ)
        {

            SummaryServ.AddSum(aServ.Serv); //Подсчитываем Итого

            // if ((System.Math.Abs(aServ.serv.reval) > 0.001m) || (System.Math.Abs(aServ.serv.realCharge) > 0.001m))
            // {
            //     AddReval(aServ.serv);
            // }
            BaseServ2 mainServ = _unionServ != null ? _unionServ.GetMainServBySlave(aServ.Serv.NzpServ) : null;
            //Определяем к какой услуге добавить
            if (mainServ == null) //услуга не объединяемая
            {
                AddSimpleServ(aServ);
            }
            else //если услуга объединяемая
            {
                AddUnionServ(mainServ, aServ);
            }
        }


        /// <summary>
        /// Установка атрибутов услуги из справочника
        /// </summary>
        /// <param name="serv">Услуга из начислений</param>
        private void SetServAttrib(BaseServ2 serv)
        {
            serv.Serv.Ordering = _listService[serv.Serv.NzpServ].Ordering;
            serv.Serv.NzpMeasure = _listService[serv.Serv.NzpServ].NzpMeasure;
            serv.Serv.OldMeasure = _listService[serv.Serv.NzpServ].NzpMeasure;
            serv.Serv.Measure = _listService[serv.Serv.NzpServ].Measure;
            serv.Serv.NameServ = _listService[serv.Serv.NzpServ].NameServ;
            serv.KommServ = _listService[serv.Serv.NzpServ].ServType == FakturaService.ServiceType.Communal;
        }



        private void GetNachWithSupplier(string tableCharge, int nzpKvar)
        {
            IDataReader reader = null;

            string sql = String.Format(
                " SELECT a.tarif, a.nzp_serv, a.nzp_frm, a.nzp_supp, a.is_device, a.rsum_tarif, a.sum_nedop," +
                "        a.reval, a.sum_tarif_sn_f, a.real_charge, a.sum_outsaldo, a.sum_charge, a.sum_money, " +
                "        a.sum_insaldo, a.c_calc, a.c_reval, a.sum_real " +
                " FROM  {0} a " +
                " WHERE a.nzp_kvar={1}" +
                "        AND a.dat_charge is null " +
                "        AND a.nzp_serv>1 "
                , tableCharge, nzpKvar);

            IDbCommand cmd = DBManager.newDbCommand(sql, _connection);

            try
            {
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var serv = new BaseServ2
                    {
                        Serv =
                        {
                            NzpServ =
                                reader["nzp_serv"] != DBNull.Value ? Int32.Parse(reader["nzp_serv"].ToString()) : 0
                        }
                    };
                    if (_listOdn.ContainsKey(serv.Serv.NzpServ))
                    {
                        serv.Serv.NzpOdnMaster = _listOdn[serv.Serv.NzpServ];
                        serv.Serv.IsOdn = true;
                    }

                    serv.Serv.NzpSupp = reader["nzp_supp"] != DBNull.Value ? Int32.Parse(reader["nzp_supp"].ToString()) : 0;
                    serv.Serv.NzpFrm = reader["nzp_frm"] != DBNull.Value ? Convert.ToInt32(reader["nzp_frm"]) : 0;
                    if (_listService.ContainsKey(serv.Serv.NzpServ))
                    {
                        SetServAttrib(serv);
                    }

                    if (_listFormuls.ContainsKey(serv.Serv.NzpFrm))
                    {
                        serv.Serv.Measure = _listFormuls[serv.Serv.NzpFrm].measure;
                        serv.Serv.NzpMeasure = _listFormuls[serv.Serv.NzpFrm].nzp_measure;
                    }


                    serv.Serv.Tarif = reader["tarif"] != DBNull.Value ? Convert.ToDecimal(reader["tarif"]) : 0; 
                    serv.Serv.IsDevice = reader["is_device"] != DBNull.Value ? Int32.Parse(reader["is_device"].ToString()) : 0;
                    serv.Serv.RsumTarif = reader["rsum_tarif"] != DBNull.Value ? Decimal.Parse(reader["rsum_tarif"].ToString()) : 0;
                    serv.Serv.SumNedop = reader["sum_nedop"] != DBNull.Value ? Decimal.Parse(reader["sum_nedop"].ToString()) : 0;
                    serv.Serv.Reval = reader["reval"] != DBNull.Value ? Decimal.Parse(reader["reval"].ToString()) : 0;
                    serv.Serv.RealCharge = reader["real_charge"] != DBNull.Value ? Decimal.Parse(reader["real_charge"].ToString()) : 0;
                    serv.Serv.SumCharge = reader["sum_charge"] != DBNull.Value ? Decimal.Parse(reader["sum_charge"].ToString()) : 0;
                    serv.Serv.SumMoney = reader["sum_money"] != DBNull.Value ? Decimal.Parse(reader["sum_money"].ToString()) : 0;
                    serv.Serv.SumInsaldo = reader["sum_insaldo"] != DBNull.Value ? Decimal.Parse(reader["sum_insaldo"].ToString()) : 0;
                    serv.Serv.SumSn = reader["sum_tarif_sn_f"] != DBNull.Value ? Decimal.Parse(reader["sum_tarif_sn_f"].ToString()) : 0;
                    serv.Serv.SumOutsaldo = reader["sum_outsaldo"] != DBNull.Value ? Decimal.Parse(reader["sum_outsaldo"].ToString()) : 0;
                    serv.Serv.CCalc = reader["c_calc"] != DBNull.Value ? Decimal.Parse(reader["c_calc"].ToString()) : 0;
                    serv.Serv.CReval = reader["c_reval"] != DBNull.Value ? Decimal.Parse(reader["c_reval"].ToString()) : 0;
                    serv.Serv.SumReal = reader["sum_real"] != DBNull.Value ? Decimal.Parse(reader["sum_real"].ToString()) : 0;
                    
                    if (serv.Serv.Tarif <= Convert.ToDecimal(0.0001)) serv.Serv.CCalc = 0;
                    serv.CopyToOdn();
                    AddServ(serv);
                    AddSupp(serv);
                  
                }


            }
            catch (Exception e)
            {
                if (!DBManager.TempTableInWebCashe(_connection, tableCharge))
                {
                    MonitorLog.WriteLog(" Счет-квитанция : Не найдена таблица "+tableCharge+" в базе данных начислений, " +
                                        " возможно выбран некорректный тип квитанции или некорректно закрыт месяц", MonitorLog.typelog.Error, true);
                }
                else
                {
                    MonitorLog.WriteLog("Счет-квитанция : Ошибка при выборка начислений " + e.Message, MonitorLog.typelog.Error, true);
                }
                

            }
            finally
            {
                if (cmd != null)
                {
                    cmd.Dispose();
                }
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
            }

        }

        private void AddSupp(BaseServ2 aServ)
        {
            //Проверка на пустоту добавляемой услуги
            if (aServ.Empty()) return;
            bool findSupp = false;
       
            foreach (var el in ListSupp)
            {
                if (el.Key == aServ.Serv.NzpSupp)
                {
                    el.Value.AddSum(aServ.Serv);
                    el.Value.Serv.NameServ += ", " + aServ.Serv.NameServ;
                    findSupp = true;
                }
            }
            if (!findSupp)
            {
                var newServ = new BaseServ2();
                newServ.AddSum(aServ.Serv);
                newServ.Serv.NameServ = aServ.Serv.NameServ;
                newServ.Serv.NzpSupp = aServ.Serv.NzpSupp;
                //ListSupp.Add(aServ.Serv.Payer.nzp_supp, newServ); //иначе добавляем 
                ListSupp.Add(aServ.Serv.NzpSupp, newServ); //иначе добавляем 
            }

        }
        /// <summary>
        /// Вызов загрузки начислений (charge_XX)
        /// </summary>
        /// <param name="pref">префикс</param>
        /// <param name="nzpKvar">код квартиры</param>
        /// <param name="nzpArea">код УК</param>
        public void PreLoadNach(string pref, int nzpKvar, int nzpArea)
        {
            string tableCharge = pref + "_charge_" + (Year - 2000) + DBManager.tableDelimiter + "charge_" +
                               Month.ToString("00");
            LoadNach(tableCharge, nzpKvar, nzpArea);
        }

        /// <summary>
        /// Вызов загрузки начислений (charge_XX_t)
        /// </summary>
        /// <param name="pref">префикс</param>
        /// <param name="nzpKvar">код квартиры</param>
        /// <param name="nzpArea">код УК</param>
        public void PreLoadNachT(string pref, int nzpKvar, int nzpArea)
        {
            string tableCharge = pref + "_charge_" + (Year - 2000) + DBManager.tableDelimiter + "charge_" +
                               Month.ToString("00") + "_t";
            Avans = true;
            LoadNach(tableCharge, nzpKvar, nzpArea);
        }

        /// <summary>
        /// Загрузка начислений 
        /// </summary>
        /// <param name="tableCharge">таблица начислений</param>
        /// <param name="nzpKvar">код квартиры</param>
        /// <param name="nzpArea">код УК</param>
        /// <returns></returns>
        public void LoadNach(string tableCharge, int nzpKvar, int nzpArea)
        {
            if (_nzpArea != nzpArea)
            {
                _nzpArea = nzpArea;
                LoadUnionServ();
            }

            GetNachWithSupplier(tableCharge, nzpKvar);

            foreach (KeyValuePair<int, BaseServ2> kp in ListServ)
            {
                if (_listPayer.ContainsKey(kp.Value.Serv.NzpSupp))
                {
                    kp.Value.Serv.Payer = _listPayer[kp.Value.Serv.NzpSupp];
                }
            }

            foreach (KeyValuePair<long, BaseServ2> kp in ListSupp)
            {
                if (_listPayer.ContainsKey(kp.Value.Serv.NzpSupp))
                {
                    kp.Value.Serv.Payer = _listPayer[kp.Value.Serv.NzpSupp];
                }
            }
        }

        public string GetServName(int nzpServ)
        {
            return _listService[nzpServ].NameServ;
        }

        public void Clear()
        {
            ListServ.Clear();
            ListSupp.Clear();
            SummaryServ.Clear();
        }
    }
}


