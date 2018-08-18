using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Facturacion
{
    public partial class Facturacion : ServiceBase
    {
        public Facturacion()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Scheduler cc = new Scheduler();
            cc.Start();
        }

        protected override void OnStop()
        {
        }
    }
}
