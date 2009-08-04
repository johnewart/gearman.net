
using System;

namespace Gearman
{


	public class RequestPacket : Packet
	{
		public RequestPacket ()
		{
			this.magic = "\0REQ".ToCharArray();
			this.size = 0; 
		}
		
		public RequestPacket(PacketType type) : this()
		{
			this.Type = type;
		}
		
		public RequestPacket(PacketType type, byte[] data) : this(type)
		{
			this.Data = data;
		}
		
		public RequestPacket(PacketType type, byte[] data, string jobhandle) 
		{
			// Set jobhandle and type first to parse data properly
			this.JobHandle = jobhandle;
			this.Type = type;
			this.Data = data; 
			
		}
	}
}
