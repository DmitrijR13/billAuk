using System;
using System.Linq;
using Bars.KP50.DB.Exchange.Unload;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;


namespace Bars.KP50.DB.Exchange.TransferHouses
{
    [TransferAttributes(Name = "dom", Descr = "Перенос домов", Priority = TransferPriority.House, Enabled = true)]
    public class TransferHouse : Transfer
    {

        protected override void TransferringDataFromOldBank()
        {
            var house = HouseParams.current_house;
            ExecSQL(string.Format("insert into {0}dom select * from {1}dom where nzp_dom = {2} and nzp_dom not in(select nzp_dom from {0}dom)", HouseParams.tPoint.pref + sDataAliasRest,
                HouseParams.fPoint.pref + sDataAliasRest, house.nzp_dom));

            //обновляем pref и nzp_wp в центральном банке
            ExecSQL(string.Format("update {0}dom set pref = '{1}' where nzp_dom = {2}", Points.Pref + sDataAliasRest, HouseParams.tPoint.pref, house.nzp_dom));

            ExecSQL(string.Format("update {0}dom set nzp_wp = (select nzp_wp from {1}s_point where trim(bd_kernel) = trim('{2}')) " +
                                  "where nzp_dom = {3}", Points.Pref + sDataAliasRest, Points.Pref + sKernelAliasRest, HouseParams.tPoint.pref, house.nzp_dom));
            
            //обновляем pref и nzp_wp в локальном банке
            ExecSQL(string.Format("update {0}dom set pref = '{1}' where nzp_dom = {2}", HouseParams.tPoint.pref + sDataAliasRest, HouseParams.tPoint.pref, house.nzp_dom));

            ExecSQL(string.Format("update {0}dom set nzp_wp = (select nzp_wp from {1}s_point where trim(bd_kernel) = trim('{2}')) " +
                                  "where nzp_dom = {3}", HouseParams.tPoint.pref + sDataAliasRest, Points.Pref + sKernelAliasRest, HouseParams.tPoint.pref, house.nzp_dom));
        }

        protected override void DeleteDataFromOldBank()
        {
            var house = HouseParams.current_house;
            ExecSQL(string.Format("delete from {0}dom where nzp_dom = {1}", HouseParams.fPoint.pref + sDataAliasRest,
                 house.nzp_dom));
        }

        protected override void RollbackAddedData()
        {
            var house = HouseParams.current_house;
            ExecSQL(string.Format("delete from {0}dom where nzp_dom = {1}", HouseParams.tPoint.pref + sDataAliasRest,
                 house.nzp_dom));

            //обновляем pref и nzp_wp в центральном банке(на данные из локального банка-источника) 
            ExecSQL(string.Format("update {0}dom set pref = '{1}' where nzp_dom = {2}", Points.Pref + sDataAliasRest, HouseParams.fPoint.pref, house.nzp_dom));

            ExecSQL(string.Format("update {0}dom set nzp_wp = (select nzp_wp from {1}s_point where trim(bd_kernel) = trim('{2}')) " +
                                  "where nzp_dom = {3}", Points.Pref + sDataAliasRest, Points.Pref + sKernelAliasRest, HouseParams.fPoint.pref, house.nzp_dom));

        }

        protected override void RollbackDeletedData()
        {
            var house = HouseParams.current_house;
            ExecSQL(string.Format("insert into {0}dom select * from {1}dom where nzp_dom = {2}", HouseParams.fPoint.pref + sDataAliasRest,
                HouseParams.tPoint.pref + sDataAliasRest, house.nzp_dom));
        }

        protected override bool CompareDataInTables()
        {
            var house = HouseParams.current_house;
            var obj1 = ExecScalar(string.Format("select count(*) from {0}dom where nzp_dom = {1}", HouseParams.fPoint.pref + sDataAliasRest, house.nzp_dom));
            var obj2 = ExecScalar(string.Format("select count(*) from {0}dom where nzp_dom = {1}", HouseParams.tPoint.pref + sDataAliasRest, house.nzp_dom));
            if (Convert.ToInt32(obj1) != Convert.ToInt32(obj2))
            {
                return false;
            }
            return true;
        }

        protected override bool Compare()
        {
            var oldBankTableField = GetTableFieldsList("dom", HouseParams.fPoint.pref + sDataAliasRest);
            var newBankTableField = GetTableFieldsList("dom", HouseParams.tPoint.pref + sDataAliasRest);
            if (oldBankTableField.Any(field => !newBankTableField.Contains(field)))
            {
                return false;
            }
            return true;
        }
    }

    [TransferAttributes(Name = "s_geu", Descr = "Перенос территорий", Priority = TransferPriority.Geu, Enabled = true)]
    public class TransferGeu : Transfer
    {
        protected override void TransferringDataFromOldBank()
        {
            var house = HouseParams.current_house;
            ExecSQL(string.Format("insert into {0}s_geu select * from {1}s_geu " +
                                  "where nzp_geu not in(select nzp_geu from {0}s_geu)", HouseParams.tPoint.pref + sDataAliasRest,
                HouseParams.fPoint.pref + sDataAliasRest, house.nzp_dom));
        }
        protected override void DeleteDataFromOldBank()
        {
            
        }

        protected override void RollbackAddedData()
        {
            var house = HouseParams.current_house;
            //ExecSQL(string.Format("delete from {0}s_geu where nzp_geu = (select nzp_geu from {2}dom where nzp_dom = {1})", HouseParams.tPoint.pref + sDataAliasRest,
            //     house.nzp_dom, HouseParams.fPoint.pref + sDataAliasRest));
        }

        protected override void RollbackDeletedData()
        {
            
        }

        protected override bool CompareDataInTables()
        {
            //var house = HouseParams.current_house;
            //var obj1 = ExecScalar(string.Format("select count(*) from {0}s_geu " +
            //                                    "where nzp_geu in(select nzp_geu from {0}dom where nzp_dom = {1})", HouseParams.fPoint.pref + sDataAliasRest, house.nzp_dom));
            //var obj2 = ExecScalar(string.Format("select count(*) from {0}s_geu" +
            //                                    " where nzp_geu in(select nzp_geu from {0}dom where nzp_dom = {1})", HouseParams.tPoint.pref + sDataAliasRest, house.nzp_dom));
            //if (Convert.ToInt32(obj1) != Convert.ToInt32(obj2))
            //{
            //    return false;
            //}
            return true;
        }

        protected override bool Compare()
        {
            var oldBankTableField = GetTableFieldsList("s_geu", HouseParams.fPoint.pref + sDataAliasRest);
            var newBankTableField = GetTableFieldsList("s_geu", HouseParams.tPoint.pref + sDataAliasRest);
            if (oldBankTableField.Any(field => !newBankTableField.Contains(field)))
            {
                return false;
            }
            return true;
        }
    }

    [TransferAttributes(Name = "s_area", Descr = "Перенос УК", Priority = TransferPriority.Area, Enabled = true)]
    public class TransferArea : Transfer
    {
        protected override void TransferringDataFromOldBank()
        {
            var house = HouseParams.current_house;
            ExecSQL(string.Format("insert into {0}s_area select * from {1}s_area " +
                                  "where nzp_area not in(select nzp_area from {0}s_area)", HouseParams.tPoint.pref + sDataAliasRest,
                HouseParams.fPoint.pref + sDataAliasRest, house.nzp_dom));
        }
        protected override void DeleteDataFromOldBank()
        {

        }

        protected override void RollbackAddedData()
        {
            var house = HouseParams.current_house;
            //ExecSQL(string.Format("delete from {0}s_area where nzp_area = (select nzp_area from {2}dom where nzp_dom = {1})", HouseParams.tPoint.pref + sDataAliasRest,
            //     house.nzp_dom, HouseParams.fPoint.pref + sDataAliasRest));
        }

        protected override void RollbackDeletedData()
        {

        }

        protected override bool CompareDataInTables()
        {
            //var house = HouseParams.current_house;
            //var obj1 = ExecScalar(string.Format("select count(*) from {0}s_geu " +
            //                                    "where nzp_geu in(select nzp_geu from {0}dom where nzp_dom = {1})", HouseParams.fPoint.pref + sDataAliasRest, house.nzp_dom));
            //var obj2 = ExecScalar(string.Format("select count(*) from {0}s_geu" +
            //                                    " where nzp_geu in(select nzp_geu from {0}dom where nzp_dom = {1})", HouseParams.tPoint.pref + sDataAliasRest, house.nzp_dom));
            //if (Convert.ToInt32(obj1) != Convert.ToInt32(obj2))
            //{
            //    return false;
            //}
            return true;
        }

        protected override bool Compare()
        {
            var oldBankTableField = GetTableFieldsList("s_area", HouseParams.fPoint.pref + sDataAliasRest);
            var newBankTableField = GetTableFieldsList("s_area", HouseParams.tPoint.pref + sDataAliasRest);
            if (oldBankTableField.Any(field => !newBankTableField.Contains(field)))
            {
                return false;
            }
            return true;
        }
    }

    [TransferAttributes(Name = "prm_2", Descr = "Перенос параметров дома", Priority = TransferPriority.Prm2, Enabled = true)]
    public class TransferPrm2 : Transfer
    {
        protected override void TransferringDataFromOldBank()
        {
            var house = HouseParams.current_house;
            var _fields = GetFields("prm_2", HouseParams.fPoint.pref + sDataAliasRest);
                ExecSQL(string.Format(" insert into {0}prm_2({3}) " +
                                      " (select {3} from {1}prm_2 where nzp = {2})",
                    HouseParams.tPoint.pref + sDataAliasRest,
                    HouseParams.fPoint.pref + sDataAliasRest, house.nzp_dom, _fields));
        }

        protected override void DeleteDataFromOldBank()
        {
            var house = HouseParams.current_house;
            ExecSQL(string.Format("delete from {0}prm_2 where nzp = {1}",
                HouseParams.fPoint.pref + sDataAliasRest, house.nzp_dom));

        }

        protected override void RollbackAddedData()
        {
            var house = HouseParams.current_house;

            ExecSQL(string.Format("delete from {0}prm_2 where nzp = {1}",
                HouseParams.tPoint.pref + sDataAliasRest, house.nzp_dom));
        }

        protected override void RollbackDeletedData()
        {
            var house = HouseParams.current_house;
            var _fields = GetFields("prm_2", HouseParams.fPoint.pref + sDataAliasRest);
            ExecSQL(string.Format(" insert into {0}prm_2({3}) " +
                                  " (select {3} " +
                                  " from {1}prm_2 where nzp = {2})",
                HouseParams.fPoint.pref + sDataAliasRest,
                HouseParams.tPoint.pref + sDataAliasRest, house.nzp_dom, _fields));
        }

        protected override bool CompareDataInTables()
        {
            var house = HouseParams.current_house;
            var obj1 =
                ExecScalar<Int32>(string.Format("select count(*) from {0}prm_2 where nzp = {1}",
                    HouseParams.fPoint.pref + sDataAliasRest, house.nzp_dom));
            var obj2 =
                ExecScalar<Int32>(string.Format("select count(*) from {0}prm_2 where nzp = {1}",
                    HouseParams.tPoint.pref + sDataAliasRest, house.nzp_dom));
            if (obj1 != obj2)
            {
                return false;
            }          
            return true;
        }

        protected override bool Compare()
        {
            var oldBankTableField = GetTableFieldsList("prm_2", HouseParams.fPoint.pref + sDataAliasRest);
            var newBankTableField = GetTableFieldsList("prm_2", HouseParams.tPoint.pref + sDataAliasRest);
            if (oldBankTableField.Any(field => !newBankTableField.Contains(field)))
            {
                return false;
            }
            return true;
        }
    }

    [TransferAttributes(Name = "prm_4", Descr = "Перенос параметров дома", Priority = TransferPriority.Prm4, Enabled = true)]
    public class TransferPrm4 : Transfer
    {
        protected override void TransferringDataFromOldBank()
        {
            var house = HouseParams.current_house;
            var _fields = GetFields("prm_4", HouseParams.fPoint.pref + sDataAliasRest);
                ExecSQL(string.Format(" insert into {0}prm_4({3}) " +
                                      " (select {3} from {1}prm_4 where nzp = {2})",
                    HouseParams.tPoint.pref + sDataAliasRest,
                    HouseParams.fPoint.pref + sDataAliasRest, house.nzp_dom, _fields));
        }

        protected override void DeleteDataFromOldBank()
        {
            var house = HouseParams.current_house;
                    ExecSQL(string.Format("delete from {0}prm_4 where nzp = {1}",
                        HouseParams.fPoint.pref + sDataAliasRest, house.nzp_dom));
        }

        protected override void RollbackAddedData()
        {
            var house = HouseParams.current_house;
                    ExecSQL(string.Format("delete from {0}prm_4 where nzp = {1}",
                        HouseParams.tPoint.pref + sDataAliasRest, house.nzp_dom));
        }

        protected override void RollbackDeletedData()
        {
            var house = HouseParams.current_house;
            var _fields = GetFields("prm_4", HouseParams.fPoint.pref + sDataAliasRest);

                ExecSQL(string.Format(" insert into {0}prm_4({3}) " +
                                      " (select {3} from {1}prm_4 where nzp = {2})",
                    HouseParams.fPoint.pref + sDataAliasRest,
                    HouseParams.tPoint.pref + sDataAliasRest, house.nzp_dom, _fields));
        }

        protected override bool CompareDataInTables()
        {
            var house = HouseParams.current_house;
            var obj1 =
                ExecScalar<Int32>(string.Format("select count(*) from {0}prm_4 where nzp = {1}",
                    HouseParams.fPoint.pref + sDataAliasRest, house.nzp_dom));
            var obj2 =
                ExecScalar<Int32>(string.Format("select count(*) from {0}prm_4 where nzp = {1}",
                    HouseParams.tPoint.pref + sDataAliasRest, house.nzp_dom));
            if (obj1 != obj2)
            {
                return false;
            }
            return true;
        }

        protected override bool Compare()
        {
            var oldBankTableField = GetTableFieldsList("prm_4", HouseParams.fPoint.pref + sDataAliasRest);
            var newBankTableField = GetTableFieldsList("prm_4", HouseParams.tPoint.pref + sDataAliasRest);
            if (oldBankTableField.Any(field => !newBankTableField.Contains(field)))
            {
                return false;
            }
            return true;
        }
    }
}
