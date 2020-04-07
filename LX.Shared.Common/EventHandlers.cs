using System;

namespace LX.Common.EventHandlers
{
	/// <summary>
	/// Szöveges eseménykezelő (eseménynapló vagy konzolos kiíráshoz)
	/// </summary>
#if EXTERN
	public
#else
	internal
#endif
	static class MessageHandler
	{
		/// <summary>
		/// Üzenet eseménykezelő
		/// </summary>
		public static event EventHandler<MessageEventArgs> Message;

		/// <summary>
		/// Üzenet típusa
		/// </summary>
		public enum MessageType
		{

			/// <summary>
			/// Információ
			/// </summary>
			Info,

			/// <summary>
			/// Hiba
			/// </summary>
			Error,

			/// <summary>
			/// Figyelmeztetés
			/// </summary>
			Warning,
		};

		/// <summary>
		/// Saját esemény argumentum szöveges üzenettel és típussal
		/// </summary>
		public class MessageEventArgs : EventArgs
		{
			/// <summary>
			/// Üzenet szövege
			/// </summary>
			public string MessageText { get; }

			/// <summary>
			/// Üzenet típusa
			/// </summary>
			public MessageType MessageType { get; }

			public int Loglevel { get; }

			/// <summary>
			/// Üzenet esemény paraméterek
			/// </summary>
			/// <param name="messageText">Üzenet szövege</param>
			/// <param name="messageType">Üzenet típusa</param>
			/// <param name="logLevel">Üzenet szintje</param>
			public MessageEventArgs(string messageText, MessageType messageType, int logLevel)
			{
				MessageText = messageText;
				MessageType = messageType;
				Loglevel = logLevel;
			}
		}

		/// <summary>
		/// Üzenet küldése
		/// </summary>
		/// <param name="sender">Küldő</param>
		/// <param name="message">Üzenet szövege</param>
		/// <param name="messageType">Üzenet típusa></param>
		public static void SendMessage(object sender, string message, MessageType messageType) =>
			Message?.Invoke(sender, new MessageEventArgs(message, messageType, 1));

		/// <summary>
		/// Üzenet küldése
		/// </summary>
		/// <param name="sender">Küldő</param>
		/// <param name="message">Üzenet szövege</param>
		/// <param name="messageType">Üzenet típusa></param>
		/// <param name="logLevel">Üzenet szintje</param>
		public static void SendMessage(object sender, string message, MessageType messageType, int logLevel) =>
			Message?.Invoke(sender, new MessageEventArgs(message, messageType, logLevel));
	}

	/// <summary>
	/// Folyamatjelzésre szolgáló eseménykezelő
	/// </summary>
#if EXTERN
	public
#else
	internal
#endif
	class ProgressHandler
	{
		/// <summary>
		/// Folyamatjelző esemény
		/// </summary>
		public event EventHandler<ProgressEventArgs> Progress;

		/// <summary>
		/// Legutolsó eseményparaméterek, összehasonlításhoz kell
		/// </summary>
		private ProgressEventArgs _lastEventArgs;

		/// <summary>
		/// Saját esemény argumentum főfolyamattal, névvel, és alfolyamattal
		/// </summary>
		public class ProgressEventArgs : EventArgs, IEquatable<ProgressEventArgs>
		{
			/// <summary>
			/// Főfolyamat állása %-ban
			/// </summary>
			public int MainProgress { get; }

			/// <summary>
			/// Főfolyamat neve
			/// </summary>
			public string MainProgressName { get; }

			/// <summary>
			/// Alfolyamat állása %-ban
			/// </summary>
			public int SubProgress { get; }

			/// <summary>
			/// Folyamatjelző esemény paraméterek
			/// </summary>
			/// <param name="mainProgress">Főfolyamat állása</param>
			/// <param name="mainProgressName">Főfolyamat neve</param>
			/// <param name="subProgress">Alfolyamat állása</param>
			public ProgressEventArgs(int mainProgress, string mainProgressName, int subProgress)
			{
				MainProgress = mainProgress;
				MainProgressName = mainProgressName;
				SubProgress = subProgress;
			}

			/// <summary>
			/// Összehasonlít két folyamatparamétert
			/// </summary>
			/// <param name="other">Másik paraméter</param>
			/// <returns>Egyezik-e</returns>
			public bool Equals(ProgressEventArgs other) =>
				MainProgress == other?.MainProgress
					&& MainProgressName == other.MainProgressName
					&& SubProgress == other.SubProgress;

			/// <summary>
			/// Összehasonlít két folyamatparamétert
			/// </summary>
			/// <param name="mainProgress">Másik paraméter főfolyamat állása</param>
			/// <param name="mainProgressName">Másik paraméter főfolyamat neve</param>
			/// <param name="subProgressr">Másik paraméter alfolyamat állása</param>
			/// <returns>Egyezik-e</returns>
			public bool Equals(int mainProgress, string mainProgressName, int subProgressr) =>
				MainProgress == mainProgress
					&& MainProgressName == mainProgressName
					&& SubProgress == subProgressr;
		}

		/// <summary>
		/// Folyamatjelző esemény küldése
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="mainProgress">Főfolyamat állása</param>
		/// <param name="mainProgressName">Főfolyamat neve</param>
		/// <param name="subProgress">Alfolyamat állása</param>
		public void OnProgress(object sender, int mainProgress, string mainProgressName, int subProgress)
		{
			if (_lastEventArgs == null)
			{
				_lastEventArgs = new ProgressEventArgs(mainProgress, mainProgressName, subProgress);
				Progress?.Invoke(sender, _lastEventArgs);
			}
			else if (!_lastEventArgs.Equals(mainProgress, mainProgressName, subProgress))
			{
				_lastEventArgs = new ProgressEventArgs(mainProgress, mainProgressName, subProgress);
				Progress?.Invoke(sender, _lastEventArgs);
			}
		}
	}
}
