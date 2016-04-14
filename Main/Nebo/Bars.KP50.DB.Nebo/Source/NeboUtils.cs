using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.IFMX.Server.SOURCE.NEBO
{
    public class DbNeboUtils : DbBase
    {
        public DbNeboUtils()
            : base()
        {

        }

        /// <summary>
        /// заполнение информации результирующего paging
        /// </summary>
        /// <param name="connectionID">соединение</param>
        /// <param name="tableName">наименование таблицы</param>
        /// <param name="fieldName">наименование поля для разделения на страницы</param>
        /// <param name="pagingInfo">результирующий объект paging</param>
        /// <param name="listCount">кол-во строк результирующего списка</param>
        /// <returns></returns>
        public Returns FillResultPaging(IDbConnection connectionID, string tableName, string fieldName, int listCount, out ResultPaging pagingInfo)
        {
            pagingInfo = new ResultPaging() { rowsInCurPage = listCount };
            Returns ret = Utils.InitReturns();
            try
            {
                object pagesCount = ExecScalar(connectionID, "SELECT MAX(" + fieldName + ") FROM " + tableName, out ret, true);
                if (ret.result && pagesCount != null && pagesCount != DBNull.Value)
                    pagingInfo.totalPagesCount = Convert.ToInt32(pagesCount);

                object count = ExecScalar(connectionID, "SELECT COUNT(*) FROM " + tableName, out ret, true);
                if (ret.result && count != null && count != DBNull.Value)
                    pagingInfo.totalRowsCount = Convert.ToInt32(count);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

            }
            return ret;
        }
    }
}
