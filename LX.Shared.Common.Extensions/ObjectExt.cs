using System.Linq;

namespace LX.Common.Extensions
{
	/// <summary>
	/// Objektum kiterjesztések
	/// </summary>
	public static class ObjectExt
	{
		/// <summary>
		/// Property-k érték szerinti másolása egyik objektumból a másikba
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="self"></param>
		/// <param name="source"></param>
		public static void CopyPropertiesFrom<T>(this T self, T source) where T : class
		{
			var convertProperties = typeof(T).GetProperties().Where(p => p.CanRead && p.CanWrite);

			foreach (var entityProperty in convertProperties.Coalesce())
			{
				var convertProperty = convertProperties.FirstOrDefault(prop => prop.Name == entityProperty.Name);
				if (convertProperty != null)
				{
					convertProperty.SetValue(self, entityProperty.GetValue(source, null), null);
				}
			}
		}
	}
}
