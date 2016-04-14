using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;
using STCLINE.KP50.EPaspXsd;
using Globals.SOURCE.INTF._EPasp.classes;

namespace STCLINE.KP50.DataBase
{
    internal static class EPaspDataConverter
    {
        public static mabService ToMabService(IDataRecord dr)
        {
            mabService t = new mabService();
            t.serviceId = DataConvert.FieldValue(dr, "nzp_serv", 0);
            t.serviceName = DataConvert.FieldValue(dr, "service", "");
            return t;
        }

        public static mabService ToMabService(DataRow dr)
        {
            mabService t = new mabService();
            t.serviceId = DataConvert.FieldValue(dr, "nzp_serv", 0);
            t.serviceName = DataConvert.FieldValue(dr, "service", "");
            return t;
        }

        public static custom_multiApartmentBuilding toMultiApartmentBuilding(DataRow dr)
        {
            custom_multiApartmentBuilding t = new custom_multiApartmentBuilding();
            t.pref = DataConvert.FieldValue(dr, "pref", "").Trim(); 
            t.uniqueNumber = Convert.ToString(DataConvert.FieldValue(dr, "uniqueNumber", 0));      
            t.addressFiasGuid = DataConvert.FieldValue(dr, "addressFiasGuid", "").Trim();     
            t.address = DataConvert.FieldValue(dr, "address", "").Trim();      
            t.buildingTotalArea = DataConvert.FieldValue(dr, "buildingtotalarea", 0.00M);
            t.commissioningYear = Convert.ToInt16(DataConvert.FieldValue(dr, "commissioningYear", 0));
            t.residentCount = Convert.ToString(DataConvert.FieldValue(dr, "residentCount", 0));  
            t.personalAccountCount = Convert.ToString(DataConvert.FieldValue(dr, "personalAccountCount", 0));
            return t;
        }

        public static custom_mabResourceSupplyOrganization toResourceSupplyOrganization(DataRow dr)
        {
            custom_mabResourceSupplyOrganization t = new custom_mabResourceSupplyOrganization();
            t.nzp_serv = Convert.ToInt32(DataConvert.FieldValue(dr, "nzp_serv", 0));
            t.communalResource = GetServiceCommunalResource(Convert.ToInt32(DataConvert.FieldValue(dr, "nzp_serv", 0)));
            t.serviceProvider = new buildingServiceProvider()
            {
               inn = DataConvert.FieldValue(dr, "inn", "").Trim(),
               name = DataConvert.FieldValue(dr, "name_supp", "").Trim(),
               juridicalAddress = DataConvert.FieldValue(dr, "address", "").Trim(),
               registrationReasonCode = DataConvert.FieldValue(dr, "kpp", "").Trim()
            };
            t.supplyStartDate = DataConvert.FieldValue(dr, "dat_s", "").Trim();
            t.supplyEndDate = DataConvert.FieldValue(dr, "dat_po", "").Trim();
            return t;
        }

        /*
        public static mabCommunalServiceProvider toCommunalServiceProvider(DataRow dr)
        {
            mabCommunalServiceProvider t = new mabCommunalServiceProvider();
            return t;
        }
        */
        
        public static custom_flat toFlat(DataRow dr)
        {
            custom_flat t = new custom_flat();
            t.uniqueNumber = Convert.ToInt32(DataConvert.FieldValue(dr, "nzp_kvar", 0));
            t.number = (DataConvert.FieldValue(dr, "nkvar", "")).Trim() + '(' + (DataConvert.FieldValue(dr, "nkvar_n", "")).Trim() + ')';
            t.entrance = (DataConvert.FieldValue(dr, "entrance", "")).Trim();
            t.floor = Convert.ToInt16(DataConvert.FieldValue(dr, "floor", "").Trim());
            t.totalArea = Convert.ToDecimal(DataConvert.FieldValue(dr, "all_pl", "").Trim());
            t.livingArea = Convert.ToDecimal(DataConvert.FieldValue(dr, "gil_pl", "").Trim());
            t.roomCount = Convert.ToInt16(DataConvert.FieldValue(dr, "kol_komnat", "").Trim());
            return t;
        }

        public static mabAccrualParameters toAccrualParams(DataRow dr)
        {
            mabAccrualParameters t = new mabAccrualParameters();
            string rajon = dr["rajon"].ToString().Trim();
            string town = dr["town"].ToString().Trim();
            t.isTownTypeSettlement = GetTownTypeSettlement(rajon, town);
            t.hasElectricRanges = ((Convert.ToInt32(dr["town"].ToString().Trim())) > 0) ? true : false;
            t.coldWaterTypeWell = ((Convert.ToInt32(dr["town"].ToString().Trim())) > 0) ? true : false;
            t.mabBathTubeType = ((Convert.ToInt32(dr["mabBathTubeType"].ToString().Trim())) > 0) ? GetBathTubeType() : mabBathTubeType.NONE;
            t.mabWashbasinType = ((Convert.ToInt32(dr["mabWashbasinType"].ToString().Trim())) > 0) ? GetWashBasinType() : mabWashbasinType.NONE;
            return t;
        }

        public static custom_personalAccount toPersonalAccount(DataRow dr)
        {
            custom_personalAccount t = new custom_personalAccount();

            custom_apartmentRegisteredPerson carp = new custom_apartmentRegisteredPerson()
            {
                naturPerson = new naturPerson()
                {
                    id = DataConvert.FieldValue(dr, "nzp_gil", 0),
                    fio = new fio()
                    {
                        firstName = dr["ima"].ToString().Trim(),
                        lastName = dr["fam"].ToString().Trim(),
                        middleName = dr["otch"].ToString().Trim()
                    },
                    birthDate = dr["dat_rog"].ToString().Trim(),
                    docLawDoc = new docLawDoc()
                    {
                        type = GetDocumentType(Convert.ToInt32(dr["nzp_dok"].ToString().Trim())),
                        series = dr["serij"].ToString().Trim(),
                        number = dr["nomer"].ToString().Trim(),
                        date = Convert.ToDateTime(dr["vid_dat"].ToString().Trim()),
                        govName = dr["vid_mes"].ToString().Trim()
                    }
                },
                registrationStartDate = (Convert.ToInt32(dr["nzp_tkrt"].ToString().Trim()) == 1) ?
                    dr["dat_ofor"].ToString().Trim() : "",
                registrationEndDate = (Convert.ToInt32(dr["nzp_tkrt"].ToString().Trim()) == 0) ?
                     dr["dat_ofor"].ToString().Trim() : ""
            };

            t.residentCountPeriods = new personalAccountNaturPersonCount() { count = Convert.ToInt16(dr["residents"].ToString().Trim()) };
            t.registeredPersonCountPeriods = new personalAccountNaturPersonCount() { count = Convert.ToInt16(dr["registered"].ToString().Trim()) };
            if (DataConvert.FieldValue(dr, "tprp", "").Trim() == "В")
                t.temporaryRegisteredPersons.Add(carp);
            else
                t.registeredPersons.Add(carp);

            return t;
        }

        public static measurementReading toMeasurementReading(DataRow dr)
        {
            measurementReading t = new measurementReading();
            t.communalResource = GetServiceCommunalResource(Convert.ToInt32(dr["nzp_kvar"].ToString().Trim()));
            t.measuringDeviceNumber = dr["num_cnt"].ToString().Trim();
            t.measuringDeviceCapacity = Convert.ToDecimal(dr["cnt_stage"].ToString().Trim());
            t.transformationCoefficient = Convert.ToDecimal(dr["mmnog"].ToString().Trim());
            return t;
        }

        public static managingOrganization toManagingOrganization(DataRow dr)
        {
            managingOrganization t = new managingOrganization();
            t.code = Convert.ToInt16(dr["nzp_area"].ToString().Trim());
            List<string> names = GetSeparateNamesFromFio(dr["ceo"].ToString().Trim());
            t.ceo.firstName = names[0];
            t.ceo.lastName = names[1];
            t.ceo.middleName = names[2];
            return t;
        }

        public static docLawDocType GetDocumentType(int docType)
        {
            switch (docType)
            {
                case 1:
                    {
                        return docLawDocType.PASSPORT_USSR;
                    }
                case 2:
                    {
                        return docLawDocType.BIRTH_CERTIFICATE;
                    }
                case 56:
                    {
                        return docLawDocType.TEMPORARY_IDENTITY_CARD_RF;
                    }
                case 57:
                    {
                        return docLawDocType.FOREIGN_PASSPORT;
                    }
                //доделать соответствие
                default:
                    {
                        return docLawDocType.OTHER_DOCUMENT;
                    }
            }
        }

        public static communalResource GetServiceCommunalResource(int serviceCode)
        {
            switch (serviceCode)
            {
                case 6:
                    {
                        return communalResource.COLD_WATER;
                    }
                case 8:
                    {
                        return communalResource.HEAT_ENERGY;
                    }
                case 9:
                    {
                        return communalResource.HOT_WATER;
                    }
                case 10:
                    {
                        return communalResource.GAS;
                    }
                case 25:
                    {
                        return communalResource.ELECTRIC_ENERGY;
                    }
                default:
                    {
                        return communalResource.DRAINAGE;
                    }
            }
        }

        /// <summary>
        /// тип населенного пункта
        /// </summary>
        /// <param name="rajon">район</param>
        /// <param name="town">город</param>
        /// <returns>true-город</returns>
        public static bool GetTownTypeSettlement(string rajon, string town)
        {
            if (rajon == "-")
            {
                if (town.Length > 2)
                {
                    if (town.Substring(town.Length - 2, 2) == "Г.")
                        return true;
                    else
                        return false;
                }
                return false;
            }
            else
            {
                if (rajon.Length > 2)
                {
                    if (rajon.Substring(rajon.Length - 2, 2) == "Г.")
                        return true;
                    else
                        return false;
                }
                return false;
            }
        }

        /// <summary>
        /// определение наличия/отсутствия: ванн, моек, душевых, умывальников
        /// </summary>
        /// <returns></returns>
        public static mabWashbasinType GetWashBasinType()
        {
            return mabWashbasinType.FULL_SCALE_BATHTUB;
        }

        /// <summary>
        /// определение наличия/отсутствия: ванн, водонагревателей
        /// </summary>
        /// <returns></returns>
        public static mabBathTubeType GetBathTubeType()
        {
            return mabBathTubeType.WATER_HEATER;
        }

        /// <summary>
        /// получение имени, фамилии, отчества из строки fio
        /// </summary>
        /// <param name="fio">строка fio</param>
        /// <returns></returns>
        public static List<string> GetSeparateNamesFromFio(string fio)
        {
            List<string> names = new List<string>();
            string[] fml = fio.Split(' ');
            for (int i = 0; i < 3; i++)
            {
                if (fml[i] != null)
                    names.Add(fml[i].Trim());
            }
            return names;
        }
    }

}
