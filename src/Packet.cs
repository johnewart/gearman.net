
using System;
using System.IO; 
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Gearman
{
	public class Packet
	{
		protected char[] magic;
		protected PacketType type; 
		protected int size;
		protected MemoryStream data; 
		protected string jobhandle; 
		
		public Packet() { }
		
		public Packet(byte[] fromdata)
		{
			byte[] typebytes = fromdata.Slice(4, 8);
			byte[] sizebytes = fromdata.Slice(8, 12);
			byte[] databytes = fromdata.Slice(12, fromdata.Length);
			
			if(BitConverter.IsLittleEndian)
			{
				Array.Reverse(typebytes);
				Array.Reverse(sizebytes);
			}
			
			this.type = (PacketType)BitConverter.ToInt32(typebytes, 0);
			this.size = BitConverter.ToInt32(sizebytes, 0);
			
			data = new MemoryStream(this.size);
			bool haveJobHandle = false; 
			jobhandle = "";
			
			foreach(byte b in databytes)
			{
				if(b != 0)
				{
					if(!haveJobHandle)
					{
						jobhandle += (char)b;
					} else { 
						data.WriteByte(b);	
					}
				} else { 
					if (!haveJobHandle) { haveJobHandle = true; } else { data.WriteByte(b); }
				}
			}
		}
		
		
		public PacketType Type {
			set { 
				this.type = (PacketType)value;
			} 
			
			get { 
				return this.type;
			}
		}
		
		public byte[] RawData {
			set {
			}
			
			get { 
				
				if ( data != null ) 
				{
					int count = 0; 
					
					byte[] output = new byte[data.Length];
					
					data.Seek(0, SeekOrigin.Begin);
				
					try { 
						while(count < data.Length)
			            {
			                output[count++] = Convert.ToByte(data.ReadByte());
			            }
					} catch ( Exception e) {
						Console.WriteLine("Exception writing data: {0} ", e.ToString());
					}

					return output;

				} else { 
					return null; 
				}
			
			}
		}
		
		public string Data { 
			set { 
			}
		
			get { 
				int idata = 0; 
				int count = 0; 
				string output = "";
				data.Seek(0, SeekOrigin.Begin);
				try { 
					while(count < data.Length)
		            {
						idata = data.ReadByte();
						
		                output += (char)Convert.ToByte(idata);
						count++;
		            }
					
					return output;
				
				} catch (Exception e) { 
					Console.WriteLine("Unable to parse data {0}: {1}", idata, e.ToString());
					return "";
				}
				
			}
		}
		
		public string JobHandle { 
			get { 
				return jobhandle;
			}
		}
		
		unsafe public byte[] ToByteArray()
		{	
			int count = 12;
			
			byte[] output = new byte[this.length()];

			byte[] typebytes = BitConverter.GetBytes((int)this.Type);
			byte[] sizebytes;
			
			if (this.data != null) 
			{
				sizebytes  = BitConverter.GetBytes((int)this.data.Length);
			} else { 
				sizebytes = BitConverter.GetBytes(0);
			}
			
			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(typebytes);
				Array.Reverse(sizebytes);
			}
			
			// TODO: Sooooo ugly...
			
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
			
			if ( data != null ) 
			{
				data.Seek(0, SeekOrigin.Begin);
			
				try { 
					while(count < this.length())
		            {
		                output[count++] = Convert.ToByte(data.ReadByte());
		            }
				} catch ( Exception e) {
					Console.WriteLine("Exception writing data: {0} ", e.ToString());
				}
			}
			
			return output;
    	}
		
		public void setData(String _data)
		{
			data = new MemoryStream(_data.Length);
			System.Text.ASCIIEncoding encoding=new System.Text.ASCIIEncoding();
		
			for(int i = 0; i < _data.Length; i++)
			{
    			data.WriteByte((byte)_data[i]);
			}
			
			this.size = _data.Length;
		}
		
		public void setJobData(byte[] _data)
		{
			data = new MemoryStream(_data.Length);
			bool done = false; 
			bool sawhandle = false; 
			int i = 0;
			
			for(i = 0; i < _data.Length && !done; i++)
			{
				if(_data[i] == 0)
				{ 
					if (!sawhandle) 
					{
						sawhandle = true;
					} else { 
						done = true;
					}
				}
					
				data.Write(_data, i, 1);
			}
			
			this.size = i;
		}
		
		public int length() 
		{
			return size + 12;
		}
    		
		public void Dump()
		{
			Console.WriteLine("Dumping packet with {0} bytes of data!", data.Length);
			data.Seek(0, SeekOrigin.Begin);
			
			try { 
				int count = 0;
				int idata; 
				int lineCnt = 0;
				byte[] snarf; 
				byte[] line = new byte[16]; 
				
				ASCIIEncoding encoding = new ASCIIEncoding( );

				while(count < data.Length)
	            {
					idata = data.ReadByte();
					
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
		
		public string ToString()
		{
			return "Packet:";
		}
	}
}
	
public static class Extensions
{
    /// <summary>
    /// Get the array slice between the two indexes.
    /// Inclusive for start index, exclusive for end index.
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
