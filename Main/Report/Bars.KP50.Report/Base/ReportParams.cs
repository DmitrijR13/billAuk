using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.Report.Base
{
    using System;

    using Castle.Windsor;

    using Globals.SOURCE.Config;
    using System.Collections.Generic;

    /// <summary>Параметры отчета</summary>
    public class ReportParams
    {
        private readonly IWindsorContainer _container;

        public ReportParams(IWindsorContainer container)
        {
            _container = container;
        }

        /// <summary>Идентификатор пользователя</summary>
        public BaseUser User { get; set; }
      

        /// <summary>Идентификатор записи в таблице excel_utility</summary>
        public int NzpExcelUtility { get; set; }

        /// <summary>Строка соединения</summary>
        public string ConnectionString
        {
            get { return _container.Resolve<IConfigProvider>().GetConfig().MainDbParams.ConnectionString; }
        }

        /// <summary>Путь сохранения отчета</summary>
        public string PathForSave
        {
            get;
            set;
            //get { return _container.Resolve<IConfigProvider>().GetConfig().Directories.ReportDir; }
        }

        /// <summary>Префикс</summary>
        public string Pref
        {
            get { return _container.Resolve<IConfigProvider>().GetConfig().MainDbParams.MainSchemaName; }
        }

        /// <summary>Расчетный месяц</summary>
        public DateTime CalcMonth { get; set; }

        /// <summary>Префикс</summary>
        public DateTime CurDateOper { get; set; }

        /// <summary>Номер записи жильца/дома/лицевого счета</summary>
        public int NzpObject { get; set; }

        /// <summary> Текущий режим запуска отчета </summary>
        public ReportKind CurrentReportKind { get; set; }

        /// <summary>Формат экспорта</summary>
        public ExportFormat ExportFormat { get; set; }

        /// <summary>
        /// Ограничения наложенные на пользователя
        /// </summary>
        /// <param name="typeCondition">Сущности Constants.role_sql_serv и т д.</param>
        /// <returns>Список кодов сущностей </returns>
        public string GetRolesCondition(long typeCondition)
        {
            if (User.RolesVal == null) return "";

            foreach (_RolesVal role in User.RolesVal)
            {
                if (role.tip == Constants.role_sql)
                    if (role.kod == typeCondition) return role.val;
            }
            return "";
        }
    }
}