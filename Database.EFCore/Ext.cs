using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Database.EFCore
{
	public static class Ext
	{
		/// <summary>
		/// Módosított SingleOrDefault, csak changetrackerben is megtalálja a rekordot, ha már megvan.
		/// Nem alkalmas annak egyértelmű vizsgálatára, hogy csak 1 predicate-nek megfelelő rekord lehet. (lehet, hogy a changetrackerben 1 van, de az adatbázisban több, és ilyenkor az adatbázisban nem ellenőriz)
		/// </summary>
		public static T SingleOrDefaultLocal<T>(this DbSet<T> dbset, Expression<Func<T, bool>> predicate) where T : class
		{
			//Megkeresem a changetrackerbe betöltött verzióját a rekordnak
			var entityLocal = dbset.Local.SingleOrDefault(predicate.Compile());
			if (entityLocal != default(T))
				return entityLocal;

			//Megkeresem az adatbázisban elérhető verzióját
			var entitydb = dbset.SingleOrDefault(predicate);
			return entitydb;
		}

		/// <summary>
		/// Az adott DbSet-ben megkeresi azt a rekordot, amit a setKeyValuesFunc-ban megadott értékekkel egyértelműen azonosítunk. 
		/// Első körben a changetrackerben keres, aztán az adatbázisban, és ha nem talál ott sem, akkor létrehoz egy újat, és hozzáadja a dbsethez
		/// </summary>
		/// <param name="setKeyValuesFunc">A rekordot egyértelműen azonosító mezőknek kell itt értéket adni. Ha Nem találjuk a rekordot, akkor az új rekordot is ez alapján hozzuk létre</param>
		public static T GetData<T>(this DbSet<T> dbset, Action<T> setKeyValuesFunc) where T : class, new()
		{
			T newEntity = new T();
			setKeyValuesFunc?.Invoke(newEntity);


			var predicate = CalcPredicate(newEntity);

			var entity = SingleOrDefaultLocal(dbset, predicate);
			//ha nincs ilyen, akkor a setKeyValuesFunc-cal létrehozottat berakom az adatbázisba, és visszaadom
			if (entity == default(T))
			{
				entity = newEntity;
				dbset.Add(entity);
			}
			return entity;
			//return null;
		}

		/// <summary>
		/// A beadott kulcsértékekkel feltöltött entity alapján létrehoz egy where feltételt. (property1 == érték1 && property2 == érték2 ....)
		/// Azok a propertyk kerülnek a where feltételbe, amik a beadott objektumon módosítva lettek a defaulthoz képest.
		/// </summary>
		private static Expression<Func<T, bool>> CalcPredicate<T>(T keyValuesEntity) where T : class, new()
		{
			var properties = keyValuesEntity.GetType().GetProperties();

			var pe = Expression.Parameter(typeof(T));
			Expression whereExp = null!;

			foreach (var prop in properties)
			{
				var a = prop.PropertyType.GetInterfaces();
				bool isCollection = prop.PropertyType.GetInterfaces().Contains(typeof(System.Collections.ICollection));
				isCollection = isCollection || 
					prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition().Equals(typeof(ICollection<>));
				isCollection = isCollection || 
					prop.PropertyType.Name.Contains("ICollection");

				if (!IsNullOrDefault(prop.GetValue(keyValuesEntity)) && !isCollection)
				{
					Expression leftExp = Expression.Property(pe, prop.Name);
					Expression rightExp = Expression.Constant(prop.GetValue(keyValuesEntity));
					Expression equalExp = Expression.Equal(leftExp, rightExp);
					if (whereExp != null)
						whereExp = Expression.And(whereExp, equalExp);
					else
						whereExp = equalExp;
				}
			}
			var predicate = Expression.Lambda<Func<T, bool>>(whereExp, new ParameterExpression[] { pe });
			return predicate;
		}

		private static bool IsNullOrDefault<TObject>(TObject argument) where TObject : class
		{
			// deal with normal scenarios
			if (argument == null)
			{
				return true;
			}
			if (object.Equals(argument, default(TObject)))
			{
				return true;
			}

			// deal with non-null nullables
			Type methodType = typeof(TObject);
			if (Nullable.GetUnderlyingType(methodType) != null)
			{
				return false;
			}

			// deal with boxed value types
			Type argumentType = argument.GetType();
			if (argumentType.IsValueType && argumentType != methodType)
			{
				object obj = Activator.CreateInstance(argument.GetType());
				return obj.Equals(argument);
			}

			return false;
		}

	}
}
