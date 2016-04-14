using KP50.DataBase.Migrator.Framework;
using System.Collections.Generic;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014124
{
    [Migration(2014122212401, MigrateDataBase.CentralBank)]
    public class Migration_2014122212401_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Supg);
            var lstQuerys = new List<string>();

            if (Database.ProviderName == "PostgreSQL")
            {
                lstQuerys = new List<string>
                {
                    "CREATE OR REPLACE FUNCTION " + CurrentSchema + ".gen_zvk_res(pzvk_no integer) " +
                    " RETURNS SETOF record AS " +
                    " $BODY$ " +

                    " DECLARE mx_exec_date DATE; " +
                    " DECLARE mx_fact_date DATE; " +
                    " DECLARE xExec_date DATE; " +
                    " DECLARE xFact_date DATE; " +
                    " DECLARE xResult_no INTEGER; " +
                    " DECLARE xReaddress FLOAT; " +
                    " DECLARE xPlan_no INTEGER; " +
                    " DECLARE xZvkResult_no INTEGER; " +

                    " DECLARE cResUndef INTEGER; " +
                    " DECLARE cResPlan INTEGER; " +
                    " DECLARE cResDone INTEGER; " +
                    " DECLARE cResOtm INTEGER; " +
                    " DECLARE cResDoing INTEGER; " +
                    " DECLARE cResReaddress INTEGER; " +
                    " DECLARE xReaddressCount FLOAT; " +
                    " DECLARE CurResult_no INTEGER; " +

                    " BEGIN " +
                    " cResUndef = 1; " +
                    " cResPlan = 2; " +
                    " cResDone = 3; " +
                    " cResOtm = 4; " +
                    " cResDoing = 5; " +
                    " cResReaddress = 6; " +
                    " CurResult_no = cResUndef; " +

                    " SELECT z.nzp_res , z.exec_date, z.fact_date " +
                    " INTO xZvkResult_no, xExec_date, xFact_date " +
                    " FROM " + CurrentSchema + ".zvk z " +
                    " WHERE z.nzp_zvk=pZvk_no; " +

                    " SELECT max(z.exec_date), max(z.fact_date), max(z.nzp_res) " +
                    " INTO mx_exec_date, mx_fact_date, xResult_no " +
                    " FROM " + CurrentSchema + ".zakaz z " +
                    " WHERE z.nzp_zvk=pZvk_no AND z.nzp_res in (cResDone, cResDoing, cResPlan); " +

                    " IF (xResult_no=cResDoing)OR(xResult_no=cResPlan) THEN mx_fact_date=null; END IF; " +

                    " SELECT count(*) " +
                    " INTO xReaddressCount " +
                    " FROM " + CurrentSchema + ".readdress " +
                    " WHERE readdress.nzp_zvk=pZvk_no; " +

                    " IF xReaddressCount<>0 THEN CurResult_no = cResReaddress; END IF; " +
                    " IF mx_exec_date IS NOT NULL THEN " +
                    "    IF mx_fact_date IS  NULL THEN " +
                    "      CurResult_no = xResult_no; " +
                    "    ELSE " +
                    "     CurResult_no = cResDone; " +
                    "    END IF; " +
                    " END IF; " +

                    " IF (xZvkResult_no=cResOtm) AND (CurResult_no=cResUndef) THEN " +
                    "      CurResult_no = cResOtm; " +
                    " END IF; " +

                    " UPDATE " + CurrentSchema + ".zvk " +
                    " SET exec_date=mx_exec_date, " +
                    "     fact_date=mx_fact_date, " +
                    "     nzp_res=CurResult_no " +
                    " WHERE nzp_zvk=pZvk_no; " +

                    " RETURN QUERY SELECT mx_exec_date, mx_fact_date, CurResult_no; " +

                    " END;$BODY$ " +
                    "   LANGUAGE plpgsql VOLATILE " +
                    "   COST 100 " +
                    "   ROWS 1000; " 
                };
                foreach (var query in lstQuerys) Database.ExecuteNonQuery(query);

                lstQuerys = new List<string>
                {
                   " ALTER FUNCTION " + CurrentSchema + ".gen_zvk_res(integer) OWNER TO postgres; "
                };
                foreach (var query in lstQuerys) Database.ExecuteNonQuery(query);
                
                lstQuerys = new List<string>
                {
                   "DROP FUNCTION " + CurrentSchema + ".get_series(integer)"
                };
                foreach (var query in lstQuerys) Database.ExecuteNonQuery(query);

                lstQuerys = new List<string>
                {
                   " CREATE OR REPLACE FUNCTION " + CurrentSchema + ".get_series(IN pkod integer) " +
                   " RETURNS TABLE(cur_val integer, ret_message character) AS " +
                   " $BODY$  " +
                   " DECLARE	xCurVal INTEGER ; " +
                   " DECLARE	xRetErr INTEGER ;  " +
                   " DECLARE	xRetMess CHAR (255) ;  " +
                   " DECLARE	xVMin INTEGER ;  " +
                   " DECLARE	xVMax INTEGER ;  " +
                   " DECLARE	xVer INTEGER ; " +
                   " BEGIN " +
                   " 	xVer = 1 ; " +
                   " 	IF pKod = 9325 THEN " +
                   " 	RETURN QUERY SELECT xVer,	'Номер версии ' || xVer::TEXT ; " +
                   "   END IF ; " +
                   "   xRetErr =- 1 ; xRetMess = 'Ошибка блокирования series' ; xRetErr =- 2 ; xRetMess = 'Ошибка обращения к series' ; xCurVal = 0 ;  " +
                   "   SELECT " +
                   " 			COALESCE (s.v_min, 0), " +
                   " 			COALESCE (s.v_max, 0), " +
                   "   		s.cur_val  " +
                   "   INTO xVMin, " +
                   " 			xVMax, " +
                   " 			xCurVal " +
                   " 	FROM " +
                   " 		" + CurrentSchema + ".series s " +
                   " 	WHERE " +
                   " 		s.kod = pKod ; " +
                   " 	IF COALESCE (xCurVal, 0) = 0 THEN " +
                   " 	RETURN QUERY SELECT	- 3,'Внутренняя ошибка series' ; " +
                   " 	END	IF ; " +
                   " 	IF NOT xCurVal BETWEEN xVMin " +
                   " 	AND xVMax THEN " +
                   " 	RETURN QUERY SELECT - 4,'Недопустимые значения series' ; " +
                   " 	END	IF ;  " +
                   "   xRetErr =- 5 ; xRetMess = 'Ошибка изменения series' ;  " +
                   "   UPDATE " + CurrentSchema + ".series	SET cur_val = xCurVal + 1 " +
                   " 	WHERE	kod = pKod ; xRetErr =- 6 ; xRetMess = 'Ошибка сохранения series' ;  " +
                   "   RETURN QUERY SELECT	xCurVal,''::char(255) ;  " +
                   "   EXCEPTION WHEN OTHERS THEN RETURN QUERY SELECT " +
                   " 											xRetErr, " +
                   " 											xRetMess ; " +
                   " END; $BODY$ " +
                   "   LANGUAGE plpgsql VOLATILE " +
                   "   COST 100 " +
                   "   ROWS 1000; "
                };
                foreach (var query in lstQuerys) Database.ExecuteNonQuery(query);

                lstQuerys = new List<string>
                {
                   " ALTER FUNCTION " + CurrentSchema + ".get_series(integer) OWNER TO postgres;"
                };
                foreach (var query in lstQuerys) Database.ExecuteNonQuery(query);

            
                lstQuerys = new List<string>
                {
                   " CREATE OR REPLACE FUNCTION " + CurrentSchema + ".lock_zk(pnzpuser integer, pnzp integer) " +
                    " RETURNS integer AS " +
                    " $BODY$ " +

                    " DECLARE xCnt integer; " +
                    " DECLARE xDateMinus20Min timestamp; " +
                    " BEGIN " +
                    " set search_path to '" + CurrentSchema + "'; " +
                    " xDateMinus20Min=(now() - interval '1 minutes');  " +
                  
                    " delete from  zk_lock where nzp_=pNzp and date_<=xDateMinus20Min;  " +

                    " update zk_lock set nzp_=pNzp, date_=now() where nzp_user=pNzpUser;  " +
                    " GET DIAGNOSTICS xCnt = ROW_COUNT; " +
                    " if xCnt=1 then return 1; end if;  " +
                 
                    " update zk_lock set nzp_user=pNzpUser, date_=now() where nzp_=pNzp and date_<=xDateMinus20Min;  " +
                    " if xCnt=1 then return 1; end if;  " +
                 
                    " insert into zk_lock (nzp_user, nzp_, date_) values (pNzpUser, pNzp, now()); " +
                    " GET DIAGNOSTICS xCnt = ROW_COUNT; " +
                    " return xCnt; " +
                
                    " END; $BODY$ " +
                    "   LANGUAGE plpgsql VOLATILE " +
                    "   COST 100; "
                };
                foreach (var query in lstQuerys) Database.ExecuteNonQuery(query);

                lstQuerys = new List<string>
                {
                    " ALTER FUNCTION " + CurrentSchema + ".lock_zk(integer, integer) OWNER TO postgres; "
                };
                foreach (var query in lstQuerys) Database.ExecuteNonQuery(query);

                lstQuerys = new List<string>
                {
                    " CREATE OR REPLACE FUNCTION " + CurrentSchema + ".lock_zvk(pnzpuser integer, pnzp integer) " +
                    " RETURNS integer AS " +
                    " $BODY$ " +

                    " DECLARE xCnt integer; " +
                    " DECLARE xDateMinus20Min timestamp; " +
                    " BEGIN " +
                    " set search_path to '" + CurrentSchema + "'; " +
                    " xDateMinus20Min=(now() - interval '1 minutes');  " +
                 
                    " delete from zvk_lock where nzp_=pNzp and date_<=xDateMinus20Min;  " +

                    " update zvk_lock set nzp_=pNzp, date_=now() where nzp_user=pNzpUser;  " +
                    " GET DIAGNOSTICS xCnt = ROW_COUNT; " +
                    " if xCnt=1 then return 1; end if;  " +
               
                    " update zvk_lock set nzp_user=pNzpUser, date_=now() where nzp_=pNzp and date_<=xDateMinus20Min;  " +
                    " GET DIAGNOSTICS xCnt = ROW_COUNT; " +
                    " if xCnt=1 then return 1; end if;  " +
                
                    " insert into zvk_lock (nzp_user, nzp_, date_) values (pNzpUser, pNzp, now()); " +
                    " GET DIAGNOSTICS xCnt = ROW_COUNT; " +
                    " return xCnt; " +
                
                    " END; $BODY$ " +
                    "   LANGUAGE plpgsql VOLATILE " +
                    "   COST 100; "
                };
                foreach (var query in lstQuerys) Database.ExecuteNonQuery(query);

                lstQuerys = new List<string>
                { 
                    " ALTER FUNCTION " + CurrentSchema + ".lock_zvk(integer, integer) OWNER TO postgres; "
                };
                foreach (var query in lstQuerys) Database.ExecuteNonQuery(query);
            }
        }
    }
}
