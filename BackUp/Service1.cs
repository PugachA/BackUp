using System;
using System.ServiceProcess;
using System.Threading;

namespace BackUp
{
    public partial class Service1 : ServiceBase
    {
        Logger logger;
        BackUp backUp;
        public Service1()
        {
            InitializeComponent();
            this.CanStop = true;
            this.CanPauseAndContinue = true;
            this.AutoLog = true;
        }

        protected override void OnStart(string[] args)
        {
            string[] paths = Properties.Settings.Default.PathToWatch.Split(new string[] { ",", ";", " " }, StringSplitOptions.RemoveEmptyEntries);
            logger = new Logger(paths, Properties.Settings.Default.PathToLog);
            Thread loggerThread = new Thread(new ThreadStart(logger.Start));

            backUp = new BackUp(logger, paths[0], Properties.Settings.Default.PathToLog, Properties.Settings.Default.Frequency, Properties.Settings.Default.StorageTime);
            Thread backUpThread = new Thread(new ThreadStart(backUp.Start));

            loggerThread.Start();
            backUpThread.Start();
        }

        protected override void OnStop()
        {
            logger.Stop();
            backUp.Stop();
            Thread.Sleep(1000);
        }
    }
}
