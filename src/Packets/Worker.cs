
using System;
using System.Text; 

namespace Gearman.Packets.Worker
{
	
	public class CanDo : Packet
	{
		public string functionName; 
		
		public CanDo(string function)
		{
			this.type = PacketType.CAN_DO;
			this.functionName = function; 
			this.size = functionName.Length;
		}
		
		override public byte[] ToByteArray()
		{
			byte[] result = new byte[this.size + 12]; 
			byte[] functionbytes = new ASCIIEncoding().GetBytes(functionName);
			Array.Copy(this.Header, result, this.Header.Length);
			Array.Copy(functionbytes, 0, result, this.Header.Length, functionbytes.Length);
			return result;
		}
	}
	
	public class CanDoTimeout : Packet { 
		public string functionName;
		
		public CanDoTimeout(string function)
		{
			this.functionName = function;
			this.size = function.Length;
			this.type = PacketType.CAN_DO_TIMEOUT;
		}
		
		override public byte[] ToByteArray()
		{
			byte[] result = new byte[this.size + 12]; 
			byte[] functionbytes = new ASCIIEncoding().GetBytes(functionName);
			Array.Copy(this.Header, result, this.Header.Length);
			Array.Copy(functionbytes, 0, result, this.Header.Length, functionbytes.Length);
			return result;
		}
	}
	
	public class CantDo : Packet {
		public string functionName;
		
		public CantDo(string function)
		{
			this.functionName = function;
			this.size = function.Length;
			this.type = PacketType.CANT_DO;
		}
		
		override public byte[] ToByteArray()
		{
			byte[] result = new byte[this.size + 12]; 
			byte[] functionbytes = new ASCIIEncoding().GetBytes(functionName);
			Array.Copy(this.Header, result, this.Header.Length);
			Array.Copy(functionbytes, 0, result, this.Header.Length, functionbytes.Length);
			return result;
		}
	}
	
	public class ResetAbilities : Packet {
		
		public ResetAbilities() 
		{
			this.type = PacketType.RESET_ABILITIES;
		}
				
	}
	
	public class PreSleep : Packet {
		public PreSleep()
		{
			this.type = PacketType.PRE_SLEEP;
		}
	}
	
	public class GrabJob : Packet {
		public GrabJob()
		{
			this.type = PacketType.GRAB_JOB;
		}
	}
	
	public class GrabJobUniq : Packet {
		public GrabJobUniq()
		{
			this.type = PacketType.GRAB_JOB_UNIQ;
		}
	}
	
	public class WorkData : Packet { 
		public String jobhandle; 
		public byte[] data; 
		
		public WorkData()
		{
			this.type = PacketType.WORK_DATA;
		}
		
		public WorkData(String jobhandle, byte[] data)
		{
			this.jobhandle = jobhandle; 
			this.data = data;
			this.size = jobhandle.Length + 1 + data.Length; 
			this.type = PacketType.WORK_DATA;
		}
		
		public WorkData(byte[] pktdata) : base(pktdata)
		{
			int pOff = 0;
			pOff = parseString(pOff, ref jobhandle);
			data = rawdata.Slice(pOff, rawdata.Length);
		}
		
		override public byte[] ToByteArray()
		{
			byte[] result = new byte[this.size + 12]; 
			byte[] header = base.ToByteArray();
			byte[] jhbytes = new ASCIIEncoding().GetBytes(jobhandle + '\0');
			Array.Copy(header, result, header.Length);
			Array.Copy(jhbytes, 0, result, header.Length, jhbytes.Length);
			Array.Copy(data, 0, result, header.Length + jhbytes.Length, data.Length);
			return result;
		}
	}
	
	public class WorkWarning : WorkData 
	{
		public WorkWarning(String jobhandle, byte[] data) : base(jobhandle, data)
		{
			this.type = PacketType.WORK_WARNING;
		}
		
		public WorkWarning(byte[] pktdata) : base(pktdata)
		{
			this.type = PacketType.WORK_WARNING;
		}
	}
	
	public class WorkComplete : WorkData 
	{
		public WorkComplete(String jobhandle, byte[] data) : base(jobhandle, data)
		{
			this.type = PacketType.WORK_COMPLETE;
		}
		
		public WorkComplete(byte[] pktdata) : base(pktdata)
		{
			this.type = PacketType.WORK_COMPLETE;
		}
		
	}		
	
	public class WorkFail : Packet
	{
		public string jobhandle; 
		
		public WorkFail(String jobhandle)
		{
			this.jobhandle = jobhandle;
			this.size = jobhandle.Length; 
			this.type = PacketType.WORK_FAIL;
		}
		
		public WorkFail(byte[] pktdata) : base(pktdata)
		{
			int pOff = 0;
			pOff = parseString(pOff, ref jobhandle);
		}
		
		override public byte[] ToByteArray()
		{
			byte[] result = new byte[this.size + 12]; 
			byte[] header = base.ToByteArray();
			byte[] jhbytes = new ASCIIEncoding().GetBytes(jobhandle);
			Array.Copy(header, result, header.Length);
			Array.Copy(jhbytes, 0, result, header.Length, jhbytes.Length);
			return result;
		}
	}
	
	public class WorkException : Packet
	{
		public string jobhandle; 
		public byte[] exception; 
		
		public WorkException(string jobhandle, byte[] exception)
		{
			this.jobhandle = jobhandle;
			this.exception = exception;
			this.size = jobhandle.Length + 1 + exception.Length;
			this.type = PacketType.WORK_EXCEPTION;
		}
		
		public WorkException(byte[] pktdata) : base(pktdata)
		{
			int pOff = 0;
			pOff = parseString(pOff, ref jobhandle);
			exception = pktdata.Slice(pOff, rawdata.Length);
		}
	}
	
	public class WorkStatus : Packet 
	{
		public string jobhandle; 
		public int completenumerator; 
		public int completedenominator;
		
		public WorkStatus()
		{ } 
		
		public WorkStatus(string jobhandle, int numerator, int denominator)
		{
			this.jobhandle = jobhandle;
			this.completenumerator = numerator;
			this.completedenominator = denominator;
			this.type = PacketType.WORK_STATUS;
			this.size = jobhandle.Length + 1 + completenumerator.ToString().Length + 1 + completedenominator.ToString().Length;
		}
		
		public WorkStatus(byte[] pktdata) : base(pktdata)
		{
			int pOff = 0;
			string numerator = "", denominator = "";
			pOff = parseString(pOff, ref jobhandle);
			pOff = parseString(pOff, ref numerator);
			pOff = parseString(pOff, ref denominator);
			
			completedenominator = Int32.Parse(denominator);
			completenumerator = Int32.Parse(numerator);
		}
		
		override public byte[] ToByteArray()
		{
			byte[] result = new byte[this.size + 12]; 
			byte[] header = base.ToByteArray();
			byte[] msgdata = new ASCIIEncoding().GetBytes(jobhandle + '\0' + completenumerator + '\0' + completedenominator);
			Array.Copy(header, result, header.Length);
			Array.Copy(msgdata, 0, result, header.Length, msgdata.Length);
			return result;
		}
	}
	
	public class SetClientID : Packet
	{
		public string instanceid;
		
		public SetClientID(string instanceid)
		{
			this.instanceid = instanceid;
			this.type = PacketType.SET_CLIENT_ID;
		}
	}
	
	/* Responses */
	
	public class NoOp : Packet
	{
		public NoOp()
		{
			this.type = PacketType.NOOP;
		}
	}
	
	public class NoJob : Packet 
	{
		public NoJob()
		{
			this.type = PacketType.NO_JOB;
		}
	}
	
	public class JobAssign : Packet 
	{
		public string jobhandle;
		public string taskname; 
		public byte[] data; 
		
		public JobAssign()
		{
			this.type = PacketType.JOB_ASSIGN;
		}
		
		public JobAssign(string jobhandle, string taskname, byte[] data)
		{
			this.jobhandle = jobhandle; 
			this.taskname = taskname;
			this.data = data; 
		}
		
		public JobAssign(byte[] pktdata) : base(pktdata)
		{
			int pOff = 0;
			pOff = parseString(pOff, ref jobhandle);
			pOff = parseString(pOff, ref taskname);
			data = rawdata.Slice(pOff, rawdata.Length);
		}
	}
	
	public class JobAssignUniq : Packet 
	{
		public string jobhandle, taskname, unique_id; 
		public byte[] data;
		
		public JobAssignUniq()
		{
			this.type = PacketType.JOB_ASSIGN_UNIQ;
		}
		
		public JobAssignUniq(byte[] pktdata) : base (pktdata)
		{
			int pOff = 0;

			pOff = parseString(pOff, ref jobhandle);
			pOff = parseString(pOff, ref taskname);
			pOff = parseString(pOff, ref unique_id);
			data = pktdata.Slice(pOff, pktdata.Length);
		}
	}
	
	public class SubmitJob : Packet
	{
		public string functionName, uniqueId; 
		public byte[] data; 
		
		
		public SubmitJob()
		{
		
		}
		
		public SubmitJob(string function, string uniqueId, byte[] data, bool background)
		{
			this.functionName = function; 
			this.uniqueId = uniqueId; 
			this.data = data;
			this.size = function.Length + 1 + uniqueId.Length + 1 + data.Length;
			
			if(background)
			{
				this.type = PacketType.SUBMIT_JOB_BG;
			} else {
				this.type = PacketType.SUBMIT_JOB;
			}
		}
		
		public SubmitJob(string function, string uniqueId, byte[] data, bool background, Gearman.Client.JobPriority priority) : this(function, uniqueId, data, background)
		{
			
			switch (priority)
			{
				case Gearman.Client.JobPriority.HIGH:
					this.type = background ? PacketType.SUBMIT_JOB_HIGH_BG : PacketType.SUBMIT_JOB_HIGH;
					break;
				case Gearman.Client.JobPriority.NORMAL:
					this.type = background ? PacketType.SUBMIT_JOB_BG : PacketType.SUBMIT_JOB;
					break;
				case Gearman.Client.JobPriority.LOW:
					this.type = background ? PacketType.SUBMIT_JOB_LOW_BG : PacketType.SUBMIT_JOB_LOW;
					break;
				default:
					break;
			}
		}
		
		override public byte[] ToByteArray()
		{
			byte[] result = new byte[this.size + 12]; 
			byte[] metadata = new ASCIIEncoding().GetBytes(functionName + '\0' + uniqueId + '\0');
			Array.Copy(this.Header, result, this.Header.Length);
			Array.Copy(metadata, 0, result, this.Header.Length, metadata.Length);
			Array.Copy(data, 0, result, Header.Length + metadata.Length, data.Length);
			return result;
		}
		
	}
	
}