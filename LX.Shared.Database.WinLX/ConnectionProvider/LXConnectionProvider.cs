using System.Data;
using FirebirdSql.Data.FirebirdClient;

namespace LX.Common.Database.ConnectionProvider
{
	/// <summary>
	/// LX-es inifile alapú adatbáziskapcsolat
	/// </summary>
#if EXTERN
	public
#else
	internal
#endif
	class LXConnectionProvider : IConnectionProvider
	{
		public static string DataSource => s_connectionStringBuilder?.DataSource;
		public static string Database => s_connectionStringBuilder?.Database;
		public static string UserId => s_connectionStringBuilder?.UserID;
		public static string IniPath { get; private set; }

		private static readonly object s_lockObject = new object();

		/// <summary>
		/// Csak egyszer szabad beállítani ezt
		/// </summary>
		public static string IniFileName { get; set; } = "winlx.ini";

		private static FbConnectionStringBuilder s_connectionStringBuilder;

		#region Interface properties

		public string ConnectionString => s_connectionStringBuilder.ConnectionString;

		public DatabaseEngine DatabaseEngine => DatabaseEngine.Firebird;

		#endregion Interface properties

		public LXConnectionProvider()
		{
			lock (s_lockObject)
			{
				if (s_connectionStringBuilder is { })
				{
					return;
				}

				s_connectionStringBuilder = new FbConnectionStringBuilder();
				ConnectionStringReload();
			}
		}


		IDbConnection IConnectionProvider.GetOpenConnection() => GetOpenConnection();

		/// <summary>
		/// Új adatbázis kapcsolat létrehozása és megnyitása
		/// </summary>
		/// <returns>Nyitott adatbáziskapcsolat</returns>
		public FbConnection GetOpenConnection()
		{
			try
			{
				return InternalConnect();
			}
			catch (FbException ex) when (ex.ErrorCode == 335544472)
			{
				ConnectionStringReload();
				return InternalConnect();
			}

			// belső függvény adatbázis kapcsolat nyitáshoz
			static FbConnection InternalConnect()
			{
				var conn = new FbConnection(s_connectionStringBuilder.ConnectionString);
				conn.Open();
				return conn;
			}
		}

		/// <summary>
		/// Kapcsolati beállítások újraulvasása
		/// </summary>
		private static void ConnectionStringReload()
		{
			lock (s_lockObject)
			{
				IniPath = LXIniFile.GetIniPath(IniFileName);
				var inireader = new IniReader(IniPath);

				s_connectionStringBuilder.Charset = "WIN1250";
				//s_connectionStringBuilder.Pooling = false; // ne cachelje a kapcsolatokat a kliens
				s_connectionStringBuilder.Pooling = true;
				s_connectionStringBuilder.DataSource = inireader.GetValue("DBServer", "Database_Connection");
				s_connectionStringBuilder.Database = inireader.GetValue("DBPath", "Database_Connection");
				s_connectionStringBuilder.UserID = inireader.GetValue("BaseUserName", "Database_Connection");
				s_connectionStringBuilder.Password = DBPassTools.GetDBPass(DataSource, Database, UserId, inireader.GetValue("BaseUserPw", "Database_Connection"));
			}
		}
	}
}
