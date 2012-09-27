using System;
using System.Data;
using System.Collections;
using System.Collections.Generic; 
using System.IO; 
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Mail; 
using System.Net.Security; 
using System.Security.Authentication; 

using SQLite; 

using Gearman; 
using Gearman.Packets.Client; 
using Gearman.Packets.Worker; 

namespace GearmanServer {
	
	public class Daemon {

		System.Net.Sockets.TcpListener TCPServer;

		public Daemon(SQLiteConnection db) {

			GearmanServer.Log.Info("Server listening on port 4730");
			
			TCPServer = new System.Net.Sockets.TcpListener(4730); 

			while(true) { 
				TCPServer.Start(); 

				if(TCPServer.Pending()) { 
					try {
						new ConnectionHandler(TCPServer.AcceptTcpClient(), db); 
					} catch (Exception e) {
						GearmanServer.Log.Error("Exception waiting for connection: ", e);
					}
				} 

				System.Threading.Thread.Sleep(1);
			} 
		} 
		
	
	} // End of class SocketDaemon 
	
	
	class ConnectionHandler
	{
		private TcpClient client; 
		private Gearman.Connection conn; 
		private NetworkStream stream;
		private SQLiteConnection db; 
		private int jobid = 1; 
		private List<string> abilities; 

		public ConnectionHandler(TcpClient c, SQLiteConnection _db)
		{
			try {
				abilities = new List<string>(); 
				db = _db; 
				client = c;
				conn = new Gearman.Connection(c.Client);

				IPAddress remoteIP = ((IPEndPoint)c.Client.RemoteEndPoint).Address;
				GearmanServer.Log.Info(String.Format("Connection made by {0}", remoteIP)); 
			
				Thread t = new Thread(new ThreadStart(run)); 
				t.Start(); 
				
			} catch (Exception e) {
				GearmanServer.Log.Error("Problem getting remote IP: ", e);
			}		   		
			
		}

		private void handleJobSubmitted(Packet p)
		{
			string jobhandle = String.Format("FOONARF:{0}", jobid);

			SubmitJob sj = (SubmitJob)p;

			var job = new Job() {
				TaskName = sj.taskname,
				JobHandle = jobhandle, 
				Unique = sj.unique_id, 
				When = DateTime.Now, 
				Priority = 0, 
				Data = sj.data, 
				Dispatched = false
			};
			
			int newRecords = db.Insert(job);
			
			Console.WriteLine("{0} == {1}", job.JobHandle, job.Id);
			GearmanServer.Log.Info ("Background job was submitted!");
			conn.sendPacket(new JobCreated(jobhandle));
			jobid++; 
		}

		private void handleJobCompletion(Packet p)
		{
			WorkComplete wc = ((WorkComplete)p); 
			var result = (
				from j in db.Table<Job>()
				where j.JobHandle == wc.jobhandle
				select j
				); 
			var job = result.First();
			db.Delete (job); 

		}

		private void grabNextJob()
		{
			foreach (var ability in abilities)
			{
				GearmanServer.Log.Info(string.Format("Trying to find job for {0}", ability)); 
				int Priority = 0; 
				DateTime now = DateTime.Now; 

				var result = (
					from j in db.Table<Job>() 
					where j.When <= now &&
					      j.TaskName == ability && 
					  	  j.Priority == Priority && 
						  j.Dispatched == false
					select j
				);
			
				if (result.Count() > 0) 
				{
					GearmanServer.Log.Info (string.Format("Found job for ability {0}", ability));
					var job = result.First(); 
					JobAssign ja = new JobAssign(job.JobHandle, job.TaskName, job.Data);
					ja.Dump (); 
					conn.sendPacket (ja);
					return;
				}
			}
		}

		private void registerAbility(Packet p)
		{
			CanDo pkt = ((CanDo)p); 
			if(!abilities.Contains(pkt.functionName)) 
				abilities.Add(pkt.functionName); 
		}
		
		
		private void run()
		{
			Packet p;
			bool okay = true;

			while(okay)
			{
				p = conn.getNextPacket();
				if (p != null) 
				{
					//p.Dump();
					// Immediately store background jobs and move on
					switch(p.Type)
					{
					case PacketType.SUBMIT_JOB:
						handleJobSubmitted(p);
						break; 

					case PacketType.SUBMIT_JOB_HIGH:
						goto case PacketType.SUBMIT_JOB;
					case PacketType.SUBMIT_JOB_LOW:
						goto case PacketType.SUBMIT_JOB;
					case PacketType.SUBMIT_JOB_BG:
						goto case PacketType.SUBMIT_JOB;
					case PacketType.SUBMIT_JOB_HIGH_BG:
						goto case PacketType.SUBMIT_JOB;
					case PacketType.SUBMIT_JOB_LOW_BG:
						goto case PacketType.SUBMIT_JOB;
					case PacketType.SUBMIT_JOB_EPOCH:
						goto case PacketType.SUBMIT_JOB;
					case PacketType.SUBMIT_JOB_SCHED:
						goto case PacketType.SUBMIT_JOB;
					
					case PacketType.GRAB_JOB:
						p.Dump ();
						grabNextJob(); 
						break; 

					case PacketType.CAN_DO:
						p.Dump ();
						registerAbility(p);
						break; 
					
					case PacketType.SET_CLIENT_ID:
						p.Dump(); 
						break; 

					case PacketType.WORK_COMPLETE:
						handleJobCompletion(p); 
						break; 

					default: 
						GearmanServer.Log.Info ("Nothing to do with this...");
						break;
					}


				} else { 
					GearmanServer.Log.Info ("No packet!");
					okay = false;
				}
			}

			GearmanServer.Log.Info("ConnectionHandler terminated.");						
		}

	}//end of class ConnectionHandler 
	
}
