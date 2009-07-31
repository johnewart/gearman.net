using System;
using Gearman; 

namespace GearmanDriver
{
	class MainClass
	{
		private static byte[] wc(byte[] indata)
		{
			byte[] outdata = new byte[1024];
			
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
