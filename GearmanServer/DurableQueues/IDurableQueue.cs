using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GearmanServer.DurableQueues
{
    public interface IDurableQueue
    {
        String toString(); 
        List<Job> restoreJobs();
        bool storeJob(Job j);
        bool removeJob(Job j);
    }
}
