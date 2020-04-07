using System.Data;

namespace LX.Common.Database.ConnectionProvider
{
	/// <summary>
	/// Adatbáziskezelő
	/// </summary>
#if EXTERN
	public
#else
	internal
#endif
	enum DatabaseEngine
	{
		Firebird,
		MySQL,
		MsSQL,
	};

	/// <summary>
	/// Adatbázis kapcsolódási interface
	/// </summary>
#if EXTERN
	public
#else
	internal
#endif
	interface IConnectionProvider
	{
		/// <summary>
		/// Új adatbázis kapcsolat létrehozása és megnyitása
		/// </summary>
		/// <returns>Nyitott adatbáziskapcsolat</returns>
		IDbConnection GetOpenConnection();

		/// <summary>
		/// Adatbáziskezelő
		/// </summary>
		DatabaseEngine DatabaseEngine { get; }

		/// <summary>
		/// Connection string lekérdezése
		/// </summary>
		string ConnectionString { get; }
	}
}
