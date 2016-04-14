namespace Globals.SOURCE.Config
{
    public class Directories
    {
        private const string ReportsDir = "reports/";
        private const string BILL_DIR = "bill/";
        private const string WebDir = "web/";
        private const string IMPORT_DIR = "import/";

        private const string SubsidyDir = "subsidy/";
        private const string ACTS_OF_SUPPLY_DIR = "acts/";
        private const string ThgfDir = "thgf/";

        private string _filesDir = "~/files/";

        private string _absoluteRootPath = string.Empty;

        /// <summary>Устанавливает абсолютный путь корня сайта</summary>
        /// <param name="path">Путь к корню сайта</param>
        public void SetRootAbsolutePath(string path)
        {
            _absoluteRootPath = path;
        }

        /// <summary>Путь к папке с платежными документами</summary>
        public string BillDir
        {
            get { return FilesDir + BILL_DIR; }
        }

        /// <summary>Путь к папке с платежными документами</summary>
        public string BillAbsoluteDir
        {
            get { return BillDir.Replace("~", _absoluteRootPath); }
        }

        /// <summary>Путь к папке с платежными документами для портала</summary>
        public string BillWebDir
        {
            get { return BillDir + WebDir; }
        }

        /// <summary>Путь к папке с платежными документами для портала</summary>
        public string BillWebAbsoluteDir
        {
            get { return BillWebDir.Replace("~", _absoluteRootPath); }
        }

        /// <summary>Путь к папке с выходными документами</summary>
        public string FilesDir
        {
            get { return _filesDir; }
            set { _filesDir = value; }
        }

        /// <summary>Путь к папке с отчетами</summary>
        public string ReportDir
        {
            get { return FilesDir + ReportsDir; }
        }

        /// <summary>Абсолютный путь к папке с отчетами</summary>
        public string ReportsAbsoluteDir
        {
            get { return ReportDir.Replace("~", _absoluteRootPath); }
        }

        /// <summary>Путь к папке со входными документами</summary>
        public string ImportDir
        {
            get { return FilesDir + IMPORT_DIR; }
        }

        /// <summary>Путь к папке со входными документами</summary>
        public string ImportAbsoluteDir
        {
            get { return ImportDir.Replace("~", _absoluteRootPath); }
        }

        /// <summary>Акты о фактической поставке</summary>
        public string ActsOfSupplyDir
        {
            get { return FilesDir + SubsidyDir + ACTS_OF_SUPPLY_DIR; }
        }

        /// <summary>Акты о фактической поставке</summary>
        public string ActsOfSupplyAbsoluteDir
        {
            get { return ActsOfSupplyDir.Replace("~", _absoluteRootPath); }
        }

        /// <summary>ТХЖФ</summary>
        public string THGFDir
        {
            get { return FilesDir + SubsidyDir + ThgfDir; }
        }

        /// <summary>ТХЖФ</summary>
        public string THGFAbsoluteDir
        {
            get { return THGFDir.Replace("~", _absoluteRootPath); }
        }
    }
}