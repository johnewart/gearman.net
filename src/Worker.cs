using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Net.Sockets;
using System.Text; 
using log4net;

namespace Gearman
{
	/// <summary>
	/// This class is a "worker" in the client <--> manager <--> worker model that Gearman follows.
	/// A worker is a simply an intermediary between some actual code and the Gearman network. The onus
	/// is on the user to write the methods that perform the actual work requested and respond with the 
	/// correct results. The worker simply talks to the manager to get requests for work, and then makes
	/// calls to callback methods written by the user. 
	/// </summary>
	/// <example>
	/// The following provides a very simple worker program that is capable of counting the words in a byte[] array
	/// using the newline charavter as the delimiter between 'words' (thus acting more like a 'line count' technically)
	/// 
	/// <code>
	/// 	private static byte[] wctest(Packet jobPacket, Connection c)
	///	{
	///		int words = 0;
	///		byte[] outdata = new byte[1024];
	///		byte[] indata = jobPacket.Data;
	///		for(int i = 0; i < indata.Length; i++) 
	///		{
	///			if (indata[i] == 10)
	///				words++;
	///		}
	///			
	///		outdata = BitConverter.GetBytes(words);
	///		Console.WriteLine("Found {0} words", words);
	///		return outdata;
	///	}
	/// 
	///	public static void Main(String[] args)
	///	{
	///		Worker w = new Worker("localhost");
	///		w.registerFunction("wc", wctest);
	///		w.workLoop();
	///	}
	/// </code>
	/// </example>

	public class Worker
	{
	
		// Manager connection
		// TODO: Add round-robin or multiple servers to the worker
		private Connection c; 
		
		// Dictionary of method mappings (string -> delegate)
		private Dictionary<string, taskdelegate> methodMap;
		
		// Running thread
		private Thread t;
		
		private static readonly ILog Log = LogManager.GetLogger(typeof(Worker));

		/// <summary>
		/// Task delegate must take a packet and a connection and return a byte array
		/// The connection is required to send data back (such as progress updates), and
		/// the packet is used to get information such as job handle, packet data, etc.
		/// </summary>
		public delegate byte[] taskdelegate(Packet jobPacket, Connection c); 
		
		/// <summary>
		/// Default constructor, initialize empty method map of strings -> delegates
		/// </summary>
		public Worker()
		{
			methodMap = new Dictionary<string, taskdelegate>();
		}
		/// <summary>
		/// Constructor requiring both the host and the port number to connect to
		/// </summary>
		/// <param name="host">
		/// A <see cref="System.String"/> representing the host to connect to
		/// </param>
		/// <param name="port">
		/// A <see cref="System.Int32"/> port to connect to (if other than the default of 4730)
		/// </param>
		public Worker (string host, int port) : this()
		{
			this.c = new Connection(host, port);
		}
		
		/// <summary>
		/// Constructor requiring only the host name; the default port of 4730 is used
		/// </summary>
		/// <param name="host">
		/// A <see cref="System.String"/> representing the host to connect to
		/// </param>
		public Worker (string host) : this(host, 4730)
		{		}

		/// <summary>
		/// Add a task <--> delegate mapping to this worker. A task name is a short string that the worker registers
		/// with the manager, for example "reverse". The delegate is a callback that the worker calls when it receives a
		/// packet of work to do that has a task name matching the registered name.
		/// </summary>
		/// <param name="taskname">
		/// A <see cref="System.String"/> that is the 'name' of the task being registered with the server
		/// </param>
		/// <param name="function">
		/// A <see cref="taskdelegate"/> that is the actual callback function 
		/// (must match the <see cref="taskdelegate"/> specification)
		/// </param>
		public void registerFunction(string taskname, taskdelegate function)
		{
			methodMap[taskname] = function;
			RequestPacket r = new RequestPacket();
			r.Type = PacketType.CAN_DO;
			r.setData(taskname);
			c.sendPacket(r);
		}
		                
		/// <summary>
		/// Get this thing going.
		/// </summary>
		public void workLoop()
		{
			t = new Thread(new ThreadStart(run));
			t.Start();
		}
		
		/// <summary>
		/// Terminate the work thread.
		/// </summary>
		public void stopWorkLoop()
		{
			if (t != null)
			{
				t.Abort();
			}
		}
		
		/// <summary>
		/// Obligatory run method for the Thread. Runs indefinitely, checking for a job every 2 seconds
		/// TODO: make this time adjustable if needed
		/// </summary>
		public void run()
		{
			while(true)
			{
				checkForJob();
				Log.DebugFormat("Sleeping for 2 seconds");
				Thread.Sleep(2000);
			}		
		}

		/// <summary>
		/// Checks with the manager for a new job to work on. The manager has a record of all the tasks that the
		/// worker is capable of working on, so most of the real work here is done by the manager to find something
		/// for the worker to work on. If something is assigned, load the data, execute the callback and then return 
		/// the data to the manager so that the worker can move on to something else.
		/// </summary>
		private void checkForJob()
		{
			ASCIIEncoding encoder = new ASCIIEncoding(); 
			// Grab job from server (if available)
			Log.DebugFormat("Checking for job...");
			RequestPacket rp = new RequestPacket();
			rp.Type = PacketType.GRAB_JOB;
			
			c.sendPacket(rp);
			
			Packet response = c.getNextPacket();
			
			if (response.Type == PacketType.JOB_ASSIGN)
			{
				Log.DebugFormat("Assigned job: {0}", response.JobHandle);
				byte[] payload = response.Data;
				bool gotTask = false;
				string task = "";
				
				for(int i = 0; i < payload.Length; i++)
				{
					if (payload[i] == 0 || i == payload.Length - 1)
					{
						if(!gotTask)
						{
							Log.DebugFormat("Got zero byte and no task yet...");
							task = encoder.GetString(payload, 0, i);
							gotTask = true; 		
						} else {
							Log.DebugFormat("Task: {0}", task); 
							byte[] result      = methodMap[task](response, c);
							string resultText  = encoder.GetString(result);
							
							Log.DebugFormat("Result: " + resultText);
							
							RequestPacket workresult = new RequestPacket(PacketType.WORK_COMPLETE, result, response.JobHandle);
							c.sendPacket(workresult);
							
							workresult.Dump();
							
						}	
					}	
				}
			} else if (response.Type == PacketType.NO_JOB) {
				Log.DebugFormat("Nothing to do!");
			} 
		}
		
	}
}
