using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Bars.KP50.Load.Obninsk.CountersLoad.Interfaces;
using Microsoft.Office.Interop.Excel;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using Points = STCLINE.KP50.Interfaces.Points;

namespace Bars.KP50.Load.Obninsk
{
   public class MustCalcCounters:ConnectionToDB, IMustCalc
    {
        private string tableForGroupMC = "";
        private string pref;
        private string sourceTableToMustCalc;
        private RecordMonth rm;
        private int nzp_user;
       private DbMustCalcNew dbMustCalcNew;

        public void Init(IDbConnection connection, string sourceTable, int nzp_user)
        {
            sourceTableToMustCalc = sourceTable;
            Connection = connection;
            tableForGroupMC = "t_must_calc";
            createTableToGroupSetMustCalc();
            this.nzp_user = nzp_user;
            dbMustCalcNew = new DbMustCalcNew(Connection);
        }

        void createTableToGroupSetMustCalc()
        {
            ExecSQL("Drop table " + tableForGroupMC, false);
            string sql = " Create temp table "+ tableForGroupMC + " (" +
            " nzp_kvar integer, " +
            " nzp_serv integer," +
            " kod integer default 1," +
            " dat_s Date," +
            " dat_po Date, " +
            " work_s Date, " +
            " work_po Date," +
            "month_calc Date)" + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        public void PrepareGroupMustcalc(string pref)
        {
            this.pref = pref;
            ExecSQL("truncate " + tableForGroupMC);
            rm = Points.GetCalcMonth(new CalcMonthParams(pref));
            string sql = "insert into "+ tableForGroupMC + "  (nzp_kvar,nzp_serv,dat_s,dat_po,work_s,work_po,month_calc) " +
              "select nzp_kvar,nzp_serv,  last_dat_uchet, date_pay - interval '1 day', last_dat_uchet, date_pay - interval '1 day', '" + rm.RecordDateTime.ToShortDateString() + "'" +
              " from " + sourceTableToMustCalc + " where date_pay < '" + rm.RecordDateTime.AddMonths(1).ToShortDateString() + "' and last_dat_uchet<date_pay and bad_cnt=0";
            ExecSQL(sql);
            sql = "insert into " +tableForGroupMC + "  (nzp_kvar,nzp_serv,dat_s,dat_po,work_s,work_po,month_calc) " +
                  "select nzp_kvar,nzp_serv, last_dat_uchet, date_pay-interval '1 month' - interval '1 day', last_dat_uchet, date_pay-interval '1 month' - interval '1 day', '" + rm.RecordDateTime.ToShortDateString() + "'" +
                  " from " + sourceTableToMustCalc + " where date_pay = '" + rm.RecordDateTime.AddMonths(1).ToShortDateString() + "' and " +
                  "(date_part('month',age(date_pay, last_dat_uchet))>1 or date_part('year',age(date_pay, last_dat_uchet))>0) and bad_cnt=0";
            ExecSQL(sql);
        }

        public void SetGroupMustCalc(string comment)
        {
            Returns ret = STCLINE.KP50.Global.Utils.InitReturns();
            MustCalcTable mustCalcTable = new MustCalcTable();
            mustCalcTable.Year = rm.RecordDateTime.Year;
            mustCalcTable.Month = rm.RecordDateTime.Month;
            mustCalcTable.Reason = MustCalcReasons.Counter;
            mustCalcTable.Kod2 = 0;
            mustCalcTable.NzpUser = nzp_user;
            mustCalcTable.Comment = comment;
            dbMustCalcNew.InsertListReason(pref+DBManager.sDataAliasRest,tableForGroupMC,mustCalcTable);
            if (!ret.result)
            {
                throw new InvalidOperationException(ret.text);
            } 
        }

        public void Dispose()
        {
            ExecSQL("Drop table " + tableForGroupMC,false);
        }
    }
}
