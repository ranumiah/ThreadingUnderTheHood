using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreadingUnderTheHood
{
    class Constants
    {
        /// <summary>
        /// This thread threshold is one factor in determining whether it is safe to start a new phase of thread analysis.
        /// The total number of threads in the current process must be under this threshold before it is considered safe to continue analysis.
        /// </summary>
        public const int ActiveThreadCountThreshold = 50;

        /// <summary>
        /// Phases of threading analysis.
        /// </summary>
        public enum AnalysisPhase
        {
            Idle,
            DetermineMaxQueuedTasksOrThreads,
            WaitForQueueToClear,
            AnalyzeThreadCreation,
            AnalysisComplete
        }

        /// <summary>
        /// The types of .Net Threading Implementations.
        /// </summary>
        public enum ThreadingImplementation
        {
            AsynchronousInvoke,
            ExplicitThreading,
            TaskParallelLibrary,
            ThreadPool
        }
    }
}