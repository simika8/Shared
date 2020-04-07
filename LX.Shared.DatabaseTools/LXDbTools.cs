using LX.Common.Database.Extensions;
using LX.Common.Extensions;

/********************************************************************
*						Elxir specifikus rész						*
********************************************************************/

namespace LX.Common.Database
{
#if EXTERN
	public
#else
	internal
#endif
	static class DbTools
	{
		/// <summary>
		/// Elixir borítékos üzenet küldése
		/// </summary>
		/// <param name="message">Üzenet szövege (max 300 karakter)</param>
		/// <param name="userId">Melyik felhasználónak menjen (-1 = mind)</param>
		public static void SendMessage(string message, int userId = -1)
			=> LXDb.New("EXECUTE PROCEDURE p_sendmessage(@userId, @message, NULL);")
				.AddParam("userId", userId)
				.AddParam("message", message.Copy(0, 300))
				.ExecSql();

		/// <summary>
		/// Elixir borítékos üzenet küldése (részletes)
		/// </summary>
		/// <param name="message">Üzenet szövege (max 300 karakter)</param>
		/// <param name="details">Részletes szöveg (korlátlan)</param>
		/// <param name="type">Üzenet típusa</param>
		/// <param name="userId">Melyik felhasználónak menjen (-1 = mind)</param>
		/// <param name="relId">Hivatkozási azonosító</param>
		public static void SendMessage(string message, string details, int? type = null, int userId = -1, int relId = -1)
			=> LXDb.New("EXECUTE PROCEDURE p_sendmessage_blob(@userId, @message, NULL, @type, @details, @relid);")
				.AddParam("userId", userId)
				.AddParam("message", message.Copy(0, 300))
				.AddParam("type", type)
				.AddParam("details", details)
				.AddParam("relid", relId)
				.ExecSql();

		/// <summary>
		/// Adott nevű adatbázis eseményt küld a feliratkozóknak
		/// </summary>
		/// <param name="eventName">Esemény neve</param>
		public static void SendEvent(string eventName)
			=> LXDb.New("EXECUTE PROCEDURE p_sendevent(@eventname);")
				.AddParam("eventname", eventName)
				.ExecSql();
	}
}
