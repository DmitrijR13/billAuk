using System;
using System.Linq;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DB.Exchange.TransferHouses
{
    [TransferAttributes(Name = "kvar", Descr = "Перенос ЛС", Priority = TransferPriority.Kvar, Enabled = true)]
    public class TransferKvars : Transfer
    {
        protected override void TransferringDataFromOldBank()
        {
            var house = HouseParams.current_house;
            ExecSQL(string.Format("insert into {0}kvar select * from {1}kvar where nzp_dom = {2} and nzp_dom not in(select nzp_dom from {0}kvar)", HouseParams.tPoint.pref + sDataAliasRest,
                HouseParams.fPoint.pref + sDataAliasRest, house.nzp_dom));

            //обновляем pref и nzp_wp в центральном банке
            ExecSQL(string.Format("update {0}kvar set pref = '{1}' where nzp_dom = {2}", Points.Pref + sDataAliasRest, HouseParams.tPoint.pref, house.nzp_dom));

            ExecSQL(string.Format("update {0}kvar set nzp_wp = (select nzp_wp from {1}s_point where trim(bd_kernel) = trim('{2}')) " +
                                  "where nzp_dom = {3}", Points.Pref + sDataAliasRest, Points.Pref + sKernelAliasRest, HouseParams.tPoint.pref, house.nzp_dom));
        }

        protected override void DeleteDataFromOldBank()
        {
            var house = HouseParams.current_house;
            ExecSQL(string.Format("delete from {0}kvar where nzp_dom = {1}", HouseParams.fPoint.pref + sDataAliasRest,
                 house.nzp_dom));
        }

        protected override void RollbackDeletedData()
        {
            var house = HouseParams.current_house;
            ExecSQL(string.Format("insert into {0}kvar select * from {1}kvar where nzp_dom = {2}", HouseParams.fPoint.pref + sDataAliasRest,
                HouseParams.tPoint.pref + sDataAliasRest, house.nzp_dom));

            //обновляем pref и nzp_wp в центральном банке(на данные из локального банка-источника) 
            ExecSQL(string.Format("update {0}kvar set pref = '{1}' where nzp_dom = {2}", Points.Pref + sDataAliasRest, HouseParams.fPoint.pref, house.nzp_dom));

            ExecSQL(string.Format("update {0}kvar set nzp_wp = (select nzp_wp from {1}s_point where trim(bd_kernel) = trim('{2}')) " +
                                  "where nzp_dom = {3}", Points.Pref + sDataAliasRest, Points.Pref + sKernelAliasRest, HouseParams.fPoint.pref, house.nzp_dom));
        }

        protected override void RollbackAddedData()
        {
            var house = HouseParams.current_house;
            ExecSQL(string.Format("delete from {0}kvar where nzp_dom = {1}", HouseParams.tPoint.pref + sDataAliasRest,
                 house.nzp_dom));
        }

        protected override bool CompareDataInTables()
        {
            var house = HouseParams.current_house;
            var obj1 = ExecScalar<Int32>(string.Format("select count(*) from {0}kvar where nzp_dom = {1}", HouseParams.fPoint.pref + sDataAliasRest, house.nzp_dom));
            var obj2 = ExecScalar<Int32>(string.Format("select count(*) from {0}kvar where nzp_dom = {1}", HouseParams.tPoint.pref + sDataAliasRest, house.nzp_dom));
            if (obj1 != obj2)
            {
                return false;
            }
            return true;
        }

        protected override bool Compare()
        {
            var oldBankTableField = GetTableFieldsList("kvar", HouseParams.fPoint.pref + sDataAliasRest);
            var newBankTableField = GetTableFieldsList("kvar", HouseParams.tPoint.pref + sDataAliasRest);
            if (oldBankTableField.Any(field => !newBankTableField.Contains(field)))
            {
                return false;
            }
            return true;
        }
    }

    [TransferAttributes(Name = "formuls", Descr = "Перенос формул расчета", Priority = TransferPriority.Formuls, Enabled = true)]
    public class TransferFormuls : Transfer
    {

        protected override void TransferringDataFromOldBank()
        {
            
             ExecSQL(string.Format("insert into {0}formuls select * from {1}formuls where nzp_frm not in (select nzp_frm from {0}formuls)",
                 HouseParams.tPoint.pref + sKernelAliasRest,
                 HouseParams.fPoint.pref + sKernelAliasRest));
            
        }

        protected override void DeleteDataFromOldBank()
        {
            
        }

        protected override void RollbackAddedData()
        {
            
        }

        protected override void RollbackDeletedData()
        {
            
        }

        protected override bool CompareDataInTables()
        {
            
            return true;
        }

        protected override bool Compare()
        {
            
            return true;
        }
    }

    [TransferAttributes(Name = "supplier", Descr = "Перенос договоров", Priority = TransferPriority.Supplier, Enabled = true)]
    public class TransferSupplier : Transfer
    {

        protected override void TransferringDataFromOldBank()
        {

            ExecSQL(string.Format("insert into {0}supplier select * from {1}supplier where nzp_supp not in (select nzp_supp from {0}supplier)",
                HouseParams.tPoint.pref + sKernelAliasRest,
                HouseParams.fPoint.pref + sKernelAliasRest));

        }

        protected override void DeleteDataFromOldBank()
        {

        }

        protected override void RollbackAddedData()
        {

        }

        protected override void RollbackDeletedData()
        {

        }

        protected override bool CompareDataInTables()
        {

            return true;
        }

        protected override bool Compare()
        {

            return true;
        }
    }

    [TransferAttributes(Name = "tarif", Descr = "Перенос списка услуг", Priority = TransferPriority.Tarif, Enabled = true)]
    public class TransferTarif : Transfer
    {

        protected override void TransferringDataFromOldBank()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());

                var _fields = GetFields("tarif", HouseParams.fPoint.pref + sDataAliasRest);

                    ExecSQL(string.Format("insert into {0}tarif({3}) select {3} from {1}tarif where nzp_kvar in ({2})",
                        HouseParams.tPoint.pref + sDataAliasRest,
                        HouseParams.fPoint.pref + sDataAliasRest, where, _fields));               
            }
        }

        protected override void DeleteDataFromOldBank()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());

                    ExecSQL(string.Format("delete from {0}tarif where nzp_kvar in ({1})",
                        HouseParams.fPoint.pref + sDataAliasRest, where));
            }
        }

        protected override void RollbackAddedData()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());

                    ExecSQL(string.Format("delete from {0}tarif where nzp_kvar in ({1})",
                        HouseParams.tPoint.pref + sDataAliasRest, where));
            }
        }

        protected override void RollbackDeletedData()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());

                var _fields = GetFields("tarif", HouseParams.fPoint.pref + sDataAliasRest);

                    ExecSQL(string.Format("insert into {0}tarif({3}) select {3} from {1}tarif where nzp_kvar in ({2})",
                        HouseParams.fPoint.pref + sDataAliasRest,
                        HouseParams.tPoint.pref + sDataAliasRest, where, _fields));
            }
        }

        protected override bool CompareDataInTables()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());
                var obj1 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}tarif where nzp_kvar in ({1})",
                        HouseParams.fPoint.pref + sDataAliasRest, where));
                var obj2 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}tarif where nzp_kvar in ({1})",
                        HouseParams.tPoint.pref + sDataAliasRest, where));
                if (obj1 != obj2)
                {
                    return false;
                }
            }
            return true;
        }

        protected override bool Compare()
        {
            var oldBankTableField = GetTableFieldsList("tarif", HouseParams.fPoint.pref + sDataAliasRest);
            var newBankTableField = GetTableFieldsList("tarif", HouseParams.tPoint.pref + sDataAliasRest);
            if (oldBankTableField.Any(field => !newBankTableField.Contains(field)))
            {
                return false;
            }
            return true;
        }
    }

    [TransferAttributes(Name = "alias_ls", Priority = TransferPriority.AliasLs, Enabled = false)]
    public class TransferAliasLs : Transfer
    {
        protected override void TransferringDataFromOldBank()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                foreach (var nzp_kvar in kvarList)
                {
                    ExecSQL(string.Format("insert into {0}alias_ls select * from {1}alias_ls where nzp_kvar = {2}",
                        HouseParams.tPoint.pref + sDataAliasRest,
                        HouseParams.fPoint.pref + sDataAliasRest, nzp_kvar));
                }
            }
        }

        protected override void DeleteDataFromOldBank()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                foreach (var nzp_kvar in kvarList)
                {
                    ExecSQL(string.Format("delete from {0}alias_ls where nzp_kvar = {1}",
                        HouseParams.fPoint.pref + sDataAliasRest,
                        nzp_kvar));
                }
            }
        }

        protected override void RollbackAddedData()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                foreach (var nzp_kvar in kvarList)
                {
                    ExecSQL(string.Format("delete from {0}alias_ls where nzp_kvar = {1}",
                        HouseParams.tPoint.pref + sDataAliasRest,
                        nzp_kvar));
                }
            }
        }

        protected override void RollbackDeletedData()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                foreach (var nzp_kvar in kvarList)
                {
                    ExecSQL(string.Format("insert into {0}alias_ls select * from {1}alias_ls where nzp_kvar = {2}",
                        HouseParams.fPoint.pref + sDataAliasRest,
                        HouseParams.tPoint.pref + sDataAliasRest, nzp_kvar));
                }
            }
        }

        protected override bool CompareDataInTables()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());
                var obj1 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}alias_ls where nzp_kvar in ({1})",
                        HouseParams.fPoint.pref + sDataAliasRest, where));
                var obj2 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}alias_ls where nzp_kvar in ({1})",
                        HouseParams.tPoint.pref + sDataAliasRest, where));
                if (obj1 != obj2)
                {
                    return false;
                }
            }
            return true;
        }

        protected override bool Compare()
        {
            var oldBankTableField = GetTableFieldsList("alias_ls", HouseParams.fPoint.pref + sDataAliasRest);
            var newBankTableField = GetTableFieldsList("alias_ls", HouseParams.tPoint.pref + sDataAliasRest);
            if (oldBankTableField.Any(field => !newBankTableField.Contains(field)))
            {
                return false;
            }
            return true;
        }
    }

    [TransferAttributes(Name = "bills", Priority = TransferPriority.Bills, Enabled = false)]
    public class TransferBills : Transfer
    {
        protected override void TransferringDataFromOldBank()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                foreach (var nzp_kvar in kvarList)
                {
                    ExecSQL(string.Format("insert into {0}bills(nzp_kvar,num_ls,id_bill,month_,year_,kod_sum) " +
                                          "(select nzp_kvar,num_ls,id_bill,month_,year_,kod_sum from {1}alias_ls where nzp_kvar = {2})",
                        HouseParams.tPoint.pref + sDataAliasRest,
                        HouseParams.fPoint.pref + sDataAliasRest, nzp_kvar));
                }
            }
        }

        protected override void DeleteDataFromOldBank()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                foreach (var nzp_kvar in kvarList)
                {
                    ExecSQL(string.Format("delete from {0}bills where nzp_kvar = {1}",
                        HouseParams.fPoint.pref + sDataAliasRest,
                        nzp_kvar));
                }
            }
        }

        protected override void RollbackAddedData()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                foreach (var nzp_kvar in kvarList)
                {
                    ExecSQL(string.Format("delete from {0}bills where nzp_kvar = {1}",
                        HouseParams.tPoint.pref + sDataAliasRest,
                        nzp_kvar));
                }
            }
        }

        protected override void RollbackDeletedData()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                foreach (var nzp_kvar in kvarList)
                {
                    ExecSQL(string.Format("insert into {0}bills(nzp_kvar,num_ls,id_bill,month_,year_,kod_sum) " +
                                          "(select nzp_kvar,num_ls,id_bill,month_,year_,kod_sum from {1}alias_ls where nzp_kvar = {2})",
                        HouseParams.fPoint.pref + sDataAliasRest,
                        HouseParams.tPoint.pref + sDataAliasRest, nzp_kvar));
                }
            }
        }

        protected override bool CompareDataInTables()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());
                var obj1 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}bills where nzp_kvar in ({1})",
                        HouseParams.fPoint.pref + sDataAliasRest, where));
                var obj2 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}bills where nzp_kvar in ({1})",
                        HouseParams.tPoint.pref + sDataAliasRest, where));
                if (obj1 != obj2)
                {
                    return false;
                }
            }
            return true;
        }

        protected override bool Compare()
        {
            var oldBankTableField = GetTableFieldsList("bills", HouseParams.fPoint.pref + sDataAliasRest);
            var newBankTableField = GetTableFieldsList("bills", HouseParams.tPoint.pref + sDataAliasRest);
            if (oldBankTableField.Any(field => !newBankTableField.Contains(field)))
            {
                return false;
            }
            return true;
        }
    }

    [TransferAttributes(Name = "family", Priority = TransferPriority.Family, Enabled = false)]
    public class TransferCountersArx : Transfer
    {
        protected override void TransferringDataFromOldBank()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                foreach (var nzp_kvar in kvarList)
                {
                    ExecSQL(string.Format("insert into {0}family(nzp_kvar,nzp_gilec,nzp_sem,dat_s,dat_po,is_actual,nzp_user,dat_when) " +
                                          "(select nzp_kvar,nzp_gilec,nzp_sem,dat_s,dat_po,is_actual,nzp_user,dat_when from {1}family where nzp_kvar = {2})",
                        HouseParams.tPoint.pref + sDataAliasRest,
                        HouseParams.fPoint.pref + sDataAliasRest, nzp_kvar));
                }
            }
        }

        protected override void DeleteDataFromOldBank()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                foreach (var nzp_kvar in kvarList)
                {
                    ExecSQL(string.Format("delete from {0}family where nzp_kvar = {1}",
                        HouseParams.fPoint.pref + sDataAliasRest,
                        nzp_kvar));
                }
            }
        }

        protected override void RollbackAddedData()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                foreach (var nzp_kvar in kvarList)
                {
                    ExecSQL(string.Format("delete from {0}family where nzp_kvar = {1}",
                        HouseParams.tPoint.pref + sDataAliasRest,
                        nzp_kvar));
                }
            }
        }

        protected override void RollbackDeletedData()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                foreach (var nzp_kvar in kvarList)
                {
                    ExecSQL(string.Format("insert into {0}family(nzp_kvar,nzp_gilec,nzp_sem,dat_s,dat_po,is_actual,nzp_user,dat_when) " +
                                          "(select nzp_kvar,nzp_gilec,nzp_sem,dat_s,dat_po,is_actual,nzp_user,dat_when from {1}family where nzp_kvar = {2})",
                        HouseParams.fPoint.pref + sDataAliasRest,
                        HouseParams.tPoint.pref + sDataAliasRest, nzp_kvar));
                }
            }
        }

        protected override bool CompareDataInTables()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());
                var obj1 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}family where nzp_kvar in ({1})",
                        HouseParams.fPoint.pref + sDataAliasRest, where));
                var obj2 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}family where nzp_kvar in ({1})",
                        HouseParams.tPoint.pref + sDataAliasRest, where));
                if (obj1 != obj2)
                {
                    return false;
                }
            }
            return true;
        }

        protected override bool Compare()
        {
            var oldBankTableField = GetTableFieldsList("family", HouseParams.fPoint.pref + sDataAliasRest);
            var newBankTableField = GetTableFieldsList("family", HouseParams.tPoint.pref + sDataAliasRest);
            if (oldBankTableField.Any(field => !newBankTableField.Contains(field)))
            {
                return false;
            }
            return true;
        }
    }

    [TransferAttributes(Name = "gil_periods", Descr = "Перенос жильцов", Priority = TransferPriority.GilPeriods, Enabled = true)]
    public class TransferGilPeriods : Transfer
    {
        protected override void TransferringDataFromOldBank()
        {
            var _fields = GetFields("gil_periods", HouseParams.fPoint.pref + sDataAliasRest);
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());

                //создаем промежуточную таблицу
                ExecSQL(string.Format(" DROP TABLE {0}gil_periods_arch", HouseParams.fPoint.pref + sDataAliasRest), false);
                ExecSQL(string.Format(" CREATE TABLE {0}gil_periods_arch AS SELECT " + _fields + " FROM {0}gil_periods WHERE nzp_kvar IN ({1})",
                        HouseParams.fPoint.pref + sDataAliasRest, where));

                //добавляем поля для нового кода жильца в промежуточную таблицу 
                ExecSQL(string.Format(" ALTER TABLE {0}gil_periods_arch ADD COLUMN new_nzp_gilec integer",
                    HouseParams.fPoint.pref + sDataAliasRest));

                //проставляем новые коды жильцов в промежуточную таблицу 
                ExecSQL(string.Format(" UPDATE {0}gil_periods_arch SET new_nzp_gilec = COALESCE((SELECT MAX(a.new_nzp_gil) FROM {0}gilec_arch a WHERE {0}gil_periods_arch.nzp_gilec = a.nzp_gil), 0)",
                        HouseParams.fPoint.pref + sDataAliasRest));

                //переносим данные из старого банка в новый
                ExecSQL(string.Format(
                    " INSERT INTO {1}gil_periods(nzp_tkrt, nzp_kvar, nzp_gilec, is_sn, is_lgt, osnov, dat_s, dat_po, is_actual," +
                    " nzp_user, dat_when, dat_del, cur_unl, nzp_wp, no_podtv_docs, id_departure_types)" +
                    " SELECT nzp_tkrt, nzp_kvar, new_nzp_gilec, is_sn, is_lgt, osnov, dat_s, dat_po, is_actual," +
                    " nzp_user, dat_when, dat_del, cur_unl, nzp_wp, no_podtv_docs, id_departure_types" +
                    " FROM {0}gil_periods_arch",
                    HouseParams.fPoint.pref + sDataAliasRest, HouseParams.tPoint.pref + sDataAliasRest));
            }
        }

        protected override void DeleteDataFromOldBank()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());
                
                ExecSQL(string.Format("delete from {0}gil_periods where nzp_kvar in ({1})",
                    HouseParams.fPoint.pref + sDataAliasRest, where));
                
            }
        }

        protected override void RollbackAddedData()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());
                
                ExecSQL(string.Format("delete from {0}gil_periods where nzp_kvar in ({1})",
                    HouseParams.tPoint.pref + sDataAliasRest, where));
                
            }
        }

        protected override void RollbackDeletedData()
        {
            ExecSQL(string.Format(" INSERT INTO {0}gil_periods(nzp_tkrt, nzp_kvar, nzp_gilec, is_sn, is_lgt, osnov, dat_s, dat_po, is_actual," +
                    " nzp_user, dat_when, dat_del, cur_unl, nzp_wp, no_podtv_docs, id_departure_types) " +
                                  "SELECT nzp_tkrt, nzp_kvar, nzp_gilec, is_sn, is_lgt, osnov, dat_s, dat_po, is_actual," +
                    " nzp_user, dat_when, dat_del, cur_unl, nzp_wp, no_podtv_docs, id_departure_types " +
                                " FROM {0}gil_periods_arch",
                HouseParams.fPoint.pref + sDataAliasRest));
        }

        protected override bool CompareDataInTables()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());
                var obj1 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}gil_periods where nzp_kvar in ({1})",
                        HouseParams.fPoint.pref + sDataAliasRest, where));
                var obj2 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}gil_periods where nzp_kvar in ({1})",
                        HouseParams.tPoint.pref + sDataAliasRest, where));
                if (obj1 != obj2)
                {
                    return false;
                }
            }
            return true;
        }

        protected override bool Compare()
        {
            var oldBankTableField = GetTableFieldsList("gil_periods", HouseParams.fPoint.pref + sDataAliasRest);
            var newBankTableField = GetTableFieldsList("gil_periods", HouseParams.tPoint.pref + sDataAliasRest);
            if (oldBankTableField.Any(field => !newBankTableField.Contains(field)))
            {
                return false;
            }
            return true;
        }
    }

    [TransferAttributes(Name = "lgots", Priority = TransferPriority.Lgots, Enabled = false)]
    public class TransferLgots : Transfer
    {
        protected override void TransferringDataFromOldBank()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var _fields = GetFields("lgots", HouseParams.fPoint.pref + sDataAliasRest);
                foreach (var nzp_kvar in kvarList)
                {
                    ExecSQL(string.Format(" insert into {0}lgots({3}) " +
                                          " (select {3} " +
                                          " from {1}lgots where nzp_kvar = {2})",
                        HouseParams.tPoint.pref + sDataAliasRest,
                        HouseParams.fPoint.pref + sDataAliasRest, nzp_kvar, _fields));
                }
            }
        }

        protected override void DeleteDataFromOldBank()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                foreach (var nzp_kvar in kvarList)
                {
                    ExecSQL(string.Format("delete from {0}lgots where nzp_kvar = {1}",
                        HouseParams.fPoint.pref + sDataAliasRest,
                        nzp_kvar));
                }
            }
        }

        protected override void RollbackAddedData()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                foreach (var nzp_kvar in kvarList)
                {
                    ExecSQL(string.Format("delete from {0}lgots where nzp_kvar = {1}",
                        HouseParams.tPoint.pref + sDataAliasRest,
                        nzp_kvar));
                }
            }
        }

        protected override void RollbackDeletedData()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var _fields = GetFields("lgots", HouseParams.fPoint.pref + sDataAliasRest);
                foreach (var nzp_kvar in kvarList)
                {
                    ExecSQL(string.Format(" insert into {0}lgots({3}) " +
                                          " (select {3} " +
                                          " from {1}lgots where nzp_kvar = {2})",
                        HouseParams.fPoint.pref + sDataAliasRest,
                        HouseParams.tPoint.pref + sDataAliasRest, nzp_kvar, _fields));
                }
            }
        }

        protected override bool CompareDataInTables()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());
                var obj1 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}lgots where nzp_kvar in ({1})",
                        HouseParams.fPoint.pref + sDataAliasRest, where));
                var obj2 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}lgots where nzp_kvar in ({1})",
                        HouseParams.tPoint.pref + sDataAliasRest, where));
                if (obj1 != obj2)
                {
                    return false;
                }
            }
            return true;
        }

        protected override bool Compare()
        {
            var oldBankTableField = GetTableFieldsList("lgots", HouseParams.fPoint.pref + sDataAliasRest);
            var newBankTableField = GetTableFieldsList("lgots", HouseParams.tPoint.pref + sDataAliasRest);
            if (oldBankTableField.Any(field => !newBankTableField.Contains(field)))
            {
                return false;
            }
            return true;
        }
    }

    [TransferAttributes(Name = "nedop_kvar", Priority = TransferPriority.NedopKvar, Enabled = false)]
    public class TransferNedopKvar : Transfer
    {
        protected override void TransferringDataFromOldBank()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var _fields = GetFields("nedop_kvar", HouseParams.fPoint.pref + sDataAliasRest);
                foreach (var nzp_kvar in kvarList)
                {
                    ExecSQL(string.Format(" insert into {0}nedop_kvar({3}) " +
                                          " (select {3} " +
                                          " from {1}nedop_kvar where nzp_kvar = {2})",
                        HouseParams.tPoint.pref + sDataAliasRest,
                        HouseParams.fPoint.pref + sDataAliasRest, nzp_kvar));
                }
            }
        }

        protected override void DeleteDataFromOldBank()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                foreach (var nzp_kvar in kvarList)
                {
                    ExecSQL(string.Format("delete from {0}nedop_kvar where nzp_kvar = {1}",
                        HouseParams.fPoint.pref + sDataAliasRest,
                        nzp_kvar));
                }
            }
        }

        protected override void RollbackAddedData()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                foreach (var nzp_kvar in kvarList)
                {
                    ExecSQL(string.Format("delete from {0}nedop_kvar where nzp_kvar = {1}",
                        HouseParams.tPoint.pref + sDataAliasRest,
                        nzp_kvar));
                }
            }
        }

        protected override void RollbackDeletedData()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var _fields = GetFields("nedop_kvar", HouseParams.fPoint.pref + sDataAliasRest);
                foreach (var nzp_kvar in kvarList)
                {
                    ExecSQL(string.Format(" insert into {0}nedop_kvar({3}) " +
                                          " (select {3} " +
                                          " from {1}nedop_kvar where nzp_kvar = {2})",
                        HouseParams.fPoint.pref + sDataAliasRest,
                        HouseParams.tPoint.pref + sDataAliasRest, nzp_kvar, _fields));
                }
            }
        }

        protected override bool CompareDataInTables()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());
                var obj1 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}nedop_kvar where nzp_kvar in ({1})",
                        HouseParams.fPoint.pref + sDataAliasRest, where));
                var obj2 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}nedop_kvar where nzp_kvar in ({1})",
                        HouseParams.tPoint.pref + sDataAliasRest, where));
                if (obj1 != obj2)
                {
                    return false;
                }
            }
            return true;
        }

        protected override bool Compare()
        {
            var oldBankTableField = GetTableFieldsList("nedop_kvar", HouseParams.fPoint.pref + sDataAliasRest);
            var newBankTableField = GetTableFieldsList("nedop_kvar", HouseParams.tPoint.pref + sDataAliasRest);
            if (oldBankTableField.Any(field => !newBankTableField.Contains(field)))
            {
                return false;
            }
            return true;
        }
    }

    [TransferAttributes(Name = "notice", Priority = TransferPriority.Notice, Enabled = false)]
    public class TransferNotice : Transfer
    {
        protected override void TransferringDataFromOldBank()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var _fields = GetFields("notice", HouseParams.fPoint.pref + sDataAliasRest);
                foreach (var nzp_kvar in kvarList)
                {
                    ExecSQL(string.Format(" insert into {0}notice({3}) " +
                                          " (select {3} " +
                                          " from {1}notice where nzp_kvar = {2})",
                        HouseParams.tPoint.pref + sDataAliasRest,
                        HouseParams.fPoint.pref + sDataAliasRest, nzp_kvar, _fields));
                }
            }
        }

        protected override void DeleteDataFromOldBank()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                foreach (var nzp_kvar in kvarList)
                {
                    ExecSQL(string.Format("delete from {0}notice where nzp_kvar = {1}",
                        HouseParams.fPoint.pref + sDataAliasRest,
                        nzp_kvar));
                }
            }
        }

        protected override void RollbackAddedData()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                foreach (var nzp_kvar in kvarList)
                {
                    ExecSQL(string.Format("delete from {0}notice where nzp_kvar = {1}",
                        HouseParams.tPoint.pref + sDataAliasRest,
                        nzp_kvar));
                }
            }
        }

        protected override void RollbackDeletedData()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var _fields = GetFields("notice", HouseParams.fPoint.pref + sDataAliasRest);
                foreach (var nzp_kvar in kvarList)
                {
                    ExecSQL(string.Format(" insert into {0}notice({3}) " +
                                          " (select {3} " +
                                          " from {1}notice where nzp_kvar = {2})",
                        HouseParams.fPoint.pref + sDataAliasRest,
                        HouseParams.tPoint.pref + sDataAliasRest, nzp_kvar, _fields));
                }
            }
        }

        protected override bool CompareDataInTables()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());
                var obj1 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}notice where nzp_kvar in ({1})",
                        HouseParams.fPoint.pref + sDataAliasRest, where));
                var obj2 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}notice where nzp_kvar in ({1})",
                        HouseParams.tPoint.pref + sDataAliasRest, where));
                if (obj1 != obj2)
                {
                    return false;
                }
            }
            return true;
        }

        protected override bool Compare()
        {
            var oldBankTableField = GetTableFieldsList("notice", HouseParams.fPoint.pref + sDataAliasRest);
            var newBankTableField = GetTableFieldsList("notice", HouseParams.tPoint.pref + sDataAliasRest);
            if (oldBankTableField.Any(field => !newBankTableField.Contains(field)))
            {
                return false;
            }
            return true;
        }
    }

    [TransferAttributes(Name = "sqadd", Priority = TransferPriority.Sqadd, Enabled = false)]
    public class TransferSqadd : Transfer
    {
        protected override void TransferringDataFromOldBank()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var _fields = GetFields("sqadd", HouseParams.fPoint.pref + sDataAliasRest);
                foreach (var nzp_kvar in kvarList)
                {
                    ExecSQL(string.Format(" insert into {0}sqadd({3}) " +
                                          " (select {3} " +
                                          " from {1}sqadd where nzp_kvar = {2})",
                        HouseParams.tPoint.pref + sDataAliasRest,
                        HouseParams.fPoint.pref + sDataAliasRest, nzp_kvar, _fields));
                }
            }
        }

        protected override void DeleteDataFromOldBank()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                foreach (var nzp_kvar in kvarList)
                {
                    ExecSQL(string.Format("delete from {0}sqadd where nzp_kvar = {1}",
                        HouseParams.fPoint.pref + sDataAliasRest,
                        nzp_kvar));
                }
            }
        }

        protected override void RollbackAddedData()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                foreach (var nzp_kvar in kvarList)
                {
                    ExecSQL(string.Format("delete from {0}sqadd where nzp_kvar = {1}",
                        HouseParams.tPoint.pref + sDataAliasRest,
                        nzp_kvar));
                }
            }
        }

        protected override void RollbackDeletedData()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var _fields = GetFields("sqadd", HouseParams.fPoint.pref + sDataAliasRest);
                foreach (var nzp_kvar in kvarList)
                {
                    ExecSQL(string.Format(" insert into {0}sqadd({3}) " +
                                          " (select {3} " +
                                          " from {1}sqadd where nzp_kvar = {2})",
                        HouseParams.fPoint.pref + sDataAliasRest,
                        HouseParams.tPoint.pref + sDataAliasRest, nzp_kvar, _fields));
                }
            }
        }

        protected override bool CompareDataInTables()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());
                var obj1 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}sqadd where nzp_kvar in ({1})",
                        HouseParams.fPoint.pref + sDataAliasRest, where));
                var obj2 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}sqadd where nzp_kvar in ({1})",
                        HouseParams.tPoint.pref + sDataAliasRest, where));
                if (obj1 != obj2)
                {
                    return false;
                }
            }
            return true;
        }

        protected override bool Compare()
        {
            var oldBankTableField = GetTableFieldsList("sqadd", HouseParams.fPoint.pref + sDataAliasRest);
            var newBankTableField = GetTableFieldsList("sqadd", HouseParams.tPoint.pref + sDataAliasRest);
            if (oldBankTableField.Any(field => !newBankTableField.Contains(field)))
            {
                return false;
            }
            return true;
        }
    }

    [TransferAttributes(Name = "sobstw", Priority = TransferPriority.Sobstw, Enabled = false)]
    public class TransferSobstw : Transfer
    {
        protected override void TransferringDataFromOldBank()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var _fields = GetFields("sobstw", HouseParams.fPoint.pref + sDataAliasRest);
                foreach (var nzp_kvar in kvarList)
                {
                    ExecSQL(string.Format(" insert into {0}sobstw({3}) " +
                                          " (select {3} " +
                                          " from {1}sobstw where nzp_kvar = {2})",
                        HouseParams.tPoint.pref + sDataAliasRest,
                        HouseParams.fPoint.pref + sDataAliasRest, nzp_kvar, _fields));
                }
            }
        }

        protected override void DeleteDataFromOldBank()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                foreach (var nzp_kvar in kvarList)
                {
                    ExecSQL(string.Format("delete from {0}sobstw where nzp_kvar = {1}",
                        HouseParams.fPoint.pref + sDataAliasRest,
                        nzp_kvar));
                }
            }
        }

        protected override void RollbackAddedData()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                foreach (var nzp_kvar in kvarList)
                {
                    ExecSQL(string.Format("delete from {0}sobstw where nzp_kvar = {1}",
                        HouseParams.tPoint.pref + sDataAliasRest,
                        nzp_kvar));
                }
            }
        }

        protected override void RollbackDeletedData()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var _fields = GetFields("sobstw", HouseParams.fPoint.pref + sDataAliasRest);
                foreach (var nzp_kvar in kvarList)
                {
                    ExecSQL(string.Format(" insert into {0}sobstw({3}) " +
                                          " (select {3} " +
                                          " from {1}sobstw where nzp_kvar = {2})",
                        HouseParams.fPoint.pref + sDataAliasRest,
                        HouseParams.tPoint.pref + sDataAliasRest, nzp_kvar, _fields));
                }
            }
        }

        protected override bool CompareDataInTables()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());
                var obj1 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}sobstw where nzp_kvar in ({1})",
                        HouseParams.fPoint.pref + sDataAliasRest, where));
                var obj2 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}sobstw where nzp_kvar in ({1})",
                        HouseParams.tPoint.pref + sDataAliasRest, where));
                if (obj1 != obj2)
                {
                    return false;
                }
            }
            return true;
        }

        protected override bool Compare()
        {
            var oldBankTableField = GetTableFieldsList("sobstw", HouseParams.fPoint.pref + sDataAliasRest);
            var newBankTableField = GetTableFieldsList("sobstw", HouseParams.tPoint.pref + sDataAliasRest);
            if (oldBankTableField.Any(field => !newBankTableField.Contains(field)))
            {
                return false;
            }
            return true;
        }
    }

    [TransferAttributes(Name = "prm_3", Descr = "Перенос параметров ЛС", Priority = TransferPriority.Prm3, Enabled = true)]
    public class TransferPrm3 : Transfer
    {
        protected override void TransferringDataFromOldBank()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());

                var _fields = GetFields("prm_3", HouseParams.fPoint.pref + sDataAliasRest);

                    ExecSQL(string.Format(" insert into {0}prm_3({3}) " +
                                          " (select {3} " +
                                          " from {1}prm_3 where nzp in ({2}) and nzp not in (select nzp from {0}prm_3))",
                        HouseParams.tPoint.pref + sDataAliasRest,
                        HouseParams.fPoint.pref + sDataAliasRest, where, _fields));
            }
        }

        protected override void DeleteDataFromOldBank()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                 var where = string.Join(",", kvarList.ToArray());

                    ExecSQL(string.Format("delete from {0}prm_3 where nzp in ({1})",
                        HouseParams.fPoint.pref + sDataAliasRest, where));
            }
        }

        protected override void RollbackAddedData()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());

                    ExecSQL(string.Format("delete from {0}prm_3 where nzp in ({1})",
                        HouseParams.tPoint.pref + sDataAliasRest, where));
            }
        }

        protected override void RollbackDeletedData()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());

                var _fields = GetFields("prm_3", HouseParams.fPoint.pref + sDataAliasRest);

                    ExecSQL(string.Format(" insert into {0}prm_3({3}) " +
                                          " (select {3} " +
                                          " from {1}prm_3 where nzp in ({2}))",
                        HouseParams.fPoint.pref + sDataAliasRest,
                        HouseParams.tPoint.pref + sDataAliasRest, where, _fields));
            }
        }

        protected override bool CompareDataInTables()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());
                var obj1 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}prm_3 where nzp in ({1})",
                        HouseParams.fPoint.pref + sDataAliasRest, where));
                var obj2 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}prm_3 where nzp in ({1})",
                        HouseParams.tPoint.pref + sDataAliasRest, where));
                if (obj1 != obj2)
                {
                    return false;
                }
            }
            return true;
        }

        protected override bool Compare()
        {
            var oldBankTableField = GetTableFieldsList("prm_3", HouseParams.fPoint.pref + sDataAliasRest);
            var newBankTableField = GetTableFieldsList("prm_3", HouseParams.tPoint.pref + sDataAliasRest);
            if (oldBankTableField.Any(field => !newBankTableField.Contains(field)))
            {
                return false;
            }
            return true;
        }
    }

    [TransferAttributes(Name = "prm_15", Descr = "Перенос параметров ЛС", Priority = TransferPriority.Prm15, Enabled = true)]
    public class TransferPrm15 : Transfer
    {
        protected override void TransferringDataFromOldBank()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());

                var _fields = GetFields("prm_15", HouseParams.fPoint.pref + sDataAliasRest);

                    ExecSQL(string.Format(" insert into {0}prm_15({3}) " +
                                          " (select {3} " +
                                          " from {1}prm_15 where nzp in ({2}))",
                        HouseParams.tPoint.pref + sDataAliasRest,
                        HouseParams.fPoint.pref + sDataAliasRest, where, _fields));
            }
        }

        protected override void DeleteDataFromOldBank()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());

                    ExecSQL(string.Format("delete from {0}prm_15 where nzp in ({1})",
                        HouseParams.fPoint.pref + sDataAliasRest, where));
            }
        }

        protected override void RollbackAddedData()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());

                    ExecSQL(string.Format("delete from {0}prm_15 where nzp in ({1})",
                        HouseParams.tPoint.pref + sDataAliasRest, where));
            }
        }

        protected override void RollbackDeletedData()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());

                var _fields = GetFields("prm_15", HouseParams.fPoint.pref + sDataAliasRest);

                    ExecSQL(string.Format(" insert into {0}prm_15({3}) " +
                                          " (select {3} " +
                                          " from {1}prm_15 where nzp in ({2}))",
                        HouseParams.fPoint.pref + sDataAliasRest,
                        HouseParams.tPoint.pref + sDataAliasRest, where, _fields));
            }
        }

        protected override bool CompareDataInTables()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());
                var obj1 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}prm_15 where nzp in ({1})",
                        HouseParams.fPoint.pref + sDataAliasRest, where));
                var obj2 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}prm_15 where nzp in ({1}) and is_actual = 1",
                        HouseParams.tPoint.pref + sDataAliasRest, where));
                if (obj1 != obj2)
                {
                    return false;
                }
            }
            return true;
        }

        protected override bool Compare()
        {
            var oldBankTableField = GetTableFieldsList("prm_15", HouseParams.fPoint.pref + sDataAliasRest);
            var newBankTableField = GetTableFieldsList("prm_15", HouseParams.tPoint.pref + sDataAliasRest);
            if (oldBankTableField.Any(field => !newBankTableField.Contains(field)))
            {
                return false;
            }
            return true;
        }
    }

    [TransferAttributes(Name = "prm_1", Descr = "Перенос параметров ЛС", Priority = TransferPriority.Prm1, Enabled = true)]
    public class TransferPrm1 : Transfer
    {
        protected override void TransferringDataFromOldBank()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());

                var _fields = GetFields("prm_1", HouseParams.fPoint.pref + sDataAliasRest);

                    ExecSQL(string.Format(" insert into {0}prm_1({3}) " +
                                          " (select {3} " +
                                          " from {1}prm_1 where nzp in ({2}))",
                        HouseParams.tPoint.pref + sDataAliasRest,
                        HouseParams.fPoint.pref + sDataAliasRest, where, _fields));
            }
        }

        protected override void DeleteDataFromOldBank()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());

                    ExecSQL(string.Format("delete from {0}prm_1 where nzp in ({1})",
                        HouseParams.fPoint.pref + sDataAliasRest, where));
            }
        }

        protected override void RollbackAddedData()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());

                    ExecSQL(string.Format("delete from {0}prm_1 where nzp in ({1})",
                        HouseParams.tPoint.pref + sDataAliasRest, where));
            }
        }

        protected override void RollbackDeletedData()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());

                var _fields = GetFields("prm_1", HouseParams.fPoint.pref + sDataAliasRest);

                    ExecSQL(string.Format(" insert into {0}prm_1({3}) " +
                                          " (select {3} " +
                                          " from {1}prm_1 where nzp in ({2}))",
                        HouseParams.fPoint.pref + sDataAliasRest,
                        HouseParams.tPoint.pref + sDataAliasRest, where, _fields));

            }
        }

        protected override bool CompareDataInTables()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());
                var obj1 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}prm_1 where nzp in ({1})",
                        HouseParams.fPoint.pref + sDataAliasRest, where));
                var obj2 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}prm_1 where nzp in ({1})",
                        HouseParams.tPoint.pref + sDataAliasRest, where));
                if (obj1 != obj2)
                {
                    return false;
                }
            }
            return true;
        }

        protected override bool Compare()
        {
            var oldBankTableField = GetTableFieldsList("prm_1", HouseParams.fPoint.pref + sDataAliasRest);
            var newBankTableField = GetTableFieldsList("prm_1", HouseParams.tPoint.pref + sDataAliasRest);
            if (oldBankTableField.Any(field => !newBankTableField.Contains(field)))
            {
                return false;
            }
            return true;
        }
    }

    [TransferAttributes(Name = "gilec", Descr = "Перенос жильцов", Priority = TransferPriority.Gilec, Enabled = true)]
    public class TransferGilec : Transfer
    {
        protected override void TransferringDataFromOldBank()
        {
            //сдвиг сиквенса в старом банке на кол-во переносимых записей, чтобы избежать пересечения ключей в случае добавления новых записей
            ExecSQL(string.Format(" SELECT setval ('{0}gilec_nzp_gil_seq'::regclass, ( (SELECT MAX(nzp_gil)::bigint FROM {0}gilec ) " +
                                  " + (SELECT count(*)::integer FROM {0}gilec)) )",
                        HouseParams.fPoint.pref + sDataAliasRest));

            //создаем промежуточную таблицу и сиквенс gilec_arch_new_nzp_gil_seq
            ExecSQL(string.Format(" DROP TABLE {0}gilec_arch", HouseParams.fPoint.pref + sDataAliasRest), false);
            ExecSQL(string.Format(" DROP SEQUENCE IF EXISTS {0}gilec_arch_new_nzp_gil_seq", HouseParams.fPoint.pref + sDataAliasRest), false);
            ExecSQL(string.Format(" CREATE SEQUENCE {0}gilec_arch_new_nzp_gil_seq", HouseParams.fPoint.pref + sDataAliasRest));
            ExecSQL(string.Format(" CREATE TABLE {0}gilec_arch(nzp_gil integer, sogl integer, new_nzp_gil integer default nextval('{0}gilec_arch_new_nzp_gil_seq'::regclass))",
                                  HouseParams.fPoint.pref + sDataAliasRest));

            //формируем новые значения последовательности для поля new_nzp_gil
            ExecSQL(string.Format(" SELECT setval ('{0}gilec_arch_new_nzp_gil_seq'::regclass, (SELECT MAX(nzp_gil)::bigint FROM {1}gilec))",
                        HouseParams.fPoint.pref + sDataAliasRest, HouseParams.tPoint.pref + sDataAliasRest));

            //переносим записи из старого банка в промежуточную таблицу 
            ExecSQL(string.Format(" INSERT INTO {0}gilec_arch(nzp_gil, sogl) SELECT * FROM {0}gilec", HouseParams.fPoint.pref + sDataAliasRest));

            //переносим жильцов из старого банка в новый
            ExecSQL(string.Format(" INSERT INTO {1}gilec(nzp_gil, sogl) SELECT new_nzp_gil, sogl FROM {0}gilec_arch",
                        HouseParams.fPoint.pref + sDataAliasRest, HouseParams.tPoint.pref + sDataAliasRest));
        }

        protected override void DeleteDataFromOldBank()
        {
            
        }

        protected override void RollbackAddedData()
        {
            ExecSQL(string.Format(" DELETE FROM {1}gilec WHERE nzp_gil IN (SELECT new_nzp_gil FROM {0}gilec_arch)",
                        HouseParams.fPoint.pref + sDataAliasRest, HouseParams.tPoint.pref + sDataAliasRest));
        }

        protected override void RollbackDeletedData()
        {
            
        }

        protected override bool CompareDataInTables()
        {
            //var kvarList = GetKvars();
            //if (kvarList.Count > 0)
            //{
            //    var where = string.Join(",", kvarList.ToArray());
            //    var obj1 =
            //        ExecScalar<Int32>(string.Format("select count(*) from {0}prm_1 where nzp in ({1})",
            //            HouseParams.fPoint.pref + sDataAliasRest, where));
            //    var obj2 =
            //        ExecScalar<Int32>(string.Format("select count(*) from {0}prm_1 where nzp in ({1})",
            //            HouseParams.tPoint.pref + sDataAliasRest, where));
            //    if (obj1 != obj2)
            //    {
            //        return false;
            //    }
            //}
            return true;
        }

        protected override bool Compare()
        {
            var oldBankTableField = GetTableFieldsList("gilec", HouseParams.fPoint.pref + sDataAliasRest);
            var newBankTableField = GetTableFieldsList("gilec", HouseParams.tPoint.pref + sDataAliasRest);
            if (oldBankTableField.Any(field => !newBankTableField.Contains(field)))
            {
                return false;
            }
            return true;
        }
    }

    [TransferAttributes(Name = "kart", Descr = "Перенос карточек прибытия/убытия", Priority = TransferPriority.Kart, Enabled = true)]
    public class TransferKart : Transfer
    {
        protected override void TransferringDataFromOldBank()
        {
            var _fields = GetFields("kart", HouseParams.fPoint.pref + sDataAliasRest);
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());

                //создаем промежуточную таблицу
                ExecSQL(string.Format(" DROP TABLE {0}kart_arch", HouseParams.fPoint.pref + sDataAliasRest), false);
                ExecSQL(string.Format(" CREATE TABLE {0}kart_arch AS SELECT " + _fields + " FROM {0}kart WHERE nzp_kvar IN ({1})",
                    HouseParams.fPoint.pref + sDataAliasRest, where));
                
                //добавляем поля для нового кода жильца в промежуточную таблицу 
                ExecSQL(string.Format(" ALTER TABLE {0}kart_arch ADD COLUMN new_nzp_gil integer",
                    HouseParams.fPoint.pref + sDataAliasRest));

                //проставляем новые коды жильцов в промежуточную таблицу 
                ExecSQL(string.Format(" UPDATE {0}kart_arch SET new_nzp_gil = COALESCE((SELECT MAX(a.new_nzp_gil) FROM {0}gilec_arch a WHERE {0}kart_arch.nzp_gil = a.nzp_gil), 0)",
                    HouseParams.fPoint.pref + sDataAliasRest));

                //переносим данные из старого банка в новый
                ExecSQL(string.Format(
                    " INSERT INTO {1}kart(nzp_gil, isactual, ischild, fam, ima, otch, dat_rog, fam_c, ima_c, otch_c, dat_rog_c, " + 
                                         " genotip, gender, tprp, dat_prop, dat_oprp, dat_pvu, who_pvu, dat_svu, nzp_bank, nzp_user, " +
                                         " nzp_tkrt, nzp_kvar, nzp_kv_p, rem_p, namereg, kod_namereg_prn, nzp_nat, nzp_rod, nzp_celp, " +
                                         " nzp_celu, nzp_nkrt, nzp_dok, serij, nomer, vid_mes, vid_dat, nzp_sud, spec, soc_kod, jobpost, " +
                                         " jobname, nzp_lnmr, nzp_stmr, nzp_tnmr, nzp_rnmr, rem_mr, nzp_kvop, nzp_lnop, nzp_stop, nzp_tnop, " +
                                         " nzp_rnop, rem_op, nzp_kvku, nzp_lnku, nzp_stku, nzp_tnku, nzp_rnku, rem_ku, dat_prib, dat_ubit, " +
                                         " dat_sost, dat_ofor, dat_izm, nzp_local, ndees, neuch, is_unl, fam_t, ima_t, otch_t, rem_mr_t, " +
                                         " vid_mes_t, cur_unl, nzp_wp, kod_podrazd, kod_lich, photo, dat_smert, strana_mr, region_mr, okrug_mr, " + 
                                         " gorod_mr, npunkt_mr, strana_op, region_op, okrug_op, npunkt_op, gorod_op, strana_ku, region_ku, okrug_ku, " + 
                                         " gorod_ku, npunkt_ku, dat_fio_c, rodstvo) " +
                    "SELECT new_nzp_gil, isactual, ischild, fam, ima, otch, dat_rog, fam_c, ima_c, otch_c, dat_rog_c, " + 
                                         " genotip, gender, tprp, dat_prop, dat_oprp, dat_pvu, who_pvu, dat_svu, nzp_bank, nzp_user, " +
                                         " nzp_tkrt, nzp_kvar, nzp_kv_p, rem_p, namereg, kod_namereg_prn, nzp_nat, nzp_rod, nzp_celp, " +
                                         " nzp_celu, nzp_nkrt, nzp_dok, serij, nomer, vid_mes, vid_dat, nzp_sud, spec, soc_kod, jobpost, " +
                                         " jobname, nzp_lnmr, nzp_stmr, nzp_tnmr, nzp_rnmr, rem_mr, nzp_kvop, nzp_lnop, nzp_stop, nzp_tnop, " +
                                         " nzp_rnop, rem_op, nzp_kvku, nzp_lnku, nzp_stku, nzp_tnku, nzp_rnku, rem_ku, dat_prib, dat_ubit, " +
                                         " dat_sost, dat_ofor, dat_izm, nzp_local, ndees, neuch, is_unl, fam_t, ima_t, otch_t, rem_mr_t, " +
                                         " vid_mes_t, cur_unl, nzp_wp, kod_podrazd, kod_lich, photo, dat_smert, strana_mr, region_mr, okrug_mr, " + 
                                         " gorod_mr, npunkt_mr, strana_op, region_op, okrug_op, npunkt_op, gorod_op, strana_ku, region_ku, okrug_ku, " + 
                                         " gorod_ku, npunkt_ku, dat_fio_c, rodstvo " +
                    " FROM {0}kart_arch",
                    HouseParams.fPoint.pref + sDataAliasRest, HouseParams.tPoint.pref + sDataAliasRest));
            }
        }

        protected override void DeleteDataFromOldBank()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());

                ExecSQL(string.Format(" DELETE FROM {0}kart WHERE nzp_kvar IN ({1})",
                    HouseParams.fPoint.pref + sDataAliasRest, where));
            }
        }

        protected override void RollbackAddedData()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());

                ExecSQL(string.Format(" DELETE FROM {0}kart WHERE nzp_kvar IN ({1})",
                    HouseParams.tPoint.pref + sDataAliasRest, where));
            }
        }

        protected override void RollbackDeletedData()
        {
            ExecSQL(string.Format(" INSERT INTO {0}kart(nzp_gil, isactual, ischild, fam, ima, otch, dat_rog, fam_c, ima_c, otch_c, dat_rog_c, " + 
                                         " genotip, gender, tprp, dat_prop, dat_oprp, dat_pvu, who_pvu, dat_svu, nzp_bank, nzp_user, " +
                                         " nzp_tkrt, nzp_kvar, nzp_kv_p, rem_p, namereg, kod_namereg_prn, nzp_nat, nzp_rod, nzp_celp, " +
                                         " nzp_celu, nzp_nkrt, nzp_dok, serij, nomer, vid_mes, vid_dat, nzp_sud, spec, soc_kod, jobpost, " +
                                         " jobname, nzp_lnmr, nzp_stmr, nzp_tnmr, nzp_rnmr, rem_mr, nzp_kvop, nzp_lnop, nzp_stop, nzp_tnop, " +
                                         " nzp_rnop, rem_op, nzp_kvku, nzp_lnku, nzp_stku, nzp_tnku, nzp_rnku, rem_ku, dat_prib, dat_ubit, " +
                                         " dat_sost, dat_ofor, dat_izm, nzp_local, ndees, neuch, is_unl, fam_t, ima_t, otch_t, rem_mr_t, " +
                                         " vid_mes_t, cur_unl, nzp_wp, kod_podrazd, kod_lich, photo, dat_smert, strana_mr, region_mr, okrug_mr, " + 
                                         " gorod_mr, npunkt_mr, strana_op, region_op, okrug_op, npunkt_op, gorod_op, strana_ku, region_ku, okrug_ku, " + 
                                         " gorod_ku, npunkt_ku, dat_fio_c, rodstvo) " +
                                  "SELECT nzp_gil, isactual, ischild, fam, ima, otch, dat_rog, fam_c, ima_c, otch_c, dat_rog_c, " + 
                                         " genotip, gender, tprp, dat_prop, dat_oprp, dat_pvu, who_pvu, dat_svu, nzp_bank, nzp_user, " +
                                         " nzp_tkrt, nzp_kvar, nzp_kv_p, rem_p, namereg, kod_namereg_prn, nzp_nat, nzp_rod, nzp_celp, " +
                                         " nzp_celu, nzp_nkrt, nzp_dok, serij, nomer, vid_mes, vid_dat, nzp_sud, spec, soc_kod, jobpost, " +
                                         " jobname, nzp_lnmr, nzp_stmr, nzp_tnmr, nzp_rnmr, rem_mr, nzp_kvop, nzp_lnop, nzp_stop, nzp_tnop, " +
                                         " nzp_rnop, rem_op, nzp_kvku, nzp_lnku, nzp_stku, nzp_tnku, nzp_rnku, rem_ku, dat_prib, dat_ubit, " +
                                         " dat_sost, dat_ofor, dat_izm, nzp_local, ndees, neuch, is_unl, fam_t, ima_t, otch_t, rem_mr_t, " +
                                         " vid_mes_t, cur_unl, nzp_wp, kod_podrazd, kod_lich, photo, dat_smert, strana_mr, region_mr, okrug_mr, " + 
                                         " gorod_mr, npunkt_mr, strana_op, region_op, okrug_op, npunkt_op, gorod_op, strana_ku, region_ku, okrug_ku, " + 
                                         " gorod_ku, npunkt_ku, dat_fio_c, rodstvo " +
                                " FROM {0}kart_arch",
                HouseParams.fPoint.pref + sDataAliasRest));
        }

        protected override bool CompareDataInTables()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());
                var obj1 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}kart where nzp_kvar in ({1})",
                        HouseParams.fPoint.pref + sDataAliasRest, where));
                var obj2 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}kart where nzp_kvar in ({1})",
                        HouseParams.tPoint.pref + sDataAliasRest, where));
                if (obj1 != obj2)
                {
                    return false;
                }
            }
            return true;
        }

        protected override bool Compare()
        {
            var oldBankTableField = GetTableFieldsList("kart", HouseParams.fPoint.pref + sDataAliasRest);
            var newBankTableField = GetTableFieldsList("kart", HouseParams.tPoint.pref + sDataAliasRest);
            if (oldBankTableField.Any(field => !newBankTableField.Contains(field)))
            {
                return false;
            }
            return true;
        }
    }

    [TransferAttributes(Name = "pere_gilec", Descr = "Перенос жильцов", Priority = TransferPriority.PereGilec, Enabled = true)]
    public class TransferPereGilec : Transfer
    {
        protected override void TransferringDataFromOldBank()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());

                var _fields = GetFields("pere_gilec", HouseParams.fPoint.pref + sDataAliasRest);
                    ExecSQL(string.Format(" insert into {0}pere_gilec({3}) " +
                                          " (select {3} " +
                                          " from {1}pere_gilec where nzp_kvar in ({2}))",
                        HouseParams.tPoint.pref + sDataAliasRest,
                        HouseParams.fPoint.pref + sDataAliasRest, where, _fields));
            }
        }

        protected override void DeleteDataFromOldBank()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());

                    ExecSQL(string.Format("delete from {0}pere_gilec where nzp_kvar in ({1})",
                        HouseParams.fPoint.pref + sDataAliasRest, where));
            }
        }

        protected override void RollbackAddedData()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());

                    ExecSQL(string.Format("delete from {0}pere_gilec where nzp_kvar in ({1})",
                        HouseParams.tPoint.pref + sDataAliasRest, where));
            }
        }

        protected override void RollbackDeletedData()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());

                var _fields = GetFields("pere_gilec", HouseParams.fPoint.pref + sDataAliasRest);

                    ExecSQL(string.Format(" insert into {0}pere_gilec({3}) " +
                                          " (select {3} " +
                                          " from {1}pere_gilec where nzp_kvar in ({2}))",
                        HouseParams.fPoint.pref + sDataAliasRest,
                        HouseParams.tPoint.pref + sDataAliasRest, where, _fields));
            }
        }

        protected override bool CompareDataInTables()
        {
            var kvarList = GetKvars();
            if (kvarList.Count > 0)
            {
                var where = string.Join(",", kvarList.ToArray());
                var obj1 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}pere_gilec where nzp_kvar in ({1})",
                        HouseParams.fPoint.pref + sDataAliasRest, where));
                var obj2 =
                    ExecScalar<Int32>(string.Format("select count(*) from {0}pere_gilec where nzp_kvar in ({1})",
                        HouseParams.tPoint.pref + sDataAliasRest, where));
                if (obj1 != obj2)
                {
                    return false;
                }
            }
            return true;
        }

        protected override bool Compare()
        {
            var oldBankTableField = GetTableFieldsList("pere_gilec", HouseParams.fPoint.pref + sDataAliasRest);
            var newBankTableField = GetTableFieldsList("pere_gilec", HouseParams.tPoint.pref + sDataAliasRest);
            if (oldBankTableField.Any(field => !newBankTableField.Contains(field)))
            {
                return false;
            }
            return true;
        }
    }
}
