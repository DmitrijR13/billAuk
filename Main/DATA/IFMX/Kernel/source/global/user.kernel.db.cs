using System.Data;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System;


namespace STCLINE.KP50.DataBase
{
//    public class DbWorkUser : DataBaseHeadServer
//    {
//        public enum UserDatabase
//        {
//            Data = 1,
//            Supg = 2
//        }


//        protected int _GetLocalUser(IDbConnection conn_db, IDbTransaction transaction, Finder finder, UserDatabase database, out Returns ret)
//        {
//            string users = "";
//            switch (database)
//            {
//                case UserDatabase.Data:
//#if PG
//                    users = Points.Pref + "_data.users";
//#else
//                    users = Points.Pref + "_data@" + DBManager.getServer(conn_db) + ":users";
//#endif
//                    break;
//                case UserDatabase.Supg:
//#if PG
//                    users = Points.Pref + "_supg.users";
//#else
//                    users = Points.Pref + "_supg@" + DBManager.getServer(conn_db) + ":users";
//#endif
//                    break;
//            }

//            string sql = " Select count(*) From " + users + " Where web_user = " + finder.nzp_user;
//            object objUserCnt = ExecScalar(conn_db, transaction, sql, out ret, true);
//            if (!ret.result)
//            {
//                ret.text = "Ошибка проверки пользователя";
//                return Constants._ZERO_;
//            }

//            try
//            {
//                if (Convert.ToInt32(objUserCnt) > 0)
//                {
//                    ret.tag = finder.nzp_user;
//                    return ret.tag;
//                }
//            }
//            finally
//            {

//            }

//            //нет пользователя, надо завести
//            if (finder.webLogin == "")
//            {
//                using (DbWorkUserClient db = new DbWorkUserClient())
//                {
//                    ret = db.GetWebUser(finder);
//                    if (!ret.result) return Constants._ZERO_;
//                }
//            }

//            sql = " Insert into " + users + " (nzp_user, name, comment, web_user) " +
//                " Values (" + finder.nzp_user + "," + Utils.EStrNull(finder.webLogin, "") + "," + Utils.EStrNull(finder.webUname, "") + "," + finder.nzp_user + " ) ";
//            ret = ExecSQL(conn_db, transaction, sql, true);
//            if (!ret.result)
//            {
//                ret.text = "Ошибка добавления пользователя";
//                return Constants._ZERO_;
//            }
//            ret.tag = finder.nzp_user;
//            if (ret.tag < 1) ret.result = false;
//            return ret.tag;
//        }


//        public int GetLocalUser(Finder finder, out Returns ret)
//        {
//            return GetLocalUser(finder, UserDatabase.Data, out ret);
//        }

//        public int GetLocalUser(Finder finder, UserDatabase database, out Returns ret)
//        {
//            ret = new Returns(true);
//            return _GetLocalUser(GetConnection(), null, finder, database, out ret);
//        }

//        public int GetLocalUser(IDbConnection conn_db, Finder finder, out Returns ret)
//        {
//            return GetLocalUser(conn_db, null, finder, UserDatabase.Data, out ret);
//        }

//        public int GetLocalUser(IDbConnection conn_db, IDbTransaction transaction, Finder finder, out Returns ret)
//        {
//            return GetLocalUser(conn_db, transaction, finder, UserDatabase.Data, out ret);
//        }

//        public int GetSupgUser(IDbConnection conn_db, IDbTransaction transaction, Finder finder, out Returns ret)
//        {
//            return GetLocalUser(conn_db, transaction, finder, UserDatabase.Supg, out ret);
//        }

//        public int GetLocalUser(IDbConnection conn_db, IDbTransaction transaction, Finder finder, UserDatabase database, out Returns ret)
//        {
//            ret = new Returns(true);
//            return _GetLocalUser(conn_db, transaction, finder, database, out ret);
//        }
//    }
}
