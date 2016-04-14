using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Runtime.Serialization;
using System.Data;
using STCLINE.KP50.Global;
using System.Collections;

namespace STCLINE.KP50.Interfaces
{
    [ServiceContract]
    public interface I_Sz
    {
        //процедура возвращает результат поиска
        [OperationContract]
        List<SzFinder> GetFindSz(SzFinder finder, out Returns ret);

        [OperationContract]
        List<SzMo> DbGetMo(SzMo finder, out Returns ret);

        [OperationContract]
        List<SzRajon> DbGetRajon(SzRajon finder, out Returns ret);

        [OperationContract]
        List<SzUK> DbGetUK(SzUK finder, out Returns ret);

        [OperationContract]
        List<SzUKPodr> DbGetUKPodr(SzUKPodr finder, out Returns ret);

        [OperationContract]
        List<SzUlica> DbGetUlica(SzUlica finder, out Returns ret);

        [OperationContract]
        SzKart GetKartSz(SzFinder finder, out Returns ret);
    }

    //класс для поиска
    [DataContract]
    public class SzFinder : Ls
    {
        public SzFinder()
            : base()
        {
            nzp_mo = 0;
            mo = "";
            nzp_uk = 0;
            name_uk = "";
            nzp_uk_podr = 0;
            name_podr = "";
            pss = "";
            nzp_key = -1;
            list_mo = "";
            list_raj = "";
            list_uk = "";
            list_uk_podr = "";
            list_ul = "";
            name_supp = "";
            cnt_gil = 0;
            cnt_lg = 0;
            s_ot = "";
            s_ot_sn = "";
        }

        [DataMember]
        public int nzp_key { get; set; }
        [DataMember]
        public int nzp_mo { get; set; }
        [DataMember]
        public string mo { get; set; } // муниципальной образование
        [DataMember]
        public int nzp_uk { get; set; }
        [DataMember]
        public string name_uk { get; set; } // управляющая компания
        [DataMember]
        public int nzp_uk_podr { get; set; }
        [DataMember]
        public string name_podr { get; set; } // территориальное отделение
        [DataMember]
        public string pss { get; set; } // ПСС

        [DataMember]
        public string list_mo { get; set; }
        [DataMember]
        public string list_raj { get; set; }
        [DataMember]
        public string list_uk { get; set; }
        [DataMember]
        public string list_uk_podr { get; set; }
        [DataMember]
        public string list_ul { get; set; }
        [DataMember]
        public string name_supp { get; set; }
        [DataMember]
        public int cnt_gil { get; set; }
        [DataMember]
        public int cnt_lg { get; set; }
        [DataMember]
        public string s_ot { get; set; }
        [DataMember]
        public string s_ot_sn { get; set; }

    }

    // муниципальной образование
    [DataContract]
    public class SzMo : Finder
    {
        public SzMo()
            : base()
        {
            nzp_mo = 0;
            mo = "";
        }

        [DataMember]
        public int nzp_mo { get; set; }
        [DataMember]
        public string mo { get; set; } 
    }

    // отделение соц. защиты
    [DataContract]
    public class SzRajon : Finder
    {
        public SzRajon()
            : base()
        {
            nzp_raj = 0;
            rajon = "";
            nzp_mo = 0;
            list_mo = "";
        }

        [DataMember]
        public int nzp_raj { get; set; }
        [DataMember]
        public string rajon { get; set; }
        [DataMember]
        public int nzp_mo { get; set; }
        [DataMember]
        public string list_mo { get; set; }
    }

    [DataContract]
    public class SzUK : Finder
    {
        public SzUK()
            : base()
        {
            nzp_key = 0;
            nzp_uk = 0;
            name_uk = "";
            nzp_raj = 0;
            nzp_mo = 0;
            list_mo = "";
            list_raj = "";
        }

        [DataMember]
        public int nzp_key { get; set; }
        [DataMember]
        public int nzp_uk { get; set; }
        [DataMember]
        public string name_uk { get; set; }
        [DataMember]
        public int nzp_mo { get; set; }
        [DataMember]
        public int nzp_raj { get; set; }
        [DataMember]
        public string list_mo { get; set; }
        [DataMember]
        public string list_raj { get; set; }
    }

    [DataContract]
    public class SzUKPodr : Finder
    {
        public SzUKPodr()
            : base()
        {
            nzp_key = 0;
            nzp_uk_podr = 0;
            name_podr = "";
            nzp_raj = 0;
            nzp_mo = 0;
            nzp_uk = 0;
            list_mo = "";
            list_raj = "";
            list_uk = "";
        }

        [DataMember]
        public int nzp_key { get; set; }
        [DataMember]
        public int nzp_uk_podr { get; set; }
        [DataMember]
        public string name_podr { get; set; }
        [DataMember]
        public int nzp_mo { get; set; }
        [DataMember]
        public int nzp_raj { get; set; }
        [DataMember]
        public int nzp_uk { get; set; }
        [DataMember]
        public string list_mo { get; set; }
        [DataMember]
        public string list_raj { get; set; }
        [DataMember]
        public string list_uk { get; set; }
    }

    [DataContract]
    public class SzUlica : Finder
    {
        public SzUlica()
            : base()
        {
            nzp_ul = 0;
            ulica = "";
            nzp_mo = 0;
            list_mo = "";
            list_raj = "";
        }

        [DataMember]
        public int nzp_ul { get; set; }
        [DataMember]
        public string ulica { get; set; }
        [DataMember]
        public int nzp_mo { get; set; }
        [DataMember]
        public string list_mo { get; set; }
        [DataMember]
        public string list_raj { get; set; }
    }

    //класс для карточки
    [DataContract]
    public class SzKart : SzFinder
    {
        public SzKart()
            : base()
        {
            list = null;
        }

        [DataMember]
        public List<SzList> list { get; set; }
    }

    //класс для списка параметров карточки
    [DataContract]
    public class SzList : SzFinder
    {
        public SzList()
            : base()
        {
            parametr  = "";
            otop = "";
            gvs = "";
            summ = "";
        }
        
        [DataMember]
        public string parametr { get; set; }
        [DataMember]
        public string otop { get; set; }
        [DataMember]
        public string gvs { get; set; }
        [DataMember]
        public string summ { get; set; }
    }
}
