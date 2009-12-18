using System;
using System.Text; 
using System.Threading;
using NUnit.Framework;
using Gearman; 
using Gearman.Packets.Worker;
using Gearman.Packets.Client; 

namespace Gearman.Tests
{

  	[TestFixture]
  	public class BackgroundTest
  	{
		
		private void bgtest(Job j)
		{
			int words = 0;
			byte[] outdata = new byte[1024];
			
			byte completediterations = 0; 
			byte totaliterations = 60;
			
			WorkStatus ws; 
			
			while(completediterations < totaliterations)
			{
				completediterations++;

				ws = new WorkStatus(j.jobhandle, completediterations, totaliterations); 
							
				j.sendWorkUpdate(ws);
			
				Thread.Sleep(500);
			}
			
			words = 10;
			outdata = BitConverter.GetBytes(words);
			Console.WriteLine("Found {0} words", words);
			j.sendResults(outdata);
			
			//return outdata;
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