using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace webroles.GenerateScriptTable.PagesTableScripts
{
    class PagesDeleteWholeScript
    {
        private int nzp_page;
        public PagesDeleteWholeScript(int nzp_page)
        {
            this.nzp_page = nzp_page;
        }
        public void GenerateScript()
        {
            ChangedRow rolePages= new ChangedRow(Tables.role_pages,"", 0);
            rolePages.State= DataRowState.Deleted;
            rolePages.AddToWhereDictionaty("nzp_page",nzp_page.ToString());
            GenerateScriptTable.GenerateScript.ChangedRowCollection.Add(rolePages);
            ChangedRow roleActions = new ChangedRow(Tables.role_actions, "", 0);
            roleActions.State = DataRowState.Deleted;
            roleActions.AddToWhereDictionaty("nzp_page", nzp_page.ToString());
            GenerateScriptTable.GenerateScript.ChangedRowCollection.Add(roleActions);
            ChangedRow actionsShow = new ChangedRow(Tables.actions_show, "", 0);
            actionsShow.State = DataRowState.Deleted;
            actionsShow.AddToWhereDictionaty("cur_page", nzp_page.ToString());
            GenerateScriptTable.GenerateScript.ChangedRowCollection.Add(actionsShow);
            ChangedRow actionslnk = new ChangedRow(Tables.actions_lnk, "", 0);
            actionslnk.State = DataRowState.Deleted;
            actionslnk.AddToWhereDictionaty("cur_page", nzp_page.ToString());
            GenerateScriptTable.GenerateScript.ChangedRowCollection.Add(actionslnk);
            ChangedRow pagesShow = new ChangedRow(Tables.pages_show, "", 0);
            pagesShow.State = DataRowState.Deleted;
            pagesShow.AddToWhereDictionaty("cur_page", nzp_page.ToString());
            pagesShow.AddToWhereDictionaty("page_url", nzp_page.ToString());
            pagesShow.WhereDelimeter = "or";
            GenerateScriptTable.GenerateScript.ChangedRowCollection.Add(pagesShow);
            ChangedRow pages = new ChangedRow(Tables.pages, "", 0);
            pages.State = DataRowState.Deleted;
            pages.AddToWhereDictionaty("nzp_page", nzp_page.ToString());
            GenerateScriptTable.GenerateScript.ChangedRowCollection.Add(pages);
        }
    }
}
