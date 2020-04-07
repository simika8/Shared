using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirebirdSql.Data.FirebirdClient;
using LX.Common.Database.DataSync;
using LX.Common.Database.Extensions;
using LX.Common.EventHandlers;
using LX.Common.Extensions;

namespace LX.Common.Database.Settings
{
#if EXTERN
	public
#else
	internal
#endif
	static class ConfigSync
	{
		/// <summary>
		/// T_SETTINGS-beli S_PARAM mező lehetséges értékei
		/// </summary>
		public enum SParams
		{
			OVF_HOST,
			OVF_USER,
			OVF_PASSWD,
			OVF_FUNCPATH,
			OVF_MUNKAMENET,
			HIBAKOD_LASTUPDATE,
			PECSET_VENY_LASTUPDATE,
			LX_OEPLoglevel,
			PUPHAX_HOST,
			PUPHAX_USER,
			PUPHAX_PASSWD
		}

		private static List<T_SETTINGS> s_configValues;
		private static List<string> s_notExists;
		private static readonly object s_lockObject = new object();

		/// <summary>
		/// Event figyelés indítása
		/// </summary>
		public static void Start()
		{
			TableSync.Enable<T_SETTINGS>();
		}

		/// <summary>
		/// Event figyelés leállítása
		/// </summary>
		public static void Stop()
			=> TableSync.Disable<T_SETTINGS>();

		/// <summary>
		/// Beállítások előcachelése
		/// </summary>
		/// <param name="settingsNameLike">Jokeres beállításnév</param>
		/// <returns>Megvárható task</returns>
		public static Task CacheLike(string settingsNameLike)
		{
			return Task.Run(() =>
			{
				const string sql = // SQL
				#region SQL
@"SELECT s_id, s_param, s_value, s_szoveg
FROM t_settings
WHERE (s_param like @s_param) AND (s_type = @s_type);";
				#endregion SQL

				var settingsRows = LXDb.New(sql)
					.AddParam("s_param", settingsNameLike)
					.AddParam("s_type", "C")
					.GetObjects<T_SETTINGS>();

				if (!settingsRows.Any())
				{
					return;
				}

				lock (s_lockObject)
				{
					s_configValues = settingsRows
						.Union(s_configValues.Coalesce())
						.ToList();
				}
			});
		}

		#region Get metódusok

		/// <summary>
		/// T_SETINGS sor lekérdezése (adatbázisból vagy cacheből)
		/// </summary>
		/// <param name="settingsName">Beállítás neve</param>
		/// <param name="readFromDb">Mindenképpen az adatbázisból olvasson</param>
		/// <returns>T_SETINGS sor</returns>
		private static T_SETTINGS BaseGet(string settingsName, bool readFromDb = false)
		{
			if (!readFromDb)
			{
				lock (s_lockObject)
				{
					if (s_notExists?.Contains(settingsName) ?? false)
					{
						return default;
					}
				}
			}

			T_SETTINGS[] settingsSor;
			lock (s_lockObject)
			{
				settingsSor = s_configValues
					.Coalesce()
					.Where(s => settingsName == s.S_PARAM)
					.ToArray();
			}

			if (settingsSor.Any() && !readFromDb)
			{
				return settingsSor.First();
			}

			settingsSor = LXDb.New(
					"SELECT FIRST 1 s_id, s_param, s_value, s_szoveg" + Environment.NewLine +
					"FROM t_settings" + Environment.NewLine +
					"WHERE (s_param = @s_param) AND (s_type = @s_type);")
				.AddParam("s_param", settingsName)
				.AddParam("s_type", "C")
				.GetObjects<T_SETTINGS>();

			if (!settingsSor.Any())
			{
				// hozzáadjuk a null listához
				lock (s_lockObject)
				{
					s_notExists = s_notExists
						.Coalesce()
						.Union(new[] { settingsName })
						.ToList();
				}

				return default;
			}

			lock (s_lockObject)
			{
				s_configValues = settingsSor
					.Union(s_configValues.Coalesce())
					.ToList();
			}

			lock (s_lockObject)
			{
				// kivesszük a null listából
				s_notExists = s_notExists?
					.Where(s => s != settingsName)
					.ToList();
			}

			return settingsSor.First();
		}

		/// <summary>
		/// Vállalati beállítás érték lekérdezése
		/// </summary>
		/// <param name="settingsName">Beállítás neve</param>
		/// <param name="readFromDb">Mindenképpen az adatbázisból olvasson</param>
		/// <returns>Beállítás érték</returns>
		public static string Get(SParams settingsName, bool readFromDb = false)
			=> Get<string>(Enum.GetName(typeof(SParams), settingsName), readFromDb);

		/// <summary>
		/// Vállalati beállítás érték lekérdezése
		/// </summary>
		/// <typeparam name="T">Eredmény típusa</typeparam>
		/// <param name="settingsName">Beállítás neve</param>
		/// <param name="readFromDb">Mindenképpen az adatbázisból olvasson</param>
		/// <returns>Beállítás érték</returns>
		public static T Get<T>(SParams settingsName, bool readFromDb = false)
			=> Get<T>(Enum.GetName(typeof(SParams), settingsName), readFromDb);

		/// <summary>
		/// Vállalati beállítás érték lekérdezése
		/// </summary>
		/// <param name="settingsName">Beállítás neve</param>
		/// <param name="readFromDb">Mindenképpen az adatbázisból olvasson</param>
		/// <returns>Beállítás érték</returns>
		public static string Get(string settingsName, bool readFromDb = false)
			=> Get<string>(settingsName, readFromDb);

		/// <summary>
		/// Vállalati beállítás érték lekérdezése
		/// </summary>
		/// <typeparam name="T">Eredmény típusa</typeparam>
		/// <param name="settingsName">Beállítás neve</param>
		/// <param name="readFromDb">Mindenképpen az adatbázisból olvasson</param>
		/// <returns>Beállítás érték</returns>
		public static T Get<T>(string settingsName, bool readFromDb = false)
			=> (BaseGet(settingsName, readFromDb)?.S_VALUE).NGetValue<T, string>();

		/// <summary>
		/// Vállalati beállítás blob érték lekérdezése
		/// </summary>
		/// <typeparam name="T">Eredmény típusa</typeparam>
		/// <param name="settingsName">Beállítás neve</param>
		/// <param name="readFromDb">Mindenképpen az adatbázisból olvasson</param>
		/// <returns>Beállítás blob érték</returns>
		public static T GetSzoveg<T>(string settingsName, bool readFromDb = false)
			=> (BaseGet(settingsName, readFromDb)?.S_SZOVEG).NGetValue<T, string>();

		/// <summary>
		/// Vállalati beállítás blob érték lekérdezése
		/// </summary>
		/// <param name="settingsName">Beállítás neve</param>
		/// <param name="readFromDb">Mindenképpen az adatbázisból olvasson</param>
		/// <returns>Beállítás blob érték</returns>
		public static string GetSzoveg(string settingsName, bool readFromDb = false)
			=> GetSzoveg<string>(settingsName, readFromDb);

		#endregion Get metódusok

		#region GetUser metódusok

		/// <summary>
		///
		/// </summary>
		/// <param name="settingsName"></param>
		/// <param name="userId"></param>
		/// <returns></returns>
		public static string GetUser(string settingsName, int userId)
			=> GetUser<string>(settingsName, userId);

		/// <summary>
		///
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="settingsName"></param>
		/// <param name="userId"></param>
		/// <returns></returns>
		public static T GetUser<T>(string settingsName, int userId)
		{
			var settingsSor = LXDb.New(
					"SELECT FIRST 1 s_id, s_param, s_value, s_szoveg" + Environment.NewLine +
					"FROM t_settings" + Environment.NewLine +
					"WHERE (s_param = @s_param) AND (s_type = @s_type) AND (s_userid = @s_userid);")
				.AddParam("s_param", settingsName)
				.AddParam("s_type", "F")
				.AddParam("s_userid", userId)
				.GetObjects<T_SETTINGS>();

			if (!settingsSor.Any())
			{
				return default;
			}

			return settingsSor.First().S_VALUE.NGetValue<T, string>();
		}

		#endregion GetUser metódusok

		#region Set metódusok

		/// <summary>
		/// Cégbeállítás módosítása
		/// </summary>
		/// <param name="settingsName">Beállítás neve</param>
		/// <param name="value">Beállítás értéke</param>
		/// <param name="szoveg">Beállítás blob szövege</param>
		/// <param name="trans">Külső tranzakció</param>
		public static void Set(SParams settingsName, string value, string szoveg = null, FbTransaction trans = null)
			=> Set(Enum.GetName(typeof(SParams), settingsName), value, szoveg, trans);

		/// <summary>
		/// Cégbeállítás módosítása
		/// </summary>
		/// <param name="settingsName">Beállítás neve</param>
		/// <param name="value">Beállítás értéke</param>
		/// <param name="szoveg">Beállítás blob szövege</param>
		/// <param name="trans">Külső tranzakció</param>
		public static void Set(string settingsName, string value, string szoveg = null, FbTransaction trans = null)
		{
			LXDb.New("execute procedure p_setuparam(-1, @PARAM, @VALUE, 'T', 'C', @SZOVEG, 'T');")
				.AddParam("PARAM", settingsName)
				.AddParam("VALUE", value)
				.AddParam("SZOVEG", szoveg)
				.ExecSql(trans);

			lock (s_lockObject)
			{
				try
				{
					// ha nincs tároló, vagy nincs benne s_paramnak megfelelő elem
					if (s_configValues is null || (!s_configValues?.Any(s => s.S_PARAM == settingsName) ?? false))
					{
						// , akkor nem csinálunk semmi mást
						return;
					}

					// egyébként töröljük az ilyen nevű beállítás(oka)t
					s_configValues = s_configValues
						.Coalesce()
						.Where(s => s.S_PARAM != settingsName)
						.ToList();
				}
				finally
				{
					// kivesszük a null listából
					s_notExists = s_notExists?
						.Where(s => s != settingsName)
						.ToList();
				}
			}
		}

		#endregion Set metódusok

		internal static Func<T_SETTINGS, bool> DeleteFilter(int[] deletedRows)
			=> oldRows => !deletedRows.Contains(oldRows.S_ID);

		/// <summary>
		/// Ez fog végrehajtódni egy adatbázis esemény érkezésekor
		/// </summary>
		/// <param name="changeTime">Utolsó változás ideje</param>
		/// <param name="tableId">Tábla azonosító</param>
		internal static void SyncCallback(DateTime changeTime, int tableId)
		{
			if (changeTime == new DateTime(2000, 1, 1))
			{
				return;
			}

			try
			{
				const string selectSql = // SQL
				#region SQL
@"SELECT s_id, s_param, s_value, s_szoveg
FROM t_settings
WHERE (s_changetime >= @s_changetime) AND (s_type = @s_type);";
				#endregion SQL

				/*Func<T_SETTINGS, bool> Filter(int[] deletedRows)
					=> oldRows => !deletedRows.Contains(oldRows.S_ID);*/

				lock (s_lockObject)
				{
					s_configValues = TableSync.Get<T_SETTINGS>()?.CallbackCore(s_configValues, ("s_changetime", changeTime), selectSql, ("s_type", "C"));

					// kivesszük a null listából
					s_notExists = s_notExists?
						.Except(s_configValues
							.Where(s => s_notExists.Contains(s.S_PARAM))
							.Select(s => s.S_PARAM)
						)
						.ToList();
				}
			}
			catch (Exception ex)
			{
				MessageHandler.SendMessage(
					nameof(ConfigSync),
					$"Hiba történt a {nameof(T_SETTINGS)} sorok lekérdezése közben:" + Environment.NewLine + ex,
					MessageHandler.MessageType.Error
				);
			}
		}
	}
}
