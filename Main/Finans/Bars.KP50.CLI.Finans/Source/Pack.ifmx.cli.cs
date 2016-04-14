using System;
using System.Data;
using System.Globalization;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;


namespace STCLINE.KP50.DataBase
{
    public class DbPackClient : DataBaseHead
    {
        public TypePayCode TypePayCode { get; set; }
        public Returns UploadDBFPacktoCache(Finder finder, DataTable dt)
        {
            if (dt == null) return new Returns(false, "Таблица не задана");
            if (finder.nzp_user < 1) return new Returns(false, "Пользователь не задан");

            IDbConnection connWeb = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(connWeb, true);
            if (!ret.result) return ret;
            try
            {

                var db = new DBUtils();
                ret = db.SaveDataTable(connWeb, finder, "dbfpack_ls", dt);
                db.Close();
            }
            finally
            {
                connWeb.Close();
            }

            return ret;
        }



        public int SaveOneLs(IDbConnection connWeb, Pack_ls packLS, out Returns ret)
        {

            ret = Utils.InitReturns();
            if (packLS == null) return 0;

            #region Сохранение пачки

            string payCodeColumn = "";
            string payCodeValue = "";
            switch (TypePayCode)
            {
                case TypePayCode.Standart:
                    payCodeColumn = "paycode";
                    payCodeValue = packLS.pkod;
                    break;
                    case TypePayCode.Specific:
                    payCodeColumn = "user_ls";
                    payCodeValue = "'" + packLS.pkod + "'";
                    break;
            }
            // вставка данных об оплатах конкретного Платежного кода
            // В зависимости от типа платежного кода, вставляем его либо в колонку paycode, либо в колонку user_ls
            // ЛС пока не обновляем
            int nzpPackLs;
            string sql = " insert into " + sDefaultSchema + "source_pack_ls(nzp_spack, " + payCodeColumn + ", num_ls, g_sum_ls, sum_ls, geton_ls, " +
                         " sum_peni , dat_month,  kod_sum, nzp_supp, paysource, id_bill, dat_vvod, anketa, " +
                         " info_num, unl, erc_code)" +
                         " values (" +
                         packLS.nzp_pack + "," +
                         payCodeValue + "," +
                         " 0, " +
                         packLS.g_sum_ls + "," +
                         packLS.sum_ls + "," +
                         " 0, " +
                         " 0, " +
                         "'" + packLS.dat_month + "'," +
                         "" + packLS.kod_sum + "," +
                         " 0," +
                         "" + packLS.paysource + "," +
                         "" + packLS.id_bill + "," +
                         "'" + packLS.dat_vvod + "',''," +
                         "" + packLS.info_num + "," +
                         "-1," +
                         "'" + packLS.erc_code + "'" + ")";
            if (!ExecSQL(connWeb, sql, true).result)
            {
                ret.text = "Ошибка при сохранения пачки, см. логи";
                return 0;
            }
            ret.tag = DBManager._affectedRowsCount;
            nzpPackLs = DBManager.GetSerialValue(connWeb);

            #endregion

            #region Соранение изменений жильца

            foreach (GilSum t in packLS.gilSums)
            {
                if (packLS.version_pack == "!1.02")
                    sql = " insert into " + sDefaultSchema +
                          "source_gil_sums(nzp_spack_ls, nzp_supp, sum_oplat, nzp_serv)" +
                          " values (" + nzpPackLs + "," + t.nzp_supp + "," +
                          (String.IsNullOrEmpty(t.sum_oplat) ? "null" : "'" + t.sum_oplat + "'") + "," +
                          t.nzp_serv + ")";
                else
                {
                    sql = " insert into " + sDefaultSchema +
                          "source_gil_sums(nzp_spack_ls, days_nedo, sum_oplat, ordering)" +
                          " values (" + nzpPackLs + "," +
                          (String.IsNullOrEmpty(t.day_nedo) ? "null" : "'" + t.day_nedo + "'") + "," +
                          (String.IsNullOrEmpty(t.sum_oplat) ? "null" : "'" + t.sum_oplat + "'") + "," +
                          t.ordering + ")";
                }
                if (!ExecSQL(connWeb, sql, true).result)
                {
                    ret.text = "Ошибка сохранения изменений жильца, см. логи";
                    return 0;
                }
            }

            #endregion



            #region Соранение показаний счетчиков
            // Для каждго ПУ
            foreach (PuVals t in packLS.puVals)
            {
                sql = " insert into " + sDefaultSchema + "source_pu_vals(nzp_spack_ls, ordering, " +
                      " nzp_serv, pu_order, num_cnt, val_cnt)" +
                      " values (" + nzpPackLs + ",'" +
                      t.ordering + "','" +
                      t.nzp_serv + "','" +
                      t.ordering + "','" +
                      t.num_cnt + "','" +
                      t.val_cnt.ToString("0.0000") +"')";
                if (!ExecSQL(connWeb, sql, true).result)
                {
                    ret.result = false;
                    return 0;
                }

            }
            #endregion

            return nzpPackLs;
        }

        public int SaveOnePack(IDbConnection connWeb, int nzpUser, int parPack,ref Pack pack, string fileName,  out Returns ret, AddedPacksInfo packsInfo)
        {
            ret = Utils.InitReturns();
            if (pack == null) return 0;
            int insertedRowsCount=0;
            #region Сохранение пачки
            string sql = " insert into "+sDefaultSchema+"source_pack(nzp_user, nzp_session, place_of_made, erc_code," +
                         " num_pack, date_pack,  date_oper, count_in_pack, sum_pack,  " +
                         " sum_nach, sum_geton, version, fileName)" +
                         " values (" + nzpUser + ", 0 ," +
                         "" + Utils.EStrNull(pack.bank) + "," +
                         "'" + pack.erc_code + "'," +
                         "'" + pack.num_pack + "'," +
                         "'" + pack.dat_pack + "'," +
                         "'" + pack.dat_calc + "'," +
                         "" + pack.count_kv + "," +
                         "" + pack.sum_pack + "," +
                         "" + pack.sum_nach + "," +
                         "" + pack.nzp_supp + "," +
                         "'" + pack.version_pack + "','" + fileName + "')";

            if (!ExecSQL(connWeb, sql, true).result)
            {
                ret.result = false;
                ret.text = "Ошибка сохранения заголовка пачки";
                return 0;
            }
            pack.nzp_pack = DBManager.GetSerialValue(connWeb);
              // простановка родительской пачки 
            sql = "update "+sDefaultSchema+"source_pack set par_pack = " + parPack +
                  " where nzp_spack=" + pack.nzp_pack;
            if (!ExecSQL(connWeb, sql, true).result)
            {
                ret.result = false;
                ret.text=" Ошибка сохранения первичного ключа из таблицы source_pack при вставке записи";
                return 0;
            }
            #endregion
            
            #region Соранение ЛС

            if (pack.listPackLs.Count > 0)
            {
                packsInfo.InitOplatyProgress(pack.listPackLs.Count);
            }
            // для каждого ЛС пачки
            foreach (Pack_ls t in pack.listPackLs)
            {
                t.nzp_pack = pack.nzp_pack;
                t.version_pack = pack.version_pack;
                // Сохраняем данные ЛС
                SaveOneLs(connWeb, t, out ret);
                insertedRowsCount += ret.tag;
                packsInfo.OnSetProgress();
                if (!ret.result)
                {
                    return insertedRowsCount;
                }
            }
            #endregion
            return insertedRowsCount;
        }

        /// <summary>
        /// Сохраняет пачку в универсальном формате в базе данных
        /// </summary>
        /// <param name="superPack">Суперпачка или пачка, если пачка в одном экземпляре</param>
        /// <returns>ret.tag - nzp_pack из source_pack</returns>
        public Returns SaveUniversalFormatToWeb(ref Pack superPack, ref AddedPacksInfo packsInfo)
        {
            IDbConnection connWeb = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(connWeb, true);
            if (!ret.result) return ret;
            //int rowsInsertCount=0;
            try
            {
                ret.result = false;
                if (superPack.listPack != null)
                {
                    //Случай суперпачки, вставка заголовка суперпачки
                    if (superPack.file_name.Length > 250) superPack.file_name = superPack.file_name.Substring(0, 250);
                    string sql = " insert into "+sDefaultSchema+"source_pack(nzp_user, nzp_session, place_of_made, erc_code," +
                                 " num_pack, date_pack, date_oper, count_in_pack, sum_pack,  " +
                                 " sum_nach, sum_geton, version, fileName)" +
                                 " values (" + superPack.nzp_user + ",0," +
                                 "" + Utils.EStrNull(superPack.bank) + "," +
                                 "'" + superPack.erc_code + "'," +
                                 "'" + superPack.num_pack + "'," +
                                 "'" + superPack.dat_pack + "'," +
                                 "'" + superPack.dat_calc + "'," +
                                 "" + superPack.count_kv + "," +
                                 "" + superPack.sum_pack + "," +
                                 "" + superPack.sum_nach + "," +
                                 "" + superPack.nzp_supp + "," +
                                 "'" + superPack.version_pack + "','" +
                                 superPack.file_name + "')";


                    if (!ExecSQL(connWeb, sql, true).result)
                    {
                        ret.text = "Ошибка при сохранении пачки, см. логи";
                        return ret;
                    }
                    // заголовок суперпачки вставился с номером
                    superPack.nzp_pack = DBManager.GetSerialValue(connWeb);
                    // у суперпачки номер родительской пачки равен номеру суперпачки
                    sql = "update " + sDefaultSchema + "source_pack set par_pack = " + superPack.nzp_pack +
                          " where nzp_spack=" + superPack.nzp_pack;
                    if (!ExecSQL(connWeb, sql, true).result)
                    {
                        ret.text = "Ошибка при сохранении подпачки, см. логи";
                        return ret;
                    }
                    if (superPack.listPack.Count>0)
                    {
                        packsInfo.InitPackProgress(superPack.listPack.Count);
                    }
                    // для каждой пачки суперпачки
                    foreach (Pack t in superPack.listPack)
                    {
                       Pack pack = t;
                        t.version_pack = superPack.version_pack;
                        // сохраняем данные пачки
                       packsInfo.InsertedCountRows+=SaveOnePack(connWeb, superPack.nzp_user, superPack.nzp_pack, ref pack,
                       superPack.file_name, out ret, packsInfo);
                        if (!ret.result)
                        {
                            return ret;
                        }
                    }
                }
                else
                {
                    packsInfo.InitPackProgress(1);
                    packsInfo.InsertedCountRows += SaveOnePack(connWeb, superPack.nzp_user, 0, ref superPack,
                    superPack.file_name,  out ret, packsInfo);
                    if (!ret.result)
                    {
                        return ret;
                    } 
                }
            }
            finally
            {
                connWeb.Close();
            }
            return ret;
        }
    } //end class

} //end namespace