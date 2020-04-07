using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Database.EFCore;
using Microsoft.EntityFrameworkCore;

namespace Settings
{
	public static class SettingsExt
	{
		/// <summary>
		/// Globális beállítás lekérdezés
		/// </summary>
		public static T Get<T>(this DbSet<T> settings, SettingType settingType) where T: class, ISetting, new()
		{
			
			var s = settings.GetData<T>(x => { x.Name = settingType.ToString(); });
			return s;
		}

		/// <summary>
		/// Referenciával rendelkező beállítás lekérdezés (több azonos nevű, de eltérő dologra hivatkozó beállítás. LEhet használni pl partnerfüggő, pénztárfüggő, felhasználófüggő stb beállítás kezelésére)
		/// </summary>
		public static T Get<T>(this DbSet<T> settings, SettingType settingType, long referenceId) where T : class, ISetting, new()
		{
			var s = settings.GetData<T>(x => { x.Name = settingType.ToString(); x.ReferenceId = referenceId; });
			return s;
		}
	}
}
