using System;
using System.Collections.Generic;
using System.Text;

namespace Settings
{
	/// <summary>
	/// Beállítás típusok
	/// </summary>
	public enum SettingType : long
	{
		ProbaGlobal,
		ProbaPartnerSetting,

		StatementsExportUrl,
		StatementsImportAnonimPartnerCode,
	}
}
