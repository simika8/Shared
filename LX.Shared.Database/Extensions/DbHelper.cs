using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using LX.Common.Database.ConnectionProvider;
using LX.Common.Extensions;

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
	static class DbHelper
	{
		/// <summary>
		/// SQL + ConnectionString
		/// </summary>
		public static SqlQuery New(string sql, string connectionString, DatabaseEngine databaseEngine = DatabaseEngine.Firebird)
			=> new SqlQuery(new ConnectionStringProvider(connectionString, databaseEngine), sql);

		/// <summary>
		/// Paraméter létrehozása
		/// </summary>
		public static IDbDataParameter CreateDataParameter<T>(DatabaseEngine databaseEngine, string parameterName, T parameterValue)
			=> DatabaseProviderLoader.CreateParameter<T>(databaseEngine, parameterName, parameterValue);

		public static IEnumerable<T> Data2Object<T>(IDataReader reader) where T : class, new()
		{
			var props = typeof(T).GetProperties();
			string[] columns = null;

			while (reader.Read())
			{
				var t = new T();

				if (columns is null)
				{
					columns = Enumerable.Range(0, reader.FieldCount).Select(i => reader.GetName(i).ToUpper()).ToArray();
				}

				foreach (var prop in props)
				{
					string propUpperName = prop.Name.ToUpper();

					if (!columns.Contains(propUpperName))
					{
						continue;
					}

					int index = Array.IndexOf(columns, propUpperName);

					if (index < 0)
					{
						continue;
					}

					object value = reader.GetValue(index);

					//bool canConvert = !(value is DBNull) && TypeDescriptor.GetConverter(value.GetType()).CanConvertTo(prop.NGetType(true));

					bool canConvert = !(value is DBNull) && prop.NGetType(true) == reader.GetFieldType(index);
					prop.SetValue(t, canConvert ? value : null, null);
				}

				yield return t;
			}
		}
	}
}
