using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using LX.Common.Database.ConnectionProvider;
using LX.Common.Database.Extensions;
using LX.Common.Extensions;

namespace LX.Common.Database
{
	//TODO: az SqlQuery struct később lecserélhető record típusra, ha lesz olyan a C#-ban

	/// <summary>
	/// Sql lekérdezést tárol (db kapcsolat kezelő és sql parancs)
	/// </summary>
#if EXTERN
	public
#else
	internal
#endif
	readonly struct SqlQuery
	{
		/// <summary>
		/// Adatbáziskapcsolat kezelő osztály
		/// </summary>
		public IConnectionProvider ConnectionProvider { get; }

		/// <summary>
		/// Sql query
		/// </summary>
		public string Sql { get; }

		/// <summary>
		/// Adatok beállíása konstruktorral
		/// </summary>
		/// <param name="connectionProvider">Adatbáziskapcsolat kezelő osztály</param>
		/// <param name="sql">Sql query</param>
		public SqlQuery(IConnectionProvider connectionProvider, string sql)
		{
			ConnectionProvider = connectionProvider;
			Sql = sql;
		}

		public override bool Equals(object obj)
			=> obj is SqlQuery other
			&& ConnectionProvider == other.ConnectionProvider
			&& Sql == other.Sql;

		public override int GetHashCode()
			=> (ConnectionProvider, Sql).GetHashCode();

		public static bool operator ==(in SqlQuery left, in SqlQuery right)
			=> left.Equals(right);

		public static bool operator !=(in SqlQuery left, in SqlQuery right)
			=> !(left == right);
	}

	/// <summary>
	/// Műveletek az SqlQuery-hez
	/// </summary>
#if EXTERN
	public
#else
	internal
#endif
	static class SqlQueryExt
	{
		public static SqlQuery Sql(this IConnectionProvider self, string sqlText) => new SqlQuery(self, sqlText);

		#region Helper stuff (private)

		private delegate void SqlAction(in SqlQuery query, IDbCommand cmd);

		/// <summary>
		/// Sql futtató keret új db kapcsolattal és tranzakcióval
		/// </summary>
		/// <param name="self">Query cucc</param>
		/// <param name="commit">Kell-e commit</param>
		/// <param name="action">Művelet a commanddal</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void WithConnection(this in (SqlQuery Query, List<IDbDataParameter> Parameters) self, bool commit, SqlAction action)
		{
			using var conn = self.Query.ConnectionProvider.GetOpenConnection();
			using var trans = conn.BeginTransaction();
			using var command = conn.CreateCommand();

			// sql szöveg beállítása
			command.CommandText = self.Query.Sql;
			// tranzakció hozzákötése
			command.Transaction = trans;

			// paraméterek beadása
			self.Parameters?.ForEach(p => command.Parameters.Add(p));

			// badott művelet futtatása
			action(self.Query, command);

			// kell-e commitolni?
			if (commit)
			{ trans.Commit(); }
			else
			{ trans.Rollback(); }
		}

		/// <summary>
		/// Sql futtató keret kintről beadott tranzakcióval
		/// </summary>
		/// <param name="self">Query cucc</param>
		/// <param name="trans">Kintről beadott tranzakció</param>
		/// <param name="action">Művelet a commanddal</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void WithoutConnection(this in (SqlQuery Query, List<IDbDataParameter> Parameters) self, IDbTransaction trans, SqlAction action)
		{
			using var command = trans.Connection.CreateCommand();

			// sql szöveg beállítása
			command.CommandText = self.Query.Sql;
			// tranzakció hozzákötése
			command.Transaction = trans;

			// paraméterek beadása
			self.Parameters?.ForEach(p => command.Parameters.Add(p));

			// badott művelet futtatása
			action(self.Query, command);
		}

		#endregion Helper stuff (private)

		#region Parameters

		/// <summary>
		/// Paraméter hozzáadása
		/// </summary>
		/// <typeparam name="T">Paraméter típusa</typeparam>
		/// <param name="self">Query</param>
		/// <param name="parameterName">Paraméter neve</param>
		/// <param name="parameterValue">Paraméter értéke</param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static (SqlQuery Query, List<IDbDataParameter> Parameters) AddParam<T>(this in SqlQuery self, in string parameterName, in T parameterValue)
			=> (self, new List<IDbDataParameter>(1)
			{
				DbHelper.CreateDataParameter(self.ConnectionProvider.DatabaseEngine, parameterName, parameterValue)
			});

		/// <summary>
		/// Paraméter hozzáadása
		/// </summary>
		/// <typeparam name="T">Paraméter típusa</typeparam>
		/// <param name="self">Query + paraméterlista</param>
		/// <param name="parameterName">Paraméter neve</param>
		/// <param name="parameterValue">Paraméter értéke</param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref readonly (SqlQuery Query, List<IDbDataParameter> Parameters) AddParam<T>(this in (SqlQuery Query, List<IDbDataParameter> Parameters) self, in string parameterName, in T parameterValue)
		{
			self.Parameters.Add(DbHelper.CreateDataParameter(self.Query.ConnectionProvider.DatabaseEngine, parameterName, parameterValue));
			return ref self;
		}

		/// <summary>
		/// Paraméterek hozzáadása
		/// </summary>
		/// <param name="self">Query</param>
		/// <param name="parameters">Paraméterek</param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static (SqlQuery Query, List<IDbDataParameter> Parameters) AddParamRange(this in SqlQuery self, IEnumerable<(string Name, object Value)> parameters)
		{
			var databaseEngine = self.ConnectionProvider.DatabaseEngine;
			return (self, parameters.Select(p => DbHelper.CreateDataParameter(databaseEngine, p.Name, p.Value)).ToList());
		}

		/// <summary>
		/// Paraméterek hozzáadása
		/// </summary>
		/// <param name="self">Query + paraméterlista</param>
		/// <param name="parameters">Paraméterek</param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref readonly (SqlQuery Query, List<IDbDataParameter> Parameters) AddParamRange(this in (SqlQuery Query, List<IDbDataParameter> Parameters) self, IEnumerable<(string Name, object Value)> parameters)
		{
			foreach (var (name, value) in parameters)
			{
				self.Parameters.Add(DbHelper.CreateDataParameter(self.Query.ConnectionProvider.DatabaseEngine, name, value));
			}
			return ref self;
		}

		#endregion Parameters

		#region ExecSql

		/// <summary>
		/// Sql utasítás végrehajtása
		/// </summary>
		/// <param name="self">Query</param>
		/// <param name="trans">Tranzakció (opcionális)</param>
		public static void ExecSql(this in SqlQuery self, IDbTransaction trans = null)
			=> ExecSql((self, null), trans);

		/// <summary>
		/// Sql utasítás végrehajtása
		/// </summary>
		/// <param name="self">Query + paraméterlista</param>
		/// <param name="trans">Tranzakció (opcionális)</param>
		public static void ExecSql(this in (SqlQuery Query, List<IDbDataParameter> Parameters) self, IDbTransaction trans = null)
		{
			static void ExecSql(in SqlQuery query, IDbCommand cmd) => cmd.ExecuteNonQuery();

			// ha a tranzakció null
			if (trans is null)
			{
				// , akkor egy új adatbáziskapcsolattal futtatjuk az Sql-t és commitoljuk
				self.WithConnection(true, ExecSql);
			}
			else
			{
				// , ha nem, akkor a beadott tranzakcióval futtatjuk az Sql-t
				self.WithoutConnection(trans, ExecSql);
			}
		}

		#endregion ExecSql

		#region GetValues

		/// <summary>
		/// Lekérdezés eredményét adja vissza a megadott típus szerint tömb formájában
		/// </summary>
		/// <typeparam name="T">Várt típus</typeparam>
		/// <param name="self">Query</param>
		/// <param name="trans">Tranzakció (opcionális)</param>
		/// <param name="commit">Kell-e commitolni (csak önálló tranzakció esetében érvényes)</param>
		/// <returns>Lekérdezés eredménye többen</returns>
		public static T[] GetValues<T>(this in SqlQuery self, IDbTransaction trans = null, bool commit = false)
			=> GetValues<T>((self, null), trans, commit);

		/// <summary>
		/// Lekérdezés eredményét adja vissza a megadott típus szerint tömb formájában
		/// </summary>
		/// <typeparam name="T">Várt típus</typeparam>
		/// <param name="self">Query + paraméterlista</param>
		/// <param name="trans">Tranzakció (opcionális)</param>
		/// <param name="commit">Kell-e commitolni (csak önálló tranzakció esetében érvényes)</param>
		/// <returns>Lekérdezés eredménye többen</returns>
		public static T[] GetValues<T>(this in (SqlQuery Query, List<IDbDataParameter> Parameters) self, IDbTransaction trans = null, bool commit = false)
		{
			T[] values = default;

			static IEnumerable<T> ReadAll(IDataReader reader)
			{
				// tuple varázslathoz
				var t = typeof(T);
				t = Nullable.GetUnderlyingType(t) ?? t;

				while (reader.Read())
				{
					yield return t.IsValueTuple()
						? reader.GetTupleValue<T>(t)
						: reader.GetValue(0).NGetValue<T, object>();
				}
			}

			void GetValues(in SqlQuery query, IDbCommand cmd)
			{
				using var reader = cmd.ExecuteReader();
				values = ReadAll(reader).ToArray();
			}

			if (trans is null)
			{
				self.WithConnection(commit, GetValues);
			}
			else
			{
				self.WithoutConnection(trans, GetValues);
			}

			return values ?? ArrayExt.Empty<T>();
		}

		#endregion GetValues

		#region GetObjects

		public static T[] GetObjects<T>(this in SqlQuery self, IDbTransaction trans = null, bool commit = false)
			where T : class, new()
			=> GetObjects<T>((self, null), trans, commit);

		public static T[] GetObjects<T>(this in (SqlQuery Query, List<IDbDataParameter> Parameters) self, IDbTransaction trans = null, bool commit = false)
			where T : class, new()
		{
			T[] objects = default;

			void GetObject(in SqlQuery query, IDbCommand cmd)
			{
				using var reader = cmd.ExecuteReader();
				objects = DbHelper.Data2Object<T>(reader).ToArray();
			}

			if (trans is null)
			{
				self.WithConnection(commit, GetObject);
			}
			else
			{
				self.WithoutConnection(trans, GetObject);
			}

			return objects ?? ArrayExt.Empty<T>();
		}

		#endregion GetValues
	}
}
