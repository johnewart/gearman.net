using System;
using System.Text; 
using Gearman;
using Gearman.Packets.Client; 
using Gearman.Packets.Worker; 

namespace GearmanRunner
{
	class MainClass
	{
		public static void Main (string[] args)
		{ 			
			Client c = new Client("localhost");
			byte[] data = new ASCIIEncoding().GetBytes("zzz\nyyy\napple\nbaz\nfoo\nnarf\nquiddle\n");
			byte[] result = c.submitJob("wc", data);
			
			string response = new ASCIIEncoding().GetString(result);
			Console.WriteLine("Result: {0}", response);		
			//w.stopWorkLoop();
		}
	}
}
