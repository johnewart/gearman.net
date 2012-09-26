using System;
using System.Data;
using System.Collections;
using System.Collections.Generic; 
using System.IO; 
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Mail; 
using System.Net.Security; 
using System.Security.Authentication; 

using Gearman; 
using Gearman.Packets.Client; 
using Gearman.Packets.Worker; 

namespace GearmanServer {
	
	public class Daemon {

		System.Net.Sockets.TcpListener TCPServer; 

		public Daemon() { 
			

			GearmanServer.Log.Info("Server listening on port 4730");
			
			TCPServer = new System.Net.Sockets.TcpListener(4730); 

			while(true) { 
				TCPServer.Start(); 

				if(TCPServer.Pending()) { 
					try {
						new ConnectionHandler(TCPServer.AcceptTcpClient()); 
					} catch (Exception e) {
						GearmanServer.Log.Error("Exception waiting for connection: ", e);
					}
				} 

				System.Threading.Thread.Sleep(1);
			} 
		} 
		
	
	} // End of class SocketDaemon 
	
	
	class ConnectionHandler
	{
		private TcpClient client; 
		private Gearman.Connection conn; 
		private NetworkStream stream; 

		public ConnectionHandler(TcpClient c)
		{
			
		
			try {
				client = c;
				conn = new Gearman.Connection(c.Client);

				IPAddress remoteIP = ((IPEndPoint)c.Client.RemoteEndPoint).Address;
				GearmanServer.Log.Info(String.Format("Connection made by {0}", remoteIP)); 
			
				Thread t = new Thread(new ThreadStart(run)); 
				t.Start(); 
				
			} catch (Exception e) {
				GearmanServer.Log.Error("Problem getting remote IP: ", e);
			}		   		
			
		}
		
		
		private void run()
		{
			Packet p;
			int jobid = 1; 
			bool okay = true; 
			while(okay)
			{
				p = conn.getNextPacket();
				if (p != null) 
				{
					p.Dump();
					// Immediately store background jobs and move on
					if (p.Type == PacketType.SUBMIT_JOB_BG)
					{
						GearmanServer.Log.Info ("Background job was submitted!");
						conn.sendPacket(new JobCreated(String.Format("FOONARF:{0}", jobid)));
						jobid++; 
					}

					if (p.Type == PacketType.SUBMIT_JOB)
					{
					}
				} else { 
					GearmanServer.Log.Info ("No packet!");
					okay = false;
				}
			}

			GearmanServer.Log.Info("ConnectionHandler terminated.");						
		}

	}//end of class ConnectionHandler 
	
}
