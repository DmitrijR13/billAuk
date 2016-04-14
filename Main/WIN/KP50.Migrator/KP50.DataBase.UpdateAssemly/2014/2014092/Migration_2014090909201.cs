using KP50.DataBase.Migrator.Framework;
using System.Collections.Generic;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014090909201, MigrateDataBase.Web)]
    public class Migration_2014090909201_Web : Migration
    {
        public override void Apply()
        {
            List<string> lstQuerys = new List<string>();
            if (Database.ProviderName == "Informix")
            {
                if (!Database.ProcedureExists(CurrentSchema, "sortnumd"))
                {
                    lstQuerys = new List<string>();
                    lstQuerys.Add(
                        "CREATE PROCEDURE \"are\".sortnumd( str_num CHAR(15),i INTEGER  ) " +
                        "RETURNING INTEGER; " +
                        "DEFINE int_num      INTEGER; " +
                        "DEFINE err_num      INTEGER; " +
                        "DEFINE str          CHAR(10); " +
                        "LET str=\"\"; " +
                        "IF  i=1 " +
                        "THEN LET str_num=str_num[1,1]; " +
                        "ELIF i=2 " +
                        "THEN LET str_num=str_num[1,2]; " +
                        "ELIF i=3 " +
                        "THEN LET str_num=str_num[1,3]; " +
                        "ELIF i=4 " +
                        "THEN LET str_num=str_num[1,4]; " +
                        "ELIF i=5 " +
                        "THEN LET str_num=str_num[1,5]; " +
                        "ELIF i=6 " +
                        "THEN LET str_num=str_num[1,6]; " +
                        "ELIF i=7 " +
                        "THEN LET str_num=str_num[1,7]; " +
                        "ELIF i=8 " +
                        "THEN LET str_num=str_num[1,8]; " +
                        "ELIF i=9 " +
                        "THEN LET str_num=str_num[1,9]; " +
                        "ELIF i=10 " +
                        "THEN LET str_num=str_num[1,10]; " +
                        "ELSE LET int_num=0; " +
                        "RETURN int_num; " +
                        "END IF " +
                        "BEGIN " +
                        "ON EXCEPTION IN (-1213) SET err_num " +
                        "LET i       = length(str_num)-1; " +
                        "LET int_num = sortnumd(str_num,i); " +
                        "END EXCEPTION WITH RESUME; " +
                        "LET int_num=str_num; " +
                        "END; " +
                        "RETURN int_num; " +
                        "END PROCEDURE; "
                        );
                    lstQuerys.Add("grant execute on function \"are\".sortnumd(char,integer) to public as are");
                    foreach (string Query in lstQuerys) Database.ExecuteNonQuery(Query);
                }

                if (!Database.ProcedureExists(CurrentSchema, "sortnum"))
                {
                    lstQuerys = new List<string>();
                    lstQuerys.Add(
                        "CREATE PROCEDURE \"are\".sortnum( str_num CHAR(15) ) "+
                        " RETURNING integer; "+
                        " DEFINE int_num      integer; "+
                        " DEFINE err_num      INTEGER; "+
                        " DEFINE str          CHAR(10); "+
                        " DEFINE i            SMALLINT; "+
                        " LET str=\"\"; "+
                        " BEGIN "+
                        " ON EXCEPTION IN (-1213) SET err_num "+
                        " LET i       = length(str_num)-1; "+
                        " LET int_num = sortnumd(str_num,i); "+
                        " END EXCEPTION WITH RESUME; "+
                        " LET int_num=str_num; "+
                        " END; "+
                        " RETURN int_num; "+
                        " END PROCEDURE; ");

                    lstQuerys.Add(
                        "grant execute on procedure \"are\".sortnum(char) to public as are"
                        );
                    foreach (string Query in lstQuerys) Database.ExecuteNonQuery(Query);
                }
            }

            if (Database.ProviderName == "PostgreSQL")
            {
                if (!Database.ProcedureExists(CurrentSchema, "sortnumd"))
                {
                    lstQuerys = new List<string>();
                    lstQuerys.Add(
                        "CREATE OR REPLACE FUNCTION " + CurrentSchema + ".sortnumd(str_num character, i integer) " +
                        "RETURNS integer AS " +
                        "$BODY$ " +
                        "DECLARE int_num      integer; " +
                        "DECLARE err_num      INTEGER; " +
                        "DECLARE str          CHAR(10); " +
                        "BEGIN " +
                        "  str=''; " +
                        "  IF  i=1 THEN " +
                        "    str_num=substring(str_num from 1 for 1); " +
                        "  ELSIF i=2 THEN " +
                        "    str_num=substring(str_num from 1 for 2); " +
                        "  ELSIF i=3 THEN " +
                        "    str_num=substring(str_num from 1 for 3); " +
                        "  ELSIF i=4 THEN " +
                        "    str_num=substring(str_num from 1 for 4); " +
                        "  ELSIF i=5 THEN " +
                        "    str_num=substring(str_num from 1 for 5); " +
                        "  ELSIF i=6 THEN " +
                        "    str_num=substring(str_num from 1 for 6); " +
                        "  ELSIF i=7 THEN " +
                        "    str_num=substring(str_num from 1 for 7); " +
                        "  ELSIF i=8 THEN " +
                        "    str_num=substring(str_num from 1 for 8); " +
                        "  ELSIF i=9 THEN " +
                        "    str_num=substring(str_num from 1 for 9); " +
                        "  ELSIF i=10 THEN " +
                        "    str_num=substring(str_num from 1 for 10); " +
                        "  ELSE " +
                        "    int_num=0; " +
                        "    RETURN int_num; " +
                        "  END IF; " +
                        "  BEGIN " +
                        "    int_num=cast(str_num as integer); " +
                        "    EXCEPTION WHEN invalid_character_value_for_cast THEN " +
                        "      i       = length(str_num)-1; " +
                        "      int_num = " + CurrentSchema + ".sortnumd(str_num,i); " +
                        "   WHEN others THEN " +
                        "      i       = length(str_num)-1; " +
                        "      int_num = " + CurrentSchema + ".sortnumd(str_num,i); " +
                        "  END; " +
                        "  RETURN int_num; " +
                        "END;$BODY$ " +
                        "  LANGUAGE plpgsql VOLATILE " +
                        "  COST 100; "
                        );
                    foreach (string Query in lstQuerys) Database.ExecuteNonQuery(Query);

                    lstQuerys = new List<string>();
                    lstQuerys.Add("ALTER FUNCTION " + CurrentSchema + ".sortnumd(character, integer) OWNER TO postgres;");
                    lstQuerys.Add("grant execute on function " + CurrentSchema + ".sortnumd(character, integer) to postgres;");
                    lstQuerys.Add("grant execute on function " + CurrentSchema + ".sortnumd(character, integer) to public;");
                    foreach (string Query in lstQuerys) Database.ExecuteNonQuery(Query);
                }
              

                if (!Database.ProcedureExists(CurrentSchema, "sortnum"))
                {
                    lstQuerys = new List<string>();
                    lstQuerys.Add(
                        "CREATE OR REPLACE FUNCTION " + CurrentSchema + ".sortnum(str_num character) " +
                        "RETURNS integer AS " +
                        "$BODY$ " +
                        "DECLARE int_num      integer; " +
                        "DECLARE err_num      INTEGER; " +
                        "DECLARE str          CHAR(10); " +
                        "DECLARE i            SMALLINT; " +
                        "BEGIN " +
                        "  str = ''; " +
                        "  BEGIN " +
                        "      int_num=cast(str_num as integer); " +
                        "    EXCEPTION WHEN invalid_character_value_for_cast THEN " +
                        "      i       = length(str_num)-1; " +
                        "      int_num = " + CurrentSchema + ".sortnumd(str_num,i); " +
                        "    WHEN others THEN " +
                        "      i       = length(str_num)-1; " +
                        "      int_num = " + CurrentSchema + ".sortnumd(str_num,i); " +
                        "  END; " +
                        "  RETURN int_num; " +
                        "END;$BODY$ " +
                        "  LANGUAGE plpgsql VOLATILE " +
                        "  COST 100;"
                        );
                    foreach (string Query in lstQuerys) Database.ExecuteNonQuery(Query);

                    lstQuerys = new List<string>();
                    lstQuerys.Add("ALTER FUNCTION " + CurrentSchema + ".sortnum(character) OWNER TO postgres;");
                    lstQuerys.Add("grant execute on function " + CurrentSchema + ".sortnum(character) to postgres;");
                    lstQuerys.Add("grant execute on function " + CurrentSchema + ".sortnum(character) to public;");
                    foreach (string Query in lstQuerys) Database.ExecuteNonQuery(Query);
                }
            }            
        }
    }
}
