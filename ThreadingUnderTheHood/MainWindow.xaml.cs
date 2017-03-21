using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;

namespace ThreadingUnderTheHood
{
    public partial class MainWindow : Window
    {
        #region Application Idle
        /// <summary>
        /// Application is considered idle during the following phases:
        /// Constants.AnalysisPhase.Idle, 
        /// Constants.AnalysisPhase.WaitForQueueToClear_and_mostWorkerThreadsToBeTerminated, 
        /// Constants.AnalysisPhase.AnalysisComplete
        /// </summary>
        bool ApplicationIdle
        {
            get
            {
                switch (analysisPhase)
                {
                    case Constants.AnalysisPhase.Idle:
                    case Constants.AnalysisPhase.WaitForQueueToClear:
                    case Constants.AnalysisPhase.AnalysisComplete:
                        return true;
                }
                return false;
            }
        }
        #endregion

        #region Background Work
        System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
        /// <summary>
        /// Initiate work to be done in the background.
        /// </summary>
        void InitiateBackgroundWork()
        {
            dispatcherTimer.Tick += BackgroundWork;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
        }
        /// <summary>
        /// Performs background work.
        /// </summary>
        void BackgroundWork(object sender, EventArgs e)
        {
            UpdateUserInterface();
            CollectGarbageWhenIdle();
        }
        /// <summary>
        /// Calls the Garbage Collector to free up memory when the application is idle.
        /// </summary>
        void CollectGarbageWhenIdle()
        {
            if (ApplicationIdle)
            {
                Process currentProcess = Process.GetCurrentProcess();
                if (Utilities.MemoryUtilization_inMegaBytes(currentProcess) > 100)
                {
                    //This application does not need to use more than 100MB of memory if it is idle.
                    //Furthermore, there is no harm in continually searching for memory to clear when the application is idle.
                    GC.Collect();
                }
            }
        }
        #endregion

        #region Chosen Queue Limit
        /// <summary>
        /// The Chosen Queue Limit is a hard limit on the number of tasks that can be queued or the number of worker threads that can be created.
        /// </summary>

        private void cmbChosenQueueLimit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbChosenQueueLimit.SelectedItem is ComboBoxItem)
            {
                chosenQueueLimit = (int)((ComboBoxItem)cmbChosenQueueLimit.SelectedItem).Tag;
            }
        }

        /// <summary>
        /// Populate the Chosen Queue Limit ComboBox, and select a default value.
        /// </summary>
        void PopulateChosenQueueLimitComboBox()
        {
            //Prepare a list of values with which to populate the ComboBox
            List<int> lstComboBoxValues = new List<int>();
            lstComboBoxValues.AddRange(new int[] { 1000, 10000 });
            for (int i = 1; i < 10; i++)
                lstComboBoxValues.Add(i * 100000);
            for (int i = 1; i <= 100; i++)
                lstComboBoxValues.Add(i * 1000000);

            //Populate the ComboBox
            foreach (int comboBoxValue in lstComboBoxValues)
                cmbChosenQueueLimit.Items.Add(new ComboBoxItem() { Content = string.Format("{0:n0}", comboBoxValue), Tag = comboBoxValue });

            //Select a default value
            cmbChosenQueueLimit.SelectedIndex = 20;
        }
        #endregion

        #region Configure Application Shutdown
        /// <summary>
        /// Configures work that needs to be done when the application shuts down, so that the application can shut down successfully.
        /// </summary>
        void ConfigureApplicationShutdown()
        {
            Closing += MainWindow_Closing;
        }
        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Set flags instructing worker threads to stop working.
            //The worker threads will terminate automatically shortly after they stopped working.
            workerThreadsContinueSimulatingWork=false;
            applicationIsInAnalysisMode = false;
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Executes startup/constructor code.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            InitiateBackgroundWork();
            InitiateThreadingImplementationComparison();
            ConfigureApplicationShutdown();
            PopulateChosenQueueLimitComboBox();
            UpdateUserInterface();
        }
        #endregion

        #region Count Worker Threads Spawned In First Second
        /// <summary>
        /// Counts the number of worker threads spawned in the first second after tasks or threads started being queued.
        /// </summary>
        void CountWorkerThreadsSpawnedInFirstSecond()
        {
            if (workerThreadsSpawnedInFirstSecond == 0 && (DateTime.Now - analysisPhase_startDateTime).Seconds >= 1)
            {
                workerThreadsSpawnedInFirstSecond = activeWorkerThreadCount;
            }
        }
        #endregion

        #region Enable/Disable The Appropriate Controls
        /// <summary>
        /// Enables/disables controls based on whether the application is in Analysis Mode. This guides the user to use controls at the appropriate time.
        /// </summary>
        void EnableDisableTheAppropriateControls()
        {
            btnAsynchronousInvoke.IsEnabled = !applicationIsInAnalysisMode;
            btnExplicitThreading.IsEnabled = !applicationIsInAnalysisMode;
            btnTaskParallelLibrary.IsEnabled = !applicationIsInAnalysisMode;
            btnThreadPool.IsEnabled = !applicationIsInAnalysisMode;
            cmbChosenQueueLimit.IsEnabled = !applicationIsInAnalysisMode;

            btnStopAnalysis.IsEnabled = applicationIsInAnalysisMode;
        }
        #endregion

        #region Execute Threading Analysis
        /// <summary>
        /// Initiates the threading analysis.
        /// </summary>
        /// <param name="threadingImplementationToAnalyze">The threading implementation to analyze.</param>
        void InitiateThreadingAnalysis(Constants.ThreadingImplementation threadingImplementationToAnalyze)
        {
            applicationIsInAnalysisMode=true;
            EnableDisableTheAppropriateControls();
            this.threadingImplementationToAnalyze = threadingImplementationToAnalyze;

            //Count the non worker threads before worker threads are spawned.
            if (nonWorkerThreadCount == 0)
            {
                nonWorkerThreadCount = Process.GetCurrentProcess().Threads.Count - activeWorkerThreadCount;
            }

            //Initiate the threading analysis.
            //The threading analysis must be executed on a background thread so that the user interface is not blocked
            new Thread(new ThreadStart(ExecuteThreadingAnalysis)).Start();
        }
       
        /// <summary>
        /// Executes the threading analysis.
        /// </summary>
        void ExecuteThreadingAnalysis()
        {
            //Configure variables before starting with analysis
            workerThreadsContinueSimulatingWork=true;
            Set_AnalysisPhase(Constants.AnalysisPhase.DetermineMaxQueuedTasksOrThreads);

            #region Determine Max Queued Tasks Or Threads
            try
            {
                while (true)
                {
                    //Exit Point: Analysis will exit here if the Stop Analysis button is clicked
                    if (!applicationIsInAnalysisMode)
                        return;

                    //Check for a Memory Fail Point.
                    //This is the point where too much memory is used and an OutOrMemory Exception is thrown.
                    //A Memory Fail Point indicates the: Max Queued Tasks Or Threads
                    //This check works for all Threading Implementations except for Explicit Threading
                    if (threadingImplementationToAnalyze != Constants.ThreadingImplementation.ExplicitThreading)
                    {
                        if (tasksOrThreads_queued % 100000 == 0)//Performance Optimization
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

                    //Chosen Queue Limit (chosen by the user):
                    //If the System Resources does not first limit the: Max Queued Tasks Or Threads,
                    //Then the Chosen Queue Limit, is the Max Queued Tasks Or Threads.
                    if (tasksOrThreads_queued == chosenQueueLimit)
                    {
                        break;
                    }

                    QueueTask_or_thread();

                    //The tasksOrThreads_queued variable is used to count the: Max Queued Tasks Or Threads
                    tasksOrThreads_queued++;
                    CountWorkerThreadsSpawnedInFirstSecond();
                }
            }
            catch (Exception)
            {
                //Do nothing.
                //The Explicit Threading throws an OutOfMemory Exception; however, it is expected and handled below.
                //The maximum number of explicit threads is determined by creating explicit threads until an error is thrown.
            }
            #endregion

            #region Count Worker Threads Spawned In First Second
            //It is possible that the Determine Max Queued Tasks Or Threads phase completed in less than 1 second.
            //As such, it is necessary to wait here until 1 second has passed.
            //This is necessary so that the Worker Threads Spawned In First Second can be counted.
            //Of course, if 1 second has already passed, then no time will be spent here.
            while (workerThreadsSpawnedInFirstSecond == 0)
            {
                Thread.Sleep(20);
                CountWorkerThreadsSpawnedInFirstSecond();
            }
            #endregion

            #region Determine Safe Queue Limit
            //Determine the Safe Queue Limit
            maxQueueLimit = tasksOrThreads_queued;
            if (maxQueueLimit == chosenQueueLimit)
            {
                //The system did not run out of resources.
                //As such, the Chosen Queue Limit is considered a safe limit.
                safeQueueLimit = chosenQueueLimit;
            }
            else
            {
                //The system did run out of resources.
                //As such, the Safe Queue Limit must be less than the Max Queued Tasks Or Threads.
                //Safe Queue Limit is set to 90% of the Max Queued Tasks Or Threads.
                safeQueueLimit = (int)((double)maxQueueLimit * .90);
            }
            #endregion

            #region Wait For Queue To Clear
            //The first part the analysis is complete.
            //The worker threads stop similuting work so that the worker threads can be terminated and the queue can clear.
            workerThreadsContinueSimulatingWork = false;
            GC.Collect();
            Set_AnalysisPhase(Constants.AnalysisPhase.WaitForQueueToClear);

            //The system waits here until it is safe to continue to the next phase of the analysis.
            //It is safe to continue when the queue is cleared and enough worker threads have been terminated.
            while (!SafeToContinueAnalysis)
            {
                Thread.Sleep(100);

                //Exit Point: Analysis will exit here if the Stop Analysis button is clicked
                if (!applicationIsInAnalysisMode)
                    return;
            }
            #endregion

            #region Analyze Thread Creation
            //The application now queues tasks or threads until it reaches the Safe Queue Limit.
            //The user can then see how many worker threads are spawned and how long it takes.
            Set_AnalysisPhase(Constants.AnalysisPhase.AnalyzeThreadCreation);
            workerThreadsContinueSimulatingWork=true;
            for (tasksOrThreads_queued = 0; tasksOrThreads_queued < safeQueueLimit; tasksOrThreads_queued++)
            {
                //Exit Point: Analysis will exit here if the Stop Analysis button is clicked
                if (!applicationIsInAnalysisMode)
                    return;

                QueueTask_or_thread();
            }

            //The application waits here until the Active Worker count reaches the Safe Queue Limit.
            //Generally, only the Explicit Threading Implementation gets past this point.
            //The other Threading Implementations can queue large numbers of tasks or threads,
            //but the application can't possible have that many Active Workers.
            //It is not a problem if the application does not get past this point.
            //The user can simply update the datagrid with the results and stop the analysis.
            while (activeWorkerThreadCount < safeQueueLimit)
            {
                Thread.Sleep(500);

                //Exit Point: Analysis will exit here if the Stop Analysis button is clicked
                if (!applicationIsInAnalysisMode)
                    return;
            }
            #endregion

            Set_AnalysisPhase(Constants.AnalysisPhase.AnalysisComplete);
        }

        #region Button Clicks
        private void btnAsynchronousInvoke_Click(object sender, RoutedEventArgs e)
        {
            Utilities.HighlightButtons(true, btnAsynchronousInvoke);
            InitiateThreadingAnalysis(Constants.ThreadingImplementation.AsynchronousInvoke);
        }
        private void btnExplicitThreading_Click(object sender, RoutedEventArgs e)
        {
            Utilities.HighlightButtons(true, btnExplicitThreading);
            InitiateThreadingAnalysis(Constants.ThreadingImplementation.ExplicitThreading);
        }
        private void btnTaskParallelLibrary_Click(object sender, RoutedEventArgs e)
        {
            Utilities.HighlightButtons(true, btnTaskParallelLibrary);
            InitiateThreadingAnalysis(Constants.ThreadingImplementation.TaskParallelLibrary);
        }
        private void btnThreadPool_Click(object sender, RoutedEventArgs e)
        {
            Utilities.HighlightButtons(true, btnThreadPool);
            InitiateThreadingAnalysis(Constants.ThreadingImplementation.ThreadPool);
        }
        #endregion
        #endregion

        #region Queue Task Or Thread
        /// <summary>
        /// Queues a single task or thread according to the Threading Implementation being analyzed.
        /// </summary>
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
        #endregion

        #region Safe To Continue Analysis
        /// <summary>
        /// Checks whether the work is completed & resources have been freed up; only then is it considered safe to continue further analysis.
        /// </summary>
        bool SafeToContinueAnalysis
        {
            get
            {
                //First check that the Worker Threads have completed their work and that no further work is queued.
                if (activeWorkerThreadCount > 0 || tasksOrThreads_queued > 0)
                    return false;

                //Lastly check that most of the worker threads have been terminated. This last check is more resource intensive.
                return Process.GetCurrentProcess().Threads.Count < Constants.ActiveThreadCountThreshold;
            }
        }
        #endregion

        #region Set Analysis Phase
        /// <summary>
        /// Sets the analysis phase, and records the start datetime of the analysis phase and the duration of the previous analysis phase.
        /// </summary>
        /// <param name="analysisPhaseToSet">The analysis phase to set.</param>
        void Set_AnalysisPhase(Constants.AnalysisPhase analysisPhaseToSet)
        {
            this.analysisPhase = analysisPhaseToSet;
            previousAnalysisPhase_duration = DateTime.Now - analysisPhase_startDateTime;
            analysisPhase_startDateTime = DateTime.Now;
        }
        #endregion

        #region Simulate Work
        //This delegate is only used by the Asynchronous Invoke Threading Implementation.
        delegate void Delegate_SimulateWork();

        /// <summary>
        /// This overloaded function is only used by the Thread Pool Threading Implementation.
        /// </summary>
        /// <param name="state">This parameter is not used; simply set it to null.</param>
        void SimulateWork(object state)
        {
            SimulateWork();
        }

        /// <summary>
        /// This function is used by all the Threading Implementations to simulate work.
        /// </summary>
        void SimulateWork()
        {
            //This function is called millions of times by numerous threads; as such, thread safety is a concern.
            //It is necessary to use the Interlocked functionality to ensure the accuracy of these variables.
            Interlocked.Increment(ref activeWorkerThreadCount);

            while (workerThreadsContinueSimulatingWork)
                Thread.Sleep(100);

            Interlocked.Decrement(ref activeWorkerThreadCount);
            Interlocked.Decrement(ref tasksOrThreads_queued);
        }
        #endregion

        #region Stop Analysis
        /// <summary>
        /// Gracefully stops the threading analysis.
        /// </summary>
        private async void btnStopAnalysis_Click(object sender, RoutedEventArgs e)
        {
            //Instructs the Threading Analysis thread to stop working and exit.
            applicationIsInAnalysisMode=false;

            //Instructs the Worker Threads to stop simulating work and exit.
            workerThreadsContinueSimulatingWork=false;

            GC.Collect();
            btnStopAnalysis.IsEnabled = false;

            //Wait of the queue to clear and enough worker threads to be terminated
            Set_AnalysisPhase(Constants.AnalysisPhase.WaitForQueueToClear);
            while (!SafeToContinueAnalysis)
            {
                //This is supposed to be non-blocking but it blocks when there are too many threads.
                await Task.Delay(500);
            }

            //Reset the Application State back to its initial state before the Threading Analysis work.
            maxQueueLimit = 0;
            safeQueueLimit = 0;
            workerThreadsSpawnedInFirstSecond = 0;
            Utilities.HighlightButtons(false, btnAsynchronousInvoke, btnExplicitThreading, btnTaskParallelLibrary, btnThreadPool);
            EnableDisableTheAppropriateControls();
            Set_AnalysisPhase(Constants.AnalysisPhase.Idle);
        }
        #endregion

        #region Threading Implementation Comparison
        /// <summary>
        /// This region populates and updates the datagrid that compares the Threading Implementations
        /// </summary>
        
        //Stores the data of the datagrid
        List<AnalysisResult> lstAnalysisResult = new List<AnalysisResult>();

        /// <summary>
        /// Populates the datagrid with a blank record for each Threading Implementations.
        /// Each record is assigned only the name of the Threading Implementations.
        /// </summary>
        void InitiateThreadingImplementationComparison()
        {
            foreach (Constants.ThreadingImplementation threadingImplementation in Enum.GetValues(typeof(Constants.ThreadingImplementation)))
            {
                lstAnalysisResult.Add(new AnalysisResult {ThreadingImplementation=Utilities.EnumToTitle(threadingImplementation) });
            }
            dgCompareThreadingImplementations.ItemsSource = lstAnalysisResult;
        }

        /// <summary>
        /// Updates the record that corresponds with the current Threading Implementation.
        /// Updates the record with the results of the Threading Analysis.
        /// </summary>
        private void btnUpdateThreadingImplementationComparison_Click(object sender, RoutedEventArgs e)
        {
            AnalysisResult a = lstAnalysisResult[(int)threadingImplementationToAnalyze];
            a.SafeQueueLimit = lblSafeQueueLimit.Content.ToString();
            a.WorkerCount = lblActiveWorkerThreadCount.Content.ToString();
            a.AverageWorkerSpawnTime = lblAverageWorkerThreadSpawnTime.Content.ToString();
            a.WorkersSpawnedInFirstSecond = lblWorkerThreadsSpawnedInFirstSecond.Content.ToString();
            a.MemoryUtilization = lblProcessMemoryUtilization.Content.ToString();

            dgCompareThreadingImplementations.Items.Refresh();
        }

        /// <summary>
        /// Datagrid item
        /// </summary>
        class AnalysisResult
        {
            public string ThreadingImplementation { get; set; }
            public string SafeQueueLimit { get; set; }
            public string WorkerCount { get; set; }
            public string AverageWorkerSpawnTime { get; set; }
            public string WorkersSpawnedInFirstSecond { get; set; }
            public string MemoryUtilization { get; set; }
        }
        #endregion

        #region Update User Interface
        /// <summary>
        /// Updates the User Interface with the results of the Threading Analysis.
        /// </summary>
        void UpdateUserInterface()
        {
            //Labels are updated in the order in which they are displayed.
            //First the left hand labels and then the right hand labels.

            //Analysis Phase
            lblAnalysisPhase.Content = "(" + (int)analysisPhase + ") " + Utilities.EnumToTitle(analysisPhase);

            //Threading Implementation
            lblThreadingImplementation.Content =
                analysisPhase == Constants.AnalysisPhase.Idle ? "" : Utilities.EnumToTitle(threadingImplementationToAnalyze);

            //Max Queue Limit
            lblMaxQueueLimit.Content = string.Format("{0:n0}", maxQueueLimit);

            //Safe Queue Limit
            lblSafeQueueLimit.Content = string.Format("{0:n0}", safeQueueLimit);

            //Tasks Or Threads Queued
            lblTasksOrThreadsQueued.Content = string.Format("{0:n0}", tasksOrThreads_queued);

            #region Active Worker Thread Count
            Process currentProcess = Process.GetCurrentProcess();
            int threadCount = currentProcess.Threads.Count;
            if (analysisPhase == Constants.AnalysisPhase.WaitForQueueToClear)
            {
                //During the Wait For Queue To Clear phase, the Active Worker Thread Count can only be estimated.
                //The worker threads increment and immediately decrement the activeWorkerThreadCount variable.
                //So the activeWorkerThreadCount variable stays at 0.
                int estimatedActiveWorkerThreadCount = threadCount - nonWorkerThreadCount;
                if (estimatedActiveWorkerThreadCount < 0)
                    estimatedActiveWorkerThreadCount = 0;
                lblActiveWorkerThreadCount.Content = "";
                lblActiveWorkerThreadCount_title.Content = "Active Worker Thread Count (estimate): " +
                    string.Format("{0:n0}", estimatedActiveWorkerThreadCount);
            }
            else
            {
                //During the other phases, the Active Worker Thread Count is calculated accurately. 
                lblActiveWorkerThreadCount_title.Content = "Active Worker Thread Count: ";
                lblActiveWorkerThreadCount.Content = string.Format("{0:n0}", activeWorkerThreadCount);
            }
            #endregion

            //Thread Count
            lblThreadCount.Content = string.Format("{0:n0}", threadCount);

            //Note explaining how long the Wait For Queue To Clear phase will continue.
            lblNote.Content = (analysisPhase == Constants.AnalysisPhase.WaitForQueueToClear && tasksOrThreads_queued <= 0 ?
                "(Waiting for thread count to go below " + Constants.ActiveThreadCountThreshold + "; this takes 5-30 seconds)" : "");

            //Analysis Phase Start Time
            if (analysisPhase == Constants.AnalysisPhase.Idle)
                lblAnalysisPhaseStartTime.Content = "00:00:00";
            else
                lblAnalysisPhaseStartTime.Content = analysisPhase_startDateTime.ToString("HH:mm:ss");

            //Analysis Phase Duration
            TimeSpan currentAnalysisPhase_duration = DateTime.Now - analysisPhase_startDateTime;
            if (analysisPhase == Constants.AnalysisPhase.Idle)
                lblAnalysisPhaseDuration.Content = "00:00:00";
            else
                lblAnalysisPhaseDuration.Content = string.Format("{0:hh\\:mm\\:ss}", currentAnalysisPhase_duration);

            #region Average Worker Thread Spawn Time
            switch (analysisPhase)
            {
                case Constants.AnalysisPhase.Idle:
                case Constants.AnalysisPhase.WaitForQueueToClear:
                    lblAverageWorkerThreadSpawnTime.Content = 0;
                    break;
                case Constants.AnalysisPhase.DetermineMaxQueuedTasksOrThreads:
                case Constants.AnalysisPhase.AnalyzeThreadCreation:
                case Constants.AnalysisPhase.AnalysisComplete:
                    if (activeWorkerThreadCount == 0)
                    {
                        lblAverageWorkerThreadSpawnTime.Content = 0;
                    }
                    else
                    {
                        //The Average Worker Thread Spawn Time is calculated based on the duration of either of the following two phases:
                        //Determine Max Queued Tasks Or Threads
                        //Analyze Thread Creation
                        //
                        //If the application is in the Analysis Complete phase, then the Average Worker Thread Spawn Time is calculated based on the duration
                        //of the Analyze Thread Creation phase.
                        TimeSpan ts = analysisPhase == Constants.AnalysisPhase.AnalysisComplete ? previousAnalysisPhase_duration : currentAnalysisPhase_duration;
                        lblAverageWorkerThreadSpawnTime.Content = string.Format("{0:n1}", Math.Round(ts.TotalMilliseconds / (double)activeWorkerThreadCount, 1));
                    }
                    break;
            }
            #endregion

            //Worker Threads Spawned In First Second
            lblWorkerThreadsSpawnedInFirstSecond.Content = string.Format("{0:n0}", workerThreadsSpawnedInFirstSecond);

            //Process Memory Utilization
            lblProcessMemoryUtilization.Content = string.Format("{0:n0}", Utilities.MemoryUtilization_inMegaBytes(currentProcess));

            //Allow the user to update the Threading Implementation Comparison datagrid with the latest results.
            btnUpdateThreadingImplementationComparison.IsEnabled = analysisPhase == Constants.AnalysisPhase.AnalyzeThreadCreation || analysisPhase == Constants.AnalysisPhase.AnalysisComplete;
        }
        #endregion

        #region Variables
        /// <summary>
        /// Most variables with Class Level Scope are located in this region.
        /// There are contradicting rules as to where variables should be located.
        /// Variables that are only used in a specific region are located in the specific region.
        /// Variables that are used in various regions are located together in this region.
        /// </summary>

        //The number of Active Worker Threads
        int activeWorkerThreadCount;

        //The current analysis phase.
        Constants.AnalysisPhase analysisPhase = Constants.AnalysisPhase.Idle;

        //Start datetime of the current analysis phase.
        DateTime analysisPhase_startDateTime=DateTime.Now;

        //Whether or not the application is in analysis mode.
        bool applicationIsInAnalysisMode;

        //The Chosen Queue Limit is a hard limit on the number of tasks that can be queued or the number of worker threads that can be created.
        int chosenQueueLimit;

        //The maximum number of tasks or threads that can be queued before getting an OutOfMemory Exception.
        int maxQueueLimit;

        //An estimate of the number of threads normally used to operate and run the application
        int nonWorkerThreadCount;

        //The duration of the previous analysis phase.
        TimeSpan previousAnalysisPhase_duration;

        //The number of tasks or threads considered safe to queue.
        int safeQueueLimit;

        //The number of tasks or threads queued.
        int tasksOrThreads_queued;

        //The number or worker threads spawned in the first 3 seconds after queing them.
        //This is an important performance indicator of how fast worker threads can be spawned.
        int workerThreadsSpawnedInFirstSecond;

        //The threading implementation being analyzed.
        Constants.ThreadingImplementation threadingImplementationToAnalyze;

        //This tells the worker threads whether or not to continue simulating work.
        bool workerThreadsContinueSimulatingWork;
        #endregion
    }
}
