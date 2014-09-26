using System.ServiceProcess;
using DbSyncService.SyncProvider;

namespace DbSyncService
{
    public partial class SyncService : ServiceBase
    {
        private SyncManager manager;
        public SyncService()
        {
            InitializeComponent();
            manager = new SyncManager();
        }

        protected override void OnStart(string[] args)
        {
            manager.Start();
        }

        protected override void OnStop()
        {
            manager.Dispose();
            manager = null;
        }
    }
}
