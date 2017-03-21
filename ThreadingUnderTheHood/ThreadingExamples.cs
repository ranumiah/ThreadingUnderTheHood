using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
namespace ThreadingUnderTheHood
{
    class ThreadingExamples
    {
        #region Asynchronous Invoke
        void CreateThread_Via_AsynchronousInvoke()
        {
            new Delegate_SimulateWork(SimulateWork).BeginInvoke(null, null);
        }
        #endregion

        #region Explicit Threading
        void CreateThread_Via_ExplicitThreading()
        {
            new Thread(new ThreadStart(SimulateWork)).Start();
        }
        #endregion

        #region Simulate Work
        delegate void Delegate_SimulateWork();
        void SimulateWork()
        {
            Thread.Sleep(1000);
        }
        void SimulateWork(object state)
        {
            Thread.Sleep(1000);
        }
        #endregion

        #region Task Parallel Library
        void CreateThread_Via_TaskParallelLibrary()
        {
            Task.Factory.StartNew(SimulateWork);
        }
        #endregion

        #region Thread Pool
        void CreateThread_Via_ThreadPool()
        {
            ThreadPool.QueueUserWorkItem(SimulateWork);
        }
        #endregion
    }
}
