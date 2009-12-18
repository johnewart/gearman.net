using System;
using Gearman;
using Gearman.Packets.Client;
using Gearman.Packets.Worker;

namespace WorkerApp
{
	class WorkerApp
	{
		private static void wctest(Job j)
		{
			byte[] outdata = new byte[1024];
			byte[] indata = j.data;
			Console.WriteLine("wc called");
			
			int words = 0; 
			
			for(int i = 0; i < indata.Length; i++) 
			{
				if (indata[i] == 10)
					words++;
			}
			
			outdata = BitConverter.GetBytes(words);
			Console.WriteLine("Found {0} words", words);
			j.sendResults(outdata);
		}
		
		
		public static void Main (string[] args)
		{
			Console.WriteLine("Creating a worker object");
			Worker w = new Worker("localhost");
			w.registerFunction("wc", wctest);
			w.workLoop();
		}
	}
}
