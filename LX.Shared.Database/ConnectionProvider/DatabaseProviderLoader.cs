using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace LX.Common.Database.ConnectionProvider
{
	internal static class DatabaseProviderLoader
	{
		private static readonly Dictionary<DatabaseEngine, Lazy<Assembly>> s_assembly = new Dictionary<DatabaseEngine, Lazy<Assembly>>
		{
			[DatabaseEngine.Firebird] = new Lazy<Assembly>(() => GetAssembly("FirebirdSql.Data.FirebirdClient")),
			[DatabaseEngine.MySQL] = new Lazy<Assembly>(() => GetAssembly("MySql.Data")),
			[DatabaseEngine.MsSQL] = new Lazy<Assembly>(() => GetAssembly("Microsoft.Data.SqlClient")),
		};

		private static readonly Dictionary<DatabaseEngine, Lazy<Type>> s_connectionType = new Dictionary<DatabaseEngine, Lazy<Type>>
		{
			[DatabaseEngine.Firebird] = new Lazy<Type>(() => FindType(DatabaseEngine.Firebird, "FbConnection")),
			[DatabaseEngine.MySQL] = new Lazy<Type>(() => FindType(DatabaseEngine.MySQL, "MySqlConnection")),
			[DatabaseEngine.MsSQL] = new Lazy<Type>(() => FindType(DatabaseEngine.MsSQL, "SqlConnection")),
		};

		private static readonly Dictionary<DatabaseEngine, Lazy<Type>> s_dbParameterType = new Dictionary<DatabaseEngine, Lazy<Type>>
		{
			[DatabaseEngine.Firebird] = new Lazy<Type>(() => FindType(DatabaseEngine.Firebird, "FbParameter")),
			[DatabaseEngine.MySQL] = new Lazy<Type>(() => FindType(DatabaseEngine.MySQL, "MySqlParameter")),
			[DatabaseEngine.MsSQL] = new Lazy<Type>(() => FindType(DatabaseEngine.MsSQL, "SqlParameter")),
		};

		public static IDbConnection CreateConnection(DatabaseEngine databaseEngine, string connectionString)
			=> Activator.CreateInstance(s_connectionType[databaseEngine].Value, new object[] { connectionString }) as IDbConnection;

		public static IDbDataParameter CreateParameter<T>(DatabaseEngine databaseEngine, string paramName, T paramValue)
			=> Activator.CreateInstance(s_dbParameterType[databaseEngine].Value, new object[] { paramName, paramValue }) as IDbDataParameter;

		private static Assembly GetAssembly(string name)
		{
			var ass = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == name);

			if (ass is null)
			{
				ass = Assembly.Load(name);
			}

			return ass;
		}

		private static Type FindType(DatabaseEngine databaseEngine, string name)
		{
			var a = s_assembly[databaseEngine].Value;

			if (a is null)
			{
				throw new Exception("fuck");
			}

			var type = a.GetTypes().FirstOrDefault(t => t.Name == name);

			return type;
		}
	}
}
