using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Net.Sockets;
using System.Text; 
using log4net;

namespace Gearman
{

	public class Worker
	{

		public delegate byte[] taskdelegate(Packet jobPacket, Connection c); 

		private Connection c; 
		private Dictionary<string, taskdelegate> methodMap;
		private Thread t; 
		public static readonly ILog Log = LogManager.GetLogger(typeof(Worker));

		
		public Worker()
		{
			methodMap = new Dictionary<string, taskdelegate>();
		}
		
		public Worker (string host, int port) : this()
		{
			this.c = new Connection(host, port);
		}
		
		public Worker (string host) : this(host, 4730)
		{		}

		public void registerFunction(string taskname, taskdelegate function)
		{
			methodMap[taskname] = function;
			RequestPacket r = new RequestPacket();
			r.Type = PacketType.CAN_DO;
			r.setData(taskname);
			c.sendPacket(r);
		}
		                        
		public void workLoop()
		{
			t = new Thread(new ThreadStart(run));
			t.Start();
		}
		
		public void stopWorkLoop()
		{
			if (t != null)
			{
				t.Abort();
			}
		}
		
		
		public void run()
		{
			while(true)
			{
				checkForJob();
				Log.DebugFormat("Sleeping for 2 seconds");
				Thread.Sleep(2000);
			}		
		}

		
		private void checkForJob()
		{
			ASCIIEncoding encoder = new ASCIIEncoding(); 
			// Grab job from server (if available)
			Log.DebugFormat("Checking for job...");
			RequestPacket rp = new RequestPacket();
			rp.Type = PacketType.GRAB_JOB;
			
			c.sendPacket(rp);
			
			Packet response = c.getNextPacket();
			
			if (response.Type == PacketType.JOB_ASSIGN)
			{
				Log.DebugFormat("Assigned job: {0}", response.JobHandle);
				byte[] payload = response.Data;
				bool gotTask = false;
				string task = "";
				
				for(int i = 0; i < payload.Length; i++)
				{
					if (payload[i] == 0 || i == payload.Length - 1)
					{
						if(!gotTask)
						{
							Log.DebugFormat("Got zero byte and no task yet...");
							task = encoder.GetString(payload, 0, i);
							gotTask = true; 		
						} else {
							Log.DebugFormat("Task: {0}", task); 
							byte[] result      = methodMap[task](response, c);
							string resultText  = encoder.GetString(result);
							
							Log.DebugFormat("Result: " + resultText);
							
							RequestPacket workresult = new RequestPacket(PacketType.WORK_COMPLETE, result, response.JobHandle);
							c.sendPacket(workresult);
							
							workresult.Dump();
							
						}	
					}	
				}
			} else if (response.Type == PacketType.NO_JOB) {
				Log.DebugFormat("Nothing to do!");
			} 
		}
		
	}
}
