
using System;
using System.Collections.Generic;
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
		private List<Connection> managers; 
		
		private int connectionIndex; 
		
		public enum JobPriority { 
			HIGH = 1, 
			NORMAL,
			LOW
		};
		
		public static readonly ILog Log = LogManager.GetLogger(typeof(Client));
			
		public Client()
		{
			managers = new List<Connection>();
		}
			
		public Client (string host, int port) : this()
		{
			Connection c = new Connection(host, port);
			managers.Add(c);
		}
		
		public Client (string host) : this(host, 4730) { }
		
		public byte[] submitJob(string callback, byte[] data)
		{
			
			try {
				RequestPacket p = new RequestPacket(PacketType.SUBMIT_JOB);
	    		Packet result = null;
				bool submitted = false;
				Connection c = null; 
				
				string jobid = System.Guid.NewGuid().ToString();
				
				byte[] preamble = new ASCIIEncoding().GetBytes(callback + "\0" + jobid + "\0"); 
				byte[] pktdata = new byte[preamble.Length + data.Length];
				Array.Copy(preamble, pktdata, preamble.Length);
				Array.Copy(data, 0, pktdata, preamble.Length, data.Length);
				
				p.Data = pktdata;

				while(!submitted) {
					
					// Simple round-robin submission for now
					c = managers[connectionIndex++ % managers.Count];
					
					c.sendPacket(p);
				
					Log.DebugFormat("Sent job request to {0}...", c);
					
					// We need to get back a JOB_CREATED packet
					result = c.getNextPacket();
					
					// If we get back a JOB_CREATED packet, we can continue
					// otherwise try the next job manager
					if (result.Type == PacketType.JOB_CREATED) 
					{
						submitted = true; 
						Log.DebugFormat("Created job {0}", result.JobHandle);	
					}
				}
				
				
				// This method handles synchronous requests, so we wait 
				// until we get a work complete packet
				while(true) { 
					
					result = c.getNextPacket(); 
				
					if(result.Type == PacketType.WORK_COMPLETE)
					{
						Log.DebugFormat("Completed job {0}", result.JobHandle);
						return result.Data;
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
				Connection c = null; 
				
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
				string jobid = System.Guid.NewGuid().ToString();
			
				p.setData(callback + "\0" + jobid + "\0" + data );
							
	    		Packet result;
				
				while(true) {
					
					// Simple round-robin submission for now
					c = managers[connectionIndex++ % managers.Count];
					
					c.sendPacket(p);
				
					Log.DebugFormat("Sent background job request to {0}...", c);
					
					// We need to get back a JOB_CREATED packet
					result = c.getNextPacket();
					
					// If we get back a JOB_CREATED packet, we can continue,
					// otherwise try the next job manager
					if (result.Type == PacketType.JOB_CREATED) 
					{
						Log.DebugFormat("Created background job {0}, with priority {1}", result.JobHandle, priority.ToString());	
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
			
			Packet result = null; 
			
			foreach (Connection conn in managers)
			{
				Log.DebugFormat("Checking for status on {0} on {1}", jobHandle, conn);
				conn.sendPacket(rp);
				
				result = conn.getNextPacket(); 
				
				if(result.Type == PacketType.STATUS_RES)
				{			
					if(result.JobHandle != jobHandle) {
					
						Log.DebugFormat("Wrong job!!");
					
					} else { 
											
						byte[] d = result.Data; 
						byte[] buffer = new byte[256];
						bool knownstatus = false, running = false;
						int offset = 0; 
						int boff = 0; 
						int percentnumer = 0, percentdenom = 0; 
						float percentdone = 0; 

						Log.DebugFormat("Hooray, this is my job!!");
						ASCIIEncoding encoder = new ASCIIEncoding();
					
						// Check to see if this response has a known status 
						// and if it's running
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
				}		
			}	
			
			return false;
		}
	}
}
