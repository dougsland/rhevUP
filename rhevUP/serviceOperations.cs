/*********************************************************************************
 *  Copyright (C) 2011
 *
 *  Douglas Schilling Landgraf <dougsland@redhat.com>
 *
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, version 2 of the License.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *********************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.IO;
using System.Threading;

namespace rhevUP
{
    class serviceOperations
    {
        /* Stop Service method */
        public void StopService(string serviceName, int timeoutMilliseconds)
        {
            ServiceController service = new ServiceController(serviceName);

            TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

            Console.WriteLine("Stopping service: " + serviceName);

            switch (service.Status)
            {
                case ServiceControllerStatus.Running:
                case ServiceControllerStatus.Paused:
                case ServiceControllerStatus.StopPending:
                case ServiceControllerStatus.StartPending:
                    try
                    {
                        service.Stop();
                        service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                        Console.WriteLine("Status:" + serviceName + " stopped");
                        return;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message.ToString());
                        return;
                    }
                default:
                    Console.WriteLine("Status:" + serviceName + " already stopped");
                    return;
            }
        }

        /* Start Service method */
        public void StartService(string serviceName)
        {
            ServiceController service = new ServiceController(serviceName);

            switch (service.Status)
            {
                case ServiceControllerStatus.Stopped:
                try
                {
                    /* FIX-ME:
                     * For some reason RHEV-M Service doesn't return Service Running when the service 
                     * is started and running. For this reason, we cannot verify the service status
                     * with service.WaitForStatus */
                    service.Start();
                    Console.WriteLine("Starting service: " + serviceName);
                    return;
                }    
                catch (Exception ex)    
                {    
                    Console.WriteLine(ex.Message.ToString());    
                    return;    
                }
                default:
                    Console.WriteLine("Status:" + serviceName + " already started");
                    return;
            }           
        }
    }
}