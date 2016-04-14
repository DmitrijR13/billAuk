using System;
using System.Collections.Generic;
using System.Data;
using Bars.KP50.Report.Base;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Bars.KP50.Report.RSO.Properties;

namespace Bars.KP50.Report.RSO.Reports
{
    class Report1523 : BaseSqlReport
    {
        public override string Name
        {
            get { return "15.2.3 Справка с места жительства"; }
        }

        public override string Description
        {
            get { return "Справка с места жительства"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> { ReportGroup.Cards };
                return result;
            }

        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_15_2_3; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.LC, ReportKind.Person }; }
        }

        /// <summary> ФИО гражданина </summary>
        private string NamePerson { get; set; }

        /// <summary> Дата рождения гражданина </summary>
        private string Birthday { get; set; }

        /// <summary> Дата регистрации </summary>
        private string DateRegistration { get; set; }

        /// <summary> Адрес </summary>
        private string Address { get; set; }

        /// <summary> Должность пасспортистки </summary>
        private string PostPassport { get; set; }

        /// <summary> ФИО </summary>
        private int Fio { get; set; }


        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>
            {
                new FioParameter() { Require = true }
            };
        }

        protected override void PrepareParams()
        {
            Fio = UserParamValues["FIO"].GetValue<int>();
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            report.SetParameterValue("name_person", NamePerson);
            report.SetParameterValue("birthday",Birthday);
            report.SetParameterValue("dat_reg",DateRegistration);
            report.SetParameterValue("adress",Address);
            report.SetParameterValue("post_pasport",PostPassport);
            report.SetParameterValue("name_pasport", STCLINE.KP50.Global.Utils.GetCorrectFIO(ReportParams.User.uname));
            report.SetParameterValue("date",DateTime.Now.ToShortDateString());
        }

        public override DataSet GetData()
        {
            int nzpKvar = -1;
            MyDataReader reader;
            DataTable tempTable = null;
            var sql = " SELECT pref " +
                      " FROM  " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar " +
                      " WHERE num_ls = " + ReportParams.NzpObject +
                      " AND nzp_wp > 1 " + GetwhereWp();
            ExecRead(out reader, sql);

            if (reader.Read())
            {
                var pref = reader["pref"].ToString().ToLower().Trim();
                sql = " SELECT TRIM(fam) || ' ' || TRIM(ima) || ' ' || TRIM(otch) AS fio, " +
                             " dat_rog, dat_ofor, nzp_kvar  " +
                      " FROM " + pref + DBManager.sDataAliasRest + "kart " +
                      " WHERE nzp_kart = " + Fio + 
                      " AND nzp_kvar = " + ReportParams.NzpObject;
                tempTable = ExecSQLToTable(sql);
                if (tempTable.Rows.Count != 0)
                {
                    DateTime datRog,datReg;
                    NamePerson = tempTable.Rows[0]["fio"].ToString().TrimEnd();
                    DateTime.TryParse(tempTable.Rows[0]["dat_rog"].ToString(), out datRog);
                    Birthday = datRog.ToShortDateString();
                    DateTime.TryParse(tempTable.Rows[0]["dat_ofor"].ToString(), out datReg);
                    DateRegistration = datReg.ToShortDateString();
                    if (!Int32.TryParse(tempTable.Rows[0]["nzp_kvar"].ToString(), out nzpKvar))
                    {
                        nzpKvar = -1;
                    }
                }
            }
            reader.Close();
            if (tempTable != null ) tempTable.Reset();
            string prefData = ReportParams.Pref + DBManager.sDataAliasRest;

            sql = " SELECT val_prm" +
                  " FROM " + prefData + "prm_10 " +
                  " WHERE is_actual = 1" +
                    " AND nzp_prm = 578 " +
                    " AND dat_s <= '" + DateTime.Now.ToShortDateString() + "' " +
                    " AND dat_po >= '" + DateTime.Now.ToShortDateString() + "' ";
            DataTable postTable = ExecSQLToTable(sql);
            if (postTable.Rows.Count != 0)
                PostPassport = postTable.Rows[0]["val_prm"].ToString().TrimEnd(); 

            sql = " SELECT (CASE WHEN TRIM(rajon) = '-' THEN TRIM(town) ELSE TRIM(rajon) END) || ', ' || " +
                                    " TRIM(ulica) || ', ' || " +
                                    " TRIM(ndom) || " +
                                    " (CASE WHEN TRIM(nkor) = '-' THEN '' ELSE ', кор. ' || TRIM(nkor) END) || " +
                                    " (CASE WHEN TRIM(nkvar) = '-' THEN '' ELSE ', кв. ' || TRIM(nkvar) END) AS adres" +
                  " FROM " + prefData + "kvar k INNER JOIN " + prefData + "dom d ON d.nzp_dom = k.nzp_dom " +
                                              " INNER JOIN " + prefData + "s_ulica u ON u.nzp_ul = d.nzp_ul " +
                                              " INNER JOIN " + prefData + "s_rajon r ON r.nzp_raj = u.nzp_raj " +
                                              " INNER JOIN " + prefData + "s_town t ON t.nzp_town = r.nzp_town " +
                  " WHERE k.nzp_kvar = " + nzpKvar;
            tempTable = ExecSQLToTable(sql);
            if (tempTable.Rows.Count != 0)
                Address = tempTable.Rows[0]["adres"].ToString();
           

            return new DataSet();
        }

        /// <summary>
        /// Получить условия органичения по банкам
        /// </summary>
        /// <returns></returns>
        private string GetwhereWp()
        {
            string whereWp = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            whereWp = whereWp.TrimEnd(',');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            return whereWp;
        }

        protected override void CreateTempTable() { }

        protected override void DropTempTable() { }
    }
}
