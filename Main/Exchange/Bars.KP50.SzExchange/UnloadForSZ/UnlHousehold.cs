using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.SzExchange.UnloadForSZ
{
    /// <summary>
    /// Выгрузка домохозяйств
    /// </summary>
    public class UnlHousehold : BaseUnloadClass
    {
        public DateTime time;

        public override int Code
        {
            get
            {
                return 2;
            }
        }

        public override string Name
        {
            get { return "UnlHousehold"; }
        }

        public override string NameText
        {
            get { return "'Домохозяйство'"; }
        }

        public override void Start()
        {

        }

        public DateTime Time
        {
            get
            {
                return time;
            }
            set
            {
                time = value;
            }
        }

        public TimeSpan GetTime(DateTime t_start, DateTime t_finish)
        {
            return (t_finish - t_start);
        }

        public override void Start(FilesImported finder)
        {
            string sql;
            string str;
            string sep = "|";
            DateTime time_finish = DateTime.Now;
            OpenConnection();
            CreateTempTable();

            WriteInFile w = new WriteInFile();
            UnlServ us = new UnlServ();
            UnlHousehold ud = new UnlHousehold();
            
            try
            {
                
                WriteInHousehold(finder, ud); //запись во временную таблицу
                SetProcessProgress(0.5m, finder.nzp_exc);

                #region Проверка для строки "Услуга": есть ли услуги с кодом формулы nzp_frm = 0 в charge_XX.charge_xx
                sql =
                    " SELECT nzp_serv, service_name " +
                    " FROM " + finder.bank + "_kernel.services " +
                    " WHERE nzp_serv IN (" +
                    "  SELECT DISTINCT nzp_serv " +
                    "  FROM " + finder.bank + "_charge_" + finder.year.Substring(2, 2) + ".charge_" + finder.month.PadLeft(2, '0') +
                    "  WHERE nzp_frm = 0)";
                foreach (DataRow rr in ExecSQLToTable(sql).Rows)
                {
                    string message = String.Format("В " + finder.bank + "_charge_" + finder.year.Substring(2, 2) + ".charge_" + finder.month.PadLeft(2, '0') + " " +
                                                   "имеется значение кода формулы nzp_frm = 0 " +
                                                   "для услуги: {0}, код услуги: {1}", rr["service_name"].ToString().Trim(), rr["nzp_serv"]);
                    AddComment(message);
                }
                #endregion

                //w.Filing(GetComment(), finder.saved_name_log);

                //выборка данных из временной таблицы
                sql =
                    " SELECT * FROM " + Name;
                foreach (DataRow rr in ExecSQLToTable(sql).Rows)
                {
                    //формирование строки домохозяйства
                    str = Code + sep + Convert.ToString(rr["number_personal_account"]).Trim() + sep + Convert.ToString(rr["num_ls"]).Trim() + sep + Convert.ToString(rr["fam"]).Trim() + sep +
                             Convert.ToString(rr["ima"]).Trim() + sep + Convert.ToString(rr["otch"]).Trim() + sep + Convert.ToString(rr["date_of_birth"]).Trim() + sep +
                             Convert.ToString(rr["town"]).Trim() + sep + Convert.ToString(rr["rajon"]).Trim() + sep + Convert.ToString(rr["ulica"]).Trim() + sep +
                             Convert.ToString(rr["ndom"]).Trim() + sep + Convert.ToString(rr["nkor"]).Trim() + sep + Convert.ToString(rr["nkvar"]).Trim() + sep +
                             Convert.ToString(rr["nkvar_n"]).Trim() + sep + Convert.ToString(rr["count_gil"]).Trim() + sep + Convert.ToString(rr["count_vrem_prib"]).Trim() + sep +
                             Convert.ToString(rr["count_vrem_ubiv"]).Trim() + sep + Convert.ToString(rr["count_lgot"]).Trim() + sep + Convert.ToString(rr["count_room"]).Trim() + sep +
                             Convert.ToString(rr["total_square"]).Trim() + sep + Convert.ToString(rr["otopl_square"]).Trim() + sep + Convert.ToString(rr["count_cows"]).Trim() + sep +
                             Convert.ToString(rr["count_pigs"]).Trim() + sep + Convert.ToString(rr["count_goats_and_sheeps"]).Trim() + sep + Convert.ToString(rr["count_horse"]).Trim() + sep +
                             Convert.ToString(rr["is_el_kotel"]).Trim() + sep + Convert.ToString(rr["is_el_plita"]).Trim() + sep + Convert.ToString(rr["is_gas_plita"]).Trim() + sep +
                             Convert.ToString(rr["is_gas_kolonka"]).Trim() + sep + Convert.ToString(rr["is_agv"]).Trim() + sep + Convert.ToString(rr["gas_type"]).Trim() + sep +
                             Convert.ToString(rr["water_type"]).Trim() + sep + Convert.ToString(rr["type_gil"]).Trim() + sep + Convert.ToString(rr["is_open_water"]).Trim() + sep +
                             Convert.ToString(rr["is_unuse_tube"]).Trim() + sep + Convert.ToString(rr["is_towl"]).Trim() + sep + Convert.ToString(rr["code_mo"]).Trim() + sep +
                             Convert.ToString(rr["count_string_lgot"]).Trim() + sep + Convert.ToString(rr["count_serv"]).Trim() + sep + Convert.ToString(rr["hishenie_com_serv"]).Trim() + sep +
                             Convert.ToString(rr["stop_payment"]).Trim() + sep;
                    w.Filing(str, finder.saved_name);

                    time_finish = DateTime.Now;
                    string tinfo = " \nВыгрузка Домохозяйства № " + rr["num_ls"] + ", время выполнения: " + GetTime(ud.time, time_finish);
                    w.Filing(tinfo, finder.saved_name_log);

                    us.Start(finder, Convert.ToInt32(rr["num_ls"]), Convert.ToInt32(rr["nzp_kvar"]));
                    
                }
                //Запись в лог файл инф-ии о строках "Домохозяйство" и "Услуга" соответственно
                w.Filing(GetComment() + us.GetComment(), finder.saved_name_log);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("UnlHousehold.Start(pref): Ошибка добавления полей в таблицу.\n: " + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                DropTempTable();
                CloseConnection();
            }

        }

        public override void CreateTempTable()
        {
            string sql;

            sql = " DROP TABLE " + Name;
            ExecSQL(sql, false);

             sql =
                " CREATE TEMP TABLE " + Name + " (" +
                " number_personal_account CHAR (20) , " + 
                " num_ls INTEGER , " + 
                " fam CHAR (50) , " + // должно быть 40 по формату,но пока мы в fam  полностью fio
                " ima CHAR (40) , " + 
                " otch CHAR (40) , " + 
                " date_of_birth DATE , " + 
                " town CHAR (30) , " + 
                " rajon CHAR (30) , " + 
                " ulica CHAR (40) , " + 
                " ndom CHAR (10) , " + 
                " nkor CHAR (3) , " + 
                " nkvar CHAR (10) , " + 
                " nkvar_n CHAR (3) , " + 
                " count_gil INTEGER , " + 
                " count_vrem_prib INTEGER , " + 
                " count_vrem_ubiv INTEGER , " + 
                " count_lgot INTEGER , " + 
                " count_room INTEGER , " + 
                " total_square DECIMAL (12,2), " +
                " otopl_square DECIMAL (12,2), " + 
                " count_cows INTEGER , " + 
                " count_pigs INTEGER , " + 
                " count_goats_and_sheeps INTEGER , " + 
                " count_horse INTEGER , " + 
                " is_el_kotel INTEGER , " + 
                " is_el_plita INTEGER , " + 
                " is_gas_plita INTEGER , " + 
                " is_gas_kolonka INTEGER , " + 
                " is_agv INTEGER , " + 
                " gas_type INTEGER , " + 
                " water_type INTEGER , " + 
                " type_gil INTEGER , " + 
                " is_open_water INTEGER , " + 
                " is_unuse_tube INTEGER , " + 
                " is_towl INTEGER , " + 
                " code_mo DECIMAL(13,0) , " + 
                " count_string_lgot INTEGER , " + 
                " count_serv INTEGER , " + 
                " hishenie_com_serv INTEGER , " + 
                " stop_payment INTEGER, " +
                " nzp_dom INTEGER, " +
                " nzp_kvar INTEGER )";
            ExecSQL(sql);

            sql =
                " CREATE INDEX " + Name + "_nzp_kvar_idx ON " + Name + " (nzp_kvar)";
            ExecSQL(sql);
            sql =
                " CREATE INDEX " + Name + "_nzp_dom_idx ON " + Name + " (nzp_dom)";
            ExecSQL(sql);
            
        }

        /// <summary>
        /// Запись во временную таблицу
        /// </summary>
        /// <param name="pref"></param>
        public void WriteInHousehold(FilesImported finder, UnlHousehold d)
        {
            //int nzp_group = -1;
            string month, year, pref;
            month = finder.month.PadLeft(2, '0');
            year = finder.year.Substring(2, 2);
            pref = finder.bank;
            WriteInFile w = new WriteInFile();
            DateTime dats = new DateTime(Convert.ToInt32(finder.year), Convert.ToInt32(finder.month), 1); //1й день месяца выгрузки
            DateTime datpo = new DateTime(Convert.ToInt32(finder.year), Convert.ToInt32(finder.month),
                DateTime.DaysInMonth(Convert.ToInt32(finder.year), Convert.ToInt32(finder.month)));      //последний день месяца выгрузки

            d.Time = DateTime.Now;

            //запись данных для домохозяйства
            string sql = " INSERT INTO " + Name +
                         " (num_ls, nzp_kvar, nzp_dom, fam, town, rajon, ulica, ndom, nkor, nkvar, nkvar_n) " +
                         " SELECT " +
                         " k.num_ls, " +
                         " k.nzp_kvar, " +
                         " k.nzp_dom, " +
                         " k.fio, " +
                         " town.town, " +
                         " raj.rajon, " +
                         " coalesce(ul.ulica, '')||' '||coalesce(ul.ulicareg, '') as ulica, " +

                         " d.ndom, " +
                         " d.nkor, " +
                         " k.nkvar, " +
                         " k.nkvar_n " +

                         " FROM " + pref + DBManager.sDataAliasRest + " kvar k " + 
                         " LEFT OUTER JOIN " + pref + DBManager.sDataAliasRest + " dom d ON k.nzp_dom = d.nzp_dom " +
                         " LEFT OUTER JOIN " + pref + DBManager.sDataAliasRest + " s_ulica ul ON ul.nzp_ul = d.nzp_ul " +
                         " LEFT OUTER JOIN " + pref + DBManager.sDataAliasRest +
                         " s_rajon raj ON ul.nzp_raj = raj.nzp_raj " +
                         " LEFT OUTER JOIN " + pref + DBManager.sDataAliasRest +
                         " s_town town ON raj.nzp_town = town.nzp_town " +
                         " WHERE k.nzp_kvar IS NOT NULL " +
                         " AND d.nzp_dom IS NOT NULL";//AND k.num_ls in(250289, 250445) для проверки   AND k.num_ls in(502759, 503022, 503423) проверка для трех лс
            ExecSQL(sql);

            sql =
                " ANALYZE " + Name;
            ExecSQL(sql);

            //Наличие эл. котла
            sql = " UPDATE " + Name + " SET is_el_kotel = " +
                  " (SELECT CAST(val_prm as INTEGER) FROM " + pref + DBManager.sDataAliasRest + " prm_1 "+
                  " WHERE is_actual = 1 AND dat_s <= '" + datpo + " ' AND dat_po >= ' " + dats + " ' " +
                  " AND nzp_prm = 449 AND nzp = " + Name + ".nzp_kvar)";
            ExecSQL(sql);
            //CheckColumnOnEmptiness("is_el_kotel", "null", "null-ы в строке " + NameText + ", в поле - 'Наличие электрического котла'", true, "0");

            //Наличие эл. плиты
            sql = " UPDATE " + Name + " SET is_el_plita = " +
                  " (SELECT CAST(val_prm as INTEGER) FROM " + pref + DBManager.sDataAliasRest + " prm_1 " + 
                  " WHERE is_actual = 1 AND dat_s <= '" + datpo + " ' AND dat_po >= ' " + dats + " ' " +
                  " AND nzp_prm = 19 AND nzp = " + Name + ".nzp_kvar)";
            ExecSQL(sql);
            //CheckColumnOnEmptiness("is_el_plita", "null", "null-ы в строке " + NameText + ", в поле - 'Наличие электрической плиты'", true, "0");

            //Наличие газовой плиты
            sql = " UPDATE " + Name + " SET is_gas_plita = " +
                  " (SELECT CAST(val_prm as INTEGER) FROM " + pref + DBManager.sDataAliasRest + " prm_1 " + 
                  " WHERE is_actual = 1 AND dat_s <= '" + datpo + " ' AND dat_po >= ' " + dats + " ' " +
                  " AND nzp_prm = 551 AND nzp = " + Name + ".nzp_kvar)";
            ExecSQL(sql);
           // CheckColumnOnEmptiness("is_gas_plita", "null", "null-ы в строке " + NameText + ",в поле - 'Наличие газовой плиты'", true, "0");

            //Наличие газовой колонки
            sql = " UPDATE " + Name + " SET is_gas_kolonka = " +
                  " (SELECT CAST(val_prm as INTEGER) FROM " + pref + DBManager.sDataAliasRest + " prm_1 " + 
                  " WHERE is_actual = 1 AND dat_s <= '" + datpo + " ' AND dat_po >= ' " + dats + " ' " +
                  " AND nzp_prm = 1 AND nzp = " + Name + ".nzp_kvar)";
            ExecSQL(sql);
            //CheckColumnOnEmptiness("is_gas_kolonka", "null", "null-ы в строке " + NameText + ",в поле - 'Наличие газовой колонки'", true, "0");

            //Наличие АГВ
            sql = " UPDATE " + Name + " SET is_agv = " +
                  " (SELECT CAST(val_prm as INTEGER) FROM " + pref + DBManager.sDataAliasRest + " prm_1 " + 
                  " WHERE is_actual = 1 AND dat_s <= '" + datpo + " ' AND dat_po >= ' " + dats + " ' " +
                  " AND nzp_prm = 552 AND nzp = " + Name + ".nzp_kvar)";
            ExecSQL(sql);
            //CheckColumnOnEmptiness("is_agv", "null", "null-ы в строке " + NameText + ",в поле - 'Наличие газовой АГВ'", true, "0");

            //Количество строк по услугам (Строка – услуга)
            sql = " UPDATE " + Name + " SET count_serv = " +
                  " (SELECT COUNT(DISTINCT nzp_serv) " +
                  " FROM " + pref + "_charge_" + year + ".charge_" + month + " c, " +
                  Name + " n " +
                  " WHERE c.num_ls = n.num_ls " +
                  " AND nzp_serv > 1)";
            ExecSQL(sql);
            CheckColumnOnEmptiness("count_serv", "null", "null-ы в строке " + NameText + ",в поле - 'Кол-во строк по услугам'", true, "0");
            /*sql = " UPDATE " + Name + " SET count_serv = " +
                  " (SELECT nzp FROM " + pref + DBManager.sDataAliasRest + " prm_1 " + //nzp_prm, val_prm
                  " WHERE is_actual = 1 AND dat_s <= '" + datpo + " ' AND dat_po >= ' " + dats + " ' " +
                  " AND nzp_prm = 1199 AND nzp = " + Name + ".nzp_kvar)";// у нас нет nzp_prm = 1199
            ExecSQL(sql);*/

            //Факт хищения коммунальных услуг
            sql = " UPDATE " + Name + " SET hishenie_com_serv = " +
                  " (SELECT CAST(val_prm as INTEGER) FROM " + pref + DBManager.sDataAliasRest + " prm_1 " + 
                  " WHERE is_actual = 1 AND dat_s <= '" + datpo + " ' AND dat_po >= ' " + dats + " ' " +
                  " AND nzp_prm = 1199 AND nzp = " + Name + ".nzp_kvar)";
            ExecSQL(sql);
            //CheckColumnOnEmptiness("hishenie_com_serv", "null", "null-ы в строке " + NameText + ",в поле - 'Факт хищения коммунальных услуг'", true, "0");

            //Код типа жилья по водоснабжению
            sql = " UPDATE " + Name + " SET water_type = " +
                  " (SELECT MAX (cast(val_prm as INTEGER))  as water_type" +
                  " FROM " + pref + DBManager.sDataAliasRest + " prm_1 " + 
                  " WHERE is_actual = 1 AND dat_s <= '" + datpo + " ' AND dat_po >= ' " + dats + " ' " +
                  " AND nzp_prm = 7 AND nzp = " + Name + ".nzp_kvar)";
             ExecSQL(sql);
             
            //Количество коров
            sql = " UPDATE " + Name + " SET count_cows = " +
                  " (SELECT cast(val_prm as INTEGER) as count_cows " +
                  " FROM " + pref + DBManager.sDataAliasRest + " prm_1 " + 
                  " WHERE is_actual = 1 AND dat_s <= '" + datpo + " ' AND dat_po >= ' " + dats + " ' " +
                  " AND nzp_prm = 322 AND nzp = " + Name + ".nzp_kvar)";
            ExecSQL(sql);
            //CheckColumnOnEmptiness("count_cows", "null", "null-ы в строке " + NameText + ",в поле - 'Количество коров'", true, "0");

            //Количество свиней
            sql = " UPDATE " + Name + " SET count_pigs = " +
                  " (SELECT cast(val_prm as INTEGER) as count_pigs " +
                  " FROM " + pref + DBManager.sDataAliasRest + " prm_1 " + 
                  " WHERE is_actual = 1 AND dat_s <= '" + datpo + " ' AND dat_po >= ' " + dats + " ' " +
                  " AND nzp_prm = 323 AND nzp = " + Name + ".nzp_kvar)";
            ExecSQL(sql);
            //CheckColumnOnEmptiness("count_pigs", "null", "null-ы в строке " + NameText + ",в поле - 'Количество свиней'", true, "0");

            //Количество коз и овец
            sql = " UPDATE " + Name + " SET count_goats_and_sheeps = " +
                  " (SELECT cast(val_prm as INTEGER) as count_goats_and_sheeps " +
                  " FROM " + pref + DBManager.sDataAliasRest + " prm_1 " +
                  " WHERE is_actual = 1 AND dat_s <= '" + datpo + " ' AND dat_po >= ' " + dats + " ' " +
                  " AND nzp_prm = 324 AND nzp = " + Name + ".nzp_kvar)";
            ExecSQL(sql);
            //CheckColumnOnEmptiness("count_goats_and_sheeps", "null", "null-ы в строке " + NameText + ",в поле - 'Количество коз и овец'", true, "0");

            //Количество лошадей
            sql = " UPDATE " + Name + " SET count_horse = " +
                  " (SELECT cast(val_prm as INTEGER) as count_horse " +
                  " FROM " + pref + DBManager.sDataAliasRest + " prm_1 " + 
                  " WHERE is_actual = 1 AND dat_s <= '" + datpo + " ' AND dat_po >= ' " + dats + " ' " +
                  " AND nzp_prm = 325 AND nzp = " + Name + ".nzp_kvar)";
            ExecSQL(sql);
            //CheckColumnOnEmptiness("count_horse", "null", "null-ы в строке " + NameText + ",в поле - 'Количество лошадей'", true, "0");

            //Наличие открытого водоразбора
            sql = " UPDATE " + Name + " SET is_open_water = " +
                  " (SELECT CAST(val_prm AS INTEGER) FROM " + pref + DBManager.sDataAliasRest + " prm_2 " + 
                  " WHERE nzp_prm = 35 AND is_actual <> 100 AND dat_s <= '" + datpo + " ' AND dat_po >= ' " + dats +
                  " ' " +
                  " AND nzp = " + Name + ".nzp_dom) ";
            ExecSQL(sql);
            CheckColumnOnEmptiness("is_open_water", "null", "null-ы в строке " + NameText + ",в поле - 'Наличие открытого водозабора'", true, "0");

            //Наличие полотенцесушителя
            sql = " UPDATE " + Name + " SET is_towl = " +
                  " (SELECT cast(Replace(val_prm,',','.')as INTEGER) as is_towl FROM " +
                  pref + DBManager.sDataAliasRest + " prm_1 " +
                  " WHERE is_actual = 1 AND dat_s <= '" + datpo + " ' AND dat_po >= ' " + dats + " ' " +
                  " AND nzp_prm = 59 AND nzp = " + Name + ".nzp_kvar)";
            ExecSQL(sql);
            CheckColumnOnEmptiness("is_towl", "null", "null-ы в строке " + NameText + ",в поле - 'Наличие полотенцесушителя'", true, "0");

            //Наличие неизолированного трудопровода
            sql = " UPDATE " + Name + " SET is_unuse_tube = " +
                  " (SELECT cast(Replace(val_prm,',','.')as INTEGER) as is_unuse_tube FROM " +
                  pref + DBManager.sDataAliasRest + " prm_1 " +
                  " WHERE is_actual = 1 AND dat_s <= '" + datpo + " ' AND dat_po >= ' " + dats + " ' " +
                  " AND nzp_prm = 327 AND nzp = " + Name + ".nzp_kvar)";
            ExecSQL(sql);
            CheckColumnOnEmptiness("is_unuse_tube", "null", "null-ы в строке " + NameText + ",в поле - 'Наличие неизолированного трубопровода'", true, "0");

            //Общая площадь
            sql = " UPDATE " + Name + " SET total_square = " +
                  " (SELECT MAX (cast(Replace(val_prm,',','.')as DECIMAL (12,5))) as total_square FROM " +
                  pref + DBManager.sDataAliasRest + " prm_1 " +
                  " WHERE is_actual = 1 AND dat_s <= '" + datpo + " ' AND dat_po >= ' " + dats + " ' " +
                  " AND nzp_prm = 4 AND nzp = " + Name + ".nzp_kvar)";
            ExecSQL(sql);
            //CheckColumnOnEmptiness("total_square", "null", "null-ы в строке " + NameText + ",в поле - 'Общая площадь'", true, "0");

            //Отапливаемая площадь
            sql = " UPDATE " + Name + " SET otopl_square = " +
                  " (SELECT MAX (cast(Replace(val_prm,',','.')as DECIMAL (12,5))) as otopl_square FROM " +
                  pref + DBManager.sDataAliasRest + " prm_1 " +
                  " WHERE is_actual = 1 AND dat_s <= '" + datpo + " ' AND dat_po >= ' " + dats + " ' " +
                  " AND nzp_prm = 133 AND nzp = " + Name + ".nzp_kvar)";
            ExecSQL(sql);
           // CheckColumnOnEmptiness("otopl_square", "null", "null-ы в строке " + NameText + ",в поле - 'Отапливаемая площадь'", true, "0");

            //Количество комнат
            sql = " UPDATE " + Name + " SET count_room = " +
                  " (SELECT MAX(CAST(val_prm AS INTEGER)) as count_room FROM " +
                  pref + DBManager.sDataAliasRest + " prm_1 " +
                  " WHERE is_actual = 1 AND dat_s <= '" + datpo + " ' AND dat_po >= ' " + dats + " ' " +
                  " AND nzp_prm = 107 AND nzp = " + Name + ".nzp_kvar)";
            ExecSQL(sql);
            CheckColumnOnEmptiness("count_room", "null", "null-ы в строке " + NameText + ",в поле - 'Количество комнат'", true, "1");

            //Количество жильцов
            sql =
                " UPDATE " + Name + " SET  count_gil = " +
                " ( " +
                " SELECT MAX(CAST(val_prm AS INTEGER)) FROM " + pref + DBManager.sDataAliasRest + "prm_1 p " +
                " WHERE p.nzp_prm = 5 " +
                "   AND p.is_actual = 1 " +
                "   AND p.nzp = " + Name + ".nzp_kvar " +
                "   AND dat_s < CAST( '" + datpo + "' as DATE) " +
                "   AND dat_po > CAST('" + dats + "' as DATE) " +
                " )" +
                " WHERE EXISTS (SELECT 1 FROM " + pref + DBManager.sDataAliasRest + "prm_1 p " +
                " WHERE p.nzp_prm = 5 " +
                " AND p.is_actual <> 100 AND p.nzp = " + Name +
                ".nzp_kvar " +
                " AND dat_s < CAST( '" + datpo + "' as DATE) " +
                " AND dat_po > CAST('" + dats + "' as DATE) " +
                " )";
            ExecSQL(sql);
            CheckColumnOnEmptiness("count_gil", "null", "null-ы в строке " + NameText + ",в поле - 'Количество жильцов'", true, "0");

            //Количество врем. прибывших жильцов
            sql = " UPDATE " + Name + " SET count_vrem_prib = " +
                  " (SELECT MAX(cast(cast(trim(val_prm) as NUMERIC)as INTEGER)) as count_vrem_prib FROM " + pref + DBManager.sDataAliasRest +
                  " prm_1 " +
                  " WHERE is_actual = 1 AND dat_s <= '" + datpo + " ' AND dat_po >= ' " + dats + " ' " +
                  " AND nzp_prm = 131 AND nzp = " + Name + ".nzp_kvar)";
            ExecSQL(sql);
            CheckColumnOnEmptiness("count_vrem_prib", "null", "null-ы в строке " + NameText + ",в поле - 'Количество временно прибывших жильцов'", true, "0");

            //Количество  врем. убывших жильцов

            DateTime dt1 = Convert.ToDateTime(datpo); // подумать как лучше написать
            string date = "01.06.2006";
            DateTime dt = Convert.ToDateTime(date);
            if (dt1 > dt)
            {
                sql = " UPDATE " + Name + " SET count_vrem_ubiv = " +
                      " (SELECT count(distinct nzp_gilec) as count_vrem_ubiv FROM " + pref +
                      DBManager.sDataAliasRest + 
                      " gil_periods g " +
                      " WHERE is_actual <>100 AND dat_s <= '" + datpo + " ' AND dat_po >= ' " + dats + " ' " +
                      " AND dat_po > dat_s + '4 day'::interval AND dat_po > cast('" + date + "'"+
                      " as DATE) + '4 day'::interval " +
                      " AND g.nzp_kvar = " + Name + ".nzp_kvar)";
                ExecSQL(sql);
            }
            else
            {
                sql = " UPDATE " + Name + " SET count_vrem_ubiv = " +
                      " (SELECT count(distinct nzp_gilec) as count_vrem_ubiv FROM " + pref +
                      DBManager.sDataAliasRest + //ntul01_data
                      " gil_periods g " +
                      " WHERE is_actual <>100 AND dat_s <= '" + datpo + " ' AND dat_po >= ' " + dats + " ' " +
                      " AND dat_po > dat_s + '14 day'::interval " +
                      " AND g.nzp_kvar = " + Name + ".nzp_kvar)";
                ExecSQL(sql);
            }
            CheckColumnOnEmptiness("count_vrem_ubiv", "null", "null-ы в строке " + NameText + ",в поле - 'Количество временно убывших жильцов'", true, "0");

            //№ персонифицированного счета
            sql = " UPDATE " + Name + " SET number_personal_account = " +
                  " (SELECT TRIM(val_prm) as number_personal_account FROM " + pref + DBManager.sDataAliasRest +
                  " prm_15 " +
                  " WHERE is_actual = 1 AND dat_s <= '" + datpo + " ' AND dat_po >= ' " + dats + " ' " +
                  " AND nzp_prm = 162 AND nzp = " + Name + ".nzp_kvar)";
            ExecSQL(sql);
            //CheckColumnOnEmptiness("number_personal_account", "null", "null-ы в строке " + NameText + ",в поле - '№ персонифицированного счета'", true, "0");
            
            //Колличество строк по льготникам
            sql = " UPDATE " + Name + " SET count_string_lgot = 0 ";  
            ExecSQL(sql);
            CheckColumnOnEmptiness("count_string_lgot", "null", "null-ы в строке 'Домохозяйство',в поле - 'Количество строк по льготникам'", true, "0");

            //Необходимость приостановки выплаты льгот за ЖКУ
            sql = " UPDATE " + Name + " SET stop_payment = 0";
            ExecSQL(sql);
            CheckColumnOnEmptiness("stop_payment", "null", "null-ы в строке " + NameText + ",в поле - 'Необходимость приостановки выплаты льгот за ЖКУ'", true, "0");

            //Код МО
            sql = " UPDATE " + Name + " SET code_mo = " +
                  " (SELECT cast(nzp_vill as DECIMAL(13,0)) FROM " + pref + DBManager.sDataAliasRest +
                  "kvar a, " + pref + DBManager.sDataAliasRest + "rajon_vill b, " +
                  pref + DBManager.sDataAliasRest + "dom c, " +
                  pref + DBManager.sDataAliasRest + "s_ulica s " +
                  " WHERE b.nzp_raj = s.nzp_raj " +
                  " AND c.nzp_dom = a.nzp_dom " +
                  " AND c.nzp_ul = s.nzp_ul " +
                  " AND a.num_ls = " + Name + ".num_ls) ";
            ExecSQL(sql);
            //CheckColumnOnEmptiness("code_mo", "null", "null-ы в строке " + NameText + ",в поле - 'Код МО'", true, "0");

            //Дата рождения квартиросъемщика
            //sql =
            //    " UPDATE " + Name + " SET date_of_birth = 0 ";
            //ExecSQL(sql);
            //CheckColumnOnEmptiness("date_of_birth","null","null-ы в строке 'Домохозяйство', в поле - 'Дата рождения квартиросъемщика'",true,"0");

            //Код типа жилья по газоснабжению !!!НЕ ЗНАЕМ!!!
            //sql =
            //    " UPDATE " + Name + " SET gas_type = 0 ";
            //ExecSQL(sql);
           
            
            //Код типа жилья
            sql = " UPDATE " + Name + " SET type_gil = 0";
            ExecSQL(sql);
            CheckColumnOnEmptiness("type_gil", "null", "null-ы в строке " + NameText + ",в поле - 'Код типа жилья'", true, "0");

            //Количество льготников с сохранением норматива
            sql = " UPDATE " + Name + " SET count_lgot = 0";
            ExecSQL(sql);
            CheckColumnOnEmptiness("count_lgot", "null", "null-ы в строке " + NameText + ",в поле - 'Количество льготников с сохранением норматива'", true, "0");
        }


        /// <summary>
        /// Запись во временную таблицу
        /// </summary>
        public void WriteInHousehold(FilesImported finder, IDbConnection conn_db, string dat_po)
        {

            #region Useless now
            // #region Выбираем наименование улицы, района, города
            // string sql = "DROP TABLE t_raj";

            // DBManager.ExecSQL(conn_db, sql, false);

            // sql = " SELECT ulica, ulicareg, rajon, town, nzp_ul, r.nzp_raj, t.nzp_town " +
            //       " INTO TEMP t_raj " +
            //       " FROM " + pref + DBManager.sDataAliasRest
            //       + "s_ulica s, " + Points.Pref + DBManager.sDataAliasRest + "s_town t, " +
            //       Points.Pref + DBManager.sDataAliasRest + "s_rajon r " +
            //       " WHERE s.nzp_raj = r.nzp_raj " +
            //       " AND r.nzp_town = t.nzp_town ";
            // DBManager.ExecSQL(conn_db, sql, true);
            // #endregion Выбираем наименование улицы, района, города

            // # region Загружаем справочник с территориями
            // sql = " DROP TABLE t_area";
            // DBManager.ExecSQL(conn_db, null, sql, false);

            // sql = " CREATE TEMP TABLE t_area (" +
            //            " nzp_area INTEGER, " +
            //            " area CHAR(40), " +
            //            " typehos INTEGER " +
            //            ")";
            // DBManager.ExecSQL(conn_db, null, sql, true);

            // sql = " DELETE FROM t_area";
            // DBManager.ExecSQL(conn_db, null, sql, true);

            // sql =
            //     " SELECT nzp_area, area, 1 as typehos " +
            //     " FROM " + pref + DBManager.sDataAliasRest + "s_area order by 2";
            // DataTable dt = ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData();
            // foreach (DataRow rr in dt.Rows)
            // {
            //     sql = " INSERT INTO t_area(nzp_area, area, typehos) " +
            //           " VALUES(" + rr["nzp_area"].ToString() + ", " + rr["area"].ToString() + ", " + rr["typehos"] +
            //           " )";
            //     DBManager.ExecSQL(conn_db, null, sql, true);
            // }
            // #endregion Загружаем справочник с территориями

            // # region Выбираем домохозяйства
            // sql =

            //     " INSERT INTO " + Name + 
            //     //" (fam, number_domhos, kvar, room, dom, korpus, ulica, , selo/village, town/raj, nzp_kvar, d.nzp_dom,   ) " +
            //     " SELECT fio, num_ls, nkvar, nkvar_n, ndom, nkor, ulica, ulicareg, rajon, town, " +
            //     " nzp_kvar , d.nzp_dom, s.nzp_ul, s.nzp_raj, s.nzp_town, area, typehos, pkod " +
            //     " FROM " + Points.Pref + DBManager.sDataAliasRest + "kvar k, " + 
            //     Points.Pref +DBManager.sDataAliasRest +"dom d, " +
            //     " t_raj s, " +
            //     " t_area sa " +
            //     " WHERE k.nzp_dom = d.nzp_dom " +
            //     " AND d.nzp_ul = s.nzp_ul " +
            //     " AND num_ls > 0 " +
            //     " AND k.nzp_area = sa.nzp_area " +
            //     " AND k.typek = 1 ";
            // if (nzp_group > 0)
            // {
            //     sql = sql + " AND k.nzp_kvar IN ( " +
            //           " SELECT nzp as nzp_kvar FROM " + pref + DBManager.sDataAliasRest + "link_group " +
            //           " WHERE nzp_group=" + nzp_group + ")";
            //     DBManager.ExecSQL(conn_db, sql, true);
            // }
            // DBManager.ExecSQL(conn_db, sql, true);

            // MonitorLog.WriteLog("Окончание загрузки адресов", MonitorLog.typelog.Info, true);

            // # endregion Выбираем домохозяйства

            // # region Загрузка параметров
            // MonitorLog.WriteLog("Старт загрузки параметров", MonitorLog.typelog.Info, true);

            // sql = "DROP TABLE t_prm";
            // DBManager.ExecSQL(conn_db, null, sql, false);

            // sql = " CREATE TEMP TABLE t_prm" +
            //       "(nzp_prm INTEGER)";
            // DBManager.ExecSQL(conn_db, null, sql, true);

            //#region Вставка параметров

            // int[] vals = new int[] { 1, 4, 7, 19, 59, 107, 131, 133, 322, 323, 324, 325, 327, 449, 551, 552, 1199 };
            // foreach (int val in vals)
            // {
            //     sql = " INSERT INTO t_prm VALUES(" + val + ")";
            //     DBManager.ExecSQL(conn_db, null, sql, true);
            // }

            // #endregion Вставка параметров

            // sql = " CREATE INDEX ix5346 ON t_prm(nzp_prm)";
            // DBManager.ExecSQL(conn_db, null, sql, true);

            // sql = " ANALYZE t_prm";
            // DBManager.ExecSQL(conn_db, null, sql, true);

            // sql = "DROP TABLE t_prm_1";
            // DBManager.ExecSQL(conn_db, null, sql, false);

            // if (nzp_group > 0)
            // {
            //     s = " AND nzp IN(" +
            //         "  SELECT nzp FROM " + pref + DBManager.sDataAliasRest + "link_group " +
            //         "  WHERE nzp_group = cast(nzp_group as CHARACTER))";
            // }
            // else
            //     s = "";


            // sql = " SELECT nzp, nzp_prm, val_prm " +
            //       " INTO TEMP t_prm_1" +
            //       " FROM " + pref + DBManager.sDataAliasRest + "prm_1 " +  
            //       " WHERE is_actual = 1" +
            //       " AND dat_s <= " + " cast(' " + dat_po + " ' as date) " + s;
            // DBManager.ExecSQL(conn_db, null, sql, true);

            // sql = " CREATE INDEX ix_tmp_8752 ON t_prm_1" +
            //       "(nzp_prm, nzp)";
            // DBManager.ExecSQL(conn_db, null, sql, true);

            // sql = " ANALYZE t_prm_1"; 
            // DBManager.ExecSQL(conn_db, null, sql, true);

            // MonitorLog.WriteLog("Окончание загрузки параметров", MonitorLog.typelog.Info, true);

            // # endregion Оканчание загрузки параметров
            #endregion

        }


    }
}
