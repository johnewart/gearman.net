
using System;
using System.Net.Sockets;
using System.Text; 

namespace Gearman
{
	public class Connection
	{
		private TcpClient conn; 
		
		public Connection()
		{	}
		
		public Connection(string hostname, int port) 
		{
			try { 
				conn = new TcpClient(hostname, port);
			} catch (Exception e) { 
				Console.WriteLine("Error initializing connection to job server: " + e.ToString());
			}
		}
	
		public void sendPacket(Packet p)
		{
			try { 
				NetworkStream stream = conn.GetStream();
				stream.Write(p.ToByteArray(),0, p.Length);
			} catch (Exception e) {
				Console.WriteLine("Unable to send packet: {0}", e.ToString());
			}
		}
		
		public Packet getNextPacket()
		{
			int totalBytes = 0; 
			bool pktDone = false; 
			int messagesize = -1; 
			
			NetworkStream stream = conn.GetStream(); 
			
			pktDone = false; 
			
			// Initialize to 12 bytes (header only), and resize later as needed
			byte[] packet = new byte[12];
			messagesize = -1; 
			totalBytes = 0; 
			
 			while (!pktDone) 
			{
				try {
					stream.Read(packet, totalBytes++, 1);
					
					// Header is comeplete, check for important data
					if(totalBytes == 12) { 
						// Check byte count
						byte[] sizebytes = packet.Slice(8,12); 
						
						if(BitConverter.IsLittleEndian)
							Array.Reverse(sizebytes);
						
						messagesize = BitConverter.ToInt32(sizebytes, 0);
						
						if (messagesize > 0) 
						{
							Console.WriteLine("Packet is another {0} bytes", messagesize);
							byte[] tmp = packet.Slice(0,12);
							
							// Grow packet buffer to fit data
							packet = new byte[messagesize + 13];
							Array.Copy(tmp, packet, 12);
						}
					}
					
					if(messagesize != -1 && messagesize == (totalBytes - 12))
					{
						Console.WriteLine("Done parsing message");
						pktDone = true;
					} 

				} catch (Exception e) { 
					Console.WriteLine("Exception reading data: {0}", e.ToString());
				}
			}
			
			Console.WriteLine("Finished processing packet!");
			
			if(packet != null) 
			{
				Packet p = new Packet(packet);
				p.Dump();
				return p; 
			} else { 
				// TODO: Throw exception here instead
				return null; 
			}
		}
	}
}
