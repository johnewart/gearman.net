
using System;
using System.Text; 

namespace Gearman.Packets.Client
{

	public class JobCreated : Packet
	{
		public string jobhandle; 
		
		public JobCreated ()
		{
		}
		
		public JobCreated(byte[] pktdata) : base(pktdata)
		{
			int pOff = 0;
			pOff = parseString(pOff, ref jobhandle);
		}
		
		public JobCreated(string jobhandle)
		{
			this.jobhandle = jobhandle;
			this.size = jobhandle.Length;
		}
	}
	
	public class StatusRes : Packet 
	{
		public string jobhandle; 
		public bool knownstatus; 
		public bool running; 
		public int pctCompleteNumerator;
		public int pctCompleteDenominator; 
			
		public StatusRes()
		{}
		
		public StatusRes(byte[] pktdata) : base(pktdata)
		{
			int pOff = 0; 
			pOff = parseString(pOff, ref jobhandle);
			knownstatus = ((int)Char.GetNumericValue((char)rawdata[pOff]) == 1);
			
			// increment past the null terminator
			pOff += 2;
			running = ((int)Char.GetNumericValue((char)rawdata[pOff]) == 1);
			
			pOff += 2;
			string numerator = "";
			pOff = parseString(pOff, ref numerator);
			
			string denominator = "";
			pOff = parseString(pOff, ref denominator);
			
			pctCompleteDenominator = int.Parse(denominator);
			pctCompleteNumerator = int.Parse(numerator);
		}
	}
	
	public class GetStatus : Packet
	{
		public string jobhandle;
		
		public GetStatus()
		{ }
		
		public GetStatus(string jobhandle)
		{
			this.jobhandle = jobhandle;
			this.type = PacketType.GET_STATUS;
			this.size = jobhandle.Length; 
		}
		
		override public byte[] ToByteArray()
		{
			byte[] result = new byte[this.size + 12]; 
			byte[] jhbytes = new ASCIIEncoding().GetBytes(jobhandle);
			Array.Copy(this.Header, result, this.Header.Length);
			Array.Copy(jhbytes, 0, result, this.Header.Length, jhbytes.Length);
			return result;
		}
		
	}
		
}
