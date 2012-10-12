using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Redis;
using ServiceStack.Redis.Generic; 


namespace GearmanServer.DurableQueues
{
    public class RedisQueue : IDurableQueue
    {
        RedisClient redisClient;
        IRedisTypedClient<Job> jobStore;
        
        public RedisQueue(RedisClient _client)
        {
            redisClient = _client;
            jobStore = redisClient.As<Job>();
        }

        public List<Job> restoreJobs()
        {
            List<Job> restored = new List<Job>();

            foreach (string key in jobStore.GetAllKeys())
            {
                if (key.StartsWith("queue:"))
                {
                    var taskname = key.Split(':')[1];

                    foreach (Job j in jobStore.GetAllItemsFromList(jobStore.Lists[key]))
                    {
                        restored.Add(j);
                    }
                }
            }

            return restored; 
        }

        public bool storeJob(Job j)
        {
            GearmanServer.Log.Debug("Adding job " + j.Unique + " to Redis"); 
            j.Id = jobStore.GetNextSequence();
            jobStore.Lists["queue:" + j.TaskName].Add(j);
            return true;
        }

        public bool removeJob(Job j)
        {
            GearmanServer.Log.Debug("Removing job " + j.Unique + " from Redis"); 
            jobStore.Lists["queue:" + j.TaskName].Remove(j);
            return true;
        }

        public String toString()
        {
            return "Redis Queue @ " + redisClient.Host + ":" + redisClient.Port;
        }
    }
}
