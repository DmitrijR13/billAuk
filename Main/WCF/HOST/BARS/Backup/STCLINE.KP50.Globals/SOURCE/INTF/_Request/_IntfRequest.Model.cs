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

        public ListPagingType listPaging;
        public decimal keyID;
        public decimal parentID;

    }

    /// <summary>
    /// 
    /// </summary>
    public class ListPagingType
    {
        public Int32 totalCount { get; set; }
        public Int32 currentPage { get; set; }
        public Int32 itemPerPage { get; set; }
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
