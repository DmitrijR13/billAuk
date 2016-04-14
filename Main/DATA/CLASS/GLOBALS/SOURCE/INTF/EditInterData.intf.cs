using System;
using System.Collections.Generic;

using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using STCLINE.KP50.Global;


namespace STCLINE.KP50.Interfaces
{
    [ServiceContract]
    public interface I_EditInterData
    {
        [OperationContract]
        void Saver(EditInterData editData, out Returns ret);
    }

    //----------------------------------------------------------------------
    [DataContract]
    public class EditInterData : Finder
    //----------------------------------------------------------------------
    {
        string _primary;
        string _table;
        string _dat_s;
        string _dat_po;

        [DataMember]
        public string primary { get { return Utils.ENull(_primary); } set { _primary = value; } }
        [DataMember]
        public string table   { get { return Utils.ENull(_table); }   set { _table = value; } }

        [DataMember]
        public bool isCentral { get; set; }

        [DataMember]
        public enDataBaseType databaseType { get; set; }

        [DataMember]
        public int year { get; set; }

        [DataMember]
        public int month { get; set; }
        
        [DataMember]
        public string dat_s   { get { return Utils.ENull(_dat_s); }   set { _dat_s = value; } }
        [DataMember]
        public string dat_po  { get { return Utils.ENull(_dat_po); }  set { _dat_po = value; } }

        [DataMember]
        public int local_user      { get; set; }

        [DataMember]
        public enIntvType intvType { get; set; }

        [DataMember]
        public bool todelete       { get; set; }

        //ключевые поля (nzp_kvar; nzp_prm; nzp_serv; ...)
        //со своим значениями
        [DataMember]
        public Dictionary<string, string> keys { get; set; }

        //эти значения надо вставить (поля val_prm; ...)
        [DataMember]
        public Dictionary<string, string> vals { get; set; }

        public EditInterData()
            : base()
        {
            primary     = "";
            table       = "";
            dat_s       = "";
            dat_po      = "";
            local_user  = 0;
            keys        = null;
            vals        = null;
            intvType    = enIntvType.intv_Day;
            todelete    = false;
        }

        public void CopyTo(EditInterData destination)
        {
            if (destination == null) return;

            base.CopyTo(destination);

            destination.primary = primary;
            destination.table = table;
            destination.dat_s = dat_s;
            destination.dat_po = dat_po;
            destination.local_user = local_user;
            destination.keys = keys;
            destination.vals = vals;
            destination.intvType = intvType;
            destination.todelete = todelete;
        }

        public override string ToString()
        {
            string s = base.ToString() +
                       " primary: " + primary +
                       " table: " + table +
                       " isCentral: " + isCentral +
                       " year: " + year +
                       " month: " + month +
                       " dat_s: " + dat_s +
                       " dat_po: " + dat_po +
                       " local_user:" + local_user +
                       " todelete:" + todelete;

            s = keys.Aggregate(s, (current, p) => current + (" " + p.Value));

            return vals.Aggregate(s, (current, p) => current + (" " + p.Value));
        }

    }

    [DataContract]
    public class EditInterDataMustCalc : EditInterData
    {
        /// <summary>
        /// Код объекта, являющегося причиной перерасчета (код параметра, код ПУ, и т.д.)
        /// </summary>
        [DataMember]
        public int kod2 { get; set; }

        [DataMember]
        public enMustCalcType mcalcType { get; set; }

        [DataMember]
        public int nzp_kvar { get; set; }

        
        public EditInterDataMustCalc()
            : base()
        {
            kod2 = 0;
            mcalcType = enMustCalcType.None;
            nzp_kvar = 0;
        }

        public override string ToString()
        {
            string s = base.ToString() +
                       " kod2:" + kod2 + " nzp_kvar:" + nzp_kvar;
            return s;
        }
    }

    //----------------------------------------------------------------------
    public struct _Series
    //----------------------------------------------------------------------
    {
        public int kod     { get; set; }
        public int v_min   { get; set; }
        public int v_max   { get; set; }
        public int cur_val { get; set; }

        public bool getAndInc;
    }
    //----------------------------------------------------------------------
    public class Series
    //----------------------------------------------------------------------
    {
        public enum Types
        {
            Kvar = 1,
            NumLs = 2,
            Dom = 3,
            Ulica = 4,
            Geu = 5,
            Area = 6,
            Supplier = 7,
            Payer = 8,
            PKod10 = 10,
            Counter = 12
        }

        private int count { get; set; }
        private _Series[] values;

        public Series(int[] mas)
        {
            count = mas.Length;
            values = new _Series[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = EmptyVal(mas[i]);
            }
        }

        /// <summary>
        /// Количество записей
        /// </summary>
        public int Count
        {
            get { return count; }
        }
        
        /// <summary>
        /// Получить пустую структуру для хранения series
        /// </summary>
        /// <param name="kod"></param>
        /// <returns></returns>
        public _Series EmptyVal(int kod)
        {
            _Series val = new _Series();

            val.kod     = kod;
            val.v_min   = Constants._ZERO_;
            val.v_max   = Constants._ZERO_;
            val.cur_val = Constants._ZERO_;
            val.getAndInc = true;

            return val;
        }
        public _Series EmptyVal()
        {
            return EmptyVal(Constants._ZERO_);
        }

        public bool PutVal(int kod)
        {
            _Series val = EmptyVal();
            val.kod = kod;
            return PutVal(val);
        }
        public bool PutVal(_Series val)
        {
            if (val.kod < 0)
            {
                return false;
            }

            int j = Constants._ZERO_;

            //изменим, если найдем 
            for (int i = 0; i < count; i++)
            {
                if (values[i].kod == val.kod)
                {
                    values[i].v_min   = val.v_min;
                    values[i].v_max   = val.v_max;
                    values[i].cur_val = val.cur_val;
                    return true;
                }
                if (values[i].kod == Constants._ZERO_)
                    j = i;
            }

            //свободных ячеек нет
            if (j == Constants._ZERO_)
                return false;

            values[j] = val;
            return true;
        }

        //получить список кодов через запятую
        public string GetStringKod()
        {
            string s = "";
            for (int i = 0; i < count; i++)
            {
                if (values[i].kod != Constants._ZERO_)
                {
                    if (i == 0) 
                        s = values[i].kod.ToString();
                    else
                        s += "," + values[i].kod.ToString();
                }
            }

            return s;
        }

        //получить список кодов
        public List<int> GetListKod()
        {
            List<int> list = new List<int>();
            for (int i = 0; i < count; i++)
            {
                if (values[i].kod != Constants._ZERO_)
                {                    
                    list.Add(values[i].kod);
                }
            }

            return list;
        }

        //вытащить значения по коду
        public _Series GetSeries(int kod)
        {
            for (int i = 0; i < count; i++)
            {
                if (values[i].kod == kod)
                {
                    return values[i]; 
                }
            }

            return EmptyVal();
        }
    }
}

