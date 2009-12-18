using System;
using System.Net.Sockets; 
using System.Text; 
using System.Threading;

using log4net;
using Gearman;
using Gearman.Packets.Worker;

namespace LoadTest
{

  	public class LoadTest
  	{
		public static readonly ILog Log = LogManager.GetLogger(typeof(LoadTest));

		private static void randomlengthjob(Job j)
		{
			Random r = new Random(); 
			int sleeptime = r.Next(1, 15);
			byte[] result = j.data;
		
			LoadTest.Log.DebugFormat("Sleeping for {0} seconds", sleeptime);
			Thread.Sleep(sleeptime * 1000);
			// Give back the same thing...
			j.sendResults(result);
		}
		
		public static void Main (string[] args)
		{
      		Random r = new Random(); 
			
			int numworkers = r.Next(15,30);
			int numclients = r.Next(50, 100);
			
			Thread[] clients = new Thread[numclients];
			Worker[] workers = new Worker[numworkers];
			
			Log.DebugFormat("Spawning {0} workers and {1} clients", numworkers, numclients);
						
			for(int i = 0; i < numworkers; i++)
			{
				Log.DebugFormat("Spawning worker #{0}", i);
				Worker w = new Worker("localhost");
				w.registerFunction("randomlength", randomlengthjob);
				w.workLoop();
				
				workers[i] = w;
			}
		
			for(int j = 0; j < numclients; j++) 
			{
				Thread t = new Thread(clientThread);
				t.Name = String.Format("Client #{0}", j);
				Log.DebugFormat("Spawning client #{0}", j);
				t.Start();
				
				clients[j] = t; 
			}
			
			
					
	    }
	
		public static void clientThread()
		{
			Client c = new Client("localhost");
			Random r = new Random(); 
			uint giveMeBack = (uint)r.Next(1,65536);
			byte[] data = BitConverter.GetBytes(giveMeBack);
			byte[] result = c.submitJob("randomlength", data);
			bool success = true;
			for(int i = 0; i < data.Length; i++) 
			{
				if(result[i] != data[i])
				{
					LoadTest.Log.DebugFormat("Failed to get the correct data back!");
					success = false;
				}
			}
			
			if(success){
				LoadTest.Log.DebugFormat("Job completed!");
			}
		}
	
	}
}