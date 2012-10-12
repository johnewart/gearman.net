using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using GearmanServer.DurableQueues; 

namespace GearmanServer
{
    public class JobQueue
    {
        Dictionary<String, Dictionary<int, SortedList<DateTime, List<Job>>>> jobBuckets;
        Dictionary<String, Job> activeJobs;
        IDurableQueue queue;

        public JobQueue()
        {
            queue = null;
            activeJobs = new Dictionary<String, Job>();
            jobBuckets = new Dictionary<String, Dictionary<int, SortedList<DateTime, List<Job>>>>();

        }

        public JobQueue(IDurableQueue _queue) : this()
        {
            queue = _queue; 
            List<Job> restored = queue.restoreJobs();
            
            foreach (Job j in restored)
            {
                storeJob(j, false);
            }

            GearmanServer.Log.Debug("Restored " + restored.Count + " jobs from " + queue.toString());
        }

        public bool storeJob(Job job, bool persist = true)
        {
            string funcName = job.TaskName;
            
            // If needed, register this bucket
            if (!jobBuckets.ContainsKey(funcName))
            {
                jobBuckets[funcName] = new Dictionary<int, SortedList<DateTime, List<Job>>>();
                foreach (JobPriority priority in Enum.GetValues(typeof(JobPriority)))
                {
                    int p = (int)priority;
                    jobBuckets[funcName][p] = new SortedList<DateTime, List<Job>>();
                }
            }

            var bucket = jobBuckets[funcName][job.Priority];

            List<Job> jobs;

            if (bucket.ContainsKey(job.When))
            {
                jobs = bucket[job.When];
            } else { 
                jobs = new List<Job>();
                bucket[job.When] = jobs;
            }

            jobs.Add(job);

            if (persist && queue != null)
            {
                queue.storeJob(job);
            }

            return true;
        }

        public bool finishedJob(String jobHandle)
        {
            if (activeJobs.ContainsKey(jobHandle))
            {
                Job job = activeJobs[jobHandle];
                activeJobs.Remove(jobHandle);
               
                if (queue != null)
                {
                    queue.removeJob(job);
                }

                GearmanServer.Log.Debug("Removed job " + jobHandle);
            }
            else
            {
                GearmanServer.Log.Debug("Could not find job " + jobHandle);
            }

            return true;
        }

        public Job getJobForQueue(String funcName)
        {
            Job returnJob = null;
            Array priorities = Enum.GetValues(typeof(JobPriority));
            Array.Reverse(priorities);
            foreach (JobPriority p in priorities)
            {
                if (jobBuckets.ContainsKey(funcName) && 
                    jobBuckets[funcName].ContainsKey((int)p))
                {
                    var bucket = jobBuckets[funcName][(int)p];
                    if (bucket.Keys.Count > 0)
                    {
                        foreach(DateTime when in bucket.Keys)
                        {
                            DateTime cmpKey = DateTime.UtcNow; 
                            if (when <= cmpKey)
                            {
                                var list = bucket[when];
                                if (list.Count > 0)
                                {
                                    returnJob = list[list.Count - 1];
                                    list.RemoveAt(list.Count - 1);
                                    activeJobs.Add(returnJob.JobHandle, returnJob);

                                    if (list.Count == 0)
                                    {
                                        bucket.Remove(when);
                                    }

                                    return returnJob; 
                                }
                               
                            }
                        }
                    }
                }
            }

            if (returnJob == null)
            {
                GearmanServer.Log.Info("Couldn't find a job");
            }

            return returnJob;
        }
    }
 
}
