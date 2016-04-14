using System;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Unloader.Models
{
    public class Header
    {
        public string formatVersion
        {
            get { return "0.9"; }
        }
        public string nameOrgPasses { get; set; }
        public string podrOrgPasses { get; set; }
        public string INN { get; set; }
        private string _KPP { get; set; }
        public string KPP
        {
            get { return string.IsNullOrEmpty(_KPP) ? "0" : _KPP; }
            set { _KPP = value; }
        }
        public string raschSchet { get; set; }
        public int? fileNumber { get; set; }
        public string fileDate
        {
            get { return DateTime.Now.ToShortDateString(); }
        }
        public string passNumber { get; set; }
        public string passName { get; set; }
        private DateTime _chargeDate { get; set; }
        public string chargeDate
        {
            get { return _chargeDate.ToShortDateString(); }
            set { _chargeDate = DateTime.Parse(value); }
        }
        public int? lsCount { get; set; }
        public string GetValues()
        {
            var strs = GetType().GetProperties()
            .Select(p => (p.GetValue(this, null) == null ? "" : p.GetValue(this, null).ToString().Trim()));
            return string.Join("|", strs);
        }
    }

    public class HarGilFond
    {
        public int lineType
        {
            get { return 2; }
        }
        private DateTime _chargeDate { get; set; }
        public string chargeDate
        {
            get { return _chargeDate.ToShortDateString(); }
            set { _chargeDate = DateTime.Parse(value); }
        }
        public string kodRashcCentra { get; set; }
        public decimal? pkod { get; set; }
        public int? numberLS { get; set; }
        public string town { get; set; }
        public string district { get; set; }
        public string street { get; set; }
        public string houseNumber { get; set; }
        private string _housingNumber { get; set; }
        public string housingNumber
        {
            get { return string.IsNullOrEmpty(_housingNumber) ? "-" : _housingNumber; }
            set { _housingNumber = value; }
        }
        public string flatNumber { get; set; }
        private string _roomNumber { get; set; }
        public string roomNumber
        {
            get { return string.IsNullOrEmpty(_roomNumber) ? "-" : _roomNumber; }
            set { _roomNumber = value; }
        }
        public int? porchNumber { get; set; }
        public string ownerName { get; set; }
        public string managmentCompany { get; set; }
        public int? comfort { get; set; }
        public int? privatizated { get; set; }
        public int? floor { get; set; }
        public int? flatOnFloor { get; set; }
        public decimal? jointFlatSquare { get; set; }
        public decimal? livingFlatSquare { get; set; }
        public decimal? heatedFlatSquare { get; set; }
        public decimal? joinedHouseSquare { get; set; }
        public decimal? commonPlacesSquare { get; set; }
        public decimal? heatedHouseSquare { get; set; }
        public int? livingCount { get; set; }
        public int? temporaryElemenatedCount { get; set; }
        public int? temporaryArrivedCount { get; set; }
        public int? roomsCount { get; set; }
        public string GetValues()
        {
            var strs = GetType().GetProperties()
            .Select(p => (p.GetValue(this, null) == null ? "" : p.GetValue(this, null).ToString().Trim()));
            return string.Join("|", strs);
        }
    }

    public class ServiceCharges
    {
        public int lineType
        {
            get { return 3; }
        }
        private DateTime _chargeDate { get; set; }
        public string chargeDate
        {
            get { return _chargeDate.ToShortDateString(); }
            set { _chargeDate = DateTime.Parse(value); }
        }
        public string kodRashcCentra { get; set; }
        public decimal? pkod { get; set; }
        public string services { get; set; }
        public string measure { get; set; }
        public int? order_print { get; set; }
        public int? serviceKod { get; set; }
        public int? baseServiceKod { get; set; }
        public string serviceGroup { get; set; }
        public decimal? tarif { get; set; }
        public decimal? servExpense { get; set; }
        public decimal? servODNExpensive { get; set; }
        public decimal? ipuExpensive { get; set; }
        public decimal? normExpensive { get; set; }
        public decimal? houseExpensive { get; set; }
        public decimal? flatExpensive { get; set; }
        public decimal? flatIPUExpensive { get; set; }
        public decimal? flatNotIPUExpensive { get; set; }
        public decimal? notLiveExpensive { get; set; }
        public decimal? liftExpensive { get; set; }
        public decimal? houseODNExpensive { get; set; }
        public decimal? odpuExpensive { get; set; }
        public decimal? chargeTarif { get; set; }
        public decimal? chargeTarifNedop { get; set; }
        public decimal? sumNedop { get; set; }
        public decimal? nedopExpensive { get; set; }
        public decimal? countNedop { get; set; }
        public decimal? sumRecalcPrevPeriod { get; set; }
        public decimal? sumChangeSaldo { get; set; }
        public decimal? sumChargePayment { get; set; }
        public decimal? sumPaymentEPD { get; set; }
        public decimal? sumOutsaldo { get; set; }
        public decimal? sumInsaldo { get; set; }
        public decimal? chargeTarifODN { get; set; }
        public decimal? sumOutsaldoODN { get; set; }
        public decimal? sumInsaldoODN { get; set; }
        public decimal? recalcODN { get; set; }
        public decimal? changeSaldoODN { get; set; }
        public decimal? chargePaymentODN { get; set; }
        public decimal? paymentEpdOdn { get; set; }
        public decimal? chargeSocNorm { get; set; }
        public decimal? koeffKorrectIPU { get; set; }
        public decimal? koeffKorrectNorm { get; set; }
        public string GetValues()
        {
            var strs = GetType().GetProperties()
            .Select(p => (p.GetValue(this, null) == null ? "" : p.GetValue(this, null).ToString().Trim()));
            return string.Join("|", strs);
        }
    }

    public class Counters
    {
        public int lineType
        {
            get { return 4; }
        }
        private DateTime _chargeDate { get; set; }
        public string chargeDate
        {
            get { return _chargeDate.ToShortDateString(); }
            set { _chargeDate = DateTime.Parse(value); }
        }
        public string kodRashcCentra { get; set; }
        public decimal? pkod { get; set; }
        public string services { get; set; }
        public string measure { get; set; }
        public string orderCounter { get; set; }
        public int? counterType { get; set; }
        public string numberCounter { get; set; }
        public int? counterKod { get; set; }
        private DateTime? _counterDate { get; set; }
        public string counterDate
        {
            get { return _counterDate != null ? _counterDate.Value.ToShortDateString() : null; }
            set { _counterDate = DateTime.Parse(value); }
        }
        public decimal? counterValue { get; set; }
        private DateTime? _counterPrevDate { get; set; }
        public string counterPrevDate
        {
            get
            {
                return _counterPrevDate != null ? _counterPrevDate.Value.ToShortDateString() : null;
            }
            set { _counterPrevDate = DateTime.Parse(value); }
        }
        public decimal? counterPrevValue { get; set; }
        public decimal? resizer { get; set; }
        public decimal? expensive { get; set; }
        public decimal? addedExpensive { get; set; }
        public decimal? notLiveExpensive { get; set; }
        public decimal? liftExpensive { get; set; }
        public decimal? counterPlace { get; set; }
        public string GetValues()
        {
            var strs = GetType().GetProperties()
            .Select(p => (p.GetValue(this, null) == null ? "" : p.GetValue(this, null).ToString().Trim()));
            return string.Join("|", strs);
        }
    }

    public class PaymentAccount
    {
        public int lineType
        {
            get { return 5; }
        }
        private DateTime _chargeDate { get; set; }
        public string chargeDate
        {
            get { return _chargeDate.ToShortDateString(); }
            set { _chargeDate = DateTime.Parse(value); }
        }
        public string kodRashcCentra { get; set; }
        public decimal? pkod { get; set; }
        public int? lineTypeReqvezit { get; set; }
        public string receiverName { get; set; }
        public string receiverBankName { get; set; }
        public string rashSchetReceiver { get; set; }
        public string korSchetBankReceiver { get; set; }
        public string bikBankReceiver { get; set; }
        public string receiverAddress { get; set; }
        public string receiverPhone { get; set; }
        public string receiverEmail { get; set; }
        public string receiverComment { get; set; }
        public string performerName { get; set; }
        public string performerBankName { get; set; }
        public string rashSchetPerformer { get; set; }
        public string korSchetBankPerformer { get; set; }
        public string bikBankPerformer { get; set; }
        public string performerAddress { get; set; }
        public string performerPhone { get; set; }
        public string performerEmail { get; set; }
        public string performerComment { get; set; }
        public string ukKod { get; set; }
        public string freedomText { get; set; }
        public string GetValues()
        {
            var strs = GetType().GetProperties()
            .Select(p => (p.GetValue(this, null) == null ? "" : p.GetValue(this, null).ToString().Trim()));
            return string.Join("|", strs);
        }
    }

    public class Payment
    {
        public int lineType
        {
            get { return 6; }
        }
        private DateTime _chargeDate { get; set; }
        public string chargeDate
        {
            get { return _chargeDate.ToShortDateString(); }
            set { _chargeDate = DateTime.Parse(value); }
        }
        public string kodRashcCentra { get; set; }
        public decimal? pkod { get; set; }
        private DateTime _paymentDate { get; set; }
        public string paymentDate
        {
            get { return _paymentDate.ToShortDateString(); }
            set { _paymentDate = DateTime.Parse(value); }
        }
        private DateTime _paymentRegistrationDate { get; set; }
        public string paymentRegistrationDate
        {
            get { return _paymentRegistrationDate.ToShortDateString(); }
            set { _paymentRegistrationDate = DateTime.Parse(value); }
        }
        private DateTime _paymentMonth { get; set; }
        public string paymentMonth
        {
            get { return _paymentMonth.ToShortDateString(); }
            set { _paymentMonth = DateTime.Parse(value); }
        }
        public decimal? paymentSum { get; set; }
        public string paymentPlace { get; set; }
        public string GetValues()
        {
            var strs = GetType().GetProperties()
            .Select(p => (p.GetValue(this, null) == null ? "" : p.GetValue(this, null).ToString().Trim()));
            return string.Join("|", strs);
        }
    }

    public class InformationSocPretender
    {
        public int lineType
        {
            get { return 7; }
        }
        private DateTime _chargeDate { get; set; }
        public string chargeDate
        {
            get { return _chargeDate.ToShortDateString(); }
            set { _chargeDate = DateTime.Parse(value); }
        }
        public string kodRashcCentra { get; set; }
        public decimal? pkod { get; set; }
        public string receiverName { get; set; }
        public string financeArticle { get; set; }
        public string financeArticleGroup { get; set; }
        public decimal? chargeSum { get; set; }
        public decimal? paymentSum { get; set; }
        public string paymentPlace { get; set; }
        public string subsidyFinanceDate { get; set; }
        public string GetValues()
        {
            var strs = GetType().GetProperties()
            .Select(p => (p.GetValue(this, null) == null ? "" : p.GetValue(this, null).ToString().Trim()));
            return string.Join("|", strs);
        }
    }
}
