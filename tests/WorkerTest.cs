using System;
using NUnit.Framework;

namespace Gearman
{

  	[TestFixture]
  	public class WorkerTest
  	{
		private static byte[] wctest(byte[] indata)
		{
			int words = 0;
			byte[] outdata = new byte[1024];
			
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
			byte[] result = c.submitJob("wc", "zzz\nyyy\napple\nbaz\nfoo\nnarf\nquiddle\n");
		
			Assert.IsNotNull(result);
			
			int resultasint = BitConverter.ToInt32(result, 0);
			
			Assert.AreEqual(resultasint, 7);
			
			w.stopWorkLoop();
	    }
	}
}