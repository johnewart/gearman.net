
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
	}
}
