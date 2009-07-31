
using System;
using System.Net;
using System.Net.Sockets;
using Gearman.Common; 

namespace Gearman.Client
{
	public class ClientConnection
	{
		private TcpClient conn; 
		
		public ClientConnection ()
		{
			try { 
				conn = new TcpClient("localhost", 4730);
				
			} catch (Exception e) { 
				Console.WriteLine("Error establishing connection: " + e.ToString());
			}
		}
		
		public void submitJob(string callback, string data)
		{
			RequestPacket p = new RequestPacket();
			p.setType((int)RequestPacketTypes.SUBMIT_JOB);
			p.setData(callback + "\0" + "12345\0" + data );
			NetworkStream foo = conn.GetStream();
			foo.Write(p.ToByteArray(),0, p.length());
		}
	}
}
