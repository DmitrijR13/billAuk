using System;
using System.Collections.Generic;
using STCLINE.KP50.Client;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.Server
{
    //----------------------------------------------------------------------
    public class srv_Calcs : srv_Base, I_Calcs //сервис справочников
    //----------------------------------------------------------------------
    {
        public List<Calcs> GetDomCalcsCollection(Calcs finder, out Returns ret)
        {
            ret = Utils.InitReturns() ;
            

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Calcs cli = new cli_Calcs(WCFParams.AdresWcfHost.CurT_Server);
                return cli.GetDomCalcsCollection(finder, out ret);
            }
            else
            {
                try
                {
                    if (finder.nzp_dom < 1 && finder.nzp_kvar > 0)
                    {
                        List<Ls> list;
                        using (var dba = new DbAdresHard())
                        {
                            list = dba.LoadLs(finder, out ret);
                        }

                        if (!ret.result) return null;

                        if (list != null && list.Count > 0) finder.nzp_dom = list[0].nzp_dom;
                    }

                    using (DBCalcs dbCalcs = new DBCalcs())
                    {
                        dbCalcs.FindDomCalcs(finder, out ret, "");
                        if (!ret.result)
                        {
                            return null;
                        }

                        List<Calcs> listCalcs = dbCalcs.GetDomCalcsCollection(out ret, finder);

                        if (ret.result)
                        {
                            return listCalcs;
                        }
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    MonitorLog.WriteLog("Ошибка GetDomCalcsCollection: " + ex.Message, MonitorLog.typelog.Error, true);
                    return null;
                }
            }
        }

        //----------------------------------------------------------------------
        public List<Calcs> GetGrPuCalcsCollection(Calcs finder, out Returns ret)
        {
            ret = Utils.InitReturns();


            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Calcs cli = new cli_Calcs(WCFParams.AdresWcfHost.CurT_Server);
                return cli.GetGrPuCalcsCollection(finder, out ret);
            }
            else
            {
                try
                {
                    if (finder.nzp_dom < 1 && finder.nzp_kvar > 0)
                    {
                        List<Ls> list;
                        using (var dba = new DbAdresHard())
                        {
                            list = dba.LoadLs(finder, out ret);
                        }

                        if (!ret.result) return null;

                        if (list != null && list.Count > 0) finder.nzp_dom = list[0].nzp_dom;
                    }

                    using (var dbCalcs = new DBCalcs())
                    {
                        dbCalcs.FindGrPuCalcs(finder, out ret, "");
                        if (!ret.result)
                        {
                            return null;
                        }

                        List<Calcs> listCalcs = dbCalcs.GetGrpuCalcsCollection(out ret, finder);

                        if (ret.result)
                        {
                            return listCalcs;
                        }
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    MonitorLog.WriteLog("Ошибка GetGrPuCalcsCollection: " + ex.Message, MonitorLog.typelog.Error, true);
                    return null;
                }
            }
        }

        //----------------------------------------------------------------------
        public List<Calcs> GetKvarCalcsCollection(Calcs finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();



            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Calcs cli = new cli_Calcs(WCFParams.AdresWcfHost.CurT_Server);
                return cli.GetKvarCalcsCollection(finder, out ret);
            }

            using (var dbCalcs = new DBCalcs())
            {
                dbCalcs.FindKvarCalcs(finder, out ret, "");
                //dbCalcs.Close();

                List<Calcs> listCalcs = dbCalcs.GetKvarCalcsCollection(out ret, finder);
                dbCalcs.Close();
                if (ret.result)
                {
                    return listCalcs;
                }
                return null;
            }
        }

        //----------------------------------------------------------------------
        public Returns CheckDatabaseExist(string pref, enDataBaseType en, string year_form, string year_to)
        {
            Returns ret = Utils.InitReturns();
            DBCalcs db = new DBCalcs();
            try
            {
                ret = db.CheckDatabaseExist(pref, en, year_form, year_to);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка CheckDatabaseExist: " + ex.Message, MonitorLog.typelog.Error, true);
                return ret;
            }
            finally
            {
                db.Close();
            }
        }
        //----------------------------------------------------------------------
    }
}
