using ConductorWorker.Base;
using NLog;

namespace ConductorWorker.Workers
{
    class SampleWorker : ConductorWorkerBase
    {
        public SampleWorker(string serverUrl, int pollingInterval = 5, int threadCount = 2, string workerId = "", string domain = "") 
            : base(serverUrl, pollingInterval, threadCount, workerId, domain) { }

        public override ConductorTask ExecuteTask(ConductorTask task)
        {
            LogManager.GetCurrentClassLogger().Info("Running sample worker - executing a task");

            task.Status = ConductorTaskStatus.COMPLETED;
            task.Output.Add("result", "Nothing to return");
            task.AddLog("Nothing to do");

            return task;
        }
    }
}
