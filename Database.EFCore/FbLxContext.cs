using Database.EFCore;
using LX.Common.Database.ConnectionProvider;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Database.EFCore
{
	/// <summary>
	/// Oldstruct táblákat tartalmazó Firebird adatbázis
	/// </summary>
	public class FbLxContext : LxContext
	{
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			//FirebirdShared.OnModelCreating(modelBuilder);
			base.OnModelCreating(modelBuilder);

			// Táblanév nagybetűsítés
			foreach (var entityType in modelBuilder.Model.GetEntityTypes())
			{
				entityType.SetTableName(entityType.GetTableName().ToUpper());
			}

			//Mezőnév nagybetűsítés
			foreach (var entityType in modelBuilder.Model.GetEntityTypes())
			{
				foreach (var property in entityType.GetProperties())
				{
					property.SetColumnName(property.GetColumnName().ToUpper());
				}
			}
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			string connstr = new LXConnectionProvider().ConnectionString;

			optionsBuilder
				.UseFirebird(connstr);

			base.OnConfiguring(optionsBuilder);
		}
	}
}
