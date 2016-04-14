namespace STCLINE.KP50.DataBase
{
    using System;
    using System.IO;
    
    /// <summary>Вспомогательный класс для работы с директориями</summary>
    public static class PathHelper
    {
        /// <summary>Получить текущую директорию</summary>
        /// <returns>Путь до текущей директории</returns>
        public static string GetCurrentDirectory()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        /// <summary>Получить путь до директории отчетных шаблонов</summary>
        /// <returns>Путь до директории отчетных шаблонов</returns>
        public static string GetReportTemplateDirectory()
        {
            return Path.Combine(GetCurrentDirectory(), "Template");
        }

        /// <summary>Получить полный путь до шаблона отчета</summary>
        /// <param name="templateName">Имя шаблона</param>
        /// <returns>Путь до шаблона</returns>
        public static string GetReportTemplatePath(string templateName)
        {
            return Path.Combine(GetReportTemplateDirectory(), templateName);
        }
    }
}