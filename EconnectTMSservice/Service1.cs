using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace EconnectTMSservice
{
    public partial class Service1 : ServiceBase
    {
        ClsMain tms = new ClsMain();
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            if (tms == null)
                tms = new ClsMain();
            tms.Start();
        }

        protected override void OnStop()
        {
            tms.Stop();
        }
    }
}
