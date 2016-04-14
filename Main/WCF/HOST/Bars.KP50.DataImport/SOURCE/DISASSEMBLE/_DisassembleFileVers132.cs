using System;
using System.Data;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.SOURCE.DISASSEMBLE
{
    public class DbDisassembleFileVers132 : DataBaseHeadServer
    {
        public Returns DisassembleFile132(FilesDisassemble finder, ref Returns ret, IDbConnection conn_db)
        {
            ret = STCLINE.KP50.Global.Utils.InitReturns();
            
            try
            {
                MonitorLog.WriteLog(
                    "Старт разбора файла 'ХАРАКТЕРИСТИКИ ЖИЛОГО ФОНДА И НАЧИСЛЕНИЯ ЖКУ'. ID пользователя:" +
                    finder.nzp_user, MonitorLog.typelog.Info, true);
                
                //ставим статус начала разбора
                string sql =
                    " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "files_imported " +
                    " SET diss_status = 'Заполнение параметров для разбора'" +
                    " WHERE nzp_file = " + finder.nzp_file;
                DBManager.ExecSQL(conn_db, null, sql, true);

                var du = new DbDisUtils(finder);
                //создание индексов
                ret = du.CreateIndexForFileTables(finder);

                //установка переменных
                ret = du.SetDisassemleVars(ref finder);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка установки значений переменным для разбора: " + ret.text, MonitorLog.typelog.Error, true);
                    return ret;
                }

                string dat_s = "01." + finder.month.ToString("00") + "." +
                               finder.year.ToString("0000");

                using (var ins = new InsertAddrrSpaceIntoLocBanks(ServerConnection))
                {
                    //заполнение адресного пространства в нижнем банке
                    ins.Run(finder, ref ret);
                }
                
                //проверяем, все ли дома разобраны
                ret = du.CheckColumnOnEmptiness("nzp_dom", "file_dom", "несопоставленные дома");
                if (!ret.result)
                    return ret;

                using (var dbInsertPayer = new DbInsertPayer())
                {
                    du.SetDissStatus(finder, "Разбор юридических лиц");
                    //разбираем ЮЛ
                    ret = dbInsertPayer.InsertPayer(finder);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка разбираем ЮЛ: " + ret.text, MonitorLog.typelog.Error, true);
                        return ret;
                    }
                    //расчетные счеты ЮЛ
                    using (var dbInsertRS = new DbInsertRS())
                    {
                        dbInsertRS.InsertRS(finder);
                    }
                }

                using (var addParamsDom = new AddParamsDom())
                {
                    du.SetDissStatus(finder, "Разбор домовых параметров");
                    //параметры дома
                    addParamsDom.Run(ServerConnection, finder.nzp_file, Convert.ToDateTime(dat_s), finder,
                        finder.dat_po);
                }

                using (var addKvarByFile = new AddKvarByFile())
                {
                    //разбираем ЛС
                    du.SetDissStatus(finder, "Разбор лс и  параметров");
                    addKvarByFile.Run(ServerConnection, finder.nzp_file, Convert.ToDateTime(dat_s), finder,
                        finder.dat_po);
                }


                ret = du.CheckColumnOnEmptiness("nzp_kvar", "file_kvar", "несопоставленные ЛС");
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка CheckColumnOnEmptiness: " + ret.text, MonitorLog.typelog.Error, true);
                    return ret;
                }


                using (var ad = new DBAddDogovor())
                {
                    du.SetDissStatus(finder, "Разбор договоров");
                    ad.AddDogovor(finder);
                }

                using (var insertAgreement = new DBInsertAgreements())
                {
                    du.SetDissStatus(finder, "Разбор соглашений");
                    //разбираем соглашения
                    ret = insertAgreement.InsertAgreements(finder);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка разбираем соглашения: " + ret.text,MonitorLog.typelog.Error, true);
                        return ret;
                    }
                }

                using (var insertPerekidki = new InsertPerekidki())
                {
                    //разбираем 33ю секцию -  перекидки
                    du.SetDissStatus(finder, "33 секция");
                    ret = insertPerekidki.DisassPerekidka(ServerConnection, finder);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка разбираем 33ю секцию -  перекидки: " + ret.text,
                            MonitorLog.typelog.Error, true);
                      //  return ret;
                    }
                }

                // секция 6 начисления текущего месяца
                using (var insertDateFromFile = new InsertDateFromFile())
                {
                    du.SetDissStatus(finder, "6-я секция");
                    insertDateFromFile.Run(ServerConnection, finder);
                }

                //формирование сальдо предыдущего месяца
                if (finder.prev_month_saldo)
                {
                    du.SetDissStatus(finder, "сальдо предыдущего месяца");
                    var d = new DbDisUtils(finder);

                    ret = d.FormPrevMonthSaldo(ServerConnection, finder);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка при формировании сальдо предыдущего месяца: " + ret.text,
                            MonitorLog.typelog.Error, true);
                   //     return ret;
                    }
                }

                // секция 7 информация о параметрах ЛС в месяце перерасчета
                using (var insertInfParamsLs = new InsertInfParamsLs())
                {
                    du.SetDissStatus(finder, "7-я секция");
                    ret = insertInfParamsLs.DisassInfParamsLs(ServerConnection, finder);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog(
                            "Ошибка секция 7 информация о параметрах ЛС в месяце перерасчета: " + ret.text,
                            MonitorLog.typelog.Error, true);
                  //      return ret;
                    }
                }

                using (var insertRecalcByServ = new InsertRecalcByServ())
                {
                    du.SetDissStatus(finder, "8-я секция");
                    //разбираем 8ую секцию - перерасчеты начислений по услугам
                    ret = insertRecalcByServ.DisassRecalcByServ(ServerConnection, finder);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка разбираем 8ую секцию - перерасчеты начислений по услугам: " + ret.text,
                            MonitorLog.typelog.Error, true);
                 //       return ret;
                    }
                }

                using (var insertIpuFromFile = new InsertIpuFromFile())
                {
                    du.SetDissStatus(finder, "Вставка ИПУ");
                    // Вставка ИПУ (file_ipu, file_ipu_p) - (11-12 секции)
                    insertIpuFromFile.Run(ServerConnection, finder);
                }

                using (var insertOdpuFromFile = new InsertOdpuFromFile())
                {
                    du.SetDissStatus(finder, "Вставка ОДПУ");
                    // Вставка ОДПУ (file_odpu, file_odpu_p) - (9-10 секции)
                    insertOdpuFromFile.Run(ServerConnection, finder);
                }

                using (var insertInfoLsByPu = new InsertInfoLsByPu())
                {
                    du.SetDissStatus(finder, "ГРПУ лс");
                    //разбираем 34ю секцию -  информация о ЛС, принадлежащих ПУ
                    ret = insertInfoLsByPu.DisassInfoLsByPu(ServerConnection, finder);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка разбираем 34ю секцию -  информация о ЛС, принадлежащих ПУ: " + ret.text,
                            MonitorLog.typelog.Error, true);
                //        return ret;
                    }
                }

                using (var uploadGilec = new UploadGilec())
                {
                    du.SetDissStatus(finder, "Вставка Жильцов");
                    // Разбор файла жилец file_gilec (15 секция)
                    uploadGilec.Run(finder, ServerConnection);
                }

                if ( !(finder.versionFull == "1.3.3")&&!(finder.versionFull == "1.3.2"))
                    using (var addOplatServ132 = new AddOplats132())
                    {
                        du.SetDissStatus(finder, "Вставка оплат по услугам 1 ver=" + finder.versionFull);
                        // Вставка оплат по услугам
                        addOplatServ132.Run(ServerConnection, finder);
                    }
                else
                    using (var addOplatServ = new AddOplatServ())
                    {
                        du.SetDissStatus(finder, "Вставка оплат по услугам 2 ver=" + finder.versionFull);
                        // Вставка оплат по услугам
                        addOplatServ.Run(ServerConnection, finder);
                    }

                using (var insertParamsLs = new DBInsertParamLS(ServerConnection))
                {
                    //дополнительные параметры ЛС
                    du.SetDissStatus(finder, "Доп.параметры");
                    ret = insertParamsLs.InsertParamLS(finder, dat_s);
                }

                using (var insertParamsDom = new InsertParamsDom(ServerConnection))
                {
                    //дополнительные параметры ЛС
                    du.SetDissStatus(finder, "Параметры дома");
                    ret = insertParamsDom.InsertParamDom(finder, dat_s);
                }

                //разбор недопоставок
                du.SetDissStatus(finder, "Недопоставки");
                ret = du.InsertNedop(finder);

                //тонкая настройка
                du.SetDissStatus(finder, "Расчетные параметры");
                ret = du.SettingsForGKU(finder);

                if (finder.FrozenCharge) DoFrozenCharge(finder);

                du.LoadFromTarifToL_foss(finder);

                ret = du.SetDissStatus(finder, "Разобран");


            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(
                    "Ошибка выполнения процедуры DisassembleFile132 : " + ex.Message + Environment.NewLine +
                    ex.StackTrace,
                    MonitorLog.typelog.Error, true);
                //изменение статуса загрузки
                using (var du = new DbDisUtils(finder))
                {
                    ret = du.SetDissStatus(finder, "Разобран c ошибками");
                }
                return new Returns(false, "Ошибка разбора файла.", -1);
            }

            ret.text = "Файл успешно разобран.";
            ret.result = true;
            ret.tag = -1;
            return ret;
        }

        /// <summary>Заморозка расчета начислений</summary>
        /// <param name="finder">Вх.параметры</param>
        private void DoFrozenCharge(FilesDisassemble finder)
        {
            string prefData = Points.Pref + DBManager.sDataAliasRest;
            try
            {
                #region логирование

                if (finder.month == 0) MonitorLog.WriteLog("DoFrozenCharge(Заморозка расчета начислений): Не указан месяц ", MonitorLog.typelog.Error, true);
                if (finder.year == 0) MonitorLog.WriteLog("DoFrozenCharge(Заморозка расчета начислений): Не указан год ", MonitorLog.typelog.Error, true);
                if (finder.bank == string.Empty) MonitorLog.WriteLog("DoFrozenCharge(Заморозка расчета начислений): Не указан префикс банка данных ", MonitorLog.typelog.Error, true);
                if (finder.nzp_file == 0) MonitorLog.WriteLog("DoFrozenCharge(Заморозка расчета начислений): Не указан идентификатор файла ", MonitorLog.typelog.Error, true);

                #endregion

                int month = finder.month,
                    year = finder.year;
                string chargeYY = finder.bank + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter +
                                  "charge_" + month.ToString("00");
                if (TempTableInWebCashe(chargeYY))
                {
                    string sql = " INSERT INTO " + prefData + "prohibited_recalc(nzp_kvar, nzp_dom, nzp_supp, nzp_serv, dat_s, dat_po, is_actual) " +
                                 " SELECT c.nzp_kvar, nzp_dom, nzp_supp, nzp_serv, " +
                                        " DATE('1." + month + "." + year + "') AS dat_s, " +
                                        " DATE('" + DateTime.DaysInMonth(year, month) + "." + month + "." + year + "') AS dat_po, " +
                                        " 1 AS is_actual " +
                                 " FROM " + chargeYY + " c INNER JOIN " + prefData + "kvar k ON k.nzp_kvar = c.nzp_kvar " +
                                 " WHERE dat_charge IS NULL " +
                                   " AND order_print IN (" + finder.nzp_file + ") " +
                                   " AND NOT EXISTS(SELECT * FROM " + prefData + "prohibited_recalc l " +
                                                  " WHERE l.nzp_kvar = c.nzp_kvar " +
                                                     " AND l.nzp_supp = c.nzp_supp " +
                                                     " AND l.nzp_serv = c.nzp_serv " +
                                                     " AND l.dat_s = DATE('1." + month + "." + year + "') " +
                                                     " AND l.is_actual = 1 ) ";
                    Returns ret = ExecSQL(sql, false);
                    if (!ret.result) throw new Exception(ret.sql_error);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка заморозки расчета начислений:\n " + ex.Message, MonitorLog.typelog.Error, true);
            }
        }
    }
}
