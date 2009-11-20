using System;
using System.Text; 
using System.Threading;
using NUnit.Framework;
using Gearman; 

namespace Gearman.Tests
{

  	[TestFixture]
  	public class BackgroundTest
  	{
		private static byte[] bgtest(Packet jobPacket, Connection c)
		{
			int words = 0;
			byte[] outdata = new byte[1024];
			
			Random r = new Random();
			int next = -1;
			byte completediterations = 0; 
			byte totaliterations = 100;
			
			RequestPacket rp; 
			
			while(completediterations < totaliterations)
			{
				next = r.Next(0, 100);
				if (next > 50)
				{
					completediterations++;
										
					byte[] data = new ASCIIEncoding().GetBytes(completediterations + "\0" + totaliterations);
				
					rp = new RequestPacket(PacketType.WORK_STATUS, data, jobPacket.JobHandle);
					rp.Dump();
					c.sendPacket(rp);
				}
				Thread.Sleep(1000);
			}
			
			words = 10;
			outdata = BitConverter.GetBytes(words);
			Console.WriteLine("Found {0} words", words);
			return outdata;
		}
		
		[Test]
    	public void testBackgroundJob()
    	{
      		Worker w = new Worker("localhost");
			w.registerFunction("bgtest", bgtest);
			w.workLoop();
			
			Client c = new Client("localhost"); 
			byte[] data = new ASCIIEncoding().GetBytes("");
			string jobhandle = c.submitJobInBackground("bgtest", data, Client.JobPriority.HIGH);
		
			while(!c.checkIsDone(jobhandle))
			{
				Console.WriteLine("Still not done!");
				Thread.Sleep(1500);
			}
			
			w.stopWorkLoop();
	    }
	}
}