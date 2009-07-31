using System;
using Gearman; 
using System.Text; 

namespace GearmanDriver
{
	class MainClass
	{
		private static byte[] wc(byte[] indata)
		{
			int words = 0;
			byte[] outdata = new byte[1024];
			
			for(int i = 0; i < indata.Length; i++) 
			{
				if (indata[i] == 10)
					words++;
			}
			
			string result = String.Format("{0,4:D}", words);
			outdata = ASCIIEncoding.UTF8.GetBytes(result);

			
			return outdata;
		}
		
		public static void Main (string[] args)
		{
			//Client c = new Client("172.16.50.1"); 
			//c.submitJob("wc", "zzz\nyyy\napple\nbaz\nfoo\nnarf\nquiddle\n");
			Worker w = new Worker("172.16.50.1");
			w.registerFunction("wc", wc);
			w.workLoop();
		}
	}
}
