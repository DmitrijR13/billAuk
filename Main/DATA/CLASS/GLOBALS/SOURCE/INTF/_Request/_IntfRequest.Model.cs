//--------------------------------------------------------------------------------80
//Файл: _IntfResult.Model.cs
//Дата создания: 25.09.2012
//Дата изменения: 25.09.2012
//Назначение: Описание сериализуемых классов для возврата кода и сообщения о результатах выполнения
//Автор: Зыкин А.А.
//Copyright (c) Научно-технический центр "Лайн", 2012. 
//--------------------------------------------------------------------------------80
using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Interfaces;
using System.Runtime.Serialization;
using STCLINE.KP50.Global;

namespace STCLINE.KP50.Interfaces
{


    public class FinderObjectType<T> : Finder where T : class, new()
    {
        public T entity;

        public FinderObjectType() : this(null) { }

        public FinderObjectType(T entity)
        {
            this.entity = entity;
        }

    }

    /// <summary>
    /// 
    /// </summary>
    public class IntfRequestType
    {
        public RequestPaging paging;
        public decimal keyID;
        public decimal parentID;
    }

    /// <summary>
    /// на входе: доп. информация о страницах/строках
    /// </summary>
    public class RequestPaging
    {
        /// <summary>
        /// номер страницы для выгрузки
        /// </summary>
        public Int32 curPageNumber { get; set; }
    }


    /// <summary>
    /// на выходе: доп. инфорация о страницах/строках
    /// </summary>
    [DataContract]
    public class ResultPaging
    {
        /// <summary>
        /// общее кол-во строк данных для выгрузки
        /// </summary>
        [DataMember]
        public Int32 totalRowsCount { get; set; }

        /// <summary>
        /// общее количество страниц 
        /// </summary>
        [DataMember]
        public Int32 totalPagesCount { get; set; }

        /// <summary>
        /// кол-во строк в текущей странице
        /// </summary>
        [DataMember]
        public Int32 rowsInCurPage { get; set; }
    }

    /// <summary>
    /// Класс для передачи в запросе объекта
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class IntfRequestObjectType<T> : IntfRequestType where T : class, new()
    {
        public T entity;

        public IntfRequestObjectType() : this(null) { }

        public IntfRequestObjectType(T entity)
        {
            this.entity = entity;
        }

    }



}
