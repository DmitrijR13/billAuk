using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using STCLINE.KP50.DataBase;
using System.Collections;
using System.Data;
using FastReport;
using System.Globalization;
using SevenZip;

namespace STCLINE.KP50.DataBase
{
    public partial class Debitor : DataBaseHead
    {
        //--Соглашения
        //список статусов соглашения
        public List<Deal> GetArgStatus(Deal finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            if (finder.nzp_user < 0) //прооверка на пользователя
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                ret.result = false;
                return null;
            }

            List<Deal> list = new List<Deal>();
            IDbConnection con_db = null;
            IDataReader reader = null;
            string zapros;
            try
            {
                #region Открываем соединение с базами

                con_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(con_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("FindArgStatus : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }
                #endregion

                zapros = "SELECT nzp_agr_statuses, name_agr_statuses FROM " + Points.Pref + "_debt.s_agr_statuses";
                if (!ExecRead(con_db, out reader, zapros, true).result)
                {
                    MonitorLog.WriteLog("GetArgStatus: Ошибка создания - " + zapros, MonitorLog.typelog.Error, 20, 201, true);
                    ret.text = "Ошибка извлечения данных";
                    ret.result = false;
                    return null;
                }
                if (reader != null)
                {
                    while (reader.Read())
                    {
                        Deal debitor = new Deal();
                        if (reader["nzp_agr_statuses"] != DBNull.Value) debitor.nzp_deal_status = Convert.ToInt32(reader["nzp_agr_statuses"]);
                        if (reader["name_agr_statuses"] != DBNull.Value) debitor.comment = reader["name_agr_statuses"].ToString();
                        list.Add(debitor);
                    }
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetArgStatus : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                if (con_db != null)
                {
                    con_db.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }
            }

            return list;
        }

        //список оплаты по соглашению
        public List<AgreementDetails> GetArgDetail(Agreement finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            if (finder.nzp_user < 0) //прооверка на пользователя
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                ret.result = false;
                return null;
            }
            if (finder.nzp_agr == 0) return null;
            List<AgreementDetails> list = new List<AgreementDetails>(); ;
            IDbConnection con_db = null;
            IDataReader reader = null;
            AgreementDetails debitor;
            string zapros;
            string skip = "";
            string rows = "";

            if (finder.skip != 0)
            {
                skip = " OFFSET " + finder.skip;
            }
            if (finder.rows != 0)
            {
                rows = " LIMIT " + finder.rows;
            }

            try
            {
                #region --Открываем соединение с базами

                con_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(con_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("GetArgDetail : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }
                #endregion

                zapros = " SELECT det.datemonth,det.imcoming_balance,det.debt_money,det.outgoing_balance " +
                         " FROM " + Points.Pref + "_debt.agreement agr, " + Points.Pref + "_debt.agreement_details det " +
                         " WHERE det.nzp_agr=agr.nzp_agr AND det.nzp_agr= " + finder.nzp_agr +
                         " ORDER BY nzp_agr_det " + skip + " " + rows + " ";
                if (!ExecRead(con_db, out reader, zapros, true).result)
                {
                    MonitorLog.WriteLog("GetArgDetail: Ошибка извлечения " + zapros, MonitorLog.typelog.Error, 20, 201, true);
                    ret.text = "Ошибка извлечения данных";
                    ret.result = false;
                    return null;
                }
                if (reader != null)
                {
                    int i = 0;
                    while (reader.Read())
                    {
                        debitor = new AgreementDetails();
                        debitor.nzp_agr_det = ++i;
                        if (reader["datemonth"] != DBNull.Value) debitor.dat_month = Convert.ToDateTime(reader["datemonth"]);
                        if (reader["imcoming_balance"] != DBNull.Value) debitor.sum_insaldo = Convert.ToDecimal(reader["imcoming_balance"]);
                        if (reader["debt_money"] != DBNull.Value) debitor.sum_money = Convert.ToDecimal(reader["debt_money"].ToString());
                        if (reader["outgoing_balance"] != DBNull.Value) debitor.sum_outsaldo = Convert.ToDecimal(reader["outgoing_balance"]);
                        list.Add(debitor);
                    }
                }

                zapros = "SELECT count(*) " +
                     " FROM " + Points.Pref + "_debt.agreement agr, " + Points.Pref + "_debt.agreement_details det " +
                         " WHERE det.nzp_agr=agr.nzp_agr AND det.nzp_agr= " + finder.nzp_agr;
                        

                object count = ExecScalar(con_db, zapros, out ret, true);
                if (ret.result && count != null && count != DBNull.Value)
                {
                    ret.tag = Convert.ToInt32(count);
                }


            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetArgDetail : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                if (con_db != null)
                {
                    con_db.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }
            }

            return list;
        }


        //todo Переделать AgreementDetails, Agreement на нормальные классы
        /// <summary>
        /// Сохарнить соглашение
        /// </summary>
        /// <param name="finder">список расчета. В каждом элементе информация о соглашении</param>
        /// <param name="ret">результат</param>
        public void SaveAgreement(List<AgreementDetails> finder, out Returns ret)
        {

            #region Проверка данных
            ret = Utils.InitReturns();

            if (finder.Count == 0)
            {
                ret = new Returns(false, "Отсутствуют данные для сохранения", -1);
                ret.result = false;
                return;
            }

            if (finder.First().nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                ret.result = false;
                return;
            }

          
            #endregion

            IDbConnection con_db = null;
            StringBuilder sql = new StringBuilder();

            try
            {
                #region Открываем соединение

                con_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(con_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("GetArgDetail : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка при открытии соединения с БД ";
                    ret.tag = 0;
                    return;
                }
                #endregion

                #region Проверка на задолжность
                AgreementDetails agr = finder.First();
                DateTime maxDate;
                MyDataReader reader;
                sql.Remove(0, sql.Length);
                sql.Append(" SELECT max(agrdel.datemonth) as maxdate ");
                sql.Append(" FROM " + Points.Pref + "_debt.agreement as agr, " + Points.Pref + "_debt.agreement_details as agrdel ");
                sql.Append(" WHERE agr.nzp_agr = agrdel.nzp_agr ");
                sql.Append(" AND agr.nzp_agr_status = " + s_agr_statuses.Сelebrates.GetHashCode());
                sql.Append(" AND nzp_deal =" + agr.nzp_deal);
                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("SaveGetArg: Ошибка сохранения: " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.text = "Ошибка сохранения данных";
                    ret.result = false;
                    ret.tag = 0;
                    return;
                }
                if (reader.Read())
                {
                    if (DateTime.TryParse(reader["maxdate"].ToString(), out maxDate))
                    {
                        if (!(maxDate.Month < agr.agr_dat.Month && maxDate.Year <= agr.agr_dat.Year))
                        {
                            ret.text = "Не возможно выполнить операцию. Имеется не погашенная расрочка.";
                            ret.result = false;
                            ret.tag = 1;
                            return;
                        }
                    }
                }
                #endregion

                #region Сохранение в agreement

                sql.Remove(0,sql.Length);
                sql.Append(" INSERT into " + Points.Pref + "_debt.agreement (number,agr_date,agr_money,agr_month_count,nzp_deal,nzp_agr_status ) ");
                sql.Append(" VALUES('" + agr.number + "','" + agr.agr_dat + "'," + agr.agr_money + "," + agr.agr_month_count + "," + agr.nzp_deal + "," + agr.nzp_agr_status + ") RETURNING nzp_agr");
                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("SaveGetArg: Ошибка сохранения: " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.text = "Ошибка сохранения данных";
                    ret.result = false;
                    ret.tag = 0;
                    return;
                }
                if (reader.Read())
                {
                    ret.tag = Convert.ToInt32(reader["nzp_agr"].ToString());
                }
                else
                {
                    ret.result = false;
                    ret.text = " Ошибка получения первичного ключа";
                    ret.tag = 0;
                    return;
                }
                reader.Close();
                #endregion

                #region Сохранение в agreement_details

                foreach (AgreementDetails agrDet in finder)
                {

                    sql.Remove(0,sql.Length);
                    sql.Append(" INSERT into " + Points.Pref +
                               "_debt.agreement_details (nzp_agr,datemonth,imcoming_balance,outgoing_balance,debt_money) ");
                    sql.Append(" VALUES(" + ret.tag + ",'" + agrDet.dat_month + "'," + agrDet.sum_insaldo + "," +
                               agrDet.sum_outsaldo + "," + agrDet.sum_money + ") ");
                    if (!ExecSQL(con_db, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("SaveGetArg: Ошибка сохранения: " + sql.ToString(), MonitorLog.typelog.Error,
                            20, 201, true);
                        ret.text = "Ошибка сохранения данных";
                        ret.tag = 0;
                        ret.result = false;
                        return;
                    }
                }

                #endregion

                //смена статуса у дела
                this.MakeOperOnDeal(new deal_states_history() { nzp_deal = agr.nzp_deal, nzp_oper = EnumOpers.CreateAgreement.GetHashCode() }, con_db, null, out ret);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры MakeOperOnDeal в SaveAgreement", MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка обновления статуса";
                    ret.tag = 0;
                    ret.result = false;
                    return;
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры SaveAgreement : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = "Ошибка сохранения данных";
                ret.tag = 0;
                ret.result = false;
                return;
            }
            finally
            {
                if (con_db != null)
                {
                    con_db.Close();
                }
            }
        }

        //todo Переделать AgreementDetails, Agreement на нормальные классы
        /// <summary>
        /// Сохарнить соглашение
        /// </summary>
        /// <param name="finder">список расчета. В каждом элементе информация о соглашении</param>
        /// <param name="ret">результат</param>
        public void DeleteAgreement(List<AgreementDetails> finder, out Returns ret)
        {

            #region Проверка данных
            ret = Utils.InitReturns();

            if (finder.Count == 0)
            {
                ret = new Returns(false, "Отсутствуют данные для сохранения", -1);
                ret.result = false;
                return;
            }

            if (finder.First().nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                ret.result = false;
                return;
            }

            if (finder.First().nzp_agr == 0)
            {
                ret = new Returns(false, "Соглашение не выбрано", -1);
                ret.result = false;
                return;
            }
            #endregion

            IDbConnection con_db = null;
            StringBuilder sql = new StringBuilder();

            try
            {
                #region Открываем соединение

                con_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(con_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("DeleteAgreement : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка удаления данных. Нет соединения с базой данных";
                    ret.result = false;
                    return;
                }
                #endregion
                sql.Remove(0,sql.Length);
                sql.Append("DELETE FROM " + Points.Pref + "_debt.agreement_details WHERE nzp_agr=" + finder.First().nzp_agr);
                if (!ExecSQL(con_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("DeleteAgreement: Ошибка удаления: " + sql.ToString(), MonitorLog.typelog.Error,
                        20, 201, true);
                    ret.text = "Ошибка удаления данных ";
                    ret.result = false;
                    return;
                }

                #region Удаление в agreement
                AgreementDetails agr = finder.First();
                sql.Remove(0, sql.Length);
                sql.Append("DELETE FROM " + Points.Pref + "_debt.agreement WHERE nzp_agr=" + finder.First().nzp_agr);
                //todo delete
                if (!ExecSQL(con_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("DeleteAgreement: Ошибка удаления: " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.text = "Ошибка удаления данных";
                    ret.result = false;
                    return;
                }
                #endregion


            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры DeleteAgreement : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = "Ошибка удаления данных";
                ret.result = false;
                return;
            }
            finally
            {
                if (con_db != null)
                {
                    con_db.Close();
                }
            }
        }

        //--Меню Настройки
        //получить список параметров УК
        public List<SettingsRequisites> GetSettingArea(SettingsRequisites finder,out Returns ret)
        {
            ret = Utils.InitReturns();

            if (finder.nzp_user < 0) //прооверка на пользователя
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                ret.result = false;
                return null;
            }

            List<SettingsRequisites> list = new List<SettingsRequisites>();
            IDbConnection con_db = null;
            IDataReader reader = null;
            string zapros;
            try
            {
                #region Открываем соединение с базами

                con_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(con_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("GetSettingArea : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }
                #endregion

                zapros = " SELECT set.nzp_supp, ar.nzp_area, ar.area, fio_dir, fio_svz, town, street, dom, kvnum, phone " +
                         " FROM " + Points.Pref + "_data.s_area ar, " + Points.Pref + "_debt.settings_requisites set " +
                         " WHERE ar.nzp_area=set.nzp_area ";
                if (!ExecRead(con_db, out reader, zapros, true).result)
                {
                    MonitorLog.WriteLog("GetArgStatus: Ошибка создания - " + zapros, MonitorLog.typelog.Error, 20, 201, true);
                    ret.text = "Ошибка извлечения данных";
                    ret.result = false;
                    return null;
                }
                if (reader != null)
                {
                    while (reader.Read())
                    {
                        SettingsRequisites areaSet = new SettingsRequisites();
                        if (reader["nzp_supp"] != DBNull.Value) areaSet.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                        if (reader["nzp_area"] != DBNull.Value) areaSet.nzp_area = Convert.ToInt32(reader["nzp_area"]);
                        if (reader["area"] != DBNull.Value) areaSet.area = reader["area"].ToString();
                        if (reader["fio_dir"] != DBNull.Value) areaSet.fio_dir = reader["fio_dir"].ToString();
                        if (reader["fio_svz"] != DBNull.Value) areaSet.fio_svz = reader["fio_svz"].ToString();
                        if (reader["town"] != DBNull.Value) areaSet.town = reader["town"].ToString();
                        if (reader["street"] != DBNull.Value) areaSet.street = reader["street"].ToString();
                        if (reader["dom"] != DBNull.Value) areaSet.dom = reader["dom"].ToString();
                        if (reader["kvnum"] != DBNull.Value) areaSet.kvnum = reader["kvnum"].ToString();
                        if (reader["phone"] != DBNull.Value) areaSet.phone = reader["phone"].ToString();
                        list.Add(areaSet);
                    }
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetSettingArea : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                if (con_db != null)
                {
                    con_db.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }
            }

            return list;
        }

        //сохранить список параметров УК
        public void SaveSetting(List<SettingsRequisites> finder, out Returns ret)
        {
            #region Проверка данных
            ret = Utils.InitReturns();

            if (finder.Count == 0)
            {
                ret = new Returns(false, "Отсутствуют данные для сохранения", -1);
                ret.result = false;
                return ;
            }

            if (finder.First().nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                ret.result = false;
                return ;
            }


            #endregion

            IDbConnection con_db = null;
            StringBuilder sql = new StringBuilder();
            try
            {
                #region Открываем соединение

                con_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(con_db, true);
                IDbTransaction transaction = con_db.BeginTransaction();
                if (!ret.result)
                {
                    MonitorLog.WriteLog("SaveSetting : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return ;
                }
                #endregion


                #region Сохранение в agreement
                SettingsRequisites sett = finder.First();

                sql.Remove(0, sql.Length);
                sql.Append(" SELECT count(*)  FROM " + Points.Pref + "_debt.settings_requisites WHERE nzp_area =" + sett.nzp_area);
                byte coun = 0;
                object count = ExecScalar(con_db,transaction, sql.ToString(), out ret, true);
                if (ret.result && count != null && count != DBNull.Value)
                {
                    coun = Convert.ToByte(count);
                }
                if (coun == 0)
                {

                    sql.Remove(0, sql.Length);
                    sql.Append(" INSERT INTO " + Points.Pref + "_debt.settings_requisites (nzp_area, nzp_supp, fio_dir, fio_svz, town, street, dom, kvnum, phone)");
                    sql.Append(" VALUES(" + sett.nzp_area + ", '" +  sett.nzp_supp + "','" + sett.fio_dir + "','" + sett.fio_svz + "','" + sett.town + "','" + sett.street + "','" + sett.dom + "','" + sett.kvnum + "','" + sett.phone + "')");
                    if (!ExecSQL(con_db, transaction, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("SaveSetting: Ошибка сохранения: " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        ret.text = "Ошибка сохранения данных";
                        ret.result = false;
                        return;
                    }
                }
                else
                {
                    sql.Remove(0, sql.Length);
                    sql.Append(" UPDATE " + Points.Pref + "_debt.settings_requisites ");
                    sql.Append(" SET (nzp_supp, fio_dir, fio_svz, town, street, dom, kvnum, phone)= ");
                    sql.Append(" ('" + sett.nzp_supp +"', '" + sett.fio_dir + "','" + sett.fio_svz + "','" + sett.town + "','" + sett.street + "','" + sett.dom + "','" + sett.kvnum + "','" + sett.phone + "') ");
                    sql.Append(" WHERE nzp_area =" + sett.nzp_area);
                    if (!ExecSQL(con_db, transaction, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("SaveSetting: Ошибка сохранения: " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        ret.text = "Ошибка сохранения данных";
                        ret.result = false;
                        return;
                    }
                }
                transaction.Commit();
                #endregion

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры SaveSetting : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = "Ошибка сохранения данных";
                ret.result = false;
                return ;
            }
            finally
            {
                if (con_db != null)
                {
                    con_db.Close();
                }
            }

        }

    }
}
