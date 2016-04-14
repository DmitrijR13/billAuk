using STCLINE.KP50.Global;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;
using STCLINE.KP50.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;


namespace STCLINE.KP50.DataBase
{
    
    /// <summary>
    /// Распределение одного ЛС
    /// </summary>
    public class DistribOneLsFonTask : BaseFonTask
    {
        public override Returns StartTask()
        {
            Returns ret;
            using (var db = new DbCalcPack())
            {
                db.CalcDistribPackLs(_task, out ret);
            }
            return ret;
        }

        public DistribOneLsFonTask(CalcFonTask task)
            : base(task)
        {
            
        }

    }



    /// <summary>
    /// Распределение одного пачки
    /// </summary>
    public class DistribPack : BaseFonTask
    {
        

        public override Returns StartTask()
        {
            string pref = Points.GetPref(_task.nzpt);
            var paramcalc = new CalcTypes.ParamCalc(0, Convert.ToInt32(_task.nzp), pref, _task.year_,
                _task.month_, _task.year_, _task.month_, _task.nzp_user, _task.nzp_key)
            {
                id_bill_pref = DbCalc.GetTestCalcPref(_task.parameters)
            };


            Returns ret;
            using (IDbConnection connDB = GetConnection(Constants.cons_Kernel))
            {
                ret = OpenDb(connDB, true);
                if (!ret.result) return ret; // return ret;

                List<string> list = GetListPref(connDB);

                PutNewTasks(list, paramcalc);

                bool isExist = GetPackLsWithoutNumLs(connDB);

                if (list.Count == 0 || isExist) ret = SetDopTask(paramcalc);


                UpdateStatusPack(!ret.result);
            }
            return ret;
        }

        private static string PackLS
        {
            get
            {
                int yy = Points.CalcMonth.year_;
                string packLS = Points.Pref + "_fin_" + (yy - 2000).ToString("00") + tableDelimiter + "pack_ls ";
                return packLS;
            }
        }


        /// <summary>
        /// Проверка, есть ли в пачке оплаты без номера лицевого счета
        /// </summary>
        /// <param name="connDB"></param>
        /// <returns></returns>
        private bool GetPackLsWithoutNumLs(IDbConnection connDB)
        {
            MyDataReader reader;
            Returns ret = ExecRead(connDB, out reader,
                " Select * From " + PackLS +
                " Where nzp_pack = " + _task.nzp +
                " and (num_ls is null or num_ls = 0)",
                true);

            if (!ret.result)
            {
                connDB.Close();
                throw new Exception();
            }
            bool result = reader.Read();
            reader.Close();
            return result;
        }


        /// <summary>
        /// Установка дополнительных заданий
        /// </summary>
        /// <param name="paramcalc"></param>
        /// <returns></returns>
        private Returns SetDopTask(CalcTypes.ParamCalc paramcalc)
        {
            Returns ret;
            _task.nzpt = 0;
            paramcalc.pref = "";

            using(var dbc2 = new DbCalcQueueClient(_task))
            { 
                ret = Dop(_task, paramcalc, dbc2.SetTaskProgress);
            }

            return ret;
        }



        /// <summary>
        /// Обновление статуса пачки
        /// </summary>
        /// <param name="updateStatus">Принудительное обновление статуса пачки</param>
        private void UpdateStatusPack(bool updateStatus)
        {
            if (updateStatus ||
                _task.TaskType == CalcFonTask.Types.CancelPackDistribution ||
                _task.TaskType == CalcFonTask.Types.DistributePack)
            {
                _task.TaskType = CalcFonTask.Types.UpdatePackStatus;
                using (var upPack = new UpdatePackStatusFonTask(_task))
                {
                    upPack.StartTask();
                }
            }
        }


        /// <summary>
        /// Добавить новую задачу
        /// </summary>
        /// <param name="list">Список префиксов схем</param>
        /// <param name="paramcalc"></param>
        private void PutNewTasks(List<string> list, CalcTypes.ParamCalc paramcalc)
        {
            if (list.Count <= 0) return;

            foreach (string prf in list)
            {
                foreach (_Point zap in Points.PointList)
                {
                    if (zap.pref != prf.Trim()) continue;

                    _task.nzpt = zap.nzp_wp;

                    using(var dbc = new DbCalcQueueClient(_task))
                    {
                        paramcalc.pref = prf.Trim();
                        Dop(_task, paramcalc, dbc.SetTaskProgress);
                    }
                }
            }
        }


        /// <summary>
        /// Получение списка схем в которые распределена пачка
        /// </summary>
        /// <param name="connDB"></param>
        /// <returns></returns>
        private List<string> GetListPref(IDbConnection connDB)
        {
            MyDataReader reader;
            string kvar = Points.Pref + "_data" + tableDelimiter + "kvar ";
            Returns ret = ExecRead(connDB, out reader,
                " Select distinct pref From " + kvar +
                " Where num_ls in ( Select num_ls " +
                " From " + PackLS + " Where nzp_pack = " + _task.nzp + ")"
                , true);

            if (!ret.result) throw new Exception();

            var list = new List<string>();
            try
            {
                while (reader.Read())
                    if (reader["pref"] != DBNull.Value) list.Add((string) reader["pref"]);
            }
            finally
            {
                reader.Close();
            }
            return list;
        }


        /// <summary>
        /// Постановка дополнительного задания
        /// </summary>
        /// <param name="calcfon"></param>
        /// <param name="paramcalc"></param>
        /// <param name="setTaskProgress"></param>
        /// <returns></returns>
        public Returns Dop(CalcFonTask calcfon, CalcTypes.ParamCalc paramcalc, DbCalcQueueClient.SetTaskProgressDelegate setTaskProgress)
        {
            Returns ret;
            if (calcfon.TaskType == CalcFonTask.Types.CancelDistributionAndDeletePack)
            {
                paramcalc.b_pack = true;
                paramcalc.b_packDel = true;
                var db = new DbCalcPack();
                db.CalcPackDel(paramcalc, out ret, setTaskProgress);
                db.Close();
            }
            else if (calcfon.TaskType == CalcFonTask.Types.CancelPackDistribution)
            {
                paramcalc.b_pack = true;
                paramcalc.b_packOt = true;
                var db = new DbCalcPack();
                db.CalcPackOt(paramcalc, out ret, setTaskProgress);
                Returns ret2;
                db.PackFonTasks_21(calcfon, out ret2);
                db.Close();
            }
            else
            {
                paramcalc.b_pack = true;
                var db = new DbCalcPack();
                db.CalcPackXX(paramcalc, calcfon.year_, false, out ret, setTaskProgress); //откат пачки
                Returns ret2;
                db.PackFonTasks_21(calcfon, out ret2);
                db.Close();

            }
            return ret;
        }
    

        public DistribPack(CalcFonTask task)
            : base(task)
        {

        }

    }

    /// <summary>
    /// Перасчет удержания при распределении
    /// </summary>
    public class RecalcUderDistrib : BaseFonTask
    {
        public override Returns StartTask()
        {
            Returns ret;
            using (IDbConnection connDB = GetConnection(Constants.cons_Kernel))
            {
                ret = OpenDb(connDB, true);
                if (!ret.result)
                {
                    return ret;
                }

                var dt = new DateTime(_task.year_, _task.month_, _task.nzpt);

                using (var db = new DbCalcPack())
                {
                    using (var dbc = new DbCalcQueueClient(_task))
                    {
                        db.RecalcUderFndistrib(connDB, dt, out ret, dbc.SetTaskProgress);
                    }
                }
            }
            return ret;
        }

        public RecalcUderDistrib(CalcFonTask task)
            : base(task)
        {

        }

    }



    /// <summary>
    /// Перенос оплат (распределить fn_distrib_xx)
    /// </summary>
    public class TransferTask : BaseFonTask
    {
        public override Returns StartTask()
        {
            Returns ret;
   
            using (var dbc = new DbCalcPack())
            {
                using (var dbc2 = new DbCalcQueueClient(_task))
                {
                    DateTime datS = DateTime.ParseExact(_task.nzp.ToString(CultureInfo.InvariantCulture), "yyyyMMdd", CultureInfo.InvariantCulture);
                    DateTime datPo = DateTime.ParseExact(_task.nzpt.ToString(CultureInfo.InvariantCulture), "yyyyMMdd", CultureInfo.InvariantCulture);
                    
                    TransferBalanceFinder finder = new TransferBalanceFinder()
                    {
                        dat_s = datS.ToShortDateString(),
                        dat_po = datPo.ToShortDateString(),
                        nzp_wp = _task.nzpt
                    };

                    dbc.DistribPaXX_1(finder, out ret, dbc2.SetTaskProgress);
                }
            }
            return ret;
        }

        public TransferTask(CalcFonTask task)
            : base(task)
        {

        }

    }


    /// <summary>
    /// Пересчет исходящего сальдо по распределениям
    /// </summary>
    public class RecalcDistribSumOutSaldo : BaseFonTask
    {
        public override Returns StartTask()
        {
            Returns ret;
            DateTime datOper;

            if (DateTime.TryParse(_task.parameters, out datOper))
                datOper = Convert.ToDateTime(_task.parameters);
            else datOper = Points.DateOper;

            int nzpPayer = _task.nzp;

            using (var db2 = new DbCalcPack())
            {
                db2.UpdateSaldoFndistrib(datOper, nzpPayer, 0, out ret);
            }
            return ret;
        }

        public RecalcDistribSumOutSaldo(CalcFonTask task)
            : base(task)
        {

        }

    }

}
