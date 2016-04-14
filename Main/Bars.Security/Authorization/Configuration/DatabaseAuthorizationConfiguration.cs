using Bars.Security.Extentions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bars.Security.Authorization.Configuration
{
    public class DatabaseAuthorizationConfiguration : Singleton<DatabaseAuthorizationConfiguration>
    {
        private string _AccessibleGroupItems;
        private string _GroupsAccesibleData;
        private string _UsersAccesibleData;
        private string _AccessibleObjects;
        private string _AccessibleGroups;
        private string _AccessibleData;
        private string _UserGroups;
        private string _UserAccess;

        private DatabaseAuthorizationConfiguration():
            base()
        {
            Schema = "Security";
            AccessibleGroupsItems = "AccessibleGroupItems";
            GroupsAccesibleData = "GroupsAccesibleData";
            UsersAccesibleData = "UsersAccesibleData";
            AccessibleObjects = "AccessibleObjects";
            AccessibleGroups = "AccessibleGroups";
            AccessibleData = "AccessibleData";
            UserGroups = "UserGroups";
            UserAccess = "UserAccess";
        }

        public string Schema { get; set; }

        public string AccessibleGroupsItems { get { return GetTableName(_AccessibleGroupItems); } set { _AccessibleGroupItems = value; } }

        public string GroupsAccesibleData { get { return GetTableName(_GroupsAccesibleData); } set { _GroupsAccesibleData = value; } }

        public string UsersAccesibleData { get { return GetTableName(_UsersAccesibleData); } set { _UsersAccesibleData = value; } }

        public string AccessibleObjects { get { return GetTableName(_AccessibleObjects); } set { _AccessibleObjects = value; } }

        public string AccessibleGroups { get { return GetTableName(_AccessibleGroups); } set { _AccessibleGroups = value; } }

        public string AccessibleData { get { return GetTableName(_AccessibleData); } set { _AccessibleData = value; } }

        public string UserGroups { get { return GetTableName(_UserGroups); } set { _UserGroups = value; } }

        public string UserAccess { get { return GetTableName(_UserAccess); } set { _UserAccess = value; } }

        private string GetTableName(string Obj) { return string.Format("{0}.{1}", Schema, Obj); }
    }
}
