using System;
using System.Collections.Generic;
using System.Data;
using System.ServiceModel;
using STCLINE.KP50.Client;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.EPaspXsd;
using System.Xml.Linq;
using Globals.SOURCE.INTF._EPasp.classes;
using System.IO;

namespace STCLINE.KP50.Server
{

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class srv_EPasp : srv_Base, I_EPasp
    {
        /// <summary>
        /// формирование Xml файла
        /// </summary>
        /// <param name="_year">год</param>
        /// <param name="_month">месяц</param>
        /// <param name="_nzp_dom">идентификатор дома</param>
        /// <returns></returns>
        public IntfResultType PrepareEPaspXml(int _year, int _month, int _nzp_dom)
        {
            IntfResultType result = new IntfResultType();
            try
            {
                monthWithYear _monthWithYear = new monthWithYear()
                {
                    month = Convert.ToInt16(_month),
                    year = Convert.ToInt16(_year)
                };

                EPaspReqPrm epaspReqPrm = new EPaspReqPrm()
                {
                    year = _year,
                    month = _month,
                    nzp_dom = _nzp_dom
                };

                //создание xml файла
                string fileName = "resultStructure.xml";
                File.Create(fileName).Dispose();
                File.AppendAllText(fileName, "<report>");
                File.AppendAllText(fileName, "<month>" + _month + "</month>");
                File.AppendAllText(fileName, "<year>" + _year + "</year>");
                
                #region managingOrganization                
                managingOrganization _org = new managingOrganization();
                {
                    IntfResultObjectType<managingOrganization> res =
                        new ClassDB().RunSqlAction(new IntfRequestObjectType<EPaspReqPrm>(epaspReqPrm), new DbEPasp().SelectManagingOrganization);
                    res.GetReturnsType().ThrowExceptionIfError();
                    _org = res.GetData();
                }
                File.AppendAllText(fileName, "<managingOrganization>");
                    File.AppendAllText(fileName, "<code>" + _org.code + "</code>");
                    File.AppendAllText(fileName, "<ceo>");
                        File.AppendAllText(fileName, "<lastName>" + _org.ceo.lastName + "</lastName>");
                        File.AppendAllText(fileName, "<firstName>" + _org.ceo.firstName + "</firstName>");
                        File.AppendAllText(fileName, "<middleName>" + _org.ceo.middleName + "</middleName>");
                    File.AppendAllText(fileName, "</ceo>");
                File.AppendAllText(fileName, "</managingOrganization>");
                #endregion managingOrganization

                #region получение списка домов
                List<custom_multiApartmentBuilding> _buildings = new List<custom_multiApartmentBuilding>();
                {
                    IntfResultObjectType<custom_multiApartmentBuilding> res =
                        new ClassDB().RunSqlAction(new IntfRequestObjectType<EPaspReqPrm>(epaspReqPrm), new DbEPasp().SelectMultiApartmentBuilding);
                    res.GetReturnsType().ThrowExceptionIfError();
                    custom_multiApartmentBuilding b = res.GetData();
                    _buildings.Add(b);
                }
                foreach (var apartmentBuilding in _buildings)
                {
                    File.AppendAllText(fileName, "<buildings>");
                        File.AppendAllText(fileName, "<uniqueNumber>" + apartmentBuilding.uniqueNumber + "</uniqueNumber>");
                        File.AppendAllText(fileName, "<addressFiasGuid>" + apartmentBuilding.addressFiasGuid + "</addressFiasGuid>");
                        File.AppendAllText(fileName, "<address>" + apartmentBuilding.address + "</address>");
                        File.AppendAllText(fileName, "<residentCount>" + apartmentBuilding.residentCount + "</residentCount>");
                        File.AppendAllText(fileName, "<personalAccountCount>" + apartmentBuilding.personalAccountCount + "</personalAccountCount>");

                        #region resourceSupplyOrganizations
                        epaspReqPrm.nzp_dom = Convert.ToInt32(apartmentBuilding.uniqueNumber);
                        epaspReqPrm.pref = apartmentBuilding.pref;
                        List<custom_mabResourceSupplyOrganization> _resources = new List<custom_mabResourceSupplyOrganization>();
                        {
                            IntfResultObjectType<List<custom_mabResourceSupplyOrganization>> res =
                                new ClassDB().RunSqlAction(new IntfRequestObjectType<EPaspReqPrm>(epaspReqPrm), new DbEPasp().SelectResourceSupplyOrganization);
                            res.GetReturnsType().ThrowExceptionIfError();
                            _resources = res.GetData();
                        }

                        foreach (var service in _resources)
                        {
                            //формирование списка resourceSupplyOrganizations
                            File.AppendAllText(fileName, "<resourceSupplyOrganizations>");
                                File.AppendAllText(fileName, "<communalResource>" + service.communalResource.GetHashCode() + "</communalResource>");
                                File.AppendAllText(fileName, "<serviceProvider>");
                                File.AppendAllText(fileName, "<registrationReasonCode>" + service.serviceProvider.registrationReasonCode + "</registrationReasonCode>");
                                File.AppendAllText(fileName, "<name>" + service.serviceProvider.name + "</name>");
                                File.AppendAllText(fileName, "<inn>" + service.serviceProvider.inn + "</inn>");
                                File.AppendAllText(fileName, "<juridicalAddress>" + service.serviceProvider.juridicalAddress + "</juridicalAddress>");
                                File.AppendAllText(fileName, "</serviceProvider>");
                                File.AppendAllText(fileName, "<supplyStartDate>" + service.supplyStartDate + "</supplyStartDate>");
                                File.AppendAllText(fileName, "<supplyEndDate>" + service.supplyEndDate + "</supplyEndDate>");
                            File.AppendAllText(fileName, "</resourceSupplyOrganizations>");
                        }
                        #endregion

                        #region communalServiceProviders
                        epaspReqPrm.nzp_dom = Convert.ToInt32(apartmentBuilding.uniqueNumber);
                        epaspReqPrm.pref = apartmentBuilding.pref;
                        List<custom_mabResourceSupplyOrganization> _serviceProviders = new List<custom_mabResourceSupplyOrganization>();
                        {
                            IntfResultObjectType<List<custom_mabResourceSupplyOrganization>> res =
                                new ClassDB().RunSqlAction(new IntfRequestObjectType<EPaspReqPrm>(epaspReqPrm), new DbEPasp().SelectResourceSupplyOrganization);
                            res.GetReturnsType().ThrowExceptionIfError();
                            _resources = res.GetData();
                        }

                        foreach (var provider in _serviceProviders)
                        {
                            //формирование списка communalServiceProviders
                            File.AppendAllText(fileName, "<communalServiceProviders>");
                                File.AppendAllText(fileName, "<communalResource>" + provider.communalResource.GetHashCode() + "</communalResource>");
                                File.AppendAllText(fileName, "<serviceProvider>");
                                File.AppendAllText(fileName, "<registrationReasonCode>" + provider.serviceProvider.registrationReasonCode + "</registrationReasonCode>");
                                File.AppendAllText(fileName, "<name>" + provider.serviceProvider.name + "</name>");
                                File.AppendAllText(fileName, "<inn>" + provider.serviceProvider.inn + "</inn>");
                                File.AppendAllText(fileName, "<juridicalAddress>" + provider.serviceProvider.juridicalAddress + "</juridicalAddress>");
                                File.AppendAllText(fileName, "</serviceProvider>");
                                File.AppendAllText(fileName, "<supplyStartDate>" + provider.supplyStartDate + "</supplyStartDate>");
                                File.AppendAllText(fileName, "<supplyEndDate>" + provider.supplyEndDate + "</supplyEndDate>");
                            File.AppendAllText(fileName, "</communalServiceProviders>");
                        }
                        #endregion

                        #region flats
                        epaspReqPrm.nzp_dom = Convert.ToInt32(apartmentBuilding.uniqueNumber);
                        epaspReqPrm.pref = apartmentBuilding.pref;
                        List<custom_flat> _flats = new List<custom_flat>();
                        {
                            IntfResultObjectType<List<custom_flat>> res =
                                new ClassDB().RunSqlAction(new IntfRequestObjectType<EPaspReqPrm>(epaspReqPrm), new DbEPasp().SelectFlats);
                            res.GetReturnsType().ThrowExceptionIfError();
                            _flats = res.GetData();
                        }
                        foreach (var flat in _flats)
                        {
                            foreach (var r in flat.personalAccount.registeredPersons)
                            {
                                File.AppendAllText(fileName, "<flats>");
                                File.AppendAllText(fileName, "<uniqueNumber>" + flat.uniqueNumber + "</uniqueNumber>");
                                File.AppendAllText(fileName, "<number>" + flat.number + "</number>");
                                File.AppendAllText(fileName, "<entrance>" + flat.entrance + "</entrance>");
                                File.AppendAllText(fileName, "<floor>" + flat.floor + "</floor>");
                                File.AppendAllText(fileName, "<totalArea>" + flat.totalArea + "</totalArea>");
                                File.AppendAllText(fileName, "<livingArea>" + flat.livingArea + "</livingArea>");
                                File.AppendAllText(fileName, "<roomCount>" + flat.roomCount + "</roomCount>");
                                File.AppendAllText(fileName, "<resourceInputs></resourceInputs>");
                                File.AppendAllText(fileName, "<personalAccount>");
                                    foreach (var t in flat.personalAccount.registeredPersons)
                                    {
                                        File.AppendAllText(fileName, "<registeredPersons>");
                                            File.AppendAllText(fileName, "<naturPerson>");
                                                File.AppendAllText(fileName, "<id>" + r.naturPerson.id + "</id>");
                                                File.AppendAllText(fileName, "<fio>" + r.naturPerson.fio + "</fio>");
                                                File.AppendAllText(fileName, "<birthDate>" + r.naturPerson.birthDate + "</birthDate>");
                                                File.AppendAllText(fileName, "<docLawDoc>");
                                                    File.AppendAllText(fileName, "<type>" + r.naturPerson.docLawDoc.type + "</type>");
                                                    File.AppendAllText(fileName, "<series>" + r.naturPerson.docLawDoc.series + "</series>");
                                                    File.AppendAllText(fileName, "<number>" + r.naturPerson.docLawDoc.number + "</number>");
                                                    File.AppendAllText(fileName, "<date>" + r.naturPerson.docLawDoc.date + "</date>");
                                                    File.AppendAllText(fileName, "<govName>" + r.naturPerson.docLawDoc.govName + "</govName>");
                                                File.AppendAllText(fileName, "</docLawDoc>");
                                                File.AppendAllText(fileName, "<registrationStartDate>" + r.registrationStartDate + "</registrationStartDate>");
                                                File.AppendAllText(fileName, "<registrationEndDate>" + r.registrationEndDate + "</registrationEndDate>");
                                            File.AppendAllText(fileName, "</naturPerson>");
                                        File.AppendAllText(fileName, "</registeredPersons>");
                                    }
                                    foreach (var t in flat.personalAccount.temporaryRegisteredPersons)
                                    {
                                        File.AppendAllText(fileName, "<temporaryRegisteredPersons>");
                                        File.AppendAllText(fileName, "<naturPerson>");
                                            File.AppendAllText(fileName, "<id>" + r.naturPerson.id + "</id>");
                                            File.AppendAllText(fileName, "<fio>" + r.naturPerson.fio + "</fio>");
                                            File.AppendAllText(fileName, "<birthDate>" + r.naturPerson.birthDate + "</birthDate>");
                                            File.AppendAllText(fileName, "<docLawDoc>");
                                                File.AppendAllText(fileName, "<type>" + r.naturPerson.docLawDoc.type + "</type>");
                                                File.AppendAllText(fileName, "<series>" + r.naturPerson.docLawDoc.series + "</series>");
                                                File.AppendAllText(fileName, "<number>" + r.naturPerson.docLawDoc.number + "</number>");
                                                File.AppendAllText(fileName, "<date>" + r.naturPerson.docLawDoc.date + "</date>");
                                                File.AppendAllText(fileName, "<govName>" + r.naturPerson.docLawDoc.govName + "</govName>");
                                            File.AppendAllText(fileName, "</docLawDoc>");
                                            File.AppendAllText(fileName, "<registrationStartDate>" + r.registrationStartDate + "</registrationStartDate>");
                                            File.AppendAllText(fileName, "<registrationEndDate>" + r.registrationEndDate + "</registrationEndDate>");
                                        File.AppendAllText(fileName, "</naturPerson>");
                                        File.AppendAllText(fileName, "</temporaryRegisteredPersons>");
                                    }
                                    File.AppendAllText(fileName, "<residentCountPeriods>" + flat.personalAccount.residentCountPeriods + "</residentCountPeriods>");
                                    File.AppendAllText(fileName, "<registeredPersonCountPeriods>" + flat.personalAccount.registeredPersonCountPeriods + "</registeredPersonCountPeriods>");
                                File.AppendAllText(fileName, "</personalAccount>");
                                File.AppendAllText(fileName, "<rooms></rooms>");

                                foreach (var measureRe in flat.measurementReading)
                                {
                                    File.AppendAllText(fileName, "<measurementReadings>");
                                        File.AppendAllText(fileName, "<communalResource>" + measureRe.communalResource.GetHashCode() + "</communalResource>");
                                        File.AppendAllText(fileName, "<measuringDeviceNumber>" + measureRe.measuringDeviceNumber + "</measuringDeviceNumber>");
                                        File.AppendAllText(fileName, "<measuringDeviceCapacity>" + measureRe.measuringDeviceCapacity + "</measuringDeviceCapacity>");
                                        File.AppendAllText(fileName, "<transformationCoefficient>" + measureRe.transformationCoefficient + "</transformationCoefficient>");
                                        File.AppendAllText(fileName, "<indiccurBeginDay></indiccurBeginDay>");
                                        File.AppendAllText(fileName, "<indiccurEndDay></indiccurEndDay>");
                                        File.AppendAllText(fileName, "<indiccurBeginNight></indiccurBeginNight>");
                                        File.AppendAllText(fileName, "<indiccurEndNight></indiccurEndNight>");
                                        File.AppendAllText(fileName, "<indiccurBeginPeak></indiccurBeginPeak>");
                                        File.AppendAllText(fileName, "<indiccurEndPeak></indiccurEndPeak>");
                                    File.AppendAllText(fileName, "</measurementReadings>");
                                }
                                File.AppendAllText(fileName, "</flats>");
                            }
                        }
                        #endregion

                        #region accrualParameters
                        mabAccrualParameters _params = new mabAccrualParameters();
                        {
                            IntfResultObjectType<mabAccrualParameters> res =
                                new ClassDB().RunSqlAction(new IntfRequestObjectType<EPaspReqPrm>(epaspReqPrm), new DbEPasp().SelectAccrualParameters);
                            res.GetReturnsType().ThrowExceptionIfError();
                            _params = res.GetData();
                        }
                        File.AppendAllText(fileName, "<accrualParameters>");
                            File.AppendAllText(fileName, "<isTownTypeSettlement>" + _params.isTownTypeSettlement + "</isTownTypeSettlement>");
                            File.AppendAllText(fileName, "<hasElectricRanges>" + _params.hasElectricRanges + "</hasElectricRanges>");
                            File.AppendAllText(fileName, "<coldWaterTypeWell>" + _params.coldWaterTypeWell.GetHashCode() + "</coldWaterTypeWell>");
                            File.AppendAllText(fileName, "<mabBathTubeType>" + _params.mabBathTubeType.GetHashCode() + "</mabBathTubeType>");
                            File.AppendAllText(fileName, "<mabWashbasinType>" + _params.mabWashbasinType.GetHashCode() + "</mabWashbasinType>");
                        File.AppendAllText(fileName, "</accrualParameters>");
                        #endregion

                        File.AppendAllText(fileName, "<communalResourceServices>");
                            
                        File.AppendAllText(fileName, "</communalResourceServices>");

                        File.AppendAllText(fileName, "</buildings>");
                }
                #endregion

                File.AppendAllText(fileName, "</report>");
                return result;
            }
            catch (Exception ex)
            {
                Returns ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
                return new IntfResultType(-1, ret.text);
            }

        }


        public IntfResultType SelectServiceSample()
        {
            try
            {
                IntfResultObjectType<List<mabService>> res =
                    new ClassDB().RunSqlAction(new IntfRequestType(), new DbEPasp().SelectMabServiceList);
                    //ClassDB.RunSqlAction(new IntfRequestType(), new DbEPasp().SelectMabServiceList);
                res.GetReturnsType().ThrowExceptionIfError();
                return new IntfResultType();
            }
            catch (Exception ex)
            {
                Returns ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
                return new IntfResultType(-1, ret.text);
            }
        }
    }

}
