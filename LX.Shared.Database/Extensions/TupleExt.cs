using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using LX.Common.Extensions;

namespace LX.Common.Database
{
	/// <summary>
	/// Black magic
	/// </summary>
#if EXTERN
	public
#else
	internal
#endif
	static class TupleExt
	{
		public static bool IsValueTuple(this Type t)
		{
			// dotnet 4.7.1+ ITuple
#if NETFX_471
			return typeof(System.Runtime.CompilerServices.ITuple).IsAssignableFrom(t);
#else
			return t.Name.StartsWith("ValueTuple");
#endif
		}

		// dotnet 4.7.1+ ITuple
#if NETFX_471
		public static void SetTupleValues<T>(this T self, IEnumerable<object> values) where T: System.Runtime.CompilerServices.ITuple
#else
		public static void SetTupleValues(this object self, IEnumerable<object> values)
#endif
		{
			int counter = 0;
			foreach (var field in self.GetType().GetFields())
			{
				object[] objects = values as object[] ?? values.ToArray();
				if ("Rest" == field.Name)
				{
#if NETFX_471
					var v = (System.Runtime.CompilerServices.ITuple)field.GetValue(self);
					SetTupleValues(v, objects.Skip(counter));
#else
					object v = field.GetValue(self);
					SetTupleValues(v, objects.Skip(counter));
#endif
					field.SetValue(self, v);
				}
				else
				{
					field.SetValue(self, objects.ElementAt(counter++).NGetValue(field.FieldType));
				}
			}
		}

		//private static IEnumerable<(object tuple, FieldInfo field)> EnumerateValueTuple(object valueTuple)
		//{
		//	var tuples = new Queue<object>();
		//	tuples.Enqueue(valueTuple);

		//	while (tuples.Count > 0 && tuples.Dequeue() is object tuple)
		//	{
		//		foreach (var field in tuple.GetType().GetFields())
		//		{
		//			if (field.Name == "Rest")
		//			{
		//				tuples.Enqueue(field.GetValue(tuple));
		//			}
		//			else
		//			{
		//				yield return (tuple, field);
		//			}
		//		}
		//	}
		//}

		public static T GetTupleValue<T>(this IDataReader reader, Type tupleType)
		{
			// dotnet 4.7.1+ ITuple
#if NETFX_471
			var tuple = (System.Runtime.CompilerServices.ITuple)Activator.CreateInstance(tupleType);
			object[] lineValues = new object[tuple?.Length ?? 0];
#else
			object tuple = Activator.CreateInstance(tupleType);
			object[] lineValues = new object[tupleType.GetTupleLength()];
#endif
			reader.GetValues(lineValues);
			tuple.SetTupleValues(lineValues);
			return (T)tuple;
		}

#if !NETFX_47

		// dotnet 4.7.1+ ITuple.Length
		private static int GetTupleLength(this Type self)
		{
			int counter = 0;
			foreach (var field in self.GetFields())
			{
				if ("Rest" == field.Name)
				{
					counter += GetTupleLength(field.FieldType);
				}
				else
				{
					counter++;
				}
			}
			return counter;
		}
#endif
	}
}
