# Implementation of conductor worker

This library is simple implementation of [worker](https://netflix.github.io/conductor/worker/) for Netflix [Conductor](https://github.com/Netflix/conductor) orchestration engine.
It is written in C# with usage of .netcore and [RestSharp](http://restsharp.org/).

## Introduction

This worker is __NOT__ a full implementation of conductor client. It allows only to process tasks from conductor queue.
Examples of full client we can find on a conductor official [github repository](https://github.com/Netflix/conductor/tree/master/client).

For more info how conductor works please visit [this page](https://netflix.github.io/conductor/).

**Feel free to contribute!**

## Usage

### Creating own worker

Before we can start using our library we have to definie worker that could process tasks from queue.
Each worker must implement `ConductorWorkerBase` class and override its `ExecuteTask` function.
This class is responsible for spawning specified in constructor 
number of threads, creating of Rest Client and execution of task.

`ExecuteTask` function should always return updated task.

Sample implementation of worker you can see below:

```csharp
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
            LogManager.GetCurrentClassLogger().Info("Executing sample task");

            task.Status = ConductorTaskStatus.COMPLETED;

            return task;
        }
    }
}
```

This implementation is just logging execution of a provided task and marking it as `COMPLETED`. 
It is important to always set `task.Status`. Available statues of a task:
```csharp
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
```

### OK, I've got my worker but how to run it?

Just create your worker instance and invoke `.Start()` method with task name that should be processed by this worker.

```csharp
using ConductorWorker.Workers;

...

String server = "http://localhost:8080/api";

SampleWorker worker = new SampleWorker(server);
worker.Start("name_of_a_task");
```

If you want to stop execution of a worker just set `.IsRunning` variable value to false.

```csharp
sampleWorker.IsRunning = false;
```

This will prevent worker from creating new threads and polling new tasks (tasks that are currently processed won't be stoped).

### Task implementation

Task implementation is compatible with definitions provided on official conductor page.
Each task has following fields:

```csharp
public string TaskType { get; set; }
public ConductorTaskStatus Status { get; set; }
public Dictionary<string, dynamic> InputData { get; set; }
public string ReferenceTaskName { get; set; }
public long RetryCount { get; set; }
public long Seq { get; set; }
public long PollCount { get; set; }
public string TaskDefName { get; set; }
public long ScheduledTime { get; set; }
public long StartTime { get; set; }
public long EndTime { get; set; }
public long UpdateTime { get; set; }
public long StartDelayInSeconds { get; set; }
public bool Retried { get; set; }
public bool Executed { get; set; }
public bool CallbackFromWorker { get; set; }
public long ResponseTimeoutSeconds { get; set; }
public Guid WorkflowInstanceId { get; set; }
public string WorkflowType { get; set; }
public Guid TaskId { get; set; }
public long CallbackAfterSeconds { get; set; }
public dynamic WorkflowTask { get; set; }
public string ConductorTaskStatus { get; set; }
public long QueueWaitTime { get; set; }
public Dictionary<string, dynamic> Output { get; set; } = new Dictionary<string, dynamic>();
public List<ConductorLog> Logs { get; set; } = new List<ConductorLog>();        
```

For more info please check source code (class `ConductorTask`).

### Reading input and output values of a task

Because of fact that input and output from a given task is always dictionary with a string key and 
dynamic value it may be difficult to get data from such thing. Here you have an example how to get value from InputData task property:

```csharp
task.Output.TryGetValue("FirstKey", out dynamic firstKey);
task.InputData.TryGetValue("ResultOfSmth", out dynamic resultOfSmth);

if (firstKey == null)
{
    ...
}
```

### Configuration options

Each worker has some configuration options that could be set using Worker constructor:

```csharp
<param name="serverUrl">Url of conductor server, ex. "http://localhost:8080/api".</param>        
<param name="pollingInterval">How much time we should wait before next polling.</param>
<param name="threadCount">How many threads our worker should use.</param>
<param name="workerId">ID of a current worker.</param>
```

## License

This project is licensed under the terms of the MIT license.