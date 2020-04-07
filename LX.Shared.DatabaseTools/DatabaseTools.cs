using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using FirebirdSql.Data.FirebirdClient;
using LX.Common.Database.ConnectionProvider;
using LX.Common.Database.Extensions;
using LX.Common.Extensions;

namespace LX.Common.Database
{
	/// <summary>
	/// Adatbázis kezelő osztály
	/// </summary>
#if EXTERN
	public
#else
	internal
#endif
	class DatabaseTools
	{
		private readonly IConnectionProvider _connectionProvider;

		/// <summary>
		/// Adatbázis kezelő
		/// </summary>
		public DatabaseTools()
		{
			_connectionProvider = new LXConnectionProvider();
		}

		/// <summary>
		/// Ellenőrzi a beadott változókat, ha valamelyik null, akkor kivételt dob
		/// </summary>
		/// <param name="args">Változók</param>
		/// <exception cref="ArgumentNullException"/>
#if NETFX_45
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		private static void CheckForNullArguments(params object[] args)
		{
			if (args.Any(arg => arg is null))
			{
				throw new ArgumentNullException();
			}
		}

		/// <summary>
		/// Létrehoz egy új adatbázis kapcsolatot a beállításokban megadott adatbázis irányába
		/// </summary>
		/// <returns>Adatbázis kapcsolat objektuma</returns>
		public FbConnection CreateAndOpenNewConnection() => (FbConnection)_connectionProvider.GetOpenConnection();

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
		[Obsolete("Elavult függvény, használd az új verziót!")]
		public void ExecuteSQL(string sql, object parameters = null)
		{
			var conn = CreateAndOpenNewConnection();
			try
			{
				using (var trans = conn.BeginTransaction())
				{
					ExecuteSQL(trans, sql, parameters);
					trans.Commit();
				}
			}
			finally
			{
				conn.Dispose();
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
		[Obsolete("Elavult függvény, használd az új verziót!")]
		public void ExecuteSQL(FbTransaction trans, string sql, object parameters = null)
		{
			using (var command = new FbCommand(sql, trans.Connection, trans))
			{
				if (parameters != null)
				{
					command.Parameters.AddParams(DatabaseEngine.Firebird, parameters);
				}

				command.ExecuteNonQuery();
			}
		}

		/// <summary>
		/// Adatokat frissít az adatbázis egy táblájában feltétel alapján (saját DB kapcsolat és tranzakció, commitol egyből)
		/// </summary>
		/// <param name="table">Tábla neve</param>
		/// <param name="parameters">Mező nevek és értékek</param>
		/// <param name="where">Feltétel</param>
		public void UpdateData(string table, object parameters, string where)
		{
			var conn = CreateAndOpenNewConnection();
			try
			{
				using (var trans = conn.BeginTransaction())
				{
					UpdateData(table, parameters, where, trans);

					trans.Commit();
				}
			}
			finally
			{
				conn.Dispose();
			}
		}

		/// <summary>
		/// Adatokat frissít az adatbázis egy táblájában feltétel alapján (NEM commitol)
		/// </summary>
		/// <param name="table">Tábla neve</param>
		/// <param name="parameters">Mező nevek és értékek</param>
		/// <param name="where">Feltétel</param>
		/// <param name="trans">Külső tranzakció</param>
		///
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
		public void UpdateData(string table, object parameters, string where, FbTransaction trans)
		{
			CheckForNullArguments(table, parameters, trans.Connection, trans);

			var paramlist = DbExt.GetParams(parameters).ToArray();

			string[] setParams = paramlist.Select(param => $"{param.Key} = @{param.Key}").ToArray();
			string sql = $"update {table} set {string.Join(",", setParams)}" + (where.IsNullOrWhitespace() ? string.Empty : $" where {where}");

			using (var command = new FbCommand(sql, trans.Connection, trans))
			{
				foreach (var param in paramlist)
				{
					command.Parameters.Add($"@{param.Key}", param.Value ?? DBNull.Value);
				}

				command.ExecuteNonQuery();
			}
		}

		/// <summary>
		/// Adatokat szúr be az adatbázis egy táblájába (saját DB kapcsolat és tranzakció, commitol egyből)
		/// </summary>
		/// <param name="table">Tábla neve</param>
		/// <param name="parameters">Mező nevek és értékek</param>
		public void InsertData(string table, object parameters)
		{
			var conn = CreateAndOpenNewConnection();
			try
			{
				using (var trans = conn.BeginTransaction())
				{
					InsertData(table, trans, parameters);

					trans.Commit();
				}
			}
			finally
			{
				conn.Dispose();
			}
		}

		/// <summary>
		/// Adatokat szúr be az adatbázis egy táblájába (NEM commitol)
		/// </summary>
		/// <param name="table">Tábla neve</param>
		/// <param name="trans">Külső tranzakció</param>
		/// <param name="parameters">Mező nevek és értékek</param>
		///
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
		public void InsertData(string table, FbTransaction trans, object parameters)
		{
			CheckForNullArguments(table, parameters, trans.Connection, trans);

			var paramlist = DbExt.GetParams(parameters).ToArray();

			string[] sqlparams = paramlist.Select(param => param.Key).ToArray();

			string fields = string.Join(",", sqlparams);
			string values = sqlparams.Any() ? $"@{string.Join(",@", sqlparams)}" : string.Empty;

			string sql = $"insert into {table} ({fields}) values ({values});";
			using (var command = new FbCommand(sql, trans.Connection, trans))
			{
				foreach (var param in paramlist)
				{
					command.Parameters.Add($"@{param.Key}", param.Value ?? DBNull.Value);
				}

				command.ExecuteNonQuery();
			}
		}

		/// <summary>
		/// Adatokat frissít vagy szúr be az adatbázis egy táblájába, feltételtűl függően (saját DB kapcsolat és tranzakció, commitol egyből)
		/// </summary>
		/// <param name="table">Tábla neve</param>
		/// <param name="parameters">Mező nevek és értékek</param>
		/// <param name="matching">Feltétel</param>
		public void UpdateOrInsertData(string table, object parameters, string matching)
		{
			var conn = CreateAndOpenNewConnection();
			try
			{
				using (var trans = conn.BeginTransaction())
				{
					UpdateOrInsertData(table, trans, parameters, matching);
					trans.Commit();
				}
			}
			finally
			{
				conn.Dispose();
			}
		}

		/// <summary>
		/// Adatokat frissít vagy szúr be az adatbázis egy táblájába, feltételtűl függően (NEM commitol)
		/// </summary>
		/// <param name="table">Tábla neve</param>
		/// <param name="trans">Külső tranzakció</param>
		/// <param name="parameters">Mező nevek és értékek</param>
		/// <param name="matching">Feltétel</param>
		///
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
		public void UpdateOrInsertData(string table, FbTransaction trans, object parameters, string matching)
		{
			CheckForNullArguments(table, parameters, trans.Connection, trans);

			var paramlist = DbExt.GetParams(parameters).ToArray();

			string[] sqlparams = paramlist.Select(param => param.Key).ToArray();

			string fields = string.Join(",", sqlparams);
			string values = sqlparams.Any() ? $"@{string.Join(",@", sqlparams)}" : string.Empty;

			string sql = $"update or insert into {table} ({fields}) values ({values})" + (matching.IsNullOrWhitespace() ? string.Empty : $" matching ({matching})");
			using (var command = new FbCommand(sql, trans.Connection, trans))
			{
				foreach (var param in paramlist)
				{
					command.Parameters.Add($"@{param.Key}", param.Value ?? DBNull.Value);
				}

				command.ExecuteNonQuery();
			}
		}

		/// <summary>
		/// Visszaadja adatként egy select eredményét, null esetében default(T) az eredmény
		/// </summary>
		/// <param name="sql">select SQL kifejezés</param>
		/// <param name="trans"></param>
		/// <param name="parameters"></param>
		/// <returns>Visszaadott érték</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
		[Obsolete("Elavult függvény, használd az új verziót!")]
		public T GetValueFromSelect<T>(string sql, FbTransaction trans, object parameters = null)
		{
			object result;

			using (var command = new FbCommand(sql, trans.Connection, trans))
			{
				if (parameters != null)
				{
					command.Parameters.AddParams(DatabaseEngine.Firebird, parameters);
				}

				result = command.ExecuteScalar();
			}

			return typeof(T) == typeof(object) ? (T)result : result.NGetValue<T, object>();
		}

		/// <summary>
		/// Visszaadja adatként egy select eredményét, null esetében default(T) az eredmény
		/// </summary>
		/// <param name="sql">select SQL kifejezés</param>
		/// <param name="parameters"></param>
		/// <param name="commit"></param>
		/// <returns>Visszaadott érték</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
		[Obsolete("Elavult függvény, használd az új verziót!")]
		public T GetValueFromSelect<T>(string sql, object parameters = null, bool commit = false)
		{
			T result;

			if (string.IsNullOrEmpty(sql))
			{
				return default;
			}

			var conn = CreateAndOpenNewConnection();
			try
			{
				using (var trans = conn.BeginTransaction())
				{
					result = GetValueFromSelect<T>(sql, trans, parameters);

					if (commit)
					{
						trans.Commit();
					}
					else
					{
						trans.Rollback();
					}
				}
			}
			finally
			{
				conn.Dispose();
			}

			return result;
		}

		/// <summary>
		/// Visszaadja több soros adatként, de egy oszlopban egy select eredményét, null esetében default(T) az eredmény
		/// </summary>
		/// <param name="sql">select SQL kifejezés</param>
		/// <param name="parameters"></param>
		/// <returns>Visszaadott érték</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
		[Obsolete("Elavult függvény, használd az új verziót!")]
		public T[] GetValuesFromSelect<T>(string sql, object parameters = null)
		{
			if (string.IsNullOrEmpty(sql))
			{
				return ArrayExt.Empty<T>();
			}

			T[] result;

			var conn = CreateAndOpenNewConnection();
			try
			{
				using (var trans = conn.BeginTransaction())
				{
					using (var command = new FbCommand(sql, conn, trans))
					{
						if (parameters != null)
						{
							command.Parameters.AddParams(DatabaseEngine.Firebird, parameters);
						}

						using (var reader = command.ExecuteReader())
						{
							result = reader.ReadColumnToEnd<T>().ToArray();
						}
					}
					trans.Rollback();
				}
			}
			finally
			{
				conn.Dispose();
			}

			return result;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
		[Obsolete("Elavult függvény, használd az új verziót!")]
		public IEnumerable<T> GetValuesFromSelect<T>(string sql, FbTransaction trans, object parameters = null)
		{
			if (string.IsNullOrEmpty(sql))
			{
				yield break;
			}

			using (var command = new FbCommand(sql, trans.Connection, trans))
			{
				if (parameters != null)
				{
					command.Parameters.AddParams(DatabaseEngine.Firebird, parameters);
				}

				using (var reader = command.ExecuteReader())
				{
					while (reader.Read())
					{
						yield return reader.GetValue(0).NGetValue<T, object>();
					}
				}
			}
		}

		/// <summary>
		/// Visszaadja egy lekérdezés eredményét
		/// </summary>
		/// <param name="sql">SQL select utasítás</param>
		/// <param name="trans">Adatbázis tranzakció</param>
		/// <param name="parameters">Opcionális paraméterek</param>
		/// <returns><see cref="DataTable"/>-ben tárolja a select eredményét</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
		public DataTable GetTableFromSelect(FbTransaction trans, string sql, object parameters = null)
		{
			var dt = new DataTable("table");

			if (string.IsNullOrEmpty(sql))
			{
				return dt;
			}

			using (var command = new FbCommand(sql, trans.Connection, trans))
			{
				if (parameters != null)
				{
					command.Parameters.AddParams(DatabaseEngine.Firebird, parameters);
				}

				using (var dataAdapter = new FbDataAdapter(command))
				{
					dataAdapter.Fill(dt);
				}
			}

			return dt;
		}

		/// <summary>
		/// Visszaadja egy lekérdezés eredményét
		/// </summary>
		/// <param name="sql">SQL select utasítás</param>
		/// <param name="parameters">Opcionális paraméterek</param>
		/// <returns><see cref="DataTable"/>-ben tárolja a select eredményét</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
		public DataTable GetTableFromSelect(string sql, object parameters = null)
		{
			if (string.IsNullOrEmpty(sql))
			{
				return new DataTable("table");
			}

			DataTable dt;

			var conn = CreateAndOpenNewConnection();
			try
			{
				using (var trans = conn.BeginTransaction())
				{
					dt = GetTableFromSelect(trans, sql, parameters);
					trans.Rollback();
				}
			}
			finally
			{
				conn.Dispose();
			}

			return dt;
		}

		/// <summary>
		/// Létrehoz egy db kapcsolatot és egy tranzakciót, majd lockolja a <see cref="DatabaseTools"/> objektumot
		/// </summary>
		/// <param name="work">Végrehajtandó műveletek</param>
		public void CreateLockedTransaction(Action<FbConnection, FbTransaction> work)
		{
			lock (this)
			{
				var conn = CreateAndOpenNewConnection();
				try
				{
					using (var trans = conn.BeginTransaction())
					{
						work(conn, trans);
					}
				}
				finally
				{
					conn.Dispose();
				}
			}
		}
		/// <summary>
		/// Visszaadja egy lekérdezés eredményét
		/// </summary>
		/// <typeparam name="T">Rekord típusa</typeparam>
		/// <param name="sql">SQL select utasítás</param>
		/// <param name="parameters">Opcionális paraméterek</param>
		/// <returns><typeparamref name="T"/> typusú osztály(ok)</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
		[Obsolete("Elavult függvény, használd az új verziót!")]
		public T[] GetDataFromSelect<T>(string sql, object parameters = null) where T : class, new()
		{
			if (string.IsNullOrEmpty(sql))
			{
				return ArrayExt.Empty<T>();
			}

			T[] result;

			var conn = CreateAndOpenNewConnection();
			try
			{
				using (var trans = conn.BeginTransaction())
				{
					result = GetDataFromSelect<T>(trans, sql, parameters);
					trans.Rollback();
				}
			}
			finally
			{
				conn.Dispose();
			}

			return result;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
		[Obsolete("Elavult függvény, használd az új verziót!")]
		public T[] GetDataFromSelect<T>(FbTransaction trans, string sql, object parameters = null) where T : class, new()
		{
			if (string.IsNullOrEmpty(sql))
			{
				return ArrayExt.Empty<T>();
			}

			T[] result;

			using (var command = new FbCommand(sql, trans.Connection, trans))
			{
				if (parameters != null)
				{
					command.Parameters.AddParams(DatabaseEngine.Firebird, parameters);
				}

				using (var reader = command.ExecuteReader())
				{
					result = DbHelper.Data2Object<T>(reader).ToArray();
				}
			}

			return result;
		}
	}
}
