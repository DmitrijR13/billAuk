namespace Bars.KP50.Report.Kapr
{
    using Bars.KP50.Report.Base;
    using STCLINE.KP50.DataBase;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text;
    using FastReport;
    using Bars.KP50.Report.Kapr.Properties;
    using Bars.KP50.Utils;

    public class Kapr2 : BaseSqlReport
    {
        public override string Name
        {
            get { return "Выгрузка по Капитальному ремонту"; }
        }

        public override string Description
        {
            get { return "Выгрузка по Капитальному ремонту"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                List<ReportGroup> result = new List<ReportGroup>();
                result.Add(ReportGroup.Reports);
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Kapr_2; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        /// <summary>Расчетный месяц</summary>
        protected int Month { get; set; }

        /// <summary>Расчетный год</summary>
        protected int Year { get; set; }

        //для временной таблички
        string temp_name = "t_nach_" + DateTime.Now.Minute + DateTime.Now.Hour;


        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
            };
        }

        public override DataSet GetData()
        {

            MyDataReader reader;


            var sql = new StringBuilder();


            sql.Remove(0, sql.Length);
            sql.Append(" SELECT * ");
            sql.Append(" FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point ");
            sql.Append(" WHERE nzp_wp>1 limit 1 ");

            ExecRead(out reader, sql.ToString());

            while (reader.Read())
            {
                var pref = reader["bd_kernel"].ToString().ToLower().Trim();

                sql.Remove(0, sql.Length);
                sql.Append(" INSERT INTO prm" + temp_name + " ( nzp,val_prm ) ");
                sql.Append(" select distinct nzp,val_prm from " + pref + DBManager.sDataAliasRest + "prm_1 where is_actual<>100 and nzp_prm =2004 group by 1,2 ");
                ExecSQL(sql.ToString());


                sql.Remove(0, sql.Length);
                sql.Append("create index i5" + temp_name + "  on prm" + temp_name + " (nzp , val_prm ) ");
                ExecSQL(sql.ToString());

                sql.Remove(0, sql.Length);
                sql.Append(" INSERT INTO prm4" + temp_name + " ( nzp,val_prm ) ");
                sql.Append(" select distinct nzp,cast(val_prm as " +DBManager.sDecimalType + "(14,2)) from " + pref + DBManager.sDataAliasRest + "prm_1  ");
                sql.Append(" where is_actual<>100 and nzp_prm = 4 group by 1,2 ");
                ExecSQL(sql.ToString());

                sql.Remove(0, sql.Length);
                sql.Append("create index i44" + temp_name + "  on prm4" + temp_name + " (nzp , val_prm ) ");
                ExecSQL(sql.ToString());



                var chargeXX = pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + Month.ToString("00");


                sql.Remove(0, sql.Length);
                sql.Append(" INSERT INTO " + temp_name + " (square,nkvar,nzp_serv, kod_kvar_erc,nzp_kvar, ls, pkod, domkod,  sum_insaldo, ");
                sql.Append(" sum_tarif, real_charge, reval, sum_money, sum_nedop, sum_outsaldo, fio, lic_type) ");
                sql.Append(" SELECT c4.val_prm, k.nkvar , a.nzp_serv, c.val_prm, k.nzp_kvar, k.num_ls, k.pkod, k.nzp_dom, sum(sum_insaldo), sum(sum_tarif),  ");
                sql.Append(" 	    sum(reval), sum(real_charge),  ");
                sql.Append(" 	    sum(sum_money), sum(sum_nedop), sum(sum_outsaldo), k.fio, k.typek   ");
                sql.Append(" FROM " + chargeXX + " a, " +
                    pref + DBManager.sDataAliasRest + "kvar k " +
                    "left join prm" + temp_name + " c on   k.nzp_kvar = c.nzp " +               
                    "left join prm4" + temp_name + " c4 on  k.nzp_kvar = c4.nzp "                
                    );
                sql.Append(" WHERE a.nzp_kvar=k.nzp_kvar   ");
                sql.Append("        AND a.dat_charge is null ");
                sql.Append("        AND a.nzp_serv>1 ");
                //sql.Append(where_adr + where_supp + where_serv);
                sql.Append(" GROUP BY  1,2,3,4,5,6,7,8,16,17  ");

                ExecSQL(sql.ToString());
                //square, prm_1 =4
                //nkvar ,nzp_serv ,nzp_ul,
                //d.ndom||' корп. '||d.nkor ,k.nkvar,a.nzp_serv,d.nzp_ul,


                sql.Remove(0, sql.Length);
                sql.Append(" Update " + temp_name + " set (nzp_ul) =((select nzp_ul from " + pref + DBManager.sDataAliasRest + "dom where nzp_dom =" + temp_name + ".domkod )) ");
                ExecSQL(sql.ToString());

                sql.Remove(0, sql.Length);
                sql.Append(" Update " + temp_name + " set (domkor) =((select ndom||' корп. '||nkor from " + pref + DBManager.sDataAliasRest + "dom where nzp_dom =" + temp_name + ".domkod )) ");
                ExecSQL(sql.ToString());

                sql.Remove(0, sql.Length);
                sql.Append(" Update " + temp_name + " set (ulica) =((select ulica from " + pref + DBManager.sDataAliasRest + "s_ulica where nzp_ul =" + temp_name + ".nzp_ul )) ");
                ExecSQL(sql.ToString());

                sql.Remove(0, sql.Length);
                sql.Append(" Update " + temp_name + " set (nzp_raj) =((select nzp_raj from " + pref + DBManager.sDataAliasRest + "s_ulica where nzp_ul =" + temp_name + ".nzp_ul )) ");
                ExecSQL(sql.ToString());

                sql.Remove(0, sql.Length);
                sql.Append(" Update " + temp_name + " set (rajon) =((select rajon from " + pref + DBManager.sDataAliasRest + "s_rajon where nzp_raj =" + temp_name + ".nzp_raj )) ");
                ExecSQL(sql.ToString());

                sql.Remove(0, sql.Length);
                sql.Append(" Update " + temp_name + " set (nzp_town) =((select nzp_town from " + pref + DBManager.sDataAliasRest + "s_rajon where nzp_raj =" + temp_name + ".nzp_raj )) ");
                ExecSQL(sql.ToString());

                sql.Remove(0, sql.Length);
                sql.Append(" Update " + temp_name + " set (town) =((select town from " + pref + DBManager.sDataAliasRest + "s_town where nzp_town =" + temp_name + ".nzp_town )) ");
                ExecSQL(sql.ToString());


                sql.Remove(0, sql.Length);
                sql.Append(" Update " + temp_name + " set (kod_kladr) =((select soato from " + pref + DBManager.sDataAliasRest + "s_ulica where nzp_ul =" + temp_name + ".nzp_ul )) ");
                ExecSQL(sql.ToString());

                sql.Remove(0, sql.Length);
                sql.Append(" Update " + temp_name + " set (kod_doma_erc) =((select max(val_prm) from " + pref + DBManager.sDataAliasRest + "prm_4 where nzp =" + temp_name + ".domkod and is_actual<>100 and nzp_prm =890 )) ");
                ExecSQL(sql.ToString());

                sql.Remove(0, sql.Length);
                sql.Append(" Update " + temp_name + " set fio = '-' where fio is null ");
                ExecSQL(sql.ToString());   


            }

            reader.Close();

            
            

            sql.Remove(0, sql.Length);
            sql.Append(" SELECT  t.ls, t.pkod, t.domkod, " + Month + " as month, " + Year + " as year, t.square, ");
            sql.Append("         sum(t.sum_insaldo) as sum_insaldo, sum(t.sum_tarif) as sum_tarif,  ");
            sql.Append("         sum(t.reval) as reval, sum(t.real_charge) as real_charge,  ");
            sql.Append("         sum(t.sum_money) as sum_money, sum(t.sum_nedop) as sum_nedop, sum(t.sum_outsaldo) as sum_outsaldo, ");
            sql.Append("          nzp_serv, town as gorod,rajon,ulica, ");
            sql.Append("   kod_kladr,t.domkod kod_doma_erc,t.ls kod_kvar_erc, domkor, nkvar, fio, lic_type ");
            sql.Append(" FROM " + temp_name + " t ");
            sql.Append(" GROUP BY 1, 2 , 3, 4, 5, 6 , 14,15,16,17,18,19,20,21,22, 23,24 ");
            //sql.Append(" ORDER BY 1,2 ");

            DataTable dt = ExecSQLToTable(sql.ToString());
            dt.TableName = "Q_master";

            if (dt.Rows.Count > 65000 && ReportParams.ExportFormat == ExportFormat.Excel2007)
            {
                var dtr = dt.Rows.Cast<DataRow>().Skip(65000).ToArray();
                dtr.ForEach(dt.Rows.Remove);
            }

            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }

        protected override void PrepareReport(Report report)
        {
            string[] months = new string[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};
            report.SetParameterValue("month1", months[Month]);
            report.SetParameterValue("year1", Year);
        }

        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].GetValue<int>();


            if (Month == 0)
            {
                throw new ReportException("Не определено значение \"Расчетный месяц\"");
            }

            if (Year == 0)
            {
                throw new ReportException("Не определено значение \"Расчетный год\"");
            }
        }

        protected override void CreateTempTable()
        {
            var sql = new StringBuilder();
            sql.Remove(0, sql.Length);
            sql.Append(" create temp table " + temp_name + " (     ");
           // temp_name = "public." + temp_name;
           // sql.Append(" create table " + temp_name + " (     ");
            sql.Append(" nzp_kvar " + DBManager.sDecimalType + "(14,0) default 0, ");
            sql.Append(" ls integer default 0,");
            sql.Append(" pkod  " + DBManager.sDecimalType + "(14,0) default 0, ");
            sql.Append(" domkod  " + DBManager.sDecimalType + "(14,0) default 0, ");
            sql.Append(" square " + DBManager.sDecimalType + "(14,2) default 0.00, ");
            sql.Append(" sum_insaldo " + DBManager.sDecimalType + "(14,2) default 0.00, ");
            sql.Append(" sum_tarif " + DBManager.sDecimalType + "(14,2) default 0.00, ");
            sql.Append(" reval " + DBManager.sDecimalType + "(14,2) default 0.00, ");
            sql.Append(" real_charge " + DBManager.sDecimalType + "(14,2) default 0.00, ");
            sql.Append(" sum_money " + DBManager.sDecimalType + "(14,2) default 0.00, ");
            sql.Append(" sum_nedop " + DBManager.sDecimalType + "(14,2) default 0.00, ");
            sql.Append(" sum_outsaldo " + DBManager.sDecimalType + "(14,2) default 0.00, ");
            sql.Append(" nzp_serv integer, ");
            sql.Append(" domkor varchar(20), ");
            sql.Append(" nkvar  varchar(10), ");
            sql.Append(" town  varchar(50), ");
            sql.Append(" rajon varchar(50), ");
            sql.Append(" ulica varchar(50), ");
            sql.Append(" nzp_ul integer, ");
            sql.Append(" nzp_raj integer, ");
            sql.Append(" nzp_town integer, ");
            sql.Append(" kod_kladr varchar(25), ");
            sql.Append(" kod_doma_erc varchar(20), ");
            sql.Append(" kod_kvar_erc varchar(20), ");
            sql.Append(" fio varchar(200), ");
            sql.Append(" lic_type integer ");
            
            sql.Append(" ) " + DBManager.sUnlogTempTable);

            ExecSQL(sql.ToString());



            sql.Remove(0, sql.Length);
            sql.Append("create index i"+temp_name+"  on " + temp_name + " (nzp_kvar , domkod ) ");
            ExecSQL(sql.ToString());
            sql.Remove(0, sql.Length);
            sql.Append("create index i1" + temp_name + "  on " + temp_name + " (nzp_kvar , kod_doma_erc ) ");
            ExecSQL(sql.ToString());
            sql.Remove(0, sql.Length);
            sql.Append("create index i2" + temp_name + "  on " + temp_name + " (nzp_kvar , kod_kvar_erc ) ");
            ExecSQL(sql.ToString());


            sql.Remove(0, sql.Length);
            sql.Append(" create temp table prm" + temp_name + " ( val_prm varchar(20), nzp integer)   ");
            ExecSQL(sql.ToString());

            sql.Remove(0, sql.Length);
            sql.Append(" create temp table prm4" + temp_name + " ( val_prm  "+DBManager.sDecimalType + "(14,2), nzp integer)   ");
            ExecSQL(sql.ToString());
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table " + temp_name , true);
            ExecSQL(" drop table prm" + temp_name, true);
            ExecSQL(" drop table prm4" + temp_name, true);
        }
    }
}
