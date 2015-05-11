using System;

namespace SpatialKey.DataManager.Lib
{
	public class BaseMessageClass
	{
		/// <summary>Logging delegate</summary>
		public delegate void Messager(MessageLevel level, string message);

		/// <summary>
		/// Gets or sets the logger.  Used by  for <see cref="Log(message)"/>
		/// </summary>
		public Messager MyMessenger { get; set; }

		/// <summary>
		/// Log the specified message if the <see cref="MyMessenger"/> is set
		/// </summary>
		protected void ShowMessage(MessageLevel level, string message)
		{
			if (MyMessenger != null)
				MyMessenger(level, message);
		}

		public BaseMessageClass(Messager messenger)
		{
			MyMessenger = messenger;
		}
	}
}

