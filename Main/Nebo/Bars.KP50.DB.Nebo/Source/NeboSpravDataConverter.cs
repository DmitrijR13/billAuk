using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;
using STCLINE.KP50.EPaspXsd;

namespace STCLINE.KP50.DataBase
{
    internal static class NeboSpravDataConverter
    {
        public static NeboService ToService(IDataRecord dr)
        {
            NeboService t = new NeboService();
            t.nzp_serv = DataConvert.FieldValue(dr, "nzp_serv", 0);
            t.service = DataConvert.FieldValue(dr, "service", "").Trim();
            t.nzp_area = DataConvert.FieldValue(dr, "nzp_area", 0);
            return t;
        }

        public static NeboDom ToDom (IDataRecord dr)
        {
            NeboDom t = new NeboDom();
            t.rajon = DataConvert.FieldValue(dr, "rajon", "").Trim();
            t.town = DataConvert.FieldValue(dr, "town", "").Trim();
            t.ulica = DataConvert.FieldValue(dr, "ulica", "").Trim();
            t.ndom = DataConvert.FieldValue(dr, "ndom", "").Trim();
            t.nkor = DataConvert.FieldValue(dr, "nkor", "").Trim();
            t.nzp_dom = DataConvert.FieldValue(dr, "nzp_dom", 0);
            t.nzp_area = DataConvert.FieldValue(dr, "nzp_area", 0);
            return t;
        }

        public static NeboSupplier ToSupplier(IDataRecord dr)
        {
            NeboSupplier t = new NeboSupplier();
            t.name_supp = DataConvert.FieldValue(dr, "name_supp", "").Trim();
            t.nzp_supp = DataConvert.FieldValue(dr, "nzp_supp", 0);
            t.inn_supp = DataConvert.FieldValue(dr, "inn_supp", 0);
            t.kpp_supp = DataConvert.FieldValue(dr, "kpp_supp", 0);
            t.nzp_area = DataConvert.FieldValue(dr, "nzp_area", 0);
            return t;
        }


        public static NeboRenters ToRenters(IDataRecord dr)
        {
            NeboRenters t = new NeboRenters();
            t.renter_id = DataConvert.FieldValue(dr, "renter_id", 0);
            t.pkod = DataConvert.FieldValue<decimal>(dr, "account_number", 0); 
            t.nzp_dom = DataConvert.FieldValue(dr, "nzp_dom", 0);
            t.kpp_kvar = DataConvert.FieldValue(dr, "kpp_kvar", "").Trim();
            t.inn_kvar = DataConvert.FieldValue(dr, "inn_kvar", "").Trim();
            t.nkvar = DataConvert.FieldValue(dr, "nkvar","").Trim();
            t.nkvar_n = DataConvert.FieldValue(dr, "nkvar_n", "").Trim();
            t.description = DataConvert.FieldValue(dr, "description", "").Trim();
            t.nzp_area = DataConvert.FieldValue(dr, "nzp_area", 0);
            return t;
        }

        public static NeboArea ToArea(IDataRecord dr)
        {
            NeboArea t = new NeboArea();
            t.inn_area = DataConvert.FieldValue(dr, "inn_area", 0);
            t.kpp_area = DataConvert.FieldValue(dr, "kpp_area", 0);
            t.area = DataConvert.FieldValue(dr, "area", "").Trim();
            t.address_area = DataConvert.FieldValue(dr, "address_area", "").Trim();
            t.nzp_area = DataConvert.FieldValue(dr, "nzp_area", 0);
            return t;
        }
    }
}
