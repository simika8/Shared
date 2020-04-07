using System;
using System.Linq;
using System.Text;
using FirebirdSql.Data.FirebirdClient;
using LX.Common.Database.Extensions;

namespace LX.Common.Database
{
#if EXTERN
	public
#else
	internal
#endif
	static class DBPassTools
	{
		private const string DBPASS_ENCRIPTIONKEY = "TABYAFIA4QBuAHQAbwB0AHQAUwBhAGoAdAA="; // BASE64, unicode, értéke: LXRántottSajt
		// private const string SYSDBA_PASS = "bABYAHMAZwBNAGEATgAhAA=="; // BASE64, unicode, értéke: lXsgMaN!
		private const string GUEST_USERNAME = "guest";
		private const string GUEST_PASS = "guest";
		private const string LXUSER = "lx";

		/// <summary>
		/// Jelszó AES256 kódolása, "lx" esetén nem kódol
		/// </summary>
		/// <param name="kodolatlanJelszo">Nem kódolt bemenet</param>
		/// <returns>Kódolt kimenet</returns>
		public static string DBPassEncode(string kodolatlanJelszo)
		{
			if (LXUSER.Equals(kodolatlanJelszo, StringComparison.OrdinalIgnoreCase))
			{
				return kodolatlanJelszo;
			}

			string key = Encoding.Unicode.GetString(Convert.FromBase64String(DBPASS_ENCRIPTIONKEY));
			return AesCrypto.AesEncryptString(kodolatlanJelszo, key, Encoding.Unicode);
		}

		/// <summary>
		/// Jelszó AES256 dekódolása, "lx" esetén nem dekódol
		/// </summary>
		/// <param name="kodoltJelszo">Kódolt bemenet</param>
		/// <returns>Nem kódolt kimenet</returns>
		public static string DBPassDecode(string kodoltJelszo)
		{
			if (LXUSER.Equals(kodoltJelszo, StringComparison.OrdinalIgnoreCase))
			{
				return kodoltJelszo;
			}

			string key = Encoding.Unicode.GetString(Convert.FromBase64String(DBPASS_ENCRIPTIONKEY));
			return AesCrypto.AesDecryptString(kodoltJelszo, key, Encoding.Unicode);
		}

		/// <summary>
		/// Megadott felhasználó jelszavát kérdezi le a megadott adatbázisból
		/// </summary>
		/// <param name="dataSource">Szerver elérhetősége</param>
		/// <param name="database">Adatbázis file</param>
		/// <param name="userName">Ennek a felhasználónak a jelszavát kérdezi le</param>
		/// <param name="fallbackPass">Hiba esetén ezt adja vissza</param>
		/// <returns>Dekódolt jelszó</returns>
		public static string GetDBPass(string dataSource, string database, string userName, string fallbackPass)
		{
			string dbpass;
			try
			{
				string cs = new FbConnectionStringBuilder
				{
					Pooling = false,
					Charset = "WIN1250",
					DataSource = dataSource,
					Database = database,
					UserID = GUEST_USERNAME,
					Password = GUEST_PASS,
				}.ConnectionString;

				// belépünk az adatbázisba a vendég userrel
				dbpass = DbHelper.New("select first 1 dbpass from t_security where lower(trim(dbuser)) = lower(trim(@dbuser));", cs)
					.AddParam("dbuser", userName)
					.GetValues<string>()
					.FirstOrDefault();
			}
#pragma warning disable CS0168 // Variable is declared but never used
			catch (Exception ex)
#pragma warning restore CS0168 // Variable is declared but never used
			{
				return fallbackPass;
			}

			try
			{
				return string.IsNullOrEmpty(dbpass)
					? fallbackPass
					: DBPassDecode(dbpass);
			}
#pragma warning disable CS0168 // Variable is declared but never used
			catch (Exception ex)
#pragma warning restore CS0168 // Variable is declared but never used
			{
				return fallbackPass;
			}
		}
	}
}
