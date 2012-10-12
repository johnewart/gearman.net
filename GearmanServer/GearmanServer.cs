[assembly: log4net.Config.XmlConfigurator(Watch=true)]



namespace GearmanServer
{
	using System;
	using System.Configuration;
	using System.Collections.Specialized;

	using ServiceStack.Redis; 

	using log4net; 
	using log4net.Config;
    using System.Net;
    using DurableQueues; 

	public class GearmanServer
	{
		public static readonly ILog Log = LogManager.GetLogger(typeof(GearmanServer));
		public static readonly NameValueCollection config = ConfigurationManager.AppSettings;

		static void Main ()
		{
            String hostName = Dns.GetHostName();
			Log.Info("Starting .NET Gearman Server v0.5 on host " + hostName);
            var redisClient = new RedisClient("mort.local");
            var redisQueue = new RedisQueue(redisClient); 
            //var jobQueue = new JobQueue(redisQueue);
			var jobQueue = new JobQueue();
			new Daemon(jobQueue, hostName); 
		}
	}
}