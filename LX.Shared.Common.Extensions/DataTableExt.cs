using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;

namespace LX.Common.Extensions
{
#if EXTERN
	public
#else
	internal
#endif
	static class DataTableExt
	{
		public static T GetAttributeFrom<T>(this PropertyInfo self) where T : Attribute
			=> (T)self.GetCustomAttributes(typeof(T), false).FirstOrDefault();

		public static DataTable ToDataTable<T>(this IEnumerable<T> self, string name = null)
		{
			var props = typeof(T).GetProperties();

			var dt = new DataTable(name);

			foreach (var p in props)
			{
				var col = dt.Columns.Add(p.Name, Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType);

				#region Mezőhossz
#if USE_DataAnnotations
				int? maxlenth = p.GetAttributeFrom<System.ComponentModel.DataAnnotations.MaxLengthAttribute>()?.Length;

				if (maxlenth.HasValue)
				{
					col.MaxLength = maxlenth.Value;
				}
#endif
				#endregion Mezőhossz

				#region Mezőnév
				string caption = p.GetAttributeFrom<DescriptionAttribute>()?.Description;

				if (!string.IsNullOrEmpty(caption))
				{
					col.Caption = caption;
				}
				#endregion Mezőnév
			}

			foreach (var row in self)
			{
				var drow = dt.NewRow();

				foreach (var p in props)
				{
					drow[p.Name] = p.GetValue(row, ArrayExt.Empty<object>()) ?? DBNull.Value;
				}

				dt.Rows.Add(drow);
			}

			return dt;
		}
	}
}
