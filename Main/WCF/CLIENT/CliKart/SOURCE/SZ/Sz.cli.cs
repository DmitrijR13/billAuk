using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.DataBase;

namespace STCLINE.KP50.Client
{
    public class cli_Sz : I_Sz  //реализация клиента сервиса Кассы
    {
        I_Sz remoteObject;

        public cli_Sz(int nzp_server)
            : base()
        {
            _cli_Sz(nzp_server);
        }

        void _cli_Sz(int nzp_server)
        {
            if (Points.IsMultiHost && nzp_server > 0)
            {
                //определить параметры доступа
                _RServer zap = MultiHost.GetServer(nzp_server);
                //remoteObject = HostChannel.CreateInstance<I_Sz>(zap.login, zap.pwd, zap.ip_adr + WCFParams.srvServ);
                remoteObject = HostChannel.CreateInstance<I_Sz>(zap.login, zap.pwd, zap.ip_adr + WCFParams.AdresWcfWeb.srvServ);
            }
            else
            {
                //по-умолчанию
                //remoteObject = HostChannel.CreateInstance<I_Sz>(WCFParams.Adres + WCFParams.srvServ);
                remoteObject = HostChannel.CreateInstance<I_Sz>(WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvServ);
            }
        }

        ~cli_Sz()
        {
            try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
            catch { }
        }

        public List<SzFinder> GetFindSz(SzFinder finder, out Returns ret)
        {
            DbSzClient db = new DbSzClient();
            List<SzFinder> list = db.GetFindSz(finder, out ret);
            db.Close();
            return list;
        }

        public List<SzMo> DbGetMo(SzMo finder, out Returns ret)
        {
            DbSzClient db = new DbSzClient();
            List<SzMo> list = db.GetMo(finder, out ret);
            db.Close();
            return list;
        }

        public List<SzRajon> DbGetRajon(SzRajon finder, out Returns ret)
        {
            DbSzClient db = new DbSzClient();
            List<SzRajon> list = db.GetRajon(finder, out ret);
            db.Close();
            return list;
        }

        public List<SzUK> DbGetUK(SzUK finder, out Returns ret)
        {
            DbSzClient db = new DbSzClient();
            List<SzUK> list = db.GetUK(finder, out ret);
            db.Close();
            return list;
        }

        public List<SzUKPodr> DbGetUKPodr(SzUKPodr finder, out Returns ret)
        {
            DbSzClient db = new DbSzClient();
            List<SzUKPodr> list = db.GetUKPodr(finder, out ret);
            db.Close();
            return list;
        }

        public List<SzUlica> DbGetUlica(SzUlica finder, out Returns ret)
        {
            DbSzClient db = new DbSzClient();
            List<SzUlica> list = db.GetUlica(finder, out ret);
            db.Close();
            return list;
        }

        public void DbFindSz(SzFinder finder, out Returns ret)
        {
            DbSzClient db = new DbSzClient();
            db.FindSz(finder, out ret);
            db.Close();
        }

        public SzKart GetKartSz(SzFinder finder, out Returns ret)
        {
            DbSzClient db = new DbSzClient();
            SzKart sz = db.GetKartSz(finder, out ret);
            db.Close();
            return sz;
        }
    }
}
