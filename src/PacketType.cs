

using System;

namespace Gearman
{
	
	/// <summary>
	/// Public enum representing the various types of packets
	/// sent to and from the manager. Each entry is a packet type, 
	/// the code comments show if it's a request or response 
	/// and who sends it (client or worker)
	/// </summary>
	public enum PacketType
	{
		CAN_DO = 1,				// REQ    Worker
		CANT_DO,					// REQ    Worker
		RESET_ABILITIES,			// REQ    Worker
		PRE_SLEEP,				// REQ    Worker
		NOTHING,					// -      -
		NOOP,					// RES    Worker
		SUBMIT_JOB,				// REQ    Client
		JOB_CREATED,				// RES    Client
		GRAB_JOB,				// REQ    Worker
		NO_JOB,					// RES    Worker
		JOB_ASSIGN,				// RES    Worker
		WORK_STATUS,				// REQ    Worker
		                        // RES    Client
		WORK_COMPLETE,			// REQ    Worker
		                        // RES    Client
		WORK_FAIL,				// REQ    Worker
		                        // RES    Client
		GET_STATUS,				// REQ    Client
		ECHO_REQ,				// REQ    Client/Worker
		ECHO_RES,				// RES    Client/Worker
		SUBMIT_JOB_BG,			// REQ    Client
		ERROR,					// RES    Client/Worker
		STATUS_RES,				// RES    Client
		SUBMIT_JOB_HIGH,			// REQ    Client
		SET_CLIENT_ID,			// REQ    Worker
		CAN_DO_TIMEOUT,			// REQ    Worker
		ALL_YOURS,				// REQ    Worker
		WORK_EXCEPTION,			// REQ    Worker
		                        // RES    Client
		OPTION_REQ,				// REQ    Client/Worker
		OPTION_RES,				// RES    Client/Worker
		WORK_DATA,				// REQ    Worker
		                        // RES    Client
		WORK_WARNING,			// REQ    Worker
		                        // RES    Client
		GRAB_JOB_UNIQ,			// REQ    Worker
		JOB_ASSIGN_UNIQ,			// RES    Worker
		SUBMIT_JOB_HIGH_BG,		// REQ    Client
		SUBMIT_JOB_LOW,			// REQ    Client
		SUBMIT_JOB_LOW_BG,		// REQ    Client
		SUBMIT_JOB_SCHED,		// REQ    Client
		SUBMIT_JOB_EPOCH,		// REQ    Client
	}
}