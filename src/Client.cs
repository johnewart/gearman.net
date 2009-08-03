
using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Text; 
using log4net;

namespace Gearman
{
	public class Client
	{
		
		public enum JobPriority { 
			HIGH = 1, 
			NORMAL,
			LOW
		};
		
		public static readonly ILog Log = LogManager.GetLogger(typeof(Client));
		
		private Connection c; 
	
		public Client()
		{	}
		
		public Client (string host) : this()
		{
			c = new Connection(host, 4730);
		}
		
		public Client (string host, int port) : this()
		{
			c = new Connection(host, port);
		}
		
		public byte[] submitJob(string callback, byte[] data)
		{
			//Log.DebugFormat("Configured foo = {0}", config["foo"]);
			
			try {
				RequestPacket p = new RequestPacket(PacketType.SUBMIT_JOB);
				byte[] preamble = new ASCIIEncoding().GetBytes(callback + "\0" + "12345\0"); 
				byte[] pktdata = new byte[preamble.Length + data.Length];
				Array.Copy(preamble, pktdata, preamble.Length);
				Array.Copy(data, 0, pktdata, preamble.Length, data.Length);
				
				p.Data = pktdata;
				
				c.sendPacket(p);
				
				Log.DebugFormat("Sent job request...");
				
	    		Packet result;
				
				// Synchronous requests only at the moment, we loop 
				// until we get a work complete packet
				while(true) { 
					
					result = c.getNextPacket(); 
				
					if(result.Type == PacketType.WORK_COMPLETE)
					{
						Log.DebugFormat("Completed job {0}", result.JobHandle);
						return result.Data;
					} else if (result.Type == PacketType.JOB_CREATED) { 
						Log.DebugFormat("Created job {0}", result.JobHandle);	
					}
				}
		
			} catch (Exception e) { 
				Log.DebugFormat("Error submitting job: {0}", e.ToString());
				return null;
			}
		}
		
		/// <summary>Submit a job to the job server in the background, with a particular priority</summary>
		/// <example>
		/// <code>
		/// // create the class that does translations
		/// GiveHelpTransforms ght = new GiveHelpTransforms();
		/// // have it load our XML into the SourceXML property
		/// ght.LoadXMLFromFile(
		///  "E:\\Inetpub\\wwwroot\\GiveHelp\\GiveHelpDoc.xml");
		/// </code>
		/// </example>
		public string submitJobInBackground(string callback, byte[] data, JobPriority priority)
		{
			try {
				PacketType pt;
				
				switch(priority) 
				{
					case JobPriority.HIGH:
						pt = PacketType.SUBMIT_JOB_HIGH_BG;
						break;
					case JobPriority.LOW:
						pt = PacketType.SUBMIT_JOB_LOW_BG;
						break;	
					default:
						pt = PacketType.SUBMIT_JOB_BG;
						break;
				}
				
				RequestPacket p = new RequestPacket(pt);
				p.setData(callback + "\0" + "12345\0" + data );
				
				c.sendPacket(p);
				
				Log.DebugFormat("Sent background job request...");
				
	    		Packet result;
				
				// Synchronous requests only at the moment, we loop 
				// until we get a job created packet, then return the
				// job ID. 
				while(true) { 
					
					result = c.getNextPacket(); 
				
					if(result.Type == PacketType.JOB_CREATED)
					{
						Log.DebugFormat("Submitted job {0}", result.JobHandle);
						return result.JobHandle;
					}
				}
		
			} catch (Exception e) { 
				Log.DebugFormat("Error submitting job: {0}", e.ToString());
				return null;
			}
		}
	
		public bool checkIsDone(string jobHandle)
		{
			RequestPacket rp = new RequestPacket(PacketType.GET_STATUS);
			rp.setData(jobHandle);
			
			Log.DebugFormat("Checking for status on {0}", jobHandle);
			c.sendPacket(rp);
			Packet result; 
			
			while(true) { 
					
				result = c.getNextPacket(); 
				
				if(result.Type == PacketType.STATUS_RES)
				{
					Log.DebugFormat("Status of job {0}", jobHandle);
					
					byte[] d = result.Data; 
					byte[] buffer = new byte[256];
					bool knownstatus = false, running = false;
					int offset = 0; 
					int boff = 0; 
					int percentnumer = 0, percentdenom = 0; 
					float percentdone = 0; 
			
					if(result.JobHandle != jobHandle) {
						Log.DebugFormat("Wrong job!!");
					} else { 
						Log.DebugFormat("Hooray, this is my job!!");
						ASCIIEncoding encoder = new ASCIIEncoding();
					
						knownstatus = ((int)Char.GetNumericValue((char)d[0]) == 1);
						running = ((int)Char.GetNumericValue((char)d[2]) == 1);
						 
						if(knownstatus && running)
						{
							offset = 4; 
							while(d[offset] != 0)
							{
								buffer[boff++] = d[offset++];
							}	
					
							percentnumer = int.Parse(encoder.GetString(buffer));
							boff = 0; 
							offset++; 
							
							while(d[offset] != 0)
							{
								buffer[boff++] = d[offset++];
							}
							percentdenom = int.Parse(encoder.GetString(buffer));
									
							if(percentdenom != 0) 
								percentdone = (float)percentnumer / (float)percentdenom; 
							else
								percentdone = 0; 
							
							Log.DebugFormat("{0}% done!", percentdone * 100);
						} else { 
							if (!knownstatus)
								Log.DebugFormat("Status of job not known!");
						
							if (!running) 
								Log.DebugFormat("Job not running!");
						}	
						
						return (percentdone == 1);
						
					}
									
					return false;
				}
			}			
		}
	}
}
