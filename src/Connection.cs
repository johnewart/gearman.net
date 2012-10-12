
using System;
using System.Net;
using System.Net.Sockets;
using System.Text; 
using System.Text.RegularExpressions;
using Gearman.Packets.Client; 
using Gearman.Packets.Worker; 

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
		/// Initializes a new instance of the <see cref="Gearman.Connection"/> class.
		/// </summary>
		/// <param name='s'>
		/// S.
		/// </param> 
		public Connection(Socket s) 
		{
			conn = s;
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
			int messagetype = -1; 
			
			// Initialize to 12 bytes (header only), and resize later as needed
			byte[] header = new byte[12];
			byte[] packet; 
			
			messagesize = -1; 
			
			try {
				// Read the first 12 bytes (header)
				conn.Receive(header, 12, 0);
						
				// Check byte count
				byte[] sizebytes = header.Slice(8,12); 
				byte[] typebytes = header.Slice(4,8);
				
				if(BitConverter.IsLittleEndian)
				{
					Array.Reverse(sizebytes);
					Array.Reverse(typebytes);
				}		
				
				messagesize = BitConverter.ToInt32(sizebytes, 0);
				messagetype = BitConverter.ToInt32(typebytes, 0);
				
				if (messagesize > 0) 
				{					
					// Grow packet buffer to fit data
					packet = new byte[12 + messagesize];
					Array.Copy(header, packet, header.Length);
					
					// Receive the remainder of the message 
					conn.Receive(packet, 12, messagesize, 0); 
				} else {
					packet = header; 
				}
										
				switch((PacketType)messagetype)
				{
				case PacketType.JOB_CREATED:
					return new JobCreated(packet);
					
				case PacketType.WORK_DATA:
					return new WorkData(packet);
					
				case PacketType.WORK_WARNING:
					return new WorkWarning(packet);
					
				case PacketType.WORK_STATUS:
					return new WorkStatus(packet);
					
				case PacketType.WORK_COMPLETE:
					return new WorkComplete(packet);
					
				case PacketType.WORK_FAIL:
					return new WorkFail(packet);
					
				case PacketType.WORK_EXCEPTION:
					return new WorkException(packet);
					
				case PacketType.STATUS_RES:
					return new StatusRes(packet);
					
				case PacketType.OPTION_RES:
					// TODO Implement option response
					break;
				
				/* Client and worker response packets */
				case PacketType.ECHO_RES:
					// TODO Implement the echo response
					break;
				case PacketType.ERROR:
					// TODO Implement the error packet
					break;
		
				/* Worker response packets */
				case PacketType.NOOP:
					return new NoOp();
					
				case PacketType.NO_JOB:
					return new NoJob();
				
				case PacketType.JOB_ASSIGN:
					return new JobAssign(packet);
					
				case PacketType.JOB_ASSIGN_UNIQ:
					return new JobAssignUniq(packet);

				/* Worker request packets */
				case PacketType.CAN_DO:
					return new CanDo(packet); 
				
				case PacketType.SET_CLIENT_ID:
					return new SetClientID(packet); 

				case PacketType.GRAB_JOB:
					return new GrabJob();
 
                case PacketType.PRE_SLEEP:
                    return new PreSleep();

				/* Client request packets */
				case PacketType.SUBMIT_JOB:
					return new SubmitJob(packet); 

				case PacketType.SUBMIT_JOB_BG:
					return new SubmitJob(packet); 

                case PacketType.SUBMIT_JOB_EPOCH:
                    return new SubmitJob(packet); 

				default: 
					Console.WriteLine("Unhandled type: {0}", (PacketType)messagetype); 
					return null;
				}
				
			} catch (Exception e) { 
				Console.WriteLine("Exception reading data: {0}", e.ToString());
				return null;
			} 
			
			return null;
		}
	}
}
