
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
		protected byte[] data; 
		protected string jobhandle; 
		
		public Packet() { }
		
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
		
		public int Length { 
			set { 
				this.size = value - 12; 
			} 
			
			get { 
				// Include header size...
				return this.DataLength + 12; 
			}
		}
		
		public PacketType Type {
			set { 
				Console.WriteLine("Setting type!");
				this.type = (PacketType)value;
			}
			
			get { 
				return this.type;
			}
		}
		
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
		
		public string JobHandle { 
			set {
				Console.WriteLine("Setting job handle to {0}", value);
				this.jobhandle = value;
			}
			
			get { 
				return jobhandle;
			}
		}
		
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
		
		public void setData(String _data)
		{
			System.Text.ASCIIEncoding encoding=new System.Text.ASCIIEncoding();
			data = encoding.GetBytes(_data);
			this.size = _data.Length;
		}
		
		public void setData(byte[] _data)
		{
			this.data = _data; 
			this.size = _data.Length; 
		}
		
		/*
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
		*/
		
	
    		
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
		
		public override string ToString()
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
