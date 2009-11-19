
using System;

namespace Gearman
{
	// TODO: Do we even need this class really for worker/clients?
	
	/// <summary>
	/// Specialized type of packet to represent requests to the manager
	/// </summary>
	public class RequestPacket : Packet
	{
		/// <summary>
		/// Default constructor, sets the Packet magic to 'REQ'
		/// </summary>
		public RequestPacket ()
		{
			this.magic = "\0REQ".ToCharArray();
			this.size = 0; 
		}
		
		/// <summary>
		/// Constructor that takes a PacketType enum to set the type of packet being sent
		/// </summary>
		/// <param name="type">
		/// A <see cref="PacketType"/> representing the type of packet this is
		/// </param>
		public RequestPacket(PacketType type) : this()
		{
			this.Type = type;
		}
		
		/// <summary>
		/// Constructor that takes a PacketType value and a byte array of data to set
		/// </summary>
		/// <param name="type">
		/// A <see cref="PacketType"/> representing the type of packet this is
		/// </param>
		/// <param name="data">
		/// A <see cref="System.Byte[]"/> array containing the raw packet data
		/// </param>
		public RequestPacket(PacketType type, byte[] data) : this(type)
		{
			this.Data = data;
		}
		
		/// <summary>
		/// Constructor that takes PacketType, byte array of data, and a job handle to be used
		/// </summary>
		/// <param name="type">
		/// A <see cref="PacketType"/> representing the type of packet this is
		/// </param>
		/// <param name="data">
		/// A <see cref="System.Byte[]"/> array containing the raw packet data
		/// </param>
		/// <param name="jobhandle">
		/// A <see cref="System.String"/> containing the job handle when needed
		/// </param>
		public RequestPacket(PacketType type, byte[] data, string jobhandle) 
		{
			// Set jobhandle and type first to parse data properly
			this.JobHandle = jobhandle;
			this.Type = type;
			this.Data = data; 
			
		}
	}
}
