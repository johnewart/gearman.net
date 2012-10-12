using System;
using System.Collections.Generic; 
using System.Collections;

namespace GearmanServer
{
    public enum JobPriority {
        LOW = 0,				
        NORMAL,					
        HIGH
    };

	public class Job
	{
		public long Id { get; set; }

		public string Unique { get; set; }

		public string TaskName { get; set; }

		public string JobHandle { get; set; }

		public byte[] Data { get; set; }

		public int Priority { get; set; }

		public DateTime When { get; set; }

		public bool Dispatched { get; set; }

	}
}
