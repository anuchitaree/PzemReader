using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace PzemReader
{
    partial class TransactionWorker : BackgroundService
    {
        private readonly ILogger<TransactionWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        public TransactionWorker(ILogger<TransactionWorker> logger,
            IServiceScopeFactory scopeFactory)
        {
            InitializeComponent();
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

       
    }
}
