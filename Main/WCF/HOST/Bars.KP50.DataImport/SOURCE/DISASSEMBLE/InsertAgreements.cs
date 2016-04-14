using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.SOURCE.DISASSEMBLE
{
    public class DBInsertAgreements: DataBaseHeadServer
    {
        public Returns InsertAgreements(FilesDisassemble finder)
        {
            Returns ret = new Returns();
            MonitorLog.WriteLog("Старт разбора соглашений (ф-ция InsertAgreements)", MonitorLog.typelog.Info, true);

            string sql =
                " INSERT INTO " + Points.Pref + sDataAliasRest + "fn_percent_dom" +
                " (nzp_payer, nzp_supp, nzp_serv, nzp_area, perc_ud, dat_s, dat_po, nzp_dom, nzp_serv_from, changed_by, changed_on)" +
                " SELECT DISTINCT fu1.nzp_payer, fdog.nzp_supp, fs1.nzp_serv, fu2.nzp_payer," +
                " fa.percent, fa.dat_s, fa.dat_po, fdom.nzp_dom, fs2.nzp_serv, " + finder.nzp_user + ", now() " +
                " FROM " + Points.Pref + sUploadAliasRest + "file_agreement fa," +
                Points.Pref + sUploadAliasRest + "file_dog fdog," +
                Points.Pref + sUploadAliasRest + "file_dom fdom," +
                Points.Pref + sUploadAliasRest + "file_services fs1," +
                Points.Pref + sUploadAliasRest + "file_services fs2," +
                Points.Pref + sUploadAliasRest + "file_urlic fu1," +
                Points.Pref + sUploadAliasRest + "file_urlic fu2" +
                " WHERE fa.id_dog = fdog.dog_id AND fa.nzp_file = fdog.nzp_file" +
                " AND (fa.id_dom" + sConvToNum + ") = (fdom.id" + sConvToNum + ") AND fa.nzp_file = fdom.nzp_file" +
                " AND fa.id_serv_to = fs1.id_serv AND fa.nzp_file = fs1.nzp_file" +
                " AND fa.id_serv_from = fs2.id_serv AND fa.nzp_file = fs2.nzp_file" +
                " AND fa.id_urlic_agent = fu1.urlic_id and fa.nzp_file = fu1.nzp_file" +
                " AND fdom.area_id = fu2.urlic_id and fa.nzp_file = fu2.nzp_file" +
                " AND NOT EXISTS" +
                " (SELECT 1 FROM " + Points.Pref + sDataAliasRest + "fn_percent_dom" +
                " WHERE nzp_payer = fu1.nzp_payer AND nzp_dom = fdom.nzp_dom " +
                " AND nzp_supp = fdog.nzp_supp AND nzp_serv = fs1.nzp_serv" +
                " AND nzp_serv_from = fs2.nzp_serv)";
            ret = ExecSQL(sql);

            return ret;
        }
    }
}
