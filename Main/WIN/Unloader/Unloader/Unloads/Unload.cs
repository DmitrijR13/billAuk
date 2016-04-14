using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Castle.Core.Internal;
using Ionic.Zip;
using Unloader.Models;
using Header = Unloader.Models.Header;

namespace Unloader.Unloads
{
    [AssembleAttribute(RegistrationName = "FormatGovService", FormatName = "Формат данных для портала гос.услуг", Version = "0.9")]
    public class TestUnload : Unload
    {
        public readonly string tmp_ls = "_tmp_ls" + DateTime.Now.Ticks;
        public override void UnloadFromFile(ref object dt)
        {
            SetProgress(0m);
            var date = string.Format("'01.{0}.{1}'", month < 10 ? "0" + month : month.ToString(), year);
            Execute(string.Format("DROP TABLE IF EXISTS {0}; CREATE TEMP TABLE {0}( nzp_kvar integer);", tmp_ls));
            points.pointList.ForEach(point => Execute(string.Format("INSERT INTO {2} SELECT nzp_kvar FROM {0}{1}kvar WHERE pkod>0;", point.pref, sDataAliasRest, tmp_ls)));
            var dict = new Dictionary<string, List<string>>
            {
                {"Файл информационного описания.txt", new List<string> {GetHeader(date)}},
                {"Характеристики жилого фонда.txt", GetThgf(date)},
                {"Начисления и расходы по услугам.txt", GetCharges(date)},
                {"Показания счетчиков.txt", GetCounters(date)},
                {"Платежные реквизиты.txt", GetPaymentAccount(date)},
                {"Оплаты.txt", GetPayment(date)},
                {"Информация органов социальной защиты.txt", GetInformationSocPretender(date)}
            };
            var zipName = string.Format("{0}\\Формат данных для портала гос услуг, Версия 0,9 от {1}", GetPath(), DateTime.Now.ToString("dd-MM-yyyy HH-mm-ss"));
            using (var zip = new ZipFile(Encoding.UTF8))
            {
                dict.ForEach(x => zip.AddEntry(x.Key, string.Join("\r\n", x.Value), Encoding.GetEncoding(1251)));
                zip.Save(zipName);
            }
            SetProgress(1m);
            dt = zipName;
        }

        protected string GetHeader(string date)
        {
            var header = First<Header>(string.Format(@"SELECT payer nameOrgPasses,
			    npayer podrOrgPasses,
			    INN,
			    KPP,
			    (SELECT erc_code FROM {0}{1}s_erc_code where is_current = 1) as raschSchet,
			    {3} fileNumber,
                phone_supp passNumber,
			    name_supp passName,
                {4} chargeDate,
			    {2} lsCount
                FROM {0}{1}s_payer p,{0}{1}supplier s WHERE nzp_payer = 
                (SELECT MAX(nzp_payer_agent) FROM {0}{1}supplier) AND p.nzp_payer = s.nzp_payer_agent", points.pref, sKernelAliasRest,
                    ExecuteScalar<int>("SELECT COUNT(*) FROM " + tmp_ls + ";"), unloadID, date));
            SetProgress(0.05m);
            return header.GetValues();
        }

        protected List<string> GetThgf(string date)
        {
            var temp_table = "_thgf" + DateTime.Now.Ticks;
            Execute(string.Format("DROP TABLE IF EXISTS {0};", temp_table));
            Execute(string.Format(@"create temp table {4} as
                select k.nzp_kvar,
                d.nzp_dom,
                {3} as chargeDate,
                (SELECT erc_code FROM {0}{2}s_erc_code where is_current = 1) as kodRashcCentra,
                k.pkod,
                k.num_ls as numberLS,
                t.town,
                r.rajon as district,
                st.ulica as street,
                d.ndom as houseNumber,
                d.nkor as housingNumber,
                k.nkvar as flatNumber,
                k.nkvar_n as roomNumber,
                k.porch::integer as porchNumber,
                k.fio as ownerName,
                (select area from {0}{1}s_area a where a.nzp_area = d.nzp_area) as managmentCompany,
                null::integer comfort,
                null::integer privatizated,
                null::integer floor,
                null::integer flatOnFloor,
                null::decimal jointFlatSquare,
                null::decimal livingFlatSquare,
                null::decimal heatedFlatSquare,
                null::decimal joinedHouseSquare,
                null::decimal commonPlacesSquare,
                null::decimal heatedHouseSquare,
                null::integer livingCount,
                null::integer temporaryElemenatedCount,
                null::integer temporaryArrivedCount,
                null::integer roomsCount
                from {5} tls,{0}{1}kvar k,{0}{1}dom d,{0}{1}s_ulica st,{0}{1}s_rajon r,{0}{1}s_town t 
                where k.nzp_kvar = tls.nzp_kvar and k.nzp_dom = d.nzp_dom and st.nzp_ul = d.nzp_ul and d.nzp_raj = r.nzp_raj and t.nzp_town = d.nzp_town;",
                points.pref, sDataAliasRest, sKernelAliasRest, date, temp_table, tmp_ls));
            Execute("create index _index_01__thgf_01_kvar on " + temp_table + "(nzp_kvar);");
            Execute("analyze " + temp_table + ";");
            points.pointList.ForEach(point =>
            {
                Execute(string.Format("update {2} set comfort = n.val from (select val_prm::int as val,nzp from {0}{1}prm_1 where nzp_prm = 3 and is_actual<>100 and " +
                                      "dat_s<=" + date + " AND dat_po>=" + date + ") n " +
                                      "where n.nzp={2}.nzp_kvar;", point.pref, sDataAliasRest, temp_table));
                Execute(string.Format("update {2} set privatizated = n.val from (select val_prm::int as val,nzp from {0}{1}prm_1 where nzp_prm = 8 and is_actual<>100 and " +
                                      "dat_s<=" + date + " AND dat_po>=" + date + ") n " +
                                      "where n.nzp={2}.nzp_kvar;", point.pref, sDataAliasRest, temp_table));
                Execute(string.Format("update {2} set floor = n.val from (select val_prm::int as val,nzp from {0}{1}prm_2 where nzp_prm = 37 and is_actual<>100 and " +
                                     "dat_s<=" + date + " AND dat_po>=" + date + ") n " +
                                     "where n.nzp={2}.nzp_dom;", point.pref, sDataAliasRest, temp_table));
                Execute(string.Format("update {2} set jointFlatSquare = n.val from (select val_prm::decimal as val,nzp from {0}{1}prm_1 where nzp_prm = 4 and is_actual<>100 and " +
                                      "dat_s<=" + date + " AND dat_po>=" + date + ") n " +
                                      "where n.nzp={2}.nzp_kvar;", point.pref, sDataAliasRest, temp_table));
                Execute(string.Format("update {2} set livingFlatSquare = n.val from (select val_prm::decimal as val,nzp from {0}{1}prm_1 where nzp_prm = 6 and is_actual<>100 and " +
                                      "dat_s<=" + date + " AND dat_po>=" + date + ") n " +
                                      "where n.nzp={2}.nzp_kvar;", point.pref, sDataAliasRest, temp_table));
                Execute(string.Format("update {2} set heatedFlatSquare = n.val from (select val_prm::decimal as val,nzp from {0}{1}prm_1 where nzp_prm = 133 and is_actual<>100 and " +
                                      "dat_s<=" + date + " AND dat_po>=" + date + ") n " +
                                      "where n.nzp={2}.nzp_kvar;", point.pref, sDataAliasRest, temp_table));
                Execute(string.Format("update {2} set joinedHouseSquare = n.val from (select val_prm::decimal as val,nzp from {0}{1}prm_2 where nzp_prm = 40 and is_actual<>100 and " +
                                      "dat_s<=" + date + " AND dat_po>=" + date + ") n " +
                                      "where n.nzp={2}.nzp_dom;", point.pref, sDataAliasRest, temp_table));
                Execute(string.Format("update {2} set commonPlacesSquare = n.val from (select val_prm::decimal as val,nzp from {0}{1}prm_2 where nzp_prm = 2049 and is_actual<>100 and " +
                                      "dat_s<=" + date + " AND dat_po>=" + date + ") n " +
                                      "where n.nzp={2}.nzp_dom;", point.pref, sDataAliasRest, temp_table));
                Execute(string.Format("update {2} set heatedHouseSquare = n.val from (select val_prm::decimal as val,nzp from {0}{1}prm_2 where nzp_prm = 36 and is_actual<>100 and " +
                                      "dat_s<=" + date + " AND dat_po>=" + date + ") n " +
                                      "where n.nzp={2}.nzp_dom;", point.pref, sDataAliasRest, temp_table));
                Execute(string.Format("update {2} set livingCount = n.val from (select val_prm::int as val,nzp from {0}{1}prm_1 where nzp_prm = 5 and is_actual<>100 and " +
                                      "dat_s<=" + date + " AND dat_po>=" + date + ") n " +
                                      "where n.nzp={2}.nzp_kvar;", point.pref, sDataAliasRest, temp_table));
                Execute(string.Format("update {2} set temporaryElemenatedCount = n.val from (select val_prm::int as val,nzp from {0}{1}prm_1 where nzp_prm = 10 and is_actual<>100 and " +
                                      "dat_s<=" + date + " AND dat_po>=" + date + ") n " +
                                      "where n.nzp={2}.nzp_kvar;", point.pref, sDataAliasRest, temp_table));
                Execute(string.Format("update {2} set temporaryArrivedCount = n.val from (select val_prm::int as val,nzp from {0}{1}prm_2 where nzp_prm = 479 and is_actual<>100 and " +
                                      "dat_s<=" + date + " AND dat_po>=" + date + ") n " +
                                      "where n.nzp={2}.nzp_dom;", point.pref, sDataAliasRest, temp_table));
                Execute(string.Format("update {2} set temporaryElemenatedCount = n.val from (select val_prm::int as val,nzp from {0}{1}prm_1 where nzp_prm = 107 and is_actual<>100 and " +
                                      "dat_s<=" + date + " AND dat_po>=" + date + ") n " +
                                      "where n.nzp={2}.nzp_kvar;", point.pref, sDataAliasRest, temp_table));
            });
            var list = new List<string>();
            var thgfList = Fetch<HarGilFond>("select * from " + temp_table + " order by pkod");
            thgfList.ForEach(x => list.Add(x.GetValues()));
            SetProgress(0.2m);
            return list;
        }

        protected List<string> GetCharges(string date)
        {
            var temp_table = "t_ch_serv" + DateTime.Now.Ticks;
            Execute("drop table if exists " + temp_table + ";");
            Execute(string.Format(@"create temp table {0} (
                nzp_kvar integer,
                nzp_dom integer,
                chargeDate text,
                kodRashcCentra text,
                pkod numeric(13,0),
                services text,
                measure text,
                order_print integer,
                serviceKod integer,
                baseServiceKod integer,
                group_serv text,
                tarif numeric(14,4),
                servExpense numeric(14,5),
                servODNExpensive numeric(14,5),
                ipuExpensive numeric(14,5),
                normExpensive numeric(14,5),
                houseExpensive numeric(14,5),
                flatExpensive numeric(14,5),
                flatIPUExpensive numeric(14,5),
                flatNotIPUExpensive numeric(14,5),
                notLiveExpensive numeric(14,5),
                liftExpensive numeric(14,5),
                houseODNExpensive numeric(14,5),
                odpuExpensive numeric(14,5),
                chargeTarif numeric(14,2),
                chargeTarifNedop numeric(14,2),
                sumNedop numeric(14,2),
                nedopExpensive numeric(14,5),
                countNedop numeric(4,2),
                sumRecalcPrevPeriod numeric(14,2),
                sumChangeSaldo numeric(14,2),
                sumChargePayment numeric(14,2),
                sumPaymentEPD numeric(14,2),
                sumOutsaldo numeric(14,2),
                sumInsaldo numeric(14,2),
                chargeTarifODN numeric(14,2),
                sumOutsaldoODN numeric(14,2),
                sumInsaldoODN numeric(14,2),
                recalcODN numeric(14,2),
                changeSaldoODN numeric(14,2),
                chargePaymentODN numeric(14,2),
                paymentEpdOdn numeric(14,2),
                chargeSocNorm numeric(14,2),
                koeffKorrectIPU numeric(14,7),
                koeffKorrectNorm numeric(14,7)
                );", temp_table));
            Execute("create index index_t_ch_serv_nzp_serv_nzp_kvar on " + temp_table + "(nzp_kvar,serviceKod);");
            Execute("create index index_t_ch_serv_nzp_serv_nzp_dom on " + temp_table + "(nzp_dom,serviceKod);");
            Execute("create index index_t_ch_serv_nzp_kvar on " + temp_table + "(nzp_kvar);");
            Execute("create index index_t_ch_serv_nzp_serv on " + temp_table + "(serviceKod);");
            Execute("analyze " + temp_table + ";");
            points.pointList.ForEach(point =>
            {
                var sql = string.Format(@"insert into {7} 
                    select 
                    k.nzp_kvar,
                    k.nzp_dom,
                    {0},
                    erc.kodRashcCentra,
                    k.pkod,
                    service,
                    measure.measure as measure,
                    ch.order_print as order_print,
                    ch.nzp_serv as serviceKod,
                    null::int as baseServiceKod,
                    grps.name_grpserv group_serv,
                    sum(ch.tarif),
                    sum(ch.c_calc),
                    null servODNExpensive,
                    null ipuExpensive,
                    null normExpensive,
                    null houseExpensive,
                    null flatExpensive,
                    null flatIPUExpensive,
                    null flatNotIPUExpensive,
                    null notLiveExpensive,
                    null liftExpensive,
                    null houseODNExpensive,
                    null odpuExpensive,
                    sum(rsum_tarif) chargeTarif,
                    sum(sum_tarif) chargeTarifNedop,
                    sum(sum_nedop) sumNedop,
                    sum((case when tarif = 0 then 0 else sum_nedop/tarif end)) nedopExpensive,
                    null countNedop,
                    sum(reval) sumRecalcPrevPeriod,
                    sum(real_charge) sumChangeSaldo,
                    sum(sum_charge) sumChargePayment,
                    sum(sum_money) sumPaymentEPD,
                    sum(sum_outsaldo) sumOutsaldo,
                    sum(sum_insaldo) sumInsaldo,
                    null chargeTarifODN,
                    null sumOutsaldoODN,
                    null sumInsaldoODN,
                    null recalcODN,
                    null changeSaldoODN,
                    null chargePaymentODN,
                    null paymentEpdOdn,
                    sum(sum_tarif_sn_f) chargeSocNorm,
                    null koeffKorrectIPU,
                    null koeffKorrectNorm
                        from {8} tls,{1}{6}kvar k, (SELECT max(erc_code) as kodRashcCentra  FROM {1}{2}s_erc_code where is_current = 1) as erc,
                        {1}{2}services s,{1}{2}s_measure measure, {3}_charge_{4}.charge_{5} ch left outer join {3}{2}grpserv_schet grpss on grpss.nzp_serv = ch.nzp_serv
                        left outer join {1}{2}s_grpserv grps on grpss.nzp_grpserv =grps.nzp_grpserv 
                        where tls.nzp_kvar = ch.nzp_kvar and k.nzp_kvar = tls.nzp_kvar 
                        and s.nzp_serv = ch.nzp_serv  and k.nzp_kvar = ch.nzp_kvar 
                        and measure.nzp_measure = s.nzp_measure  
                        and ch.nzp_serv>1 and ch.dat_charge is null 
                        group by 1,2,3,4,5,6,7,8,9,10,11;", date, points.pref, sKernelAliasRest, point.pref, year.ToString(CultureInfo.InvariantCulture).Substring(2, 2), month.ToString("00"), sDataAliasRest, temp_table, tmp_ls);
                Execute(sql);

                Execute(string.Format(@"update {5} t set 
                        servODNExpensive = n.c_calc
                            from 
                        ( select sum(c_calc) c_calc,ch.nzp_serv,ch.nzp_kvar from  {0}_charge_{1}.charge_{2} ch,
                       {3}{4}serv_odn sodn where sodn.nzp_serv_link = ch.nzp_serv and ch.nzp_serv>1 and ch.dat_charge is null group by 2,3) n
                        where n.nzp_serv = t.serviceKod and n.nzp_kvar = t.nzp_kvar;", point.pref, year.ToString(CultureInfo.InvariantCulture).Substring(2, 2), month.ToString("00"), points.pref, sKernelAliasRest, temp_table));

                Execute(string.Format(@"update {5} t set 
                        servODNExpensive = n.c_calc,
                        chargeTarifODN = n.rsum_tarif,
                        sumOutsaldoODN = n.sum_outsaldo,
                        sumInsaldoODN = n.sum_insaldo,
                        recalcODN = n.reval,
                        changeSaldoODN = n.real_charge,
                        chargePaymentODN = n.sum_charge,
                        paymentEpdOdn = n.sum_money
                          from 
                        ( select sum(c_calc) c_calc,sum(rsum_tarif) rsum_tarif,sum(sum_outsaldo) sum_outsaldo,sum(sum_insaldo) sum_insaldo,sum(reval) reval,sodn.nzp_serv_link nzp_serv,ch.nzp_kvar,
                        sum(real_charge) real_charge,sum(sum_charge) sum_charge,sum(sum_money) sum_money
                        from  {0}_charge_{1}.charge_{2} ch,
                        {3}{4}serv_odn sodn where sodn.nzp_serv_link = ch.nzp_serv and ch.nzp_serv>1 and ch.dat_charge is null group by 6,7) n
                        where n.nzp_serv = t.serviceKod and n.nzp_kvar = t.nzp_kvar;", point.pref, year.ToString(CultureInfo.InvariantCulture).Substring(2, 2), month.ToString("00"), points.pref, sKernelAliasRest, temp_table));

                Execute(string.Format(@"update {3} t set ipuExpensive = n.c_calc  from 
                        ( select sum(c_calc) c_calc,ch.nzp_serv,ch.nzp_kvar from  {0}_charge_{1}.charge_{2} ch where (ch.is_device = 1  or ch.is_device = 9) 
                        and ch.nzp_serv>1 and ch.dat_charge is null group by 2,3) n
                        where n.nzp_serv = t.serviceKod and n.nzp_kvar = t.nzp_kvar;", point.pref, year.ToString(CultureInfo.InvariantCulture).Substring(2, 2), month.ToString("00"), temp_table));

                Execute(string.Format(@"update {3} t set normExpensive = n.c_calc  from 
                        ( select sum(c_calc) c_calc,ch.nzp_serv,ch.nzp_kvar from  {0}_charge_{1}.charge_{2} ch where (ch.is_device = 0) 
                        and ch.nzp_serv>1 and ch.dat_charge is null group by 2,3) n
                        where n.nzp_serv = t.serviceKod and n.nzp_kvar = t.nzp_kvar;", point.pref, year.ToString(CultureInfo.InvariantCulture).Substring(2, 2), month.ToString("00"), temp_table));

                Execute(string.Format(@"update {3} t set
                         houseExpensive = n.value1,
                         flatExpensive = n.value2,
                         flatIPUExpensive = n.value3,
                         flatNotIPUExpensive = n.value4,
                         houseODNExpensive = n.value5,
                         odpuExpensive = n.value6,
                         koeffKorrectIPU = n.value7,
                         koeffKorrectNorm = n.value8
                         from 
                        (select val3 as value1,val1+val2 as value2,val2 as value3,val1 as value4,val3-val2-val1 value5,val4 value6,kf307 value7,kf307n value8,c.nzp_serv,c.nzp_dom 
                        from  {0}_charge_{1}.counters_{2} c where c.stek = 3 and c.nzp_type = 1 ) n
                        where n.nzp_serv = t.serviceKod and n.nzp_dom = t.nzp_dom;", point.pref, year.ToString(CultureInfo.InvariantCulture).Substring(2, 2), month.ToString("00"), temp_table));

                Execute(string.Format(@"update {3} t set
                         notLiveExpensive = n.value
                         from 
                        (select sum(ngp_cnt) as value,c.nzp_serv,c.nzp_dom from {0}_charge_{1}.counters_{2} c where c.stek = 3 and c.nzp_type = 1 group by 2,3) n
                        where n.nzp_serv = t.serviceKod and n.nzp_dom = t.nzp_dom;", point.pref, year.ToString(CultureInfo.InvariantCulture).Substring(2, 2), month.ToString("00"), temp_table));

                Execute(string.Format(@"update {3} t set normExpensive = n.value  from 
                        ( select sum(EXTRACT(epoch FROM cnts)/3600/24-EXTRACT(epoch FROM cnts_del)/3600/24) as value,ch.nzp_kvar 
                          from {0}_charge_{1}.nedo_{2} ch where ch.dat_charge is null group by 2) n
                        where n.nzp_kvar = t.nzp_kvar;", point.pref, year.ToString(CultureInfo.InvariantCulture).Substring(2, 2), month.ToString("00"), temp_table));
            });
            var list = new List<string>();
            var chargeList = Fetch<ServiceCharges>("select * from " + temp_table + " order by pkod");
            chargeList.ForEach(x => list.Add(x.GetValues()));
            SetProgress(0.5m);
            return list;
        }

        protected List<string> GetCounters(string date)
        {
            var temp_table = "t_counters" + DateTime.Now.Ticks;
            Execute("drop table if exists " + temp_table + ";");
            Execute(string.Format(@"create temp table {0}(
                    nzp_kvar integer,
                    nzp_dom integer,
                    nzp_counter integer,
                    nzp_serv integer,
                    chargeDate text,
                    kodRashcCentra text,
                    pkod decimal,
                    services text,
                    measure text,
                    orderCounter text,
                    counterType integer,
                    numberCounter text,
                    counterKod integer,
                    counterDate date,
                    counterValue numeric(14,5),
                    counterPrevDate date,
                    counterPrevValue numeric(14,5),
                    resizer numeric(5,2),
                    expensive numeric(14,5),
                    addedExpensive numeric(14,5),
                    notLiveExpensive numeric(14,5),
                    liftExpensive numeric(14,5),
                    counterPlace text
                    );", temp_table));

            points.pointList.ForEach(point =>
            {
                Execute(string.Format(@"insert into {5}
                        select 
                        k.nzp_kvar,
                        k.nzp_dom,
                        c.nzp_counter,
                        c.nzp_serv,
                        {0},
                        erc.kodRashcCentra,
                        k.pkod,
                        service services,
                        measure.measure as measure,
                        s.ordering,
                        cs.nzp_type,
                        cs.num_cnt num_cnt,
                        cs.nzp_cnttype,
                        max(c.dat_uchet),
                        c.val_cnt,
                        null::date,
                        null,
                        null resizer,
                        null expensive
                        from {6} tls,{1}{4}kvar k, (SELECT max(erc_code) as kodRashcCentra  FROM {1}{2}s_erc_code where is_current = 1) as erc,
                          {1}{2}services s,{1}{2}s_measure measure,{3}{4}counters c left outer join {3}{4}counters_spis cs on cs.nzp_counter = c.nzp_counter 
                          where k.nzp_kvar = tls.nzp_kvar 
                         and s.nzp_serv = c.nzp_serv 
                         and c.nzp_kvar = tls.nzp_kvar
                         and c.nzp_serv>1 
                         and c.is_actual = 1
                         and c.dat_uchet = {0}
                         and measure.nzp_measure = s.nzp_measure group by 1,2,3,4,5,6,7,8,9,10,11,12,13,15,16 order by 2,1;", date, points.pref, sKernelAliasRest, point.pref, sDataAliasRest, temp_table, tmp_ls));

                Execute(string.Format(@"update {3} t set 
                        counterPrevDate = dat,
                        counterPrevValue =value
                        from (select dat_uchet as dat,val_cnt as value,nzp_counter,nzp_kvar,nzp_serv from {0}{1}counters where is_actual = 1 and dat_uchet < {2} ) t1
                        where t.nzp_kvar = t1.nzp_kvar and t.nzp_serv = t1.nzp_serv;", point.pref, sDataAliasRest, date, temp_table));

                Execute(string.Format(@"update {3} t set 
                        resizer = t1.resizer,
                        expensive =t1.expensive
                        from (select mmnog as resizer,rashod as expensive,nzp_counter,nzp_kvar,nzp_serv from {0}_charge_{1}.counters_{2} ) t1
                        where t.nzp_kvar = t1.nzp_kvar and t.nzp_serv = t1.nzp_serv;", point.pref, year.ToString(CultureInfo.InvariantCulture).Substring(2, 2), month.ToString("00"), temp_table));

                //                Execute(string.Format(@"insert into {5}
                //                        select 
                //                        null::integer,
                //                        k.nzp_dom,
                //                        c.nzp_counter,
                //                        c.nzp_serv,
                //                        {0},
                //                        erc.kodRashcCentra,
                //                        null::integer,
                //                        service services,
                //                        measure.measure as measure,
                //                        s.ordering,
                //                        cs.nzp_type,
                //                        cs.num_cnt num_cnt,
                //                        cs.nzp_cnttype,
                //                        max(c.dat_uchet),
                //                        c.val_cnt,
                //                        null::date,
                //                        null::numeric,
                //                        null::numeric resizer,
                //                        null::numeric expensive,
                //                        null::numeric,
                //                        ngp_cnt,
                //                        ngp_lift
                //                        from {6} tls,{1}{4}kvar k, (SELECT max(erc_code) as kodRashcCentra  FROM {1}{2}s_erc_code where is_current = 1) as erc,
                //                          {1}{2}services s,{1}{2}s_measure measure,{3}{4}counters_dom c left outer join {3}{4}counters_spis cs on cs.nzp_counter = c.nzp_counter 
                //                          where k.nzp_kvar = tls.nzp_kvar 
                //                         and s.nzp_serv = c.nzp_serv 
                //                         and c.nzp_dom = k.nzp_dom
                //                         and c.nzp_serv>1 
                //                         and c.is_actual = 1
                //                         and c.dat_uchet = {0}
                //                         and measure.nzp_measure = s.nzp_measure group by 1,2,3,4,5,6,7,8,9,10,11,12,13,15,16,17,18,19,20,21,22 order by 2,1;",
                //                          date, points.pref, sKernelAliasRest, point.pref, sDataAliasRest,temp_table,tmp_ls));

                //                Execute(string.Format(@"update {3} t set 
                //                        counterPrevDate = dat,
                //                        counterPrevValue =value
                //                        from (select dat_uchet as dat,val_cnt as value,nzp_counter,nzp_dom,nzp_serv from {0}{1}counters_dom where is_actual = 1 and dat_uchet < {2} ) t1
                //                        where t.nzp_dom = t1.nzp_dom and t.nzp_serv = t1.nzp_serv;", point.pref, sDataAliasRest, date,temp_table));

                //                Execute(string.Format(@"update {3} t set 
                //                        resizer = t1.resizer,
                //                        expensive =t1.expensive
                //                        from (select mmnog as resizer,rashod as expensive,nzp_counter,nzp_dom,nzp_serv from {0}_charge_{1}.counters_{2}  ) t1
                //                        where t.nzp_dom = t1.nzp_dom and t.nzp_serv = t1.nzp_serv;", point.pref, year.ToString(CultureInfo.InvariantCulture).Substring(2, 2), month.ToString("00"),temp_table));
            });
            var list = new List<string>();
            var counterList = Fetch<Counters>("select * from " + temp_table + " order by pkod");
            counterList.ForEach(x => list.Add(x.GetValues()));
            SetProgress(0.7m);
            return list;
        }

        public List<string> GetPaymentAccount(string date)
        {
            var sql = string.Format(@"select 
                distinct
                {0} chargeDate,
                erc.kodRashcCentra,
                pkod,
                1 lineTypeReqvezit,
                supplier.payer receiverName,
                supplier.bank_name receiverBankName,
                supplier.rcount rashSchetReceiver,
                supplier.kcount korSchetBankReceiver,
                supplier.bik bikBankReceiver,
                supplier.adres_supp receiverAddress,
                supplier.phone_supp receiverPhone,
                supplier.email receiverEmail,
                null receiverComment,
                agent.payer performerName,
                agent.bank_name performerBankName,
                agent.rcount rashSchetPerformer,
                agent.kcount korSchetBankPerformer,
                agent.bik bikBankPerformer,
                null performerAddress,
                null performerPhone,
                null performerEmail,
                null performerComment,
                erc.kodRashcCentra ukKod,
                null freedomText
                    from {4} tls,
                    (SELECT max(erc_code) as kodRashcCentra  FROM {1}{2}s_erc_code where is_current = 1) as erc,
                    (select distinct k.pkod ,sp.payer,k.nzp_kvar,nzp_payer_agent,bank_name,fnb.rcount,fnb.kcount,fnb.bik,name_supp,adres_supp,phone_supp,email
                    from {1}{3}dom d,{1}{3}s_area a,{1}{3}kvar k,{1}{2}supplier supp,{1}{2}s_payer sp 
                    left outer join {1}{3}fn_bank fnb on fnb.nzp_payer = sp.nzp_payer
                    where d.nzp_dom = k.nzp_dom and d.nzp_area = a.nzp_area and a.nzp_payer = sp.nzp_payer and sp.nzp_payer = supp.nzp_payer_supp and k.pkod>0) supplier,
                    (select distinct sp.nzp_payer,payer,bank_name,fnb.rcount,fnb.kcount,fnb.bik from {1}{2}s_payer sp left outer join {1}{3}fn_bank 
                fnb on fnb.nzp_payer = sp.nzp_payer) agent
                    where supplier.nzp_kvar = tls.nzp_kvar and agent.nzp_payer = supplier.nzp_payer_agent order by pkod", date, points.pref, sKernelAliasRest, sDataAliasRest, tmp_ls);
            var list = new List<string>();
            var accountList = Fetch<PaymentAccount>(sql);
            accountList.ForEach(x => list.Add(x.GetValues()));
            SetProgress(0.8m);
            return list;
        }

        public List<string> GetPayment(string date)
        {
            var sql = string.Format(@"select 
                {0} chargeDate,
                (case when pl.erc_code is null then erc.kodRashcCentra else pl.erc_code end),
                k.pkod,
                pl.dat_vvod::text paymentDate,
                pl.dat_uchet::text paymentRegistrationDate,
                pl.dat_month::text paymentMonth,
                pl.g_sum_ls paymentSum,
                sb.bank paymentPlace
                from (SELECT max(erc_code) as kodRashcCentra  FROM {1}{2}s_erc_code where is_current = 1) as erc, 
                {1}_fin_{3}.pack_ls pl,{5} tls,{1}{4}kvar k,{1}_fin_{3}.pack p,{1}{2}s_bank sb
                where dat_month<={0} and tls.nzp_kvar= k.nzp_kvar and k.num_ls = pl.num_ls and p.nzp_pack = pl.nzp_pack and p.nzp_bank = sb.nzp_bank
                order by pkod,paymentmonth;", date, points.pref, sKernelAliasRest, year.ToString(CultureInfo.InvariantCulture).Substring(2, 2), sDataAliasRest, tmp_ls);
            var list = new List<string>();
            var paymentList = Fetch<Payment>(sql);
            paymentList.ForEach(x => list.Add(x.GetValues()));
            SetProgress(0.9m);
            return list;
        }

        public List<string> GetInformationSocPretender(string date)
        {
            var temp_table = "_tmp_pretender" + DateTime.Now.Ticks;
            Execute(string.Format("drop table if exists {0};", temp_table));
            Execute(string.Format(@"CREATE TEMP TABLE {0}(
	            chargeDate text,
	            kodRashcCentra text,
	            pkod numeric,
	            receiverName text,
	            financeArticle text,
	            financeArticleGroup text,
	            chargeSum numeric,
	            paymentSum numeric,
	            paymentPlace text,
	            subsidyFinanceDate text
            );	", temp_table));
            points.pointList.ForEach(point => Execute(string.Format(@" 
            INSERT INTO {7}
            SELECT 
	            {0} chargeDate,
	            erc.kodRashcCentra,
	            k.pkod,
                COALESCE(fam,'')||' '||COALESCE(ima,'')||' '||COALESCE(otch,'') receiverName,
                bfe.expence financeArticle,
                (CASE when fn.nzp_exp=6 then 'ЛЬГОТА'
                              when ((fn.nzp_exp>10) and (fn.nzp_exp<20))or(fn.nzp_exp=128) then 'СМО'
                              when (fn.nzp_exp>0 and fn.nzp_exp<11 and fn.nzp_exp<>3 and fn.nzp_exp<>6)or(fn.nzp_exp=21) then 'ЕДВ'
                              when fn.nzp_exp=28 then 'Компенсация за тепло'
                              when fn.nzp_exp in (120) then 'Компенсация за тепло' 
                              when fn.nzp_exp in (3,22) then 'Льгота на связь' END) financeArticleGroup,
                sum(sum_must) chargeSum,
                sum(sum_charge) paymentSum,
                bank paymentPlace,
                start_subsidy subsidyFinanceDate
            FROM {1}_charge_{2}.calc_sz_fin_01 fn,(SELECT max(erc_code) as kodRashcCentra  FROM {3}{4}s_erc_code where is_current = 1) as erc,
                {3}{5}kvar k,{3}{4}bf_expence bfe, {6} tls
            WHERE tls.nzp_kvar = k.nzp_kvar AND
                k.num_ls = fn.num_ls AND
                fn.nzp_exp = bfe.nzp_exp AND
                ABS(sum_charge)>0.001    
                AND ((fn.nzp_exp>0 AND fn.nzp_exp<23) OR (fn.nzp_exp IN (6,28,35,36,137,120))) 
           GROUP BY 1,2,3,4,5,6,9,10
           ORDER BY pkod;", date, point.pref, year.ToString(CultureInfo.InvariantCulture).Substring(2, 2), points.pref, sKernelAliasRest, sDataAliasRest, tmp_ls, temp_table)));

            var list = new List<string>();
            var socList = Fetch<InformationSocPretender>(string.Format("select * from {0};", temp_table));
            socList.ForEach(x => list.Add(x.GetValues()));
            SetProgress(0.95m);
            return list;
        }
    }

}
