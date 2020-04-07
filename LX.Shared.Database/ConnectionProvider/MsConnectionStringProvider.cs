using System;
using System.Data;

namespace LX.Common.Database.ConnectionProvider
{
	/// <summary>
	/// Egyszerű connectionstring alapú adatbázis csatlakozás
	/// </summary>
#if EXTERN
	public
#else
	internal
#endif
	readonly struct MsConnectionStringProvider : IConnectionProvider
	{
		/// <summary>
		/// Adatbázis kapcsolódáshoz szükséges adatok
		/// </summary>
		public string ConnectionString { get; }

		/// <summary>
		/// Adatbáziskezelő típusa
		/// </summary>
		public DatabaseEngine DatabaseEngine { get; }

		/// <summary>
		/// Adatbáziskapcsolat felépítése
		/// </summary>
		/// <returns></returns>
		public IDbConnection GetOpenConnection()
		{
			var conn = DatabaseProviderLoader.CreateConnection(DatabaseEngine, ConnectionString);
			conn.Open();
			return conn;
		}

		/// <summary>
		/// Adatbáziskezelő inicializálása
		/// </summary>
		/// <param name="database">Adatbázis neve</param>
		public MsConnectionStringProvider(string database)
		{
			ConnectionString = $@"Server=(localdb)\mssqllocaldb;Database={database};Trusted_Connection=True;MultipleActiveResultSets=true";
			DatabaseEngine = DatabaseEngine.MsSQL;
		}

	}
}
