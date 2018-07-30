using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace ConductorWorker.Base
{
    /// <summary>
    /// Auxiliary enumerator that allow to set task status easily
    /// </summary>
    public enum ConductorTaskStatus
    {
        IN_PROGRESS,
        CANCELED,
        FAILED,
        COMPLETED,
        SCHEDULED,
        TIMED_OUT,
        READY_FOR_RERUN,
        SKIPPED
    }

    /// <summary>
    /// Task implementation used to conversion beetween JSON string and C# object
    /// </summary>
    public class ConductorTask
    {
        [JsonProperty("taskType")]
        public string TaskType { get; set; }

        [JsonProperty("status")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ConductorTaskStatus Status { get; set; }

        [JsonProperty("inputData")]
        public Dictionary<string, dynamic> InputData { get; set; }

        [JsonProperty("referenceTaskName")]
        public string ReferenceTaskName { get; set; }

        [JsonProperty("retryCount")]
        public long RetryCount { get; set; }

        [JsonProperty("seq")]
        public long Seq { get; set; }

        [JsonProperty("pollCount")]
        public long PollCount { get; set; }

        [JsonProperty("taskDefName")]
        public string TaskDefName { get; set; }

        [JsonProperty("scheduledTime")]
        public long ScheduledTime { get; set; }

        [JsonProperty("startTime")]
        public long StartTime { get; set; }

        [JsonProperty("endTime")]
        public long EndTime { get; set; }

        [JsonProperty("updateTime")]
        public long UpdateTime { get; set; }

        [JsonProperty("startDelayInSeconds")]
        public long StartDelayInSeconds { get; set; }

        [JsonProperty("retried")]
        public bool Retried { get; set; }

        [JsonProperty("executed")]
        public bool Executed { get; set; }

        [JsonProperty("callbackFromWorker")]
        public bool CallbackFromWorker { get; set; }

        [JsonProperty("responseTimeoutSeconds")]
        public long ResponseTimeoutSeconds { get; set; }

        [JsonProperty("workflowInstanceId")]
        public Guid WorkflowInstanceId { get; set; }

        [JsonProperty("workflowType")]
        public string WorkflowType { get; set; }

        [JsonProperty("taskId")]
        public Guid TaskId { get; set; }

        [JsonProperty("callbackAfterSeconds")]
        public long CallbackAfterSeconds { get; set; }

        [JsonProperty("workflowTask")]
        public dynamic WorkflowTask { get; set; }

        [JsonProperty("taskStatus")]
        public string ConductorTaskStatus { get; set; }

        [JsonProperty("queueWaitTime")]
        public long QueueWaitTime { get; set; }

        [JsonProperty("outputData")]
        public Dictionary<string, dynamic> Output { get; set; } = new Dictionary<string, dynamic>();

        [JsonProperty("logs")]
        public List<ConductorLog> Logs { get; set; } = new List<ConductorLog>();        

        /// <summary>
        /// This method is used to make adding logs for task easy
        /// </summary>
        /// <param name="message"></param>
        public void AddLog(string message)
        {           
            ConductorLog log = new ConductorLog()
            {
                Message = message,
                TaskId = this.TaskId,
                CreatedTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString()
            };            

            this.Logs.Add(log);
        }
       
    }    

    /// <summary>
    /// Conductor log implementation
    /// </summary>
    public class ConductorLog
    {
        [JsonProperty("log")]
        public string Message { get; set; }

        [JsonProperty("taskId")]
        public Guid TaskId { get; set; }

        [JsonProperty("createdTime")]
        public string CreatedTime { get; set; }
    }

}
