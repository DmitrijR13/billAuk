using System.Data;
using STCLINE.KP50.DataBase;
using System;
using globalsUtils = STCLINE.KP50.Global.Utils;

namespace Bars.KP50.Report
{
    public abstract class BaseSqlReportCounterFlow : BaseSqlReport
    {
        /// <summary>
        /// Получить показания ПУ
        /// </summary>
        /// <param name="pref"></param>
        /// <param name="inCounterValTable"></param>
        public void GetCounterFlow(string tempTable, int TypePU, string pref, DateTime DatUchet, DateTime DatUchetPo)
        {
            string inCounterValTable = "";

            switch (TypePU)
            {
                case 1: inCounterValTable = "counters_dom"; break;
                case 2: inCounterValTable = "counters_group"; break;
                case 3: inCounterValTable = "counters"; break;
            }

            string counterValTable = pref + DBManager.sDataAliasRest + inCounterValTable;

            // дата текущего показания
            string sql = " update " + tempTable + " t set " +
                " dat_uchet = (select max(b.dat_uchet) from " + counterValTable + " b " +
                " where t.nzp_counter = b.nzp_counter " +
                "   and b.dat_uchet BETWEEN " + globalsUtils.EStrNull(DatUchet.AddDays(1).ToShortDateString()) + " AND " + globalsUtils.EStrNull(DatUchetPo.AddMonths(1).ToShortDateString()) +
                "   and b.val_cnt is not null " +
                "   and b.is_actual <> 100) ";
            ExecSQL(sql);
            ExecSQL("create index ix_t_couns_dat_uchet on " + tempTable + " (dat_uchet)");
            ExecSQL(DBManager.sUpdStat + " " + tempTable);
            // текущее показание
            sql = "update " + tempTable + " t set " +
                " val_cnt = (select max(b.val_cnt) " +
                " from " + counterValTable + " b" +
                " where b.nzp_counter = t.nzp_counter " +
                "   and b.dat_uchet   = t.dat_uchet " +
                "   and b.val_cnt is not null " +
                "   and b.is_actual <> 100)" +
                " where t.dat_uchet is not null";
            ExecSQL(sql);
            // расход на лифты 
            if (TypePU == 1)
            {
                GetNpgCntNgpLift(tempTable, counterValTable);
            }
            else
            {
                sql = "update " + tempTable + " t set ngp_cnt = 0 ";
                ExecSQL(sql);    
            }

            // дата предыдущего показания опреляется в 2 шага:
            // 1. Получить минимальную дату учета, которая меньше или равна DatUchet
            // 2. Если такой даты нет, то получить максимальную дату учета, которая меньше DatUchet   

            // шаг 1
            sql = "update " + tempTable + " t set " +
                " dat_uchet_pred = (select min(b.dat_uchet) " +
                " from " + counterValTable + " b" +
                " where b.nzp_counter = t.nzp_counter " +
                "   and b.dat_uchet >= " + globalsUtils.EStrNull(DatUchet.ToShortDateString()) +
                "   and b.dat_uchet < t.dat_uchet " +
                "   and b.val_cnt is not null " +
                "   and b.is_actual <> 100)";
            ExecSQL(sql);
            // шаг 2
            sql = "update " + tempTable + " t set " +
                " dat_uchet_pred = (select max(b.dat_uchet) " +
                " from " + counterValTable + " b" +
                " where b.nzp_counter = t.nzp_counter " +
                "   and b.dat_uchet < " + globalsUtils.EStrNull(DatUchet.ToShortDateString()) +
               // "   and b.dat_uchet < t.dat_uchet " +
                "   and b.val_cnt is not null " +
                "   and b.is_actual <> 100) " +
                " where t.dat_uchet_pred is null ";
            ExecSQL(sql);
            ExecSQL("create index ix_t_couns_dat_uchet_pred on " + tempTable + " (dat_uchet_pred)");
            ExecSQL(DBManager.sUpdStat + " " + tempTable);

            // предыдущее показание
            sql = "update " + tempTable + " t set " +
                " val_cnt_pred = (select max(b.val_cnt) " +
                " from " + counterValTable + " b" +
                " where b.nzp_counter = t.nzp_counter " +
                "   and b.dat_uchet   = t.dat_uchet_pred " +
                "   and b.val_cnt is not null " +
                "   and b.is_actual <> 100) " +
                " where dat_uchet_pred is not null";
            ExecSQL(sql);

            // рассчитать расход
            CalculateFlow(tempTable, TypePU);
        }

        private void GetNpgCntNgpLift(string tempTable, string counterValTable)
        {
            string sql = "update " + tempTable + " t set " +
                    " ngp_lift = (select max(b.ngp_lift) " +
                    " from " + counterValTable + " b" +
                    " where b.nzp_counter = t.nzp_counter " +
                    "   and b.dat_uchet   = t.dat_uchet " +
                    "   and b.val_cnt     = t.val_cnt " +
                    "   and b.val_cnt is not null " +
                    "   and b.is_actual <> 100)" +
                    " where t.val_cnt is not null";
            ExecSQL(sql);

            sql = "update " + tempTable + " t set " +
                " ngp_cnt = (select max(b.ngp_cnt) " +
                " from " + counterValTable + " b" +
                " where b.nzp_counter = t.nzp_counter " +
                "   and b.dat_uchet   = t.dat_uchet " +
                "   and b.val_cnt     = t.val_cnt " +
                "   and b.val_cnt is not null " +
                "   and b.is_actual <> 100)" +
                " where t.val_cnt is not null";
            ExecSQL(sql);
        }

        private void CalculateFlow(string tempTable, int TypePu)
        {
            string sql = "";

            if (TypePu == 1)
            {
                sql = " update " + tempTable + " set " +
                    " rashod = (val_cnt - val_cnt_pred) * mmnog - ngp_cnt - ngp_lift " +
                    " where dat_uchet_pred IS NOT NULL ";
                ExecSQL(sql);
            }
            else
            {
                sql = " update " + tempTable + " set " +
                    " rashod = (val_cnt - val_cnt_pred) * mmnog - ngp_cnt " +
                    " where dat_uchet_pred IS NOT NULL ";
                ExecSQL(sql);
            }
            
            // переход через ноль
            sql = " update " + tempTable + " set rashod = rashod + pow(10, cnt_stage) * mmnog " +
                " where dat_uchet_pred IS NOT NULL " +
                "   and rashod < 0";
            ExecSQL(sql);
        }
    }
}
