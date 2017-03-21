
[Source](https://www.codeproject.com/articles/1176173/threading-under-the-hood "Permalink to Threading - Under the Hood")

# Threading - Under the Hood

A Threading Implementation is simply a way to create threads - add parallelism and concurrency to applications.

All the research and analysis provided in this article is proven programmatically and the source code is provided. The results are certainly interesting and useful for threading intensive applications.

## Queuing

None of the Threading Implementations create threads immediately. Requests for threads are queued and the .NET Runtime decides when to create the threads. The first few threads are created nearly immediately but the speed at which subsequent threads are created depends on the Threading Implementation.

## Explicit vs. Implicit Threading

.NET offers various Threading Implementations that can be categorized into two Threading Categories:

* Explicit Threading
    * Threads are created explicitly: new Thread(new ThreadStart(Work)).Start();
    * All threads are created nearly immediately.
    * Thousands of threads can be queued.
* Implicit Threading
    * Threads are created implicitly: After a task is queued, the thread is created automatically in the background.
    * Only the first few threads are created nearly immediately.
    * Millions of tasks can be queued.

## 4 Threading Implementations

There are 4 Threading Implementations, 4 ways to create threads - add parallelism and concurrency to applications. The following are the 4 Threading Implementations:
* Asynchronous Invoke
* Explicit Threading
* Task Parallel Library (TPL)
* Thread Pool

## Asynchronous Invoke

Asynchronous Invoke is in the Implicit Threading Category. Following is basic sample code:
    
    
    void CreateThread_Via_AsynchronousInvoke()
    {
         new Delegate_SimulateWork(SimulateWork).BeginInvoke(null, null);
    }
    delegate void Delegate_SimulateWork();
    void SimulateWork()
    {
         Thread.Sleep(1000);
    }

## Explicit Threading

There is only one Threading Implementation in the Explicit Threading Category, and this Threading Implementation is also called: Explicit Threading. Following is basic sample code:
    
    
    void CreateThread_Via_ExplicitThreading()
    {
         new Thread(new ThreadStart(SimulateWork)).Start();
    }
    void SimulateWork()
    {
         Thread.Sleep(1000);
    }

## Task Parallel Library (TPL)

Task Parallel Library (TPL) is in the Implicit Threading Category. Following is basic sample code:
    
    
    void CreateThread_Via_TaskParallelLibrary()
    {
        Task.Factory.StartNew(SimulateWork);
    }
    void SimulateWork()
    {
        Thread.Sleep(1000);
    }

## Thread Pool

Thread Pool is in the Implicit Threading Category. Following is basic sample code:
    
    
    void CreateThread_Via_ThreadPool()
    {
        ThreadPool.QueueUserWorkItem(SimulateWork);
    }
    void SimulateWork(object state)
    {
        Thread.Sleep(1000);
    }

## Threading Implementation Analysis Software

This software analyses Threading Implementations:

* Determines Max &amp; Safe Queue Limits
* Tests the speed at which Worker Threads are created
* Tests the number of Worker Threads that can be created
* Compares the Threading Implementations

The software is easy to use as buttons are only enabled at the right time; however, further details on the software follows after this screenshot.

![][1]

The following is a brief description of each of the controls in the above screenshot:

* Buttons
    * 4 Buttons at the top left: these initiate analysis of the 4 Threading Implementations.
    * Stop Analysis: this stops any analysis that may be underway.
    * Update Threading Implementation Comparison: updates the data grid at the bottom with the analysis results.
* Chosen Queue Limit: the software determines the Max Queue Limit of a Threading Implementation based on resource consumption; however, if the Chosen Queue Limit is hit before resources run out, then the Chosen Queue Limit will be used as the Max Queue Limit.
* Labels:
    * Analysis Phase: the phase of the analysis that is underway.
    * Threading Implementation: the Threading Implementation that is being analysed.
    * Max Queue Limit: the maximum number of tasks or threads that can be queued before the application throws an Out Of Memory Exception.
    * Safe Queue Limit: the Safe Queue Limit is 90% of the Max Queue Limit; however, if the Chosen Queue Limit was used as the Max Queue Limit, then the Safe Queue Limit is 100% of the Max Queue Limit.
    * Tasks Or Threads Queued: the number of Tasks Or Threads Queued as the analysis progresses.
    * Active Worker Thread Count: the number of threads actively simulating work as the analysis progresses.
    * Thread Count: the total number of threads in the application process.
    * Analysis Phase Start Time: the time when the current Analysis Phase started.
    * Analysis Phase Duration: the duration of the current Analysis Phase.
    * Average Worker Spawn Time (ms): the average time in milliseconds that it takes to spawn a worker thread.
    * Workers Spawned In First Second: the number of worker threads spawned in the first second after starting to queue Tasks Or Threads.
    * Process Memory Utilization (MB) : the amount of memory the application process is using.
* Data Grid Columns:
    * Safe Limit: same as the 'Safe Queue Limit' label.
    * Workers: same as the 'Active Worker Thread Count' label.
    * Ave. Spawn Time: same as the 'Average Worker Spawn Time (ms)' label.
    * Workers In 1 Sec: same as the 'Workers Spawned In First Second' label.
    * Memory Used: same as the 'Process Memory Utilization (MB)' label.

## Threading Analysis Phases

* Idle: no analysis is taking place.
* Determine Max Queued Tasks Or Threads: determine the maximum number of tasks or threads that can be queued for a Threading Implementation.
* Wait For Queue To Clear: wait for the queue of tasks or threads to be cleared before continuing to the next phase.
* Analyse Thread Creation: tests how many worker threads can be spawned and how long it takes.
* Analysis Complete: analysis is complete and the user can update the Threading Implementation Comparison data grid.

## Implicit Threading Analysis Does Not Complete

Unless a lower Queue Limit is chosen, millions of tasks are queued when analysing Implicit Threading Implementations and today's hardware generally can't run millions of concurrent threads; as such, Implicit Threading Analysis generally does not reach the Analysis Complete phase.&nbsp;The user can watch the average time to create a thread and the number of threads created. There comes a time when thread creation practically stops or becomes too slow, and this represents the limitations of the Threading Implementation being analysed. At this time the Threading Implementation Comparison data grid can be updated and the analysis can be stopped.

## Threading Implementation Comparison

The data in the Threading Implementation Comparison data grid is the point of this whole exercise. From this data we can see the following strengths:

* Explicit Threading
    * Spawns more worker threads
    * Spawns threads much faster
* Implicit Threading
    * Allows millions of tasks to be queued

The following chart visualizes the Threading Implementation Comparison data. To visualize the data on the same chart, it is necessary to divide the Safe Queue Limit by 10,000 to ensure all the values are in the same numeric range (0-3000).

Note: results will vary based on Computer/Server Specs; however, the strengths/weaknesses of the various Threading Implementations should remain constant.

![][2]

## Executing The Code Inside Visual Studio vs. Outside

It is interesting to note the following improvements when executing the code outside of Visual Studio:

* Double the Memory Availability for the application and as a result:
    * Explicit Threading
        * Double the Queue Limit
        * Spawns double the number of worker threads.
    * Implicit Threading:
        * Double the Queue Limits
        * Strangely, there is no real improvement in the number of worker threads spawned.
* Performance:
    * Explicit Threading: spawns worker threads 6X faster
    * Implicit Threading: strangely, no real improvement

## Code In Article

All the code is very well documented, so it should be easy to find your way. Only the most important/interesting code will be shown in this article.

### Simulate Work

The 4 Threading Implementations use the following code to simulate work.
    
    
    
    delegate void Delegate_SimulateWork();
    
    void SimulateWork(object state)
    {
        SimulateWork();
    }
    
    void SimulateWork()
    {
        
        
        Interlocked.Increment(ref activeWorkerThreadCount);
    
        
        
        while (workerThreadsContinueSimulatingWork)
            Thread.Sleep(100);
    
        Interlocked.Decrement(ref activeWorkerThreadCount);
        Interlocked.Decrement(ref tasksOrThreads_queued);
    }

### Queue Task Or Thread

The following code queues tasks or threads according to the Threading Implementation being analysed.
    
    
    void QueueTask_or_thread()
    {
        switch (threadingImplementationToAnalyze)
        {
            case Constants.ThreadingImplementation.AsynchronousInvoke:
                new Delegate_SimulateWork(SimulateWork).BeginInvoke(null, null);
                break;
            case Constants.ThreadingImplementation.ExplicitThreading:
                new Thread(new ThreadStart(SimulateWork)).Start();
                break;
            case Constants.ThreadingImplementation.TaskParallelLibrary:
                Task.Factory.StartNew(SimulateWork);
                break;
            case Constants.ThreadingImplementation.ThreadPool:
                ThreadPool.QueueUserWorkItem(SimulateWork);
                break;
        }
    }

### Determine Max Queued Tasks Or Threads

This code is used to determine the maximum number of tasks or threads that can be queued for a Threading Implementation.
    
    
    try
    {
        while (true)
        {
            
            if (!applicationIsInAnalysisMode)
                return;
    
            
            
            
            
            if (threadingImplementationToAnalyze != Constants.ThreadingImplementation.ExplicitThreading)
            {
                if (tasksOrThreads_queued % 100000 == 0)
                {
                    try
                    {
                        new System.Runtime.MemoryFailPoint(100);
                    }
                    catch (Exception)
                    {
                        break;
                    }
                }
            }
    
            
            
            
            if (tasksOrThreads_queued == chosenQueueLimit)
            {
                break;
            }
    
            QueueTask_or_thread();
    
            
            tasksOrThreads_queued++;
            CountWorkerThreadsSpawnedInFirstSecond();
        }
    }
    catch (Exception)
    {
        
        
        
    }

### Determine Safe Queue Limit

Here the Safe Queue Limit is determined based on the Max Queue Limit and the Chosen Queue Limit.
    
    
    maxQueueLimit = tasksOrThreads_queued;
    if (maxQueueLimit == chosenQueueLimit)
    {
        
        
        safeQueueLimit = chosenQueueLimit;
    }
    else
    {
        
        
        
        safeQueueLimit = (int)((double)maxQueueLimit * .90);
    }

### Analyze Thread Creation

Now that the Safe Queue Limit is known, the application now queues tasks or threads until it reaches the Safe Queue Limit. The user can then see how many worker threads are spawned and how long it takes.
    
    
    workerThreadsContinueSimulatingWork=true;
    for (tasksOrThreads_queued = 0; tasksOrThreads_queued &lt; safeQueueLimit; tasksOrThreads_queued++)
    {
        
        if (!applicationIsInAnalysisMode)
            return;
    
        QueueTask_or_thread();
    }

## See Something - Say Something

The goal is to have clear, error free content and your help in this regard is much appreciated. Be sure to comment if you see an error or potential improvement. All feedback is welcome.

## Summary

This article has taken an in depth look 'under the hood' of .NET threading. Though it is by no means fully comprehensive, it highlights major differences in performance and scalability between the various .NET Threading Implementations. Hopefully this article and software will be helpful next time you develop a threading intensive application and need to choose a Threading Implementation.

[1]: https://www.codeproject.com/KB/threads/1176173/image001.png
[2]: https://www.codeproject.com/KB/threads/1176173/Chart4.png