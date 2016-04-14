using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace webroles
{
    public struct Tables
    {
        public static string pages
        {
            get { return "pages"; }
        }
        public static string actions_lnk
        {
            get { return "actions_lnk"; }
        }
        public static string actions_show
        {
            get { return "actions_show"; }
        }

        public static string s_roles
        {
            get { return "s_roles"; }
        }
        public static string role_pages
        {
            get { return "role_pages"; }
        }
        public static string role_actions
        {
            get { return "role_actions"; }
        }
        public static string img_lnk
        {
            get { return "img_lnk"; }
        }
        public static string page_links
        {
            get { return "page_links"; }
        }

        public static string roleskey
        {
            get { return "roleskey"; }
        }

        public static string s_actions
        {
            get { return "s_actions"; }
        }

        public static string report
        {
            get { return "report"; }
        }

        public static string profile_roles
        {
            get { return "profile_roles"; }
        }
        public static string profiles
        {
            get { return "profiles"; }
        } 

        // промежуточные таблицы
        public static string pages_intermed
        {
            get { return "pages_intermed"; }
        }

        public static string pages_intermed_rp
        {
            get { return "pages_intermed_rp"; }
        }
        public static string role_actions_intermed
        {
            get { return "role_actions_intermed"; }
        }
        public static string role_pages_intermed
        {
            get { return "role_pages_intermed"; }
        }

        public static string roleskey_intermed
        {
            get { return "roleskey_intermed"; }
        }

        public static string s_actions_intermed
        {
            get { return "s_actions_intermed"; }
        }

        public static string s_roles_intermed
        {
            get { return "s_roles_intermed"; }
        }
        public static string pages_show
        {
            get { return "pages_show"; }
        }

        public static string t_role_merging
        {
            get { return "t_role_merging"; }
        }

        public static string page_groups
        {
            get { return "page_groups"; }
        }

        public static string users
        {
            get { return "users"; }
        }

        public static string page_types
        {
            get { return "page_types"; }
        }

        public static string useInRolePages
        {
            get { return "useInRolePages"; }
        }
        public static string useInRoleActions
        {
            get { return "useInRoleActions"; }
        } 


    }
}
