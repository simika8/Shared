using System.IO;
using Newtonsoft.Json;

namespace LX.Common.JSON
{
#if EXTERN
	public
#else
	internal
#endif
	static class JSONExt
	{
		/// <summary>
		/// Objektum JSON stringbe szerializálása
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="self">Szerializálandó objektum</param>
		/// <param name="formatting">Formázás</param>
		public static string JSerialize<T>(this T self, Formatting formatting = Formatting.None)
			=> JsonConvert.SerializeObject(self, formatting);

		/// <summary>
		/// Objektum JSON fileba szerializálása
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="self">Szerializálandó objektum</param>
		/// <param name="fileName">Cél filenév</param>
		/// <param name="formatting">Formázás</param>
		public static void JSerializeToFile<T>(this T self, string fileName, Formatting formatting = Formatting.None)
			=> File.WriteAllText(fileName, self.JSerialize(formatting));
	}
}
