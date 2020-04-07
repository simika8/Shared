using LX.Common.Database.ConnectionProvider;

namespace LX.Common.Database.Extensions
{
	/// <summary>
	/// Segéd osztály sql query létrehozásához
	/// </summary>
#if EXTERN
	public
#else
	internal
#endif
	static class LXDb
	{
		/// <summary>
		/// SQL + LX db connection
		/// </summary>
		public static SqlQuery New(string sql)
			=> new SqlQuery(new LXConnectionProvider(), sql);
	}
}
