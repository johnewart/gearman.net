
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
	/// <summary>
	/// This class represents a client connecting to the Gearman network. The client can connect to one or more 
	/// manager daemons, and submit jobs to the network. The jobs are sent as <see cref="System.byte[]"/> arrays
	/// containing the data to work on, a string to identify the type of work to be done, a unique job id, and an 
	/// an optional priority (for background jobs)
	/// </summary>
	public class Client
	{
		// A list of managers to cycle through
		private List<Connection> managers; 
		
		// Which connection 
		private int connectionIndex; 
		
		// log4net log instance
		private static readonly ILog Log = LogManager.GetLogger(typeof(Client));
		
		/// <summary>
		/// Public enum describing the priority of the job
		/// </summary>
		public enum JobPriority { 
			HIGH = 1, 
			NORMAL,
			LOW
		};
		
			
		/// <summary>
		/// Constructor (default), initializes an empty list of managers
		/// </summary>
		public Client()
		{
			managers = new List<Connection>();
		}
			
		/// <summary>
		/// Constructor connecting to a specific host / port combination. Adds the connection to the managers list
		/// </summary>
		/// <param name="host">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="port">
		/// A <see cref="System.Int32"/>
		/// </param>
		public Client (string host, int port) : this()
		{
			Connection c = new Connection(host, port);
			managers.Add(c);
		}
		
		/// <summary>
		/// Constructor connecting to a specific host, on the default port TCP 4730
		/// </summary>
		/// <param name="host">
		/// A <see cref="System.String"/> representing the host to connect to.
		/// </param>
		public Client (string host) : this(host, 4730) { }
		
		/// <summary>
	    /// Submit a job to a job server for processing. The callback string is used as the 
	   	/// task for the manager to hand off the job to a worker. 
	    /// </summary>
		/// <param name="callback">
		/// A string containing the name of the operation to ask the manager to find a worker for
		/// </param>
		/// <param name="data">
		/// A byte array containing the data to be worked on. This data is passed to the worker who can 
		/// work on a job of the requested type.
		/// </param>
		/// <returns>
		/// A byte array containing the response from the worker that completed the task
		/// </returns>
		/// <example>
		///  <code>
		///   Client c = new Client("localhost");
		///   byte[] data = new ASCIIEncoding().GetBytes("foo\nbar\nquiddle\n");
		///   byte[] result = c.submitJob("wc", data);
		///  </code>
		/// </example>
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
	
		
		// TODO: Implement a percentage done feedback in the future?
		
		/// <summary>
		/// Query the manager to determine if a job with the unique job handle provided is done or not. The server returns
		/// a "percentage" done, if that's 100%, then the job is complete. This is mainly used for background jobs, in case
		/// the progress needs to be reported. 
		/// </summary>
		/// <param name="jobHandle">
		/// A <see cref="System.String"/> containing the unique job ID to query
		/// </param>
		/// <returns>
		/// True if complete, False otherwise
		/// </returns>
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
