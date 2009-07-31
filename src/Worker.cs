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
            ASCIIEncoding encoder = new ASCIIEncoding();
			NetworkStream stream = conn.GetStream(); 
			byte[] buf = new byte[1];
			byte[] pdata;
			int totalBytes = 0; 
			int bytesRead;
			bool done = false; 
			int poffset = 0; 
			int messagesize = 0; 
			RequestPacket rp; 
		
			while(true)
			{
				// Grab job from server (if available)
				Console.WriteLine("Checking for job...");
				rp = new RequestPacket();
				rp.Type = PacketType.GRAB_JOB;
				conn.GetStream().Write(rp.ToByteArray(), 0, rp.length());
				conn.GetStream().Flush();
				
				pdata = new byte[1024];
				
     			while (!done) 
				{
					bytesRead = stream.Read(buf, 0, 1);
					pdata[totalBytes++] = buf[0];
					Console.Write(encoder.GetString(buf, 0, 1));
					
					if(totalBytes == 12) { 
						// Check byte count
						byte[] sizebytes = pdata.Slice(8,12); 
						
						if(BitConverter.IsLittleEndian)
							Array.Reverse(sizebytes);
						
						messagesize = BitConverter.ToInt32(sizebytes, 0);
						Console.WriteLine("Packet is another {0} bytes", messagesize);
					}
					
					if(messagesize == (totalBytes - 12))
					{
						Console.WriteLine();
						done = true;
					} 
				}
				
				Console.WriteLine("Finished processing packet!");
				Packet p = new Packet(pdata);
				p.Dump();
				totalBytes = 0;

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
								methodMap[task](payload.Slice(taskoffset, i));
							}
							
						}
						
						
					}
					
				}
				
				if (p.Type == PacketType.NO_JOB)
				{
					Console.WriteLine("Nothing to do!");
				} 
				
				Console.WriteLine("Sleeping for two seconds");
				Thread.Sleep(2000);
				done = false;
			}
		}
	}
}
