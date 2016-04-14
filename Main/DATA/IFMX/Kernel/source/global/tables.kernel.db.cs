using System;
using System.Data;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
        /// <summary>
        /// Названия таблиц центрального банка данных
        /// </summary>
        public class DbTables
        {
            string _server;

            public DbTables(string ol_server)
            {
                _server = ol_server;
            }

            public DbTables(IDbConnection connectionID)
            {
                _server = DBManager.getServer(connectionID);
            }
#if PG
        public string calc_method
        {
            get { return Points.Pref + "_kernel.calc_method"; }
        }

        public string measure
        {
            get { return Points.Pref + "_kernel.s_measure"; }
        }

        public string payer
        {
            get { return Points.Pref + "_kernel.s_payer"; }
        }

        public string payer_types
        {
            get { return Points.Pref + "_kernel.s_payer_types"; }
        }

        public string document_base
        {
            get { return Points.Pref + "_data.document_base"; }
        }

        public string s_type_doc
        {
            get { return Points.Pref + "_kernel.s_type_doc"; }
        }

        public string s_typercl
        {
            get { return Points.Pref + "_kernel.s_typercl"; }
        }

        public string payertypes
        {
            get { return Points.Pref + "_kernel.payer_types"; }
        }

        public string bank
        {
            get { return Points.Pref + "_kernel.s_bank"; }
        }

        public string supplier
        {
            get { return Points.Pref + "_kernel.supplier"; }
        }

        public string services
        {
            get { return Points.Pref + "_kernel.services"; }
        }

        public string kvar
        {
            get { return Points.Pref + "_data.kvar"; }
        }

        public string kvar_pkodes
        {
            get { return Points.Pref + "_data.kvar_pkodes"; }
        }

        public string dom
        {
            get { return Points.Pref + "_data.dom"; }
        }

        public string ulica
        {
            get { return Points.Pref + "_data.s_ulica"; }
        }

        public string rajon
        {
            get { return Points.Pref + "_data.s_rajon"; }
        }

        public string stat
        {
            get { return Points.Pref + "_data.s_stat"; }
        }

        public string land
        {
            get { return Points.Pref + "_data.s_land"; }
        }

        public string prefer
        {
            get { return Points.Pref + "_data.prefer"; }
        }

        public string rajon_dom
        {
            get { return Points.Pref + "_data.s_rajon_dom"; }
        }

        public string rajon_vill
        {
            get { return Points.Pref + "_data.rajon_vill"; }
        }
        public string vill
        {
            get { return Points.Pref + "_kernel.s_vill"; }
        }
        public string sr_rajon
        {
            get { return Points.Pref + "_kernel.sr_rajon"; }
        }

        public string town
        {
            get { return Points.Pref + "_data.s_town"; }
        }

        public string area
        {
            get { return Points.Pref + "_data.s_area"; }
        }

        public string geu
        {
            get { return Points.Pref + "_data.s_geu"; }
        }

        public string point
        {
            get { return Points.Pref + "_kernel.s_point"; }
        }

        public string res_y
        {
            get { return Points.Pref + "_kernel.res_y"; }
        }

        public string user
        {
            get { return Points.Pref + "_data.users"; }
        }

        public string file_area
        {
            get { return Points.Pref + "_data.file_area"; }
        }

        public string file_dom
        {
            get { return Points.Pref + "_data.file_dom"; }
        }

        public string file_kvar
        {
            get { return Points.Pref + "_data.file_kvar"; }
        }

        public string file_supp
        {
            get { return Points.Pref + "_data.file_supp"; }
        }

        public string file_ipu
        {
            get { return Points.Pref + "_data.file_ipu"; }
        }

        public string reason
        {
            get { return Points.Pref + "_kernel.s_reason"; }
        }

        public string prm_name
        {
            get { return Points.Pref + "_kernel.prm_name"; }
        }

        public string formuls
        {
            get { return Points.Pref + "_kernel.formuls"; }
        }

        public string formuls_opis
        {
            get { return Points.Pref + "_kernel.formuls_opis"; }
        }

        public string prm_tarifs
        {
            get { return Points.Pref + "_kernel.prm_tarifs"; }
        }

        public string prm_frm
        {
            get { return Points.Pref + "_kernel.prm_frm"; }
        }


        public string s_remark
        {
            get { return Points.Pref + "_data.s_remark"; }
        }

        public string s_baselist
        {
            get { return Points.Pref + "_kernel.s_baselist"; }
        }


        public string simple_load
        {
            get { return Points.Pref + "_data.simple_load"; }
        }

        public string pack_types
        {
            get { return Points.Pref + "_kernel.pack_types"; }
        }

         public string area_codes
        {
            get { return Points.Pref + "_data.area_codes"; }
        }

        public string fn_bank
        {
            get { return Points.Pref + "_data.fn_bank"; }
        }

        public string bc_types
        {
            get { return Points.Pref + "_kernel.bc_types"; }
        }
#else
            public string calc_method
            {
                get { return Points.Pref + "_kernel@" + _server + ":calc_method"; }
            }

            public string measure
            {
                get { return Points.Pref + "_kernel@" + _server + ":s_measure"; }
            }

            public string payer
            {
                get { return Points.Pref + "_kernel@" + _server + ":s_payer"; }
            }

            public string document_base
            {
                get { return Points.Pref + "_data@" + _server + ":document_base"; }
            }

            public string s_type_doc
            {
                get {return Points.Pref + "_kernel@" + _server + ":s_type_doc"; }
            }

            public string s_typercl
            {
                get { return Points.Pref + "_kernell@" + _server + ":s_typercl"; }
            }

            public string payer_types
            {
                get { return Points.Pref + "_kernel@" + _server + ":s_payer_types"; }
            }

            public string s_baselist
            {
                get { return Points.Pref + "_kernel@" + _server + ":s_baselist"; }
            }

            public string payertypes
            {
                get { return Points.Pref + "_kernel@" + _server + ":payer_types"; }
            }

            public string bank
            {
                get { return Points.Pref + "_kernel@" + _server + ":s_bank"; }
            }

            public string supplier
            {
                get { return Points.Pref + "_kernel@" + _server + ":supplier"; }
            }

            public string services
            {
                get { return Points.Pref + "_kernel@" + _server + ":services"; }
            }

            public string kvar
            {
                get { return Points.Pref + "_data@" + _server + ":kvar"; }
            }

            public string kvar_pkodes
            {
                get { return Points.Pref + "_data@" + _server + ":kvar_pkodes"; }
            }


            public string dom
            {
                get { return Points.Pref + "_data@" + _server + ":dom"; }
            }

            public string ulica
            {
                get { return Points.Pref + "_data@" + _server + ":s_ulica"; }
            }

            public string rajon
            {
                get { return Points.Pref + "_data@" + _server + ":s_rajon"; }
            }

            public string stat
            {
                get { return Points.Pref + "_data@" + _server + ":s_stat"; }
            }

            public string land
            {
                get { return Points.Pref + "_data@" + _server + ":s_land"; }
            }

            public string prefer
            {
                get { return Points.Pref + "_data@" + _server + ":prefer"; }
            }

            public string rajon_dom
            {
                get { return Points.Pref + "_data@" + _server + ":s_rajon_dom"; }
            }

            public string rajon_vill
            {
                get { return Points.Pref + "_data@" + _server + ":rajon_vill"; }
            }
            public string vill
            {
                get { return Points.Pref + "_kernel@" + _server + ":s_vill"; }
            }
            public string sr_rajon
            {
                get { return Points.Pref + "_kernel@" + _server + ":sr_rajon"; }
            }

            public string town
            {
                get { return Points.Pref + "_data@" + _server + ":s_town"; }
            }

            public string area
            {
                get { return Points.Pref + "_data@" + _server + ":s_area"; }
            }

            public string geu
            {
                get { return Points.Pref + "_data@" + _server + ":s_geu"; }
            }

            public string point
            {
                get { return Points.Pref + "_kernel@" + _server + ":s_point"; }
            }

            public string res_y
            {
                get { return Points.Pref + "_kernel@" + _server + ":res_y"; }
            }

            public string simple_load
            {
                get {  return Points.Pref + "_data@" + _server + ":simple_load"; }
            }

            public string user
            {
                get { return Points.Pref + "_data@" + _server + ":users"; }
            }

            public string file_area
            {
                get { return Points.Pref + "_data@" + _server + ":file_area"; }
            }

            public string file_dom
            {
                get { return Points.Pref + "_data@" + _server + ":file_dom"; }
            }

            public string file_kvar
            {
                get { return Points.Pref + "_data@" + _server + ":file_kvar"; }
            }

            public string file_supp
            {
                get { return Points.Pref + "_data@" + _server + ":file_supp"; }
            }

            public string file_ipu
            {
                get { return Points.Pref + "_data@" + _server + ":file_ipu"; }
            }

            public string reason
            {
                get { return Points.Pref + "_kernel@" + _server + ":s_reason"; }
            }

            public string prm_name
            {
                get { return Points.Pref + "_kernel@" + _server + ":prm_name"; }
            }

            public string formuls
            {
                get { return Points.Pref + "_kernel@" + _server + ":formuls"; }
            }

            public string formuls_opis
            {
                get { return Points.Pref + "_kernel@" + _server + ":formuls_opis"; }
            }

            public string prm_tarifs
            {
                get { return Points.Pref + "_kernel@" + _server + ":prm_tarifs"; }
            }

            public string prm_frm
            {
                get { return Points.Pref + "_kernel@" + _server + ":prm_frm"; }
            }

            public string s_remark
            {
                get { return Points.Pref + "_data@" + _server + ":s_remark"; }
            }

            public string pack_types
            {
                get { return Points.Pref + "_kernel@" + _server + ":pack_types"; }
            }

            public string area_codes
            {
                get { return Points.Pref + "_data@" + _server + ":area_codes"; }
            }

            public string fn_bank
            {
                get { return Points.Pref + "_data@" + _server + ":fn_bank"; }
            }

            public string bc_types
            {
                get { return Points.Pref + "_kernel@" + _server + ":bc_types"; }
            }
#endif

        }
}
