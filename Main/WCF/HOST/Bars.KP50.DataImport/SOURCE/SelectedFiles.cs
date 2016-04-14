using System;
using System.Collections.Generic;
using System.Data;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.SOURCE
{
    public class SelectedFiles : DataBaseHeadServer
    {
        protected int GetRecordCount(string sql, IDbConnection con_db)
        {
            Returns ret = new Returns();
            object obj = ExecScalar(con_db, "select count (*) from (" + sql + ") as foo", out ret, true);
            if (!ret.result) throw new Exception(ret.text);

            int cnt = 0;
            try
            { cnt = Convert.ToInt32(obj); }
            catch
            { cnt = 0; }

            return cnt;
        }

        protected string WhereNzpFile(List<int> selectedFiles, string tableName = "")
        {
            string _where = "";
            
            for (int i = 0; i < selectedFiles.Count; i++)
            {
                if (_where != "") _where += ",";
                _where += selectedFiles[i].ToString();
            }

            if (_where != "")
            {
                _where = " and " + (tableName.Trim() != "" ? tableName.Trim() + "." : "") + "nzp_file in (" + _where + ") ";
            }
            else
            {
                _where = " and 1=0";
            }
            
            return _where;
        }

        protected string SetLimitOffset(string sql, Finder finder)
        {
            return DBManager.SetLimitOffset(sql, finder.rows, finder.skip);
        }

        protected string GetFio(string fam, string ima, string otch)
        {
            string fio = "";

            fam = fam.Trim();
            ima = ima.Trim();
            otch = otch.Trim();

            if (fam != "" && fam != "-")
            {
                fio += fam;            
            }

            if (ima != "" && ima != "-")
            {
                if (fio != "") fio += " ";
                fio += ima;
            }

            if (otch != "" && otch != "-")
            {
                if (fio != "") fio += " ";
                fio += otch;
            }

            return fio;
        }

        protected string GetAddress(string town, string rajon = "", string ulica = "", string ndom = "", string nkor = "", string nkvar = "", string nkvar_n = "")
        {
            string address = "";
            string dom = "";

            town = town.Trim();
            rajon = rajon.Trim();
            ulica = ulica.Trim();
            ndom = ndom.Trim();
            nkor = nkor.Trim();
            nkvar = nkvar.Trim();
            nkvar_n = nkvar_n.Trim();

            if (town != "" && town != "-")
            {
                address += town;
            }

            if (rajon != "" && rajon != "-")
            {
                if (address != "") address += ", ";
                address += rajon;
            }

            if (ulica != "" && ulica != "-")
            {
                if (address != "") address += ", ";
                address += ulica;
            }

            dom = GetHouse(ndom, nkor);
            if (dom != "" && dom != "-")
            {
                if (address != "") address += ", ";
                address += dom;
            }


            if (nkvar != "")
            {
                if (address != "") address += ", ";
                address += "КВ. " + nkvar;
            }

            if (nkvar_n != "" && nkvar_n != "-")
            {
                if (address != "") address += ", ";
                address += "КОМ. " + nkvar_n;
            }

            return address;
        }

        protected string GetHouseAddress(string ulica, string town, string rajon)
        {
            string address = "";
            town = town.Trim();
            rajon = rajon.Trim();
            ulica = ulica.Trim();

            if (ulica != "" && ulica != "-")
            {
                address += ulica;
            }

            rajon = GetAddress(town, rajon);

            if (rajon != "" && rajon != "-")
            {
                if (address != "") address += ", ";
                address += rajon;
            }

            return address;
        }

        protected string GetHouse(string ndom, string nkor)
        {
            string address = "";

            ndom = ndom.Trim();
            nkor = nkor.Trim();

            if (ndom != "" && ndom != "-")
            {
                if (address != "") address += ", ";
                address += ndom;
            }

            if (nkor != "" && nkor != "-")
            {
                if (address != "") address += "/";
                address += nkor;
            }

            return address;
        }
    }
}
