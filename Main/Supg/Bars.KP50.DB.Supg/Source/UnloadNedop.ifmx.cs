using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace STCLINE.KP50.DataBase
{
    public partial class Supg : DataBaseHead
    { 
        /// <summary>
        /// Формирование недопоставок
        /// </summary>
        /// <param name="nzp_user">пользователь</param>
        /// <param name="ret"></param>
        /// <returns>bool</returns>
        public bool NedopForming(Journal finder, out Returns ret)
        {
            ret = new Returns(true);

            using (ClassUnloadNedop unloadNedop = new ClassUnloadNedop())
            {
                return unloadNedop.NedopForming(finder, out ret);
            }
        }
    }
    
    
    class ClassUnloadNedop : DataBaseHead
    {
        private string _d_begin = "";
        private string _d_end = "";
        private string _reg_begin = "";
        private string _reg_end = "";
        private int _isActual = 0;

        private int _jrn_number = 0;
        private DateTime _calcMonth;
        
        private int nzp_User = 0;

        private int createNedopCount = 0; //количество созданных недопоставок
        private int cancelNedopCount = 0; //количество отмененных недопоставок

        private IDbConnection _connDb = null;

        /// <summary>
        /// Формирование недопоставок
        /// </summary>
        /// <param name="nzp_user">пользователь</param>
        /// <param name="ret"></param>
        /// <returns>bool</returns>
        public bool NedopForming(Journal finder, out Returns ret)
        {
            ret = new Returns(true);

            if (!CheckInputData(finder, out ret)) return false;

            _calcMonth = new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1);

            bool formzk = ((finder.is_actual == 0) || (finder.is_actual == 1));
            bool formpr = ((finder.is_actual == 0) || (finder.is_actual == 2));
            _isActual = finder.is_actual;

            // подговить даты
            PrepareDates(finder);

            try
            {
                _connDb = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(_connDb, true);
                if (!ret.result) return false;

                nzp_User = finder.nzp_user;

                /*using (DbWorkUser db = new DbWorkUser())
                {
                    nzp_User = db.GetSupgUser(_connDb, null, new Finder() { nzp_user = finder.nzp_user }, out ret);
                }
                if (!ret.result) throw new Exception("Ошибка определения локального пользователя :" + ret.text);*/

                // Назначить номер операции
                _jrn_number = GetJournalNumber();
                if (_jrn_number <= 0) throw new Exception("Не определен номер операции");

                // Удалить созданные недопоставки, которые ссылаются на несуществующий номер операции
                DeleteNedop();
                
                if (formzk)
                {
                    // Создание недопоставок по нарядам-заказам
                    CreateJobTicketNedop();
                }

                if (formpr)
                {
                    // Создание недопоставок по актам о недопоставке (плановые работы)
                    CreateActNedop();
                }

                // Количество созданных недопоставок
                createNedopCount = GetNedopCnt();
                if (createNedopCount == 0)
                {
                    ret.text = "Не обнаружено данных для формирования недопоставок";
                    return false;
                }
                
                // Запись в журнале
                SaveJournalNedop();

                return true;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                ret = new Returns(false, "Ошибка при формировании недопоставок");
                return false;
            }
            finally
            {
                // Закрытие соединения
                if (_connDb != null) _connDb.Close();
            }
        }

        /// <summary>
        /// Проверка входных данных
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        private bool CheckInputData(Journal finder, out Returns ret)
        {
            ret = new Returns(true);

            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Подготовка дат
        /// </summary>
        /// <param name="finder"></param>
        private void PrepareDates(Journal finder)
        {
            DateTime tmp_date;

#if PG
            if (DateTime.TryParse(finder.d_begin, out tmp_date))
            {
                _d_begin = Utils.EStrNull(tmp_date.ToString()) + "::timestamp";
            }

            if (DateTime.TryParse(finder.d_end, out tmp_date))
            {
                _d_end = Utils.EStrNull(tmp_date.ToString()) + "::timestamp";
            }

            if (DateTime.TryParse(finder.doc_begin, out tmp_date))
            {
                _reg_begin = Utils.EStrNull(tmp_date.ToString()) + "::timestamp";
            }

            if (DateTime.TryParse(finder.doc_end, out tmp_date))
            {
                _reg_end = Utils.EStrNull(tmp_date.ToString()) + "::timestamp";
            }
#else
            _d_begin = Utils.FormatDateMDY(finder.d_begin);
            _d_end = Utils.FormatDateMDY(finder.d_end);

            _reg_begin = Utils.FormatDateMDY(finder.doc_begin);
            _reg_end = Utils.FormatDateMDY(finder.doc_end);
#endif

        }

        /// <summary>
        /// Назначить номер операции
        /// </summary>
        /// <returns></returns>
        private int GetJournalNumber()
        {
            string seq = Points.Pref + sSupgAliasRest + "nedop_kvar_nzp_jrn_seq";
            string sql = "";
            
#if PG
            sql = " SELECT nextval('" + seq + "') ";
#else
            sql = " SELECT " + seq + ".nextval FROM  " + Points.Pref + sSupgAliasRest + "dual";
#endif
            Returns ret = new Returns(true);
            object jrnNumberObj = ExecScalar(_connDb, null, sql, out ret, true);
            if (!ret.result) throw new Exception("Ошибка назначения номера операции: " + ret.text);

            return Convert.ToInt32(jrnNumberObj);
        }

        /// <summary>
        /// Удалить созданные недопоставки, которые ссылаются на несуществующий номер операции
        /// </summary>
        private void DeleteNedop()
        {
            string sql = " DELETE FROM " + Points.Pref + sSupgAliasRest + "nedop_kvar" +
                  " WHERE nzp_jrn NOT IN" +
                  "      (SELECT no FROM " + Points.Pref + sSupgAliasRest + "jrn_upg_nedop)";
            Returns ret = ExecSQL(_connDb, sql, true);
            if (!ret.result) throw new Exception("Ошибка при предварительном удалении недопоставок: " + ret.text);
        }

        /// <summary>
        ///  Создание недопоставок по нарядам-заказам
        /// </summary>
        private void CreateJobTicketNedop()
        {
            string where_act = "";
            if (_d_end != "") where_act += " AND zk.act_s < " + _d_end;
            if (_d_begin != "") where_act += " AND zk.act_po > " + _d_begin;

            string sql = " INSERT INTO " + Points.Pref + sSupgAliasRest + " nedop_kvar" +
                " (nzp_kvar, act_no, nzp_serv, nzp_kind, " + 
                "   tn, " + 
                "   dat_s, " + 
                "   dat_po, " + 
                "   is_actual, " + 
                "   dat_when, " + 
                "   month_calc, " + 
                "   nzp_wp, " + 
                "   comment, " + 
                "   nzp_jrn) " +
                " SELECT z.nzp_kvar, zk.nzp_zk, d.nzp_serv, zk.act_num_nedop, " + 
                "   case when " + DBManager.sNvlWord + "(zk.act_temperature,0) = 0 then null else zk.act_temperature end, " +
                // дата начала наряда-заказа
                (_d_begin == "" ? "zk.act_s" : " case when zk.act_s < " + _d_begin + " then " + _d_begin + " else zk.act_s end") + "," +
                // дата окончания
                " case when zk.act_po < " + _d_end + " then zk.act_po else " + _d_end + " end, " +
                " 12, " + 
                DBManager.sCurDateTime + "," +
                DBManager.MDY(_calcMonth.Month, _calcMonth.Day, _calcMonth.Year) + "," + 
                "k.nzp_wp," +
                Utils.EStrNull("УПГ наряд-заказ №") + "||zk.nzp_zk, " + 
                _jrn_number + 
                " FROM " + Points.Pref + sSupgAliasRest + "zvk z," +
                    Points.Pref + sSupgAliasRest + "zakaz zk, " +
                    Points.Pref + sSupgAliasRest + "s_dest d, " +
                    Points.Pref + sDataAliasRest + "kvar k " +
                " WHERE z.nzp_zvk = zk.nzp_zvk " +
                    " AND z.nzp_kvar = k.nzp_kvar " +
                    " AND zk.nzp_dest = d.nzp_dest " +
                    " AND " + 
                    " (" +
                        " (zk.act_actual = 1 and zk.act_s is not null " + where_act + " ) " +
                        " OR " +
                        " (zk.ds_actual = 1) " + // акт не сформирован, но стоит признак "Формировать недопоставку"
                        " ) " + 
                    " AND k.is_open <> '2' " +
                    " AND k.nzp_wp <> '99' " +
                (_reg_begin != "" ? " AND zk.nedop_reg >= " + _reg_begin : "") +
                (_reg_end != "" ? " AND zk.nedop_reg < " + _reg_end : "");

            Returns ret = ExecSQL(_connDb, sql, true);
            if (!ret.result) throw new Exception("Ошибка при формировании недопоставок по нарядам-заказам: " + ret.text);
        }

        /// <summary>
        /// Создание недопоставок по актам о недопоставке (плановые работы)
        /// </summary>
        private void CreateActNedop()
        {
            // сформировать запрос на вставку и условия на данные
            string insertlist = " INSERT INTO " + Points.Pref + sSupgAliasRest + "nedop_kvar" +
                " (nzp_kvar, act_no, nzp_serv, nzp_kind, tn, dat_s, dat_po, is_actual," +
                " dat_when, month_calc, nzp_wp, comment, nzp_jrn) " +
                " SELECT distinct " +
                " k.nzp_kvar, a.nzp_act, a.nzp_serv, a.nzp_kind, case when " + sNvlWord + "(a.tn,0)=0 then null else a.tn end, " +
                // 
                (_d_begin == "" ? "a.dat_s" : "case when a.dat_s < " + _d_begin + " then " + _d_begin + " else a.dat_s end") + "," +
                // 
                " case when a.dat_po < " + _d_end + " then a.dat_po else " + _d_end + " end, " +
                " 14, " + sCurDateTime + ", " + DBManager.MDY(_calcMonth.Month, _calcMonth.Day, _calcMonth.Year) + "," +
                " k.nzp_wp, \'УПГ акт № \'||a.number, " + _jrn_number;

            string whereperiod = (_d_end != "" ? " and a.dat_s< " + _d_end : "") +
                (_d_begin != "" ? " and a.dat_po > " + _d_begin : "") +
                (_reg_begin != "" ? " and a._date >= " + _reg_begin : "") +
                (_reg_end != "" ? " and a._date < " + _reg_end : "");
            
            // Выбрать все недопоставки по актам о недопоставках
            // пустые квартиры  
            string sql = insertlist +
                " From " + Points.Pref + sSupgAliasRest + "act a," +
                Points.Pref + sSupgAliasRest + "act_obj ao, " +
                Points.Pref + sDataAliasRest + "kvar k " +
                " where a.is_actual= '2' " + 
                "   and a.nzp_act=ao.nzp_act " + 
                "   and ao.nzp_dom=k.nzp_dom " + 
                "   and k.is_open= '1' " + 
                "   and k.nzp_wp<> '99' " +
                "   and " + DBManager.sNvlWord + "(ao.nzp_kvar,0) = 0 " +
                whereperiod;
            
            Returns ret = ExecSQL(_connDb, sql, true);
            if (!ret.result) throw new Exception("Ошибка при предварительном создании недопоставок по актам о недопоставке : " + ret.text);

            // пустые дома
            sql = insertlist +
                " From " + Points.Pref + sSupgAliasRest + "act a," +
                Points.Pref + sSupgAliasRest + "act_obj ao, " +
                Points.Pref + sDataAliasRest + "kvar k, " +
                Points.Pref + sDataAliasRest + "dom d " +
                " WHERE a.is_actual= '2' " + 
                "   and a.nzp_act = ao.nzp_act " + 
                "   and ao.nzp_ul = d.nzp_ul " + 
                "   and ao.nzp_dom = 0 " + 
                "   and d.nzp_dom=k.nzp_dom " + 
                "   and k.is_open= '1' " + 
                "   and k.nzp_wp<> '99' " +
                whereperiod;

            ret = ExecSQL(_connDb, sql, true);
            if (!ret.result) throw new Exception("Ошибка при предварительном создании недопоставок по актам о недопоставке : " + ret.text);

            // непустые квартиры
            sql = insertlist +
                " from " + Points.Pref + sSupgAliasRest + "act a," +
                Points.Pref + sSupgAliasRest + "act_obj ao, " +
                Points.Pref + sDataAliasRest + "kvar k " +
                " where a.is_actual = '2' " + 
                "   and a.nzp_act = ao.nzp_act " + 
                "   and ao.nzp_kvar = k.nzp_kvar " +
                "   and k.is_open= '1' " + 
                "   and k.nzp_wp<> '99' " +
                whereperiod;

            ret = ExecSQL(_connDb, sql, true);
            if (!ret.result) throw new Exception("Ошибка при предварительном создании недопоставок по актам о недопоставке : " + ret.text);
        }
        
        /// <summary>
        /// Получить количество недопоставок
        /// </summary>
        /// <returns></returns>
        private int GetNedopCnt()
        {
            Returns ret = new Returns(true);
            
            string sql = "select count(*) cnt from " + Points.Pref + sSupgAliasRest + "nedop_kvar where nzp_jrn = " + _jrn_number;

            object objCnt = ExecScalar(_connDb, sql, out ret, true);
            if (!ret.result) throw new Exception("Ошибка определения количества созданных недопоставок: " + ret.text);

            return Convert.ToInt32(objCnt);
        }

        /// <summary>
        /// Запись в журнал
        /// </summary>
        private void SaveJournalNedop()
        {
            string sql = " insert into " + Points.Pref + sSupgAliasRest + "jrn_upg_nedop " +
                " (no, d_begin, d_end, reg_begin, reg_end, " + 
                " d_when, nzp_user, crt_count, cnc_count, " + 
                " is_actual, status) " +
                " values" +
                " (" + _jrn_number.ToString() + ", " +
                StrNull(_d_begin) + "," + StrNull(_d_end) + "," + StrNull(_reg_begin) + "," + StrNull(_reg_end) + "," +
                DBManager.sCurDateTime + ", " + nzp_User + "," + createNedopCount + "," + cancelNedopCount + "," +
                _isActual + ", 0)";

            Returns ret = ExecSQL(_connDb, sql, true);
            if (!ret.result) throw new Exception("Ошибка создания записи в журнале: " + ret.text);
        }

        /// <summary>
        /// Запись в журнале
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private string StrNull(string str)
        {
            if (str == "") return "null";
            else return str;
        }
    }
}
