using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Runtime.Serialization;
using System.Data;
using STCLINE.KP50.Global;
using System.Collections;
using Newtonsoft.Json;

namespace STCLINE.KP50.Interfaces
{
    public class Field
    {
        public string N { get; set; }

        public string NT { get; set; }

        public int IS { get; set; }

        public int P { get; set; }

        public string T { get; set; }

        public int L { get; set; }

        public string V { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(new
            {
                Name = N,
                NameText = NT,
                IsRequired = IS,
                Place = P,
                Type = T,
                Length = L,
                Value = V
            });
        }
    }

    public class ShortField
    {
        public string N { get; set; }

        public string V { get; set; }
       
        public void Assign(Field field)
        { 
            N = field.N;

            V = field.V;
        }

    }

    public class FieldsUnload
    {
        public List<Field> F = new List<Field>();

        public List<ParamUnload> P = new List<ParamUnload>();

        //public List<ParamUnload> Params { get; set; }
        
        public override string ToString()
        {
            return JsonConvert.SerializeObject(new
            {
                Fields = F
            });
        }
    }

    public class FieldsParamsUnload : FieldsUnload
    {
        public List<ParamUnload> P = new List<ParamUnload>();

        public override string ToString()
        {
            return JsonConvert.SerializeObject(new
            {
                F = F,
                P = P
            });
        }
    }

    public class DataFields
    {
        public List<FieldsUnload> Data = new List<FieldsUnload>();

        public override string ToString()
        {
            return JsonConvert.SerializeObject(new
            {
                Data = Data
            });
        }
    }

    public class SectionUnload
    {
        public int TS { get; set; }

        public string N { get; set; }

        public string NT { get; set; }

        public List<FieldsUnload> D { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(new
            {
                TypeSection = TS,
                Name = N,
                NameText = NT,
                Data = D
            });
        }
    }

    public class SectionsUnload
    {
        public List<SectionUnload> Sections = new List<SectionUnload>();

        public  List<string> Types = new List<string>{"DateTimeType", "DateType", "DecimalType", "IntType", "TextType"};

        public override string ToString()
        {
            return JsonConvert.SerializeObject(new
            {
                Types = Types,
                //Sections = Sections
                Sections = Sections.Select(x => new
                {

                    N = x.N,
                    D = x.D.Select(y => new
                    {
                        
                        F = y.F.Select(z => new
                        {
                            z.N,
                            z.V
                        }),
                        y.P
                    })
                })
            });
        }
    }

    public class ParamUnload
    {
        public string C { get; set; }

        public string V { get; set; }

        public string SD { get; set; }

        public string ED { get; set; }
    }

}
