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

using ServiceStack.Redis; 

using Gearman; 
using Gearman.Packets.Client; 
using Gearman.Packets.Worker;
using ServiceStack.Redis.Generic;
using System.Collections.Concurrent; 

namespace GearmanServer {
	
	public class Daemon {

		System.Net.Sockets.TcpListener TCPServer;
        ConcurrentDictionary<string, List<Gearman.Connection>> workers; 

		public Daemon(JobQueue queue, string hostName) {

			GearmanServer.Log.Info("Server listening on port 4730");
			
			TCPServer = new System.Net.Sockets.TcpListener(4730);
            workers = new ConcurrentDictionary<string, List<Connection>>(); 

			while(true) { 
				TCPServer.Start(); 

				if(TCPServer.Pending()) { 
					try {
                        new ConnectionHandler(TCPServer.AcceptTcpClient(), queue, hostName, workers); 
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
        private JobQueue queue;
        private string hostName;
        private ConcurrentDictionary<string, List<Gearman.Connection>> workers; 
        private List<string> abilities;

        public ConnectionHandler(TcpClient c, JobQueue _queue, string _hostName, ConcurrentDictionary<string, List<Gearman.Connection>> _workers)
		{
			try {
                hostName = _hostName;
				abilities = new List<string>(); 
				queue = _queue;
                workers = _workers; 

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

		private void handleJobSubmitted(Packet p, JobPriority priority, bool background)
		{
            Guid jobid = Guid.NewGuid();
			string jobhandle = String.Format("{0}:{1}", hostName, jobid);

			SubmitJob sj = (SubmitJob)p;
            
            var job = new Job() {
				TaskName = sj.taskname,
				JobHandle = jobhandle, 
				Unique = sj.unique_id, 
				When = sj.when, 
				Priority = (int)priority, 
				Data = sj.data, 
				Dispatched = false,
            };

            bool success = queue.storeJob(job, true);
			
			Console.WriteLine("{0} == {1}", job.JobHandle, job.Id);
			GearmanServer.Log.Info ("Job was submitted!");
			conn.sendPacket(new JobCreated(jobhandle));

            if(sj.when <= DateTime.UtcNow && workers.ContainsKey(sj.taskname))
            {
                foreach (Gearman.Connection c in workers[sj.taskname])
                {
                    c.sendPacket(new NoOp());
                }
            }
		}

		private void handleJobCompletion(Packet p)
		{
			WorkComplete wc = ((WorkComplete)p);
            queue.finishedJob(wc.jobhandle);
		}

		private void grabNextJob()
		{
			foreach (var ability in abilities)
			{
				GearmanServer.Log.Info(string.Format("Trying to find job for {0}", ability));
                Job job = queue.getJobForQueue(ability);

				if (job != null) 
				{
					GearmanServer.Log.Info (string.Format("Found job for ability {0}", ability));
					JobAssign ja = new JobAssign(job.JobHandle, job.TaskName, job.Data);
					ja.Dump (); 
					conn.sendPacket (ja);
					return;
				} else {
                    conn.sendPacket(new NoJob());
                }
			}
		}

		private void registerAbility(Packet p)
		{
			CanDo pkt = ((CanDo)p); 
			if(!abilities.Contains(pkt.functionName)) 
				abilities.Add(pkt.functionName);

            if (!workers.ContainsKey(pkt.functionName))
            {
                workers[pkt.functionName] = new List<Gearman.Connection>();
            }

            workers[pkt.functionName].Add(this.conn);
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
						handleJobSubmitted(p, JobPriority.NORMAL, false);
						break; 

					case PacketType.SUBMIT_JOB_HIGH:
						handleJobSubmitted(p, JobPriority.HIGH, false);
						break;
					case PacketType.SUBMIT_JOB_LOW:
						handleJobSubmitted(p, JobPriority.LOW, false);
						break;
					case PacketType.SUBMIT_JOB_BG:
						handleJobSubmitted(p, JobPriority.NORMAL, true);
						break;
					case PacketType.SUBMIT_JOB_HIGH_BG:
						handleJobSubmitted(p, JobPriority.HIGH, true);
						break;
					case PacketType.SUBMIT_JOB_LOW_BG:
						handleJobSubmitted(p, JobPriority.LOW, true);
						break;
					case PacketType.SUBMIT_JOB_EPOCH:
						handleJobSubmitted(p, JobPriority.NORMAL, true);
						break;
					case PacketType.SUBMIT_JOB_SCHED:
						handleJobSubmitted(p, JobPriority.NORMAL, true);
						break;
					
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

                    case PacketType.WORK_FAIL:
                        handleJobCompletion(p);
                        break; 

                    case PacketType.WORK_EXCEPTION:
                        handleJobCompletion(p);
                        break; 
                    
                    case PacketType.PRE_SLEEP:
                        p.Dump();
                        GearmanServer.Log.Debug("Worker sleeping...");
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
            foreach (String funcName in workers.Keys)
            {
                GearmanServer.Log.Info("Removing connection from worker list"); 
                workers[funcName].Remove(this.conn); 
            }
		}

	}//end of class ConnectionHandler 
	
}
