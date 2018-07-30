using Newtonsoft.Json;
using NLog;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ConductorWorker.Base
{
    /// <summary>
    /// Base of our conductor worker. 
    /// Partial due to Execute method that should be implemented in derived class.
    /// </summary>
    public abstract partial class ConductorWorkerBase
    {
        /// <summary>
        /// Used to stop worker.
        /// </summary>
        public bool IsRunning = true;

        /// <summary>
        /// Url of conductor server, ex. "http://localhost:8080/api"
        /// </summary>
        public string ServerUrl;

        /// <summary>
        /// How much time we should wait before next polling.
        /// </summary>
        public int PollingInterval;

        /// <summary>
        /// How many threads our worker should use.
        /// </summary>
        public int ThreadCount;

        /// <summary>
        /// ID of a current worker.
        /// </summary>
        public string WorkerId;

        /// <summary>
        /// Domain.
        /// </summary>
        public string Domain;

        /// <summary>
        /// Simple REST client to ask conductor API
        /// </summary>
        public RestClient Client;

        /// <summary>
        /// Constructor of conductor worker class
        /// </summary>
        /// <param name="serverUrl">Url of conductor server, ex. "http://localhost:8080/api".</param>        
        /// <param name="pollingInterval">How much time we should wait before next polling.</param>
        /// <param name="threadCount">How many threads our worker should use.</param>
        /// <param name="workerId">ID of a current worker.</param>
        public ConductorWorkerBase(string serverUrl, int pollingInterval = 5, int threadCount = 2, string workerId = "", string domain = "")
        {
            ServerUrl = serverUrl;
            PollingInterval = pollingInterval;
            ThreadCount = threadCount;
            WorkerId = workerId != "" ? workerId : "";
            Domain = domain != "" ? domain : "";

            Client = new RestClient(ServerUrl);
        }

        /// <summary>
        /// Method that spawns ThreadCount number of threads 
        /// and runs PollAndExecute method for each of thread.
        /// </summary>
        public void Start(string taskType)
        {
            for(int iterator = 0; iterator < ThreadCount; iterator++)
            {
                Thread thread = new Thread(new ThreadStart(()=> PollAndExecute(taskType)));
                thread.Start();

                LogManager.GetCurrentClassLogger().Info("Starting new thread with ID: " + thread.ManagedThreadId);
            }
        }

        /// <summary>
        /// In this function you can process polled task.
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public abstract ConductorTask ExecuteTask(ConductorTask task);

        // In case if someone want to return tuple instead updated task...
        //public abstract (ConductorTaskStatus, String, String) ExecuteTask(ConductorTask task);

        /// <summary>
        /// Method that runs poll - ack - update cycle in a single thread.
        /// </summary>
        /// <param name="taskType">Task that should be processed ex. "task_1". </param>
        /// <param name="workerId">ID of a current worker. Use it if task that you want to poll has workerId assigned.</param>
        /// <param name="domain"></param>
        public void PollAndExecute(string taskType)
        {
            LogManager.GetCurrentClassLogger().Info("Starting poll and execute");

            while (IsRunning)
            {                
                Thread.Sleep(PollingInterval);
                ConductorTask task = PollForSingleTask(taskType, WorkerId, Domain);

                if (task != null)
                {
                    LogManager.GetCurrentClassLogger().Info("Polled new task: " + JsonConvert.SerializeObject(task));

                    AckTask(task.TaskId, WorkerId);
                    ConductorTask processedTask = ExecuteTask(task);
                    UpdateTask(processedTask);
                    // In case if someone want to return tuple instead updated task...
                    // (ConductorTaskStatus status, String output, String logs) = ExecuteTask(task);
                    // UpdateTask(task, status, output);             

                    // remove object after processing
                    processedTask = null;
                }
            }            
        }

        /// <summary>
        /// Update task status. Use it if task has already set status, output and logs.
        /// </summary>
        /// <param name="task">Instance of conductor task. </param>
        /// <returns></returns>
        public string UpdateTask(ConductorTask task)
        {
            var request = new RestRequest($"tasks/", Method.POST);

            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "application/json");            
            
            request.AddParameter("application/json; charset=utf-8", JsonConvert.SerializeObject(task), ParameterType.RequestBody);

            var queryResult = Client.Execute(request);

            LogManager.GetCurrentClassLogger().Info("Updating task with ID: " + task.TaskId.ToString() + " . Result of update: " + queryResult.Content);

            return queryResult.Content;
        }

        /// <summary>
        /// Update task status, output and logs. Check ConductorTaskStatus to see available statuses.
        /// </summary>
        /// <param name="task">Instance of conductor task. </param>
        /// <param name="status">Status of conductor task. </param>
        /// <param name="output">Here you can pass parameters that task returned. </param>
        /// <returns></returns>
        public string UpdateTask(ConductorTask task, ConductorTaskStatus status, Dictionary<string, dynamic> output)
        {
            var request = new RestRequest($"tasks/", Method.POST);

            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "application/json");

            task.Status = status;
            task.Output = output;

            request.AddJsonBody(task);

            var queryResult = Client.Execute(request);

            LogManager.GetCurrentClassLogger().Info("Updating task with ID: " + task.TaskId.ToString() + " . Result of update: " + queryResult.Content);

            return queryResult.Content;
        }

        /// <summary>
        /// Ack - Task is recieved.
        /// </summary>
        /// <param name="taskId">ID of a task that we want to ACK. </param>
        /// <param name="workerId">ID of a current worker. </param>
        /// <returns></returns>
        public string AckTask(Guid taskId, string workerId = "")
        {
            var request = new RestRequest($"tasks/{taskId}/ack", Method.POST);

            request.AddHeader("Accept", "application/json");

            if (workerId != "")
                request.AddParameter("workerid", workerId);            

            var queryResult = Client.Execute(request);

            LogManager.GetCurrentClassLogger().Info("Sending ACK for task ID: " + taskId.ToString() + " . Result of ACK: " + queryResult.Content);

            return queryResult.Content;
        }

        /// <summary>
        /// Used to pull single task. If you want to batch pull task check method PollForMultipleTasks.
        /// </summary>
        /// <param name="taskType">Task that should be processed ex. "task_1". </param>
        /// <param name="workerId">ID of a current worker. Use it if task that you want to poll has workerId assigned.</param>
        /// <param name="domain"></param>
        /// <returns>
        /// Null if no task found or ConductorTask object if any
        /// </returns>
        public ConductorTask PollForSingleTask(string taskType, string workerId = "", string domain = "")
        {
            var request = new RestRequest($"tasks/poll/{taskType}", Method.GET);

            request.AddHeader("Accept", "application/json");

            if (workerId != "")
                request.AddParameter("workerid", workerId);

            if (domain != "")
                request.AddParameter("domain", domain);

            var queryResult = Client.Execute(request);

            if (queryResult.Content == "")
                return null;
            else
                return JsonConvert.DeserializeObject<ConductorTask>(queryResult.Content);
        }

        /// <summary>
        /// Used to poll multiple tasks. 
        /// </summary>
        /// <param name="taskType">Task that should be processed ex. "task_1". </param>
        /// <param name="count">How many task we should poll. </param>
        /// <param name="timeout">Simple timeout. </param>
        /// <param name="workerId">ID of a current worker. Use it if task that you want to poll has workerId assigned.</param>
        /// <param name="domain"></param>
        /// <returns></returns>
        public List<ConductorTask> PollForMultipleTasks(string taskType, int count = 10, int timeout = 120, string workerId = "", string domain = "")
        {
            var request = new RestRequest($"tasks/poll/batch/{taskType}", Method.GET);

            request.AddHeader("Accept", "application/json");
            request.AddParameter("count", count);
            request.AddParameter("timeout", timeout);

            if (workerId != "")
                request.AddParameter("workerid", workerId);

            if (domain != "")
                request.AddParameter("domain", domain);

            var queryResult = Client.Execute(request);

            if (queryResult.Content == "")
                return null;
            else
                return JsonConvert.DeserializeObject<List<ConductorTask>>(queryResult.Content);

        }        
       
    }

}
