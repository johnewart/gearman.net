
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
	public abstract class Packet
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
		
		protected byte[] rawdata; 
		protected byte[] pHeader; 
		
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
			this.rawdata = new byte[this.size];
			Array.Copy(fromdata, 12, this.rawdata, 0, this.size);
		}
				
		public int parseString(int offset, ref string storage)
		{
			int pStart; 
			int pOff;
			pOff = pStart = offset; 
			for(; pOff < rawdata.Length && rawdata[pOff] != 0; pOff++);
			storage = new ASCIIEncoding().GetString(rawdata.Slice(pStart, pOff));
			// Return 1 past where we are...
			return pOff + 1;
		}
		
		/// <summary>
		/// The length of the data portion of the packet, including any job handle (if it exists)
		/// </summary>
		public int Length { 
			set { 
			}
			
			get { 
				return this.size + 12;
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
		/// Default ToByteArray function that returns a byte array that contains the packet header.
		/// Useful as a default function because some packets have no data beyond packet type as a
		/// simple message packet. This is called to serialize to a socket, or possibly even a file.
		/// </summary>
		/// <returns>
		/// A <see cref="System.Byte[]"/> array that represents the packet
		/// </returns>
		public virtual byte[] ToByteArray() 
		{ 
			return Header;
		}

		// TODO Implement a dirty flag?
		
		public abstract byte[] Header { get; }
		
    	/// <summary>
    		/// Convenience debug method to display the contents of a packet.
    		/// </summary>	
		public void Dump()
		{
			Console.WriteLine("Dumping {0} packet with {1} bytes of data in the body:", type.ToString("g"), this.size);
			
			try { 
				int count = 0;
				int idata; 
				byte[] line = new byte[16]; 
				
				ASCIIEncoding encoding = new ASCIIEncoding( );

				if(rawdata != null)
				{
					while(count < this.size)
		            {
						idata = rawdata[count];
						
						if ( (((count % 16) == 0) && count != 0) || (count == rawdata.Length - 1) )
						{
							string output = "";
							string text = encoding.GetString(line);
	
							string result = Regex.Replace(text, @"[^0-9a-zA-Z]", ".");
								
							for(int i = 0; i < rawdata.Length && i < 16; i+=2) 
							{		
								output += String.Format("{0:x2}{1:x2} ", line[i], line[i+1]);
							}
							
							Console.WriteLine("0x{0:x4}: {1} {2}", count-16, output, result);
							line = new byte[16];
						} 
						
						line[count % 16] = Convert.ToByte(idata);
						
						
						count++;
		            }
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
			return string.Format("{0} packet. Data: {1} bytes", type.ToString("g"), rawdata.Length);
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
