using System;
using System.Collections.Generic;
using System.Data;
using System.ServiceModel;
using STCLINE.KP50.Client;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.Server
{

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public partial  class srv_Nebo : srv_Base, I_Nebo
    {
        public IntfResultObjectType<List<NeboService>> GetServiceList(int nzp_area, RequestPaging paging)
        {
            try
            {
                IntfResultObjectType<List<NeboService>> res =
                    new ClassDB().RunSqlAction(new IntfRequestType() { keyID = nzp_area, paging = paging }, new DbNeboSprav().SelectServiceList);
                res.GetReturnsType().ThrowExceptionIfError();
                return res;
            }
            catch (Exception ex)
            {
                Returns ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
                return new IntfResultObjectType<List<NeboService>>(-1, ret.text);
            }
        }

        public IntfResultObjectType<List<NeboDom>> GetDomList(int nzp_area, RequestPaging paging)
        {
            try
            {
                IntfResultObjectType<List<NeboDom>> res =
                    new ClassDB().RunSqlAction(new IntfRequestType() { keyID = nzp_area, paging = paging }, new DbNeboSprav().SelectDomList);
                res.GetReturnsType().ThrowExceptionIfError();
                return res;
            }
            catch (Exception ex)
            {
                Returns ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
                return new IntfResultObjectType<List<NeboDom>>(-1, ret.text);
            }
        }

        public IntfResultObjectType<List<NeboSupplier>> GetSupplierList(int nzp_area, RequestPaging paging)
        {
            try
            {
                IntfResultObjectType<List<NeboSupplier>> res =
                    new ClassDB().RunSqlAction(new IntfRequestType() { keyID = nzp_area, paging = paging }, new DbNeboSprav().SelectSupplierList);
                res.GetReturnsType().ThrowExceptionIfError();
                return res;
            } 
            catch (Exception ex)
            {
                Returns ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
                return new IntfResultObjectType<List<NeboSupplier>>(-1, ret.text);
            }
        }


        public IntfResultObjectType<List<NeboRenters>> GetRentersList(int nzp_area, RequestPaging paging)
        {
            try
            {
                IntfResultObjectType<List<NeboRenters>> res =
                    new ClassDB().RunSqlAction(new IntfRequestType() { keyID = nzp_area, paging = paging }, new DbNeboSprav().GetRentersList);
                res.GetReturnsType().ThrowExceptionIfError();
                return res;
            }
            catch (Exception ex)
            {
                Returns ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
                return new IntfResultObjectType<List<NeboRenters>>(-1, ret.text);
            }
        }


        public IntfResultObjectType<List<NeboArea>> GetAreaList(int nzp_area)
        {
            try
            {
                IntfResultObjectType<List<NeboArea>> res =
                    new ClassDB().RunSqlAction(new IntfRequestType() { keyID = nzp_area }, new DbNeboSprav().SelectAreaList);
                res.GetReturnsType().ThrowExceptionIfError();
                return res;
            }
            catch (Exception ex)
            {
                Returns ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
                return new IntfResultObjectType<List<NeboArea>>(-1, ret.text);
            }
        }
                

    }

}
