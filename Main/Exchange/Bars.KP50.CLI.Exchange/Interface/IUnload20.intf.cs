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
    public interface IUnload20
    {
        /// <summary>Уникальный идентификатор</summary>
        int Code { get; }

        /// <summary>Название</summary>
        string Name { get; }

        string NameText { get; }

        StreamWriter Writer { get; set; }

        List<FieldsUnload> Data { get; set; }

        /// <summary>Выполнить</summary>
        void Start();

        /// <summary>Выполнить</summary>
        /// /// <param name="pref">Префикс</param>
        void Start(string pref);

        /// <summary>Выполнить</summary>
        void StartSelect();

        /// <summary>Создать временные таблицы</summary>
        void CreateTempTable();

        /// <summary>Удалить временные таблицы</summary>
        void DropTempTable();
    }

    

}
