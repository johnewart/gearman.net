[assembly: log4net.Config.XmlConfigurator(Watch=true)]



namespace GearmanServer
{
	using System;
	using System.Configuration;
	using System.Collections.Specialized;

	
	using log4net; 
	using log4net.Config; 

	public class GearmanServer
	{
		public static readonly ILog Log = LogManager.GetLogger(typeof(GearmanServer));
		public static readonly NameValueCollection config = ConfigurationManager.AppSettings;

		static void Main ()
		{
			Log.Info("Starting .NET Gearman Server v0.5");
			new Daemon(); 
		}
	}
}
