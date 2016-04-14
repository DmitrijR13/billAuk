using System;
using System.Data;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using globalsUtils = STCLINE.KP50.Global.Utils;

namespace Bars.KP50.DataImport.SOURCE.COMPARE
{
    public partial class LinkData : SelectedFiles
    {
        /// <summary>
        /// автоматическое сопоставление населенных пунктов
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public Returns LinkRajonAutom(FilesImported finder)
        {
            using (RajonLinkAuto link = new RajonLinkAuto())
            {
                return link.AutoLinkData(finder);
            }
        }
        
        public Returns LinkEmptyStreetAutom(FilesImported finder)
        {
            using (EmptyStreetLinkAuto link = new EmptyStreetLinkAuto())
            {
                return link.AutoLinkData(finder);
            }
        }

        /// <summary>
        /// автоматическое сопоставление улиц
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public Returns LinkStreetAutom(FilesImported finder)
        {
            using (StreetLinkAuto link = new StreetLinkAuto())
            {
                return link.AutoLinkData(finder);
            }
        }

        /// <summary>
        /// автоматическое сопоставление ЛС
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public Returns LinkLsAutom(FilesImported finder)
        {
            using (LsAutoLink link = new LsAutoLink())
            {
                return link.AutoLinkData(finder);
            }
        }

        /// <summary>
        /// получение несопоставленных домов
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public Returns AddAllHouse(FilesImported finder)
        {
            using (HouseAutoLink link = new HouseAutoLink(AutoLinkMode.Add))
            {
                return link.AutoLinkData(finder);
            }
        }

        /// <summary>
        /// Автоматическое сопоставление домов без добавления
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns LinkHouseOnly(FilesImported finder)
        {
            using (HouseAutoLink link = new HouseAutoLink(AutoLinkMode.WithoutAdd))
            {
                return link.AutoLinkData(finder);
            }
        }

        /// <summary>
        /// Функция автоматического сопоставления юридических лиц с добавлением
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns LinkPayerWithAdd(FilesImported finder)
        {
            using (PayerAutoLink link = new PayerAutoLink(AutoLinkMode.Add))
            {
                return link.AutoLinkData(finder);
            }
        }

        /// <summary>
        /// Функция автоматического сопоставления юридических лиц
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns LinkPayerAutom(FilesImported finder)
        {
            using (PayerAutoLink link = new PayerAutoLink(AutoLinkMode.WithoutAdd))
            {
                return link.AutoLinkData(finder);
            }
        }

        /// <summary>
        /// Функция автоматического сопоставления параметров
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns LinkParAutomWithAdd(FilesImported finder)
        {
            using (ParameterAutoLink link = new ParameterAutoLink(AutoLinkMode.Add))
            {
                return link.AutoLinkData(finder);
            }
        }

        /// <summary>
        /// Функция автоматического сопоставления параметров
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns LinkParamsAutom(FilesImported finder)
        {
            using (ParameterAutoLink link = new ParameterAutoLink(AutoLinkMode.WithoutAdd))
            {
                return link.AutoLinkData(finder);
            }
        }

        /// <summary>
        /// Функция автоматического сопоставления услуг
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns LinkServiceAuto(FilesImported finder)
        {
            using (ServiceAutoLink link = new ServiceAutoLink())
            {
                return link.AutoLinkData(finder);
            }
        }
    }
}
