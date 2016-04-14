using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;

namespace Bars.KP50.DB.Exchange.Unload
{
    public class Sections  : BaseUnload20
    {
        public int _year;
        public int _month;

        public Sections(int year, int month, List<int> nzp_area)
        {
        }

        public override void Start()
        {
        }

        public override void Start(string pref)
        {
        }

        public override void StartSelect()
        {
        }

        public override void CreateTempTable()
        {
        }

        public void StartWithObject(object obj)
        {

            var nzpUser = ((FilesImported) obj).nzp_user;
            var month = ((FilesImported) obj).month;
            var year = ((FilesImported) obj).year;
            var pref = ((FilesImported) obj).pref;
            var listNzpArea = ((FilesImported) obj).ListNzpArea;
            GetUnload(nzpUser, year, month, pref, listNzpArea);
        }

        public void DropTempTable()
        {
        }
        

        
    }
    
    
}
