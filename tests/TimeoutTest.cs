using System;
using System.Net.Sockets; 
using System.Text; 
using System.Threading;

using NUnit.Framework;
using Gearman;
using Gearman.Packets.Worker;

namespace Gearman.Tests
{

  	[TestFixture]
  	public class TimeoutTest
  	{
		private void infinite(Job j)
		{
			// Loop forever
			while(true){
				Thread.Sleep(1000);
			}
		}
		
		[Test]
    		public void testTimeout()
    		{
      		Worker w = new Worker("localhost");
			w.registerFunction("infinite", infinite);
			w.workLoop();
		
			
			DateTime start = DateTime.Now;
			Console.WriteLine("Started: {0}", start);
			
			Client c = new Client("localhost");
			byte[] data = new ASCIIEncoding().GetBytes("zzz\nyyy\napple\nbaz\nfoo\nnarf\nquiddle\n");
			c.submitJob("infinite", data);
			DateTime stop = DateTime.Now;
			
			Console.WriteLine("Connection died at {1}", stop);
						
			w.stopWorkLoop();
	    }
	
	
	}
}