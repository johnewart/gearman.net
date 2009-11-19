
using System;
using System.IO; 
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Gearman
{
	/// <summary>
	/// A class representing the basic type of packet (both request and response packages use this). However, the client 
	/// and worker only make requests (even when handing back data), so there isn't really much of a need to lead a packet's 
	/// preamble with 'RES'.
	/// </summary>
	public class Packet
	{
		/// <summary>
		/// Header magic (i.e REQ/RES data)
		/// </summary>
		protected char[] magic;
		
		/// <summary>
		/// The type of packet that this is, as a <see cref="PacketType"/>
		/// </summary>
		protected PacketType type; 
		
		/// <summary>
		/// <see cref="int"/> size of the packet
		/// </summary>
		protected int size;
		
		/// <summary>
		/// <see cref="System.byte[]"/> array representing the raw data of the packet
		/// </summary>
		protected byte[] data; 
		
		/// <summary>
		/// A <see cref="System.string"/> containing the unique job handle
		/// </summary>
		protected string jobhandle; 
		
		/// <summary>
		/// Default constructor (inline, does nothing)
		/// </summary>
		public Packet() { }
		
		/// <summary>
		/// Constructor that takes a byte array to form a packet. The byte array 
		/// typically comes from the Connection class' getNextPacket() method but 
		/// can also be used anywhere to generate a raw GM packet.
		/// </summary>
		/// <param name="fromdata">
		/// A <see cref="System.Byte[]"/> array of data, where the first 12 bytes
		/// are the header, containing the null-terminated 'REQ' / 'RES' string, 
		/// 4 bytes representing the "type" of packet (see protocol.txt), and then
		/// 4 bytes representing the size of the packet.
		/// </param>
		public Packet(byte[] fromdata)
		{
			byte[] typebytes = fromdata.Slice(4, 8);
			byte[] sizebytes = fromdata.Slice(8, 12);
			
			if(BitConverter.IsLittleEndian)
			{
				Array.Reverse(typebytes);
				Array.Reverse(sizebytes);
			}
			
			this.type = (PacketType)BitConverter.ToInt32(typebytes, 0);
			this.size = BitConverter.ToInt32(sizebytes, 0);		
			this.Data = fromdata.Slice(12, fromdata.Length);

		}
		
		/// <summary>
		/// The length of the data portion of the packet, including any job handle (if it exists)
		/// </summary>
		public int DataLength { 
			set { 
			}
			
			get { 
				int result = this.size; 
				
				// If there's a job handle and we need to include it...
				if (jobhandle != null && jobhandle != "") 
				{
					result += jobhandle.Length + 1; 
				}
				
				return result; 
			}
		}
		
		/// <summary>
		/// The length of the entire packet, header included.
		/// </summary>
		public int Length { 
			set { 
				this.size = value - 12; 
			} 
			
			get { 
				// Include header size...
				return this.DataLength + 12; 
			}
		}
		
		/// <summary>
		/// An integer from the PacketType enum representing the type of packet this is
		/// </summary>
		public PacketType Type {
			set { 
				Console.WriteLine("Setting type!");
				this.type = (PacketType)value;
			}
			
			get { 
				return this.type;
			}
		}
		
		/// <summary>
		/// A <see cref="System.byte[]"/> array of the data stored in the packet. 
		/// </summary>
		public byte[] Data {
			set {			
				Console.WriteLine("Setting data");
				switch(this.type) 
				{ 
					case PacketType.JOB_CREATED:
					case PacketType.JOB_ASSIGN:
					case PacketType.WORK_COMPLETE:
					case PacketType.WORK_STATUS:
					case PacketType.STATUS_RES:
						bool sawhandle = false; 
						int i = 0;
						int handleoffset = 0; 
					
						if (this.JobHandle == null || this.JobHandle == "")
						{
							Console.WriteLine("No job handle, yet...");
							for(i = 0; i < value.Length && !sawhandle; i++)
							{
								if(value[i] == 0)
								{ 
									this.jobhandle = new ASCIIEncoding().GetString(value.Slice(0, i));
									handleoffset = i+1; 
									sawhandle = true; 
								}
							}
						} else { 
							i = value.Length;
						}	
								
						if (value[value.Length-1] != 0)
						{	
							// Pad the end with a zero-byte if there isn't one in the data
							this.data = new byte[value.Length + 1];
							Array.Copy(value, handleoffset, data, 0, value.Length - handleoffset);
							this.size = (i  - handleoffset) + 1;

						} else {
							this.data = value.Slice(handleoffset, value.Length);
							this.size = i - handleoffset;
						}

					
						break;
					default:	
						this.data = value;
						this.size = value.Length;
						break;
				}
			}
			
			get { 
				return data;
			}
		}
		
		/// <summary>
		/// A string representing a packet's job handle
		/// </summary>
		public string JobHandle { 
			set {
				Console.WriteLine("Setting job handle to {0}", value);
				this.jobhandle = value;
			}
			
			get { 
				return jobhandle;
			}
		}
		
		/// <summary>
		/// Convert the entire packet into a byte array. Typical usage is for sending the packet across the wire
		/// but could also be used for serializing data to a file if needed. Endian-ness is taken care of, making 
		/// this acceptable for network transmission.
		/// </summary>
		/// <returns>
		/// A <see cref="System.Byte[]"/>
		/// </returns>
		unsafe public byte[] ToByteArray()
		{				
			byte[] output = new byte[this.Length];

			byte[] typebytes = BitConverter.GetBytes((int)this.Type);
			byte[] sizebytes;
			
			if (this.data != null) 
			{
				sizebytes  = BitConverter.GetBytes((int)this.DataLength);
			} else { 
				sizebytes = BitConverter.GetBytes(0);
			}
			
			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(typebytes);
				Array.Reverse(sizebytes);
			}
			
			// HACK: Sooooo ugly... replace with the magic[] data
			
			output[0] = (byte)'\0';
			output[1] = (byte)'R';
			output[2] = (byte)'E';
			output[3] = (byte)'Q';
			
			output[4] = typebytes[0];
			output[5] = typebytes[1];
			output[6] = typebytes[2];
			output[7] = typebytes[3];
			
			output[8] = sizebytes[0];
			output[9] = sizebytes[1];
			output[10] = sizebytes[2];
			output[11] = sizebytes[3];
			
			int offset = 12; 
			
			if (jobhandle != null && jobhandle != "") 
			{
				byte[] jhbytes = new ASCIIEncoding().GetBytes(jobhandle);
				Array.Copy(jhbytes, 0, output, 12, jhbytes.Length);
				// Skip one byte, because jobhandle is null-byte terminated
				offset += jhbytes.Length + 1;
			}
			
			if (data != null) 
			{
				Array.Copy(data, 0, output, offset, data.Length);
			}
			
			return output;
    		}
		
		/// <summary>
		/// Convenience method to set the packet data using a string. This internally just converts the string to 
		/// ASCII data and then converts that to a byte array and updates the size as needed.
		/// </summary>
		/// <param name="_data">
		/// A <see cref="String"/> that contains the entire packet. Something such as 'callback + "\0" + jobid + "\0" + data'
		/// </param>
		/// <example>
		/// Typically this would be used with a RequestPacket for convenience, for example:
		/// <code>
		/// RequestPacket p = new RequestPacket(pt);
		/// string jobid = System.Guid.NewGuid().ToString();
		/// p.setData(callback + "\0" + jobid + "\0" + data );
		/// </code>
		/// </example>
		public void setData(String _data)
		{
			System.Text.ASCIIEncoding encoding=new System.Text.ASCIIEncoding();
			data = encoding.GetBytes(_data);
			this.size = _data.Length;
		}
		
    		/// <summary>
    		/// Convenience debug method to display the contents of a packet.
    		/// </summary>	
		public void Dump()
		{
			Console.WriteLine("Dumping {0} packet with {1} bytes of data, total size: {2}!", type.ToString("g"), data.Length, this.DataLength);
			
			try { 
				int count = 0;
				int idata; 
				byte[] line = new byte[16]; 
				
				ASCIIEncoding encoding = new ASCIIEncoding( );

				while(count < data.Length)
	            {
					idata = data[count];
					
					if ( (((count % 16) == 0) && count != 0) || (count == data.Length - 1) )
					{
						string output = "";
						string text = encoding.GetString(line);

						string result = Regex.Replace(text, @"[^0-9a-zA-Z]", ".");
							
						for(int i = 0; i < data.Length && i < 16; i+=2) 
						{		
							output += String.Format("{0:x2}{1:x2} ", line[i], line[i+1]);
						}
						
						Console.WriteLine("0x{0:x4}: {1} {2}", count-16, output, result);
						line = new byte[16];
					} 
					
					line[count % 16] = Convert.ToByte(idata);
					
					
					count++;
	            }
				
				Console.WriteLine("");
			} catch ( Exception e) {
				Console.WriteLine("Exception writing data: {0} ", e.ToString());
			}
		}
		
		
		//TODO: Look at making this a replacement for Dump?
		
		/// <summary>
		/// Convenience method to display a little information about the packet as a string. 
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/>
		/// </returns>
		public override string ToString()
		{
			return string.Format("{0} packet. Data: {1} bytes, length: {2} bytes", type.ToString("g"), data.Length, this.DataLength);
		}
	}
}
	
public static class Extensions
{
    /// <summary>
    /// Get the array slice between the two indexes, similar to Python or other languages' array slices.
    /// Index is inclusive for start index, exclusive for end index. (i.e 0-2 is really elements 0,1)
    /// Credit for this goes to http://dotnetperls.com/array-slice
    /// </summary>
    public static T[] Slice<T>(this T[] source, int start, int end)
    {
        // Handles negative ends
        if (end < 0)
        {
            end = source.Length - start - end - 1;
        }
        int len = end - start;

        // Return new array
        T[] res = new T[len];
        for (int i = 0; i < len; i++)
        {
            res[i] = source[i + start];
        }
        return res;
    }
}
