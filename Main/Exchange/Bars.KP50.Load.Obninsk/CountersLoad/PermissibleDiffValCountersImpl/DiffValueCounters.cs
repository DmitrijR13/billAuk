using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Bars.KP50.Load.Obninsk.CountersLoad.Interfaces;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.Load.Obninsk
{
    /// <summary>
    /// Класс для вычисления максимального допустимого отклонения между текущим и предыдущим показаниями по разным услугам
    /// </summary>
    public  class DiffValueCounters:ConnectionToDB, IPermissibleDiffValCounters
    {
        private Dictionary<string, Dictionary<int, decimal>> maxDiffBetweenValues { get; set; }
        public void Init( IDbConnection connection, List<int> list_nzp_wp )
        {
            Connection = connection;
            maxDiffBetweenValues = GetMaxDiffBetweenValuesDict(list_nzp_wp);
        }


        /// <summary>
        /// Получить максимально допустимую разницу между показаниями ПУ
        /// </summary>
        /// <param name="pref"></param>
        /// <param name="nzpServ"></param>
        /// <returns></returns>
        public decimal GetDiffByServ(string pref, int nzpServ)
        {
            if (maxDiffBetweenValues.ContainsKey(pref))
            {
                if (maxDiffBetweenValues[pref].ContainsKey(nzpServ))
                {
                    return maxDiffBetweenValues[pref][nzpServ];
                }


            }
            else if (maxDiffBetweenValues.ContainsKey(Points.Pref))
            {
                if (maxDiffBetweenValues[Points.Pref].ContainsKey(nzpServ))
                {
                    return maxDiffBetweenValues[Points.Pref][nzpServ];
                }
            }
            return 1000000;
        }

        /// <summary>
        /// заполняем макимальную разницу показаний для отдельного банка
        /// </summary>
        /// <param name="pref">префикс банка</param>
        /// <returns></returns>
        private Dictionary<int, decimal> GetMaxDiffBetweenValuesDictForOneBank(string pref)
        {
            var resDictionary = new Dictionary<int, decimal>
            {
                {25, GetMaxDiffBetweenValuesOneServ(pref, 2081)},
                {9, GetMaxDiffBetweenValuesOneServ(pref, 2082)},
                {6, GetMaxDiffBetweenValuesOneServ(pref, 2083)},
                {10, GetMaxDiffBetweenValuesOneServ(pref, 2084)}
            };
            return resDictionary;
        }

        /// <summary>
        ///  заполняем макимальную разницу показаний по одной услуге для отдельного банка из системных параметров
        /// </summary>
        /// <param name="pref">префикс банка</param>
        /// <param name="param">код параметра</param>
        /// <returns></returns>
        private decimal GetMaxDiffBetweenValuesOneServ(string pref, int param)
        {
            Returns ret;
            string sql =
                " SELECT " + DBManager.sNvlWord + "(max(p.val_prm " + DBManager.sConvToNum + "), 10000) " +
                " FROM " + pref + DBManager.sKernelAliasRest + "prm_name pn " +
                " LEFT JOIN " + pref + DBManager.sDataAliasRest + "prm_10 p ON pn.nzp_prm = p.nzp_prm  and p.is_actual=1 " +
                " WHERE pn.nzp_prm = " + param;

            object obj = ExecScalar(sql, out ret, true);
            decimal result = (ret.result && obj != DBNull.Value) ? Convert.ToDecimal(obj) : 10000m;
            return result;
        }



        /// <summary>
        /// заполняем максимальную разницу показаний по верхнему и всем локальным банкам
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, Dictionary<int, decimal>> GetMaxDiffBetweenValuesDict(List<int> list_nzp_wp)
        {
            var resDictionary = new Dictionary<string, Dictionary<int, decimal>>();
            foreach (int nzp_wp in list_nzp_wp)
            {
                string pref = Points.GetPref(nzp_wp);
                resDictionary.Add(pref,
                   GetMaxDiffBetweenValuesDictForOneBank(pref));
            }
               
            return resDictionary;
        }
        /// <summary>
        /// Получение наименования услуги по номеру
        /// </summary>
        /// <param name="nzpServ"></param>
        /// <returns></returns>
        public string GetServName(int nzpServ)
        {
            switch (nzpServ)
            {
                case 6:
                    return "Холодная вода";
                case 9:
                    return "Горячая вода";
                case 25:
                    return "Дневно электроснабжение";
                case 210:
                    return "Ночное электроснабжение";
            }
            return "Неопределенная услуга";
        }
    }
}
