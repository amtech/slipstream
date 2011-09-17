﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace ObjectServer.Daemon
{
    static class Win32ServiceProgram
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            var ServicesToRun = new ServiceBase[]  { 
				new ObjectServerService() 
			};

            ServiceBase.Run(ServicesToRun);
        }
    }
}
