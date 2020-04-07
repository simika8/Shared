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
	readonly struct ConnectionStringProvider : IConnectionProvider, IEquatable<ConnectionStringProvider>
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
		/// <param name="connectionString">Adatbázis kapcsolódáshoz szükséges adatok</param>
		/// <param name="databaseEngine">Adatbáziskezelő típusa</param>
		public ConnectionStringProvider(string connectionString, DatabaseEngine databaseEngine)
		{
			ConnectionString = connectionString;
			DatabaseEngine = databaseEngine;
		}

		#region Egyenlőségvizsgálathoz szükséges függvények

		public override bool Equals(object obj)
			=> obj is ConnectionStringProvider other && Equals(other);

		public override int GetHashCode()
			=> (DatabaseEngine, ConnectionString).GetHashCode();

		public static bool operator ==(in ConnectionStringProvider left, in ConnectionStringProvider right)
			=> left.Equals(right);

		public static bool operator !=(in ConnectionStringProvider left, in ConnectionStringProvider right)
			=> !(left == right);

		public bool Equals(ConnectionStringProvider other)
			=> (other.DatabaseEngine, other.ConnectionString) == (DatabaseEngine, ConnectionString);

		#endregion Egyenlőségvizsgálathoz szükséges függvények
	}
}
