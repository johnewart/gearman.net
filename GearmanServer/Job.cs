using System;
using SQLite; 

namespace GearmanServer
{
	public class Job
	{
		[PrimaryKey, AutoIncrement]
		public int Id { get; set; }

		[MaxLength(128)]
		public string Unique { get; set; }

		[MaxLength(128)]
		public string TaskName { get; set; }

		[MaxLength(128)]
		public string JobHandle { get; set; }

		public byte[] Data { get; set; }

		public int Priority { get; set; }

		public DateTime When { get; set; }

		public bool Dispatched { get; set; }

	}
}
