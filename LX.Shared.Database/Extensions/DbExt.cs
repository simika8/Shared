using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using LX.Common.Database.ConnectionProvider;
using LX.Common.Extensions;

namespace LX.Common.Database.Extensions
{
#if EXTERN
	public
#else
	internal
#endif
	static class DbExt
	{
		public static void AddParams<T>(this DbParameterCollection self, DatabaseEngine databaseEngine, T paramObject) where T : class
		{
			var properties = paramObject.GetType().GetProperties();

			foreach (var prop in properties)
			{
				self.Add(DbHelper.CreateDataParameter(databaseEngine, $"@{prop.Name}", prop.GetValue(paramObject, null)));
			}
		}

		public static IEnumerable<KeyValuePair<string, object>> GetParams(object paramObject)
		{
			if (paramObject == null)
			{
				yield break;
			}

			var properties = paramObject.GetType().GetProperties();

			foreach (var prop in properties)
			{
				yield return new KeyValuePair<string, object>(prop.Name, prop.GetValue(paramObject, null));
			}
		}

		public static IEnumerable<T> ReadColumnToEnd<T>(this IDataReader self)
		{
			while (self.Read())
			{
				yield return self.GetValue(0).NGetValue<T, object>();
			}
		}
	}
}
