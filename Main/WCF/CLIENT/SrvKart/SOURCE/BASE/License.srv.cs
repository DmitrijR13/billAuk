using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Global;
using STCLINE.KP50.DataBase;
using System.Runtime.InteropServices;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.Server
{
    /// <summary>
    /// Класс для работы с лицензиями программы
    /// </summary>
    public class srv_License : I_License
    {
        /// <summary>
        /// Список загруженных лицензий в памяти
        /// </summary>
        public Dictionary<int, License> licenses = new Dictionary<int, License>();

        #region Функции из CryptDES.dll
        /// <summary>
        /// Расшифровка полученного ответа с сервера лицензии
        /// </summary>
        /// <param name="key">Ключ ответа сервера лицензий</param>
        /// <param name="number">Номер лицензии пользователя</param>
        /// <param name="l1">Количество символов в ключе ответа</param>
        /// <param name="l2">Количество символов в номере лицензии</param>
        /// <param name="res">Признак успешной расшифровки</param>
        /// <param name="KeyID">идентификатор ключа активации</param>
        /// <param name="ReqCreateDate">Дата создания ключа запроса активации</param>
        /// <param name="CreateDate">Дата создания ключа активации</param>
        /// <param name="Item">Метка ключа активации</param>
        /// <param name="CompanyName">Название организации</param>
        /// <param name="CompanyCode">Код покупателя в реестре разработчика</param>
        /// <param name="CountLS">Количество лиц. счетов</param>
        /// <param name="Connections">Количество рабочих мест (одновременных подключений)</param>
        /// <param name="StartDate">Дата начала действия лицензии</param>
        /// <param name="StopDate">Дата окончания действия лицензии</param>
        /// <param name="MaxVer">Максимально допустимая версия программы</param>
        /// <param name="code_bank">Код банка (код получателя средств)</param>
        /// <param name="Options">Опции программы</param>
        /// <param name="nzp">nzp ключа из таблицы keys</param>
        [DllImport(@"CryptDES.dll")]
        public static extern void Decrypt(ref string key, ref string number, int l1, int l2, ref bool res,
            ref byte KeyID,
            ref DateTime ReqCreateDate,
            ref DateTime CreateDate,
            ref int Item,
            ref string CompanyName,
            ref Int64 CompanyCode,
            ref int CountLS,
            ref int Connections,
            ref DateTime StartDate,
            ref DateTime StopDate,
            ref string MaxVer,
            ref int code_bank,
            ref int Options,
            ref int nzp);

        /// <summary>
        /// Получает строку запроса к серверу лицензий на основе номера лицензии
        /// </summary>
        /// <param name="Lic">Номер лицензии</param>
        /// <param name="l1">Количество символов в номере лицензии (т.к. dll неправильно получает длину строки с номером лицензии)</param>
        /// <param name="res">Признак успешной генерации</param>
        /// <param name="resultKey">Полученная строка запроса</param>
        [DllImport(@"CryptDES.dll")]
        public static extern void Encrypt(ref string Lic, int l1, ref bool res, ref string resultKey);
        #endregion

        /// <summary>
        /// Проверка текущей лицензии
        /// </summary>
        /// <param name="nzp_user">код пользователя</param>
        /// <param name="key">ключ (null если не получен с сервера лицензий)</param>
        /// <returns>Возвращает релультат проверки ключа лицензии</returns>
        public Returns CheckLicense(int nzp_user, string key)
        {
            Returns ret = Utils.InitReturns();

            #region Проверка наличия ключа в списке загруженных ключей

            License lic;
            if (string.IsNullOrEmpty(key))
            {
                if (!licenses.TryGetValue(nzp_user, out lic) || (lic == null))
                {
                    // получение ключа из БД и закрузка в память
                    DbLicense db = new DbLicense();
                    lic = db.GetLicense(nzp_user, out ret);
                    if ((!ret.result) || (lic == null))
                        return ret;

                    licenses.Add(nzp_user, lic);
                    key = lic.key_value;
                }
            }
            #endregion

            #region Расшифровка ключа
            byte KeyID = new byte();
            DateTime ReqCreateDate = new DateTime();
            DateTime CreateDate = new DateTime();
            int Item = 0;
            string CompanyName = "";
            Int64 CompanyCode = new long();
            int CountLS = 0;
            int Connections = 0;
            DateTime StartDate = new DateTime();
            DateTime StopDate = new DateTime();
            string MaxVer = "";
            int code_bank = 0;
            int Options = 0;
            int nzp = 0;

            bool res = false; // разельтат успешности шифрования
            try
            {
                DbLicense db = new DbLicense();
                string number = db.GetLicenseNumber(nzp_user, out ret);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Не найдена лицензия для пользователя.", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = "Не найдена лицензия для пользователя.";
                    return ret;
                }
                Decrypt(ref key, ref number, key.Length, number.Length, ref res,
                    ref KeyID,
                    ref ReqCreateDate,
                    ref CreateDate,
                    ref Item,
                    ref CompanyName,
                    ref CompanyCode,
                    ref CountLS,
                    ref Connections,
                    ref StartDate,
                    ref StopDate,
                    ref MaxVer,
                    ref code_bank,
                    ref Options,
                    ref nzp);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка в библиотеке CryptDll: " + ex.ToString(), MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = "Ошибка в библиотеке CryptDll.";
                return ret;
            }
            #endregion

            #region Проверка ключа
            if (!res)
            {
                ret.result = false;
                ret.text = "Ключ не расшифрован!";
                return ret;
            }

            if (DateTime.Now > StopDate)
            {
                ret.result = false;
                ret.text = "Срок лицензии истек";
                return ret;
            }
            #endregion

            ret.result = true;
            return ret;
        }

        /// <summary>
        /// Получения ключа запроса по коду пользователя
        /// </summary>
        /// <param name="nzp_user">код пользователя</param>
        /// <returns>Возвращает строку для получения ключа активации с сервера лицензий</returns>
        public string GetRequestKey(int nzp_user)
        {
            string key = "";
            Returns ret = Utils.InitReturns();
            DbLicense db = new DbLicense();
            string Lic = db.GetLicenseNumber(nzp_user, out ret);
            bool res = false;

            Encrypt(ref Lic, Lic.Length, ref res, ref key);
            if (!res)
                throw new Exception("false");
            if (key == "")
                throw new Exception("Empty string");
            return key;
        }
    }
}
