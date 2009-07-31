
using System;
using System.Net;
using System.Net.Sockets;
using System.Text; 

namespace Gearman
{
	public class Client
	{
		private TcpClient conn; 
		private string hostname; 
		private int port; 
		
		public Client (string host)
		{
			this.port = 4730;
			this.hostname = host;
			this.init();
		}
		
		public Client (string host, int port)
		{
			this.hostname = host; 
			this.port = port; 
			this.init();
		}
		
		public void submitJob(string callback, string data)
		{
			try {
				RequestPacket p = new RequestPacket();
				p.Type = PacketType.SUBMIT_JOB;
				p.setData(callback + "\0" + "12345\0" + data );
				NetworkStream stream = conn.GetStream();
				stream.Write(p.ToByteArray(),0, p.length());
				stream.Flush();   
     			
				Console.WriteLine("Sending data...");
				
	    		// Receive the response.
	    		do {
	    			// Buffer to store the response bytes.
	    			Byte[] response = new Byte[256];
	
	    			// String to store the response ASCII representation.
	    			String responseData = String.Empty;
	
	    			// Read the first batch of the TcpServer response bytes.
	   				Int32 bytes = stream.Read(response, 0, response.Length);
	    			responseData = System.Text.Encoding.ASCII.GetString(response, 0, bytes);
					Packet result = new Packet(response); 
	    			//Console.WriteLine("Received: {0}", responseData);

					if(result.Type == PacketType.WORK_COMPLETE)
					{
						Console.WriteLine("Job Handle: " + result.JobHandle);
						Console.WriteLine("Result: " + result.Data);
					} else {
						Console.WriteLine("Dumping result packet!");
						result.Dump();
					}
					
					
					
				} while (stream.DataAvailable);			
			} catch (Exception e) { 
				Console.WriteLine("Error writing job: {0}", e.ToString());
			}
		}
		
		private void init()
		{
			try { 
				conn = new TcpClient(hostname, port);
			} catch (Exception e) { 
				Console.WriteLine("Error establishing connection: " + e.ToString());
			}
		}
	}
}
