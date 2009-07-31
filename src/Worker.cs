using System;
using System.Collections.Generic;
using System.Threading;
using System.Net.Sockets;
using System.Text; 

namespace Gearman
{

	public class Worker
	{
		
		public delegate byte[] taskdelegate(byte[] indata); 
		
		private TcpClient conn; 
		private string hostname; 
		private int port;
		private Dictionary<string, taskdelegate> methodMap;
		
		
		public Worker (string host)
		{
			this.port = 4730;
			this.hostname = host;
			this.init();
		}
		
		public Worker (string host, int port)
		{
			this.hostname = host; 
			this.port = port; 
			this.init();
		}
		
		public void registerFunction(string taskname, taskdelegate function)
		{
			methodMap[taskname] = function;
			RequestPacket r = new RequestPacket();
			r.Type = PacketType.CAN_DO;
			r.setData(taskname);
			NetworkStream stream = conn.GetStream();
			stream.Write(r.ToByteArray(),0, r.length());
			stream.Flush();
		}
		                        
		public void init()
		{
			try { 
				methodMap = new Dictionary<string, taskdelegate>();
				conn = new TcpClient(hostname, port);
			} catch (Exception e) { 
				Console.WriteLine("Error initializing Worker: " + e.ToString());
			}
		}
		
		public void workLoop()
		{
			Thread t = new Thread(new ThreadStart(run));
			t.Start();
		}
		
		public void run()
		{
 
			while(true)
			{
				nextPacket();
				Console.WriteLine("Sleeping for 2 seconds");
				Thread.Sleep(2000);
			}
				
		}

		
		private void nextPacket()
		{
			byte[] buf = new byte[1];
			byte[] pdata = null, message = null;
			int totalBytes = 0; 
			int bytesRead;
			bool pktDone = false; 
			int poffset = 0; 
			int messagesize = -1; 
			
			ASCIIEncoding encoder = new ASCIIEncoding();
			NetworkStream stream = conn.GetStream(); 
			
			// Grab job from server (if available)
			Console.WriteLine("Checking for job...");
			RequestPacket rp = new RequestPacket();
			rp.Type = PacketType.GRAB_JOB;
			conn.GetStream().Write(rp.ToByteArray(), 0, rp.length());
			conn.GetStream().Flush();
			pktDone = false; 
			message = new byte[65536];
			messagesize = -1; 
			totalBytes = 0; 
			
 			while (!pktDone) 
			{
				try {
					bytesRead = stream.Read(buf, 0, 1);
					message[totalBytes++] = buf[0];
				
					if(totalBytes == 12) { 
						// Check byte count
						byte[] sizebytes = message.Slice(8,12); 
						
						if(BitConverter.IsLittleEndian)
							Array.Reverse(sizebytes);
						
						messagesize = BitConverter.ToInt32(sizebytes, 0);
						Console.WriteLine("Packet is another {0} bytes", messagesize);
					}
					
					if(messagesize != -1 && messagesize == (totalBytes - 12))
					{
						Console.WriteLine("Done parsing message");
						pktDone = true;
						pdata = new byte[totalBytes+1];
						Array.Copy(message, pdata, totalBytes);
					} 

				} catch (Exception e) { 
					Console.WriteLine("Exception reading data: {0}", e.ToString());
				}
			} 
			
			Console.WriteLine("Finished processing packet!");
			if(pdata != null) 
			{
				Packet p = new Packet(pdata);
				p.Dump();
			
			
				if (p.Type == PacketType.JOB_ASSIGN)
				{
					Console.WriteLine("Assigned job: {0}", p.JobHandle);
					byte[] payload = p.RawData;
					bool gotTask = false;
					string task = "";
					int taskoffset = 0; 
					
					for(int i = 0; i < payload.Length; i++)
					{
						if (payload[i] == 0)
						{
							if(!gotTask)
							{
								Console.WriteLine("Got zero byte and no task yet...");
								task = encoder.GetString(payload, 0, i);
								taskoffset = i;
								gotTask = true; 		
							} else {
								Console.WriteLine("Task: {0}", task); 
								byte[] result    = methodMap[task](payload.Slice(taskoffset, i));
								byte[] jobhandle = ASCIIEncoding.UTF8.GetBytes(p.JobHandle);
								
								byte[] d = new byte[jobhandle.Length + result.Length + 1];
								
								Array.Copy(jobhandle, d, jobhandle.Length);
								Array.Copy(result, 0, d, jobhandle.Length + 1, result.Length);
							
								RequestPacket workresult = new RequestPacket();
								workresult.Type = PacketType.WORK_COMPLETE; 
								workresult.setJobData(d);
								
								workresult.Dump();
								
								conn.GetStream().Write(workresult.ToByteArray(), 0, workresult.length());
								conn.GetStream().Flush();
							}
							
						}
						
						
					}
					
				}
				
				if (p.Type == PacketType.NO_JOB)
				{
					Console.WriteLine("Nothing to do!");
				} 
			}
		}
		
	}
}
