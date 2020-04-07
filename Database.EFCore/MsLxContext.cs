using System;
using System.Collections.Generic;
using System.Text;
using LX.Common.Database.ConnectionProvider;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Database.EFCore
{
	public class MsLxContext : LxContext
	{
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			//új ms sql alapú adatbázisrendszernél minden főmodul külön táblában lesz, ennek nevét ki kell tölteni.
			if (DatabaseName == null)
				throw new Exception("MsLxContext.DatabaseName megadása közelező.");

			string connstr = new MsConnectionStringProvider(DatabaseName).ConnectionString;

			optionsBuilder
				.UseSqlServer(connstr);

			base.OnConfiguring(optionsBuilder);
		}

		
	}
}
