using STCLINE.KP50.DataBase;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using STCLINE.KP50.Interfaces;
using Bars.KP50.DB.Faktura;
using STCLINE.KP50.Global;

namespace Bars.KP50.Faktura.Source.Base
{
    /// <summary>
    /// Объект-связка услуга-порядок
    /// </summary>
    public class FakturaOrdering
    {
        public int NzpServ;//Код услуги
        public int OrderPrint;//Порядок печати 

        public FakturaOrdering()
        {
            NzpServ = 0;
            OrderPrint = 0;
        }

    }

    /// <summary>
    /// Объект-связка квартира-услуги
    /// </summary>
    public class LsFaktura
    {
        public int NzpKvar; // номер лс     
        public List<FakturaOrdering> ListServ; //список услуг лс

        public LsFaktura(int nzp_kvar)
        {
            NzpKvar = nzp_kvar;
            ListServ = new List<FakturaOrdering>();
        }

        public void AddServ(int NzpServ, int OrderPrint)
        {
            FakturaOrdering serv = new FakturaOrdering();
            serv.NzpServ = NzpServ;
            serv.OrderPrint = OrderPrint;
            ListServ.Add(serv);
        }

    }

    public class DbFakturaOrdering
    {
        private readonly IDbConnection _conDb;

        /// <summary>
        /// Ссылка на схему/базу с начислениями по лс
        /// </summary>
        private string _baseCharge;       
        /// <summary>
        /// список объектов-связок квартира-услуги
        /// </summary>
        public List<LsFaktura> listLsFaktura;     

        public DbFakturaOrdering(IDbConnection connDb, string tableCharge)
        {
            _conDb = connDb;
            _baseCharge = tableCharge;
            listLsFaktura = new List<LsFaktura>();
        }

        public void SetChargeTable(string tableCharge)
        {
            _baseCharge = tableCharge;
        }

        public void AddFaktura(LsFaktura LsListServ)
        {
            listLsFaktura.Add(LsListServ);
        }

        public void Clear()
        {       
            _baseCharge = String.Empty;
            listLsFaktura.Clear();
        }

        /// <summary>
        /// Обновляем order_print в charge_XX для печатаемых услуг в ЕПД
        /// </summary>
        public void UpdateOrderPrint()
        {
            StringBuilder sql = new StringBuilder();
            try
            {
                foreach (var LsServ in listLsFaktura)
                {
                    sql.Remove(0, sql.Length);
                    foreach (var serv in LsServ.ListServ)
                    {
                        sql.Append("UPDATE " + _baseCharge + " SET (order_print)=(" + serv.OrderPrint + ") WHERE dat_charge is null and nzp_kvar=" + LsServ.NzpKvar + " and nzp_serv=" + serv.NzpServ + ";");
                    }
                    Returns ret = DBManager.ExecSQL(_conDb, sql.ToString(), true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Счет-квитанция : Ошибка при обновлении order_print, sql: " + sql.ToString() , MonitorLog.typelog.Error, true);
                        if (_conDb != null) _conDb.Close();                        
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Счет-квитанция : Ошибка при обновлении order_print " + ex.Message, MonitorLog.typelog.Error, true);
                if (_conDb != null) _conDb.Close();
            }
        }

    }
}

