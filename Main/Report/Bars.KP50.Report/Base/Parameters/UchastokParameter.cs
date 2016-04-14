using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Bars.KP50.Report
{
	/// <summary>Участок</summary>
	public class UchastokParameter : ComboBoxParameter
	{
		public UchastokParameter(bool multiSelect = true) : base(multiSelect) {
			TypeValue = typeof(List<int>);
			Name = "Участок";
			Code = "Uchastok";
			Require = false;
			ComboBoxConfig = new
			{
				valueField = "Id",
				displayField = "Name",
				editable = false,
				mode = "remote",
				triggerAction = "all",
				store = new JRaw("new Ext.data.JsonStore({ url: 'UchastokHandler.axd', idProperty: 'Id', fields: ['Id', 'Name'], autoLoad: true, root: 'data' })")
			};
		}

		public string Pref { get; private set; }

		public virtual UchastokParameter SetPref(string pref) {
			Pref = pref;
			ComboBoxConfig = new
			{
				valueField = "Id",
				displayField = "Name",
				editable = false,
				mode = "remote",
				triggerAction = "all",
				store = new JRaw("new Ext.data.JsonStore({ url: 'UchastokHandler.axd?pref=" + pref + "', idProperty: 'Id', fields: ['Id', 'Name'], autoLoad: true, root: 'data' })")
			};

			return this;
		}
	}
}
