
using System;
using System.Net;
using System.Net.Sockets;
using System.Text; 
using System.Text.RegularExpressions;

namespace Gearman
{	
	/// <summary>
	/// Connection is a class that wraps a TCP client connection, and the connection data (hostname, port)
	/// and adds some methods for reading/writing GM packets between the client/worker and the manager. 
	/// Methods are provided to easily read and write Packet structures for ease of use.
	/// </summary>
	public class Connection
	{
		private Socket conn; 
		private string hostname;
		private int port;
		private IPAddress ipa;
		private IPEndPoint endpoint;
		
		/// <summary>
		/// Default constructor
		/// </summary>
		public Connection()
		{	}
		
		/// <summary>
		/// Constructor connecting to a specific host / port as provided
		/// </summary>
		/// <param name="hostname">
		/// A <see cref="System.String"/> containing the hostname to connect to
		/// </param>
		/// <param name="port">
		/// A <see cref="System.Int32"/> containing the port to connect on
		/// </param>
		public Connection(string hostname, int port) 
		{	
			this.hostname = hostname; 
			this.port = port; 
				
			try {
				ipa = IPAddress.Parse(hostname);
			} catch (Exception) {
				// Connect to the first DNS entry
				ipa = Dns.GetHostAddresses(hostname)[0];	
			}
			
			
			endpoint = new IPEndPoint(ipa, port);
		
			if(ipa.AddressFamily.Equals(AddressFamily.InterNetworkV6))
			{
				//IPv6 hostname
				conn = new Socket(
    					AddressFamily.InterNetworkV6,
    					SocketType.Stream,
    					ProtocolType.Tcp);
			} else {
				// IPv4 IP or hostname
				conn = new Socket(
				    AddressFamily.InterNetwork, 
				    SocketType.Stream,
				    ProtocolType.Tcp);
			}		
			
			try { 
				conn.Connect(endpoint);
			} catch (Exception e) { 
				Console.WriteLine("Error initializing connection to job server: " + e.ToString());
			}

		}
	
		/// <summary>
		/// Serialize a <see cref="Packet"/> to the network stream (just calls ToByteArray())
		/// </summary>
		/// <param name="p">
		/// A <see cref="Packet"/>
		/// </param>
		public void sendPacket(Packet p)
		{
			try { 
				conn.Send(p.ToByteArray());
			} catch (Exception e) {
				Console.WriteLine("Unable to send packet: {0}", e.ToString());
			}
		}
		
		/// <summary>
		/// Convenience method to display the connection as a String
		/// </summary>
		/// <returns>
		/// A <see cref="String"/> formatted as hostname:port
		/// </returns>
		public override String ToString()
		{
			return String.Format("{0}:{1}", this.hostname, this.port);
		}
		
		/// <summary>
		/// This method fetches the next complete response packet from the connection
		/// to the job manager. Each response packet has a 12 byte header which includes
		/// a size of the packet, the type, and the data being returned. 
		/// </summary>
		/// <returns>
		/// A <see cref="Packet"/> that is the next fully formed packet from the manager
		/// </returns>
		public Packet getNextPacket()
		{
			int messagesize = -1; 
				
			// Initialize to 12 bytes (header only), and resize later as needed
			byte[] packet = new byte[12];
			messagesize = -1; 
			
			try {
				// Read the first 12 bytes (header)
				conn.Receive(packet, 12, 0);
						
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
					
					// Receive the remainder of the message *after* the header
					conn.Receive(packet,12,messagesize,0); 
				}
			
			} catch (Exception e) { 
				Console.WriteLine("Exception reading data: {0}", e.ToString());
			}
		
			
			Console.WriteLine("Finished processing packet!");
			
			if(packet != null) 
			{
				Packet p = new Packet(packet);
				// TODO: remove this or change it to debug mode only
				p.Dump();
				return p; 
			} else { 
				// TODO: Throw exception here instead
				return null; 
			}
		}
	}
}
