using System;
using System.Net.Sockets; 
using System.Text; 
using System.Threading;

using log4net;
using NUnit.Framework;
using Gearman;
using Gearman.Packets.Worker;

namespace Gearman.Tests
{

  	[TestFixture]
  	public class LoadTest
  	{
		public static readonly ILog Log = LogManager.GetLogger(typeof(LoadTest));
	
		Worker w;
		
		private void randomlengthjob(Job j)
		{
			Random r = new Random(); 
			int sleeptime = r.Next(1, 15);
			byte[] result = new byte[1];
			result[0] = 1;
			Log.DebugFormat("Sleeping for {0} seconds", sleeptime);
			Thread.Sleep(sleeptime * 1000);
			j.sendResults(result);
		}
		
		[Test]
    		public void testLoad()
    		{
      		Random r = new Random(); 
			
			int numworkers = r.Next(15,30);
			int numclients = r.Next(50, 100);
			
			Log.DebugFormat("Spawning {0} workers and {1} clients", numworkers, numclients);
			 
			for(int i = 0; i < numworkers; i++)
			{
				w = new Worker("localhost");
				w.registerFunction("randomlength", randomlengthjob);
				w.workLoop();
			}
		
			for(int j = 0; j < numclients; j++) 
			{
				Thread t = new Thread(clientThread);
				t.Name = String.Format("Client #{0}", j);
				t.Start();
			}
					
	    }
	
		public void clientThread()
		{
			Client c = new Client("localhost");
			byte[] data = new ASCIIEncoding().GetBytes("\n");
			c.submitJob("randomlength", data);
		}
	
	}
}