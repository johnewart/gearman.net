using System;
using System.Net.Sockets; 
using System.Text; 

using NUnit.Framework;
using Gearman;

namespace Gearman.Tests
{

  	[TestFixture]
  	public class WorkerTest
  	{
		private static byte[] wctest(Packet jobPacket, Connection c)
		{
			int words = 0;
			byte[] outdata = new byte[1024];
			byte[] indata = jobPacket.Data;
			for(int i = 0; i < indata.Length; i++) 
			{
				if (indata[i] == 10)
					words++;
			}
			

			outdata = BitConverter.GetBytes(words);
			Console.WriteLine("Found {0} words", words);
			return outdata;
		}
		
		[Test]
    		public void testWC()
    		{
      		Worker w = new Worker("localhost");
			w.registerFunction("wc", wctest);
			w.workLoop();
			
			Client c = new Client("localhost");
			byte[] data = new ASCIIEncoding().GetBytes("zzz\nyyy\napple\nbaz\nfoo\nnarf\nquiddle\n");
			byte[] result = c.submitJob("wc", data);
		
			Assert.IsNotNull(result);
			
			int resultasint = BitConverter.ToInt32(result, 0);
			
			Assert.AreEqual(resultasint, 7);
			
			w.stopWorkLoop();
	    }
	
		[Test]
		public void testWordCountIPv6()
		{
		    Worker w = new Worker("::1");
			w.registerFunction("wc", wctest);
			w.workLoop();
			
			Client c = new Client("::1");
			byte[] data = new ASCIIEncoding().GetBytes("zzz\nyyy\napple\nbaz\nfoo\nnarf\nquiddle\n");
			byte[] result = c.submitJob("wc", data);
		
			Assert.IsNotNull(result);
			
			int resultasint = BitConverter.ToInt32(result, 0);
			
			Assert.AreEqual(resultasint, 7);
			
			w.stopWorkLoop();			
		}
	
	}
}