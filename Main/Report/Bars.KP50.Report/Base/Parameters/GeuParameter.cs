namespace Bars.KP50.Report
{
	using System.Collections.Generic;

	using Newtonsoft.Json.Linq;

	/// <summary>ЖЭУ</summary>
	public class GeuParameter : ComboBoxParameter
	{
		public GeuParameter(bool multiSelect = true)
			: base(multiSelect) {
			TypeValue = typeof(List<int>);
			Name = "ЖЭУ";
			Code = "Geu";
			Require = false;
			ComboBoxConfig = new
			{
				valueField = "Id",
				displayField = "Name",
				editable = false,
				mode = "remote",
				triggerAction = "all",
				store = new JRaw("new Ext.data.JsonStore({ url: 'GeuHandler.axd', idProperty: 'Id', fields: ['Id', 'Name'], autoLoad: true, root: 'data' })")
			};
		}

		public string Pref { get; private set; }

		public virtual GeuParameter SetPref(string pref) {
			Pref = pref;
			ComboBoxConfig = new
			{
				valueField = "Id",
				displayField = "Name",
				editable = false,
				mode = "remote",
				triggerAction = "all",
				store = new JRaw("new Ext.data.JsonStore({ url: 'GeuHandler.axd?pref=" + pref + "', idProperty: 'Id', fields: ['Id', 'Name'], autoLoad: true, root: 'data' })")
			};

			return this;
		}
	}
}