using System;

namespace Gearman
{
	public class RequestPacket :  Packet
	{
		public RequestPacket ()
		{
		}

		public RequestPacket(byte[] fromdata) : base(fromdata)
		{
		}

		/// <summary>
		/// Calculate the header as a byte array. This is the same across every type of packet, so it's
		/// in here.
		/// </summary>
		/// <returns>
		/// A <see cref="System.Byte[]"/> 
		/// </returns>
		public override byte[] Header
		{				
			get {
				if (pHeader == null)
				{
					pHeader = new byte[12];
					
					byte[] typebytes = BitConverter.GetBytes((int)this.Type);
					byte[] sizebytes;
					
					sizebytes  = BitConverter.GetBytes((int)this.size);
					
					if (BitConverter.IsLittleEndian)
					{
						Array.Reverse(typebytes);
						Array.Reverse(sizebytes);
					}
					
					// HACK: Sooooo ugly... replace with the magic[] data
					
					pHeader[0] = (byte)'\0';
					pHeader[1] = (byte)'R';
					pHeader[2] = (byte)'E';
					pHeader[3] = (byte)'Q';
					
					pHeader[4] = typebytes[0];
					pHeader[5] = typebytes[1];
					pHeader[6] = typebytes[2];
					pHeader[7] = typebytes[3];
					
					pHeader[8] = sizebytes[0];
					pHeader[9] = sizebytes[1];
					pHeader[10] = sizebytes[2];
					pHeader[11] = sizebytes[3];		
				}
				
				return pHeader;
			}
			
		}	
	}
}

