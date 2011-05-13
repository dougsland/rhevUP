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
using System.Text;
using System.IO;
using System.Collections;
using Gnu.Getopt;

namespace rhevUP
{
    class main
    {
        public void usage()
        {
            
            Console.WriteLine("-d\t\t Destination directory for backup");
            Console.WriteLine("-n\t\t SQL serverName");
            Console.WriteLine("-s\t\t Source directory which contains the backup for restore");
            Console.WriteLine("-r\t\t Restore RHEV Environment");
            Console.WriteLine("-b\t\t Backup RHEV Environment");
            Console.WriteLine("-q\t\t Quit after backup and start RHEVM Services (script mode)");
            Console.WriteLine("-i\t\t IIS wwwroot path (Ex.: c:\\inetpub\\wwwroot)");
            Console.WriteLine("-p\t\t path RHEVM files (Ex.: c:\\Program Files\\RedHat\\RHEVManager)");
            
            Console.WriteLine("\nEx.: (backup)");
            Console.WriteLine("C:\\>rhevUP -b -n \"(local)\\sqlexpress\" -d C:\\myBackupDir -i C:\\inetpub\\wwwroot -p c:\\Program Files\\RedHat\\RHEVManager\n");

            Console.WriteLine("\nEx.: (restore)");
            Console.WriteLine("C:\\>rhevUP -r -n \"(local)\\sqlexpress\" -s C:\\myBackupDir -i C:\\inetpub\\wwwroot -p c:\\Program Files (x86)\\RedHat\\RHEVManager");

            Console.WriteLine("\nEx.: (script mode)");
            Console.WriteLine("C:\\>rhevUP -q -b -n \"(local)\\sqlexpress\" -d C:\\myBackupDir -i C:\\inetpub\\wwwroot -p c:\\Program Files (x86)\\RedHat\\RHEVManager");

            Environment.Exit(-1);
        }

        static void Main(string[] args)
        {
            /* flags */
            bool foundOpt   = false;
            bool has_R_flag = false; /* restore flag */
            bool has_B_flag = false; /* backup flag */
            bool has_S_flag = false; /* source flag */
            bool has_D_flag = false; /* destination flag */
            bool has_Q_flag = false; /* quit flag */
            bool has_I_flag = false; /* inetpub flag */
            bool has_P_flag = false; /* inetpub flag */

            /* variables to hold options */
            string sqlServerName = null;
            string destDir       = null;
            string sourceDir     = null;
            string selectedOpt   = null;
            string inetpubDir    = null;
            string rhevpath      = null;

            /* others */
            int c;

            main m = new main();

            if (args.Length < 9)
            {
                m.usage();
                Environment.Exit(-1);
            }

            Getopt g = new Getopt("rhevUP", args, "d:i:rbs:u:n:p:h:q");

            while ((c = g.getopt()) != -1)
            {
                switch (c)
                {
                    case 'd':
                        /* destination flag*/
                        foundOpt = true;
                        has_D_flag = true;
                        
                        /* setting value */      
                        destDir = g.Optarg;
                        break;

                    case 'r':
                        /* recover flag */
                        foundOpt = true;
                        has_R_flag = true;
                        
                        /* setting value */
                        selectedOpt = "restore";
                        break;

                    case 'b':
                        /* backup flag */
                        foundOpt = true;
                        has_B_flag = true;

                        /* setting value */
                        selectedOpt = "backup";
                        break;

                    case 's':
                        /* source */
                        foundOpt = true;
                        has_S_flag = true;

                        /* setting value */
                        sourceDir = g.Optarg;
                        break;

                    case 'n':
                        /* SQL Server Name */
                        foundOpt = true;

                        /* setting */
                        sqlServerName = g.Optarg;
                        break;

                    case 'q':
                        has_Q_flag = true;
                        foundOpt   = true;
                        break;

                    case 'h':
                        m.usage();
                        break;

                    case 'i':
                        has_I_flag = true;
                        inetpubDir = g.Optarg;
                        foundOpt   = true;
                        break;

                    case 'p':
                        has_P_flag = true;
                        rhevpath = g.Optarg;
                        foundOpt = true;
                        break;

                    default:
                        Console.WriteLine("getopt() returned " + c);
                        m.usage();
                        break;
                }
            }

            /* In case customer doesn't provide '-' argument, print usage */
            if (foundOpt != true)
            {
                m.usage();
            }

            if (has_P_flag == false)
            {
                Console.WriteLine("Cannot proceed, you must specify -p (path RHEVM files) option, aborting..");
                Console.WriteLine("Ex.: -p c:\\Program Files\\RedHat\\RHEVManager");
                Environment.Exit(-1);
            }

            if (has_I_flag == false)
            {
                Console.WriteLine("Cannot proceed, you must specify -i (wwwroot) option, aborting..");
                Console.WriteLine("Ex.: -i c:\\inetpub\\wwwroot");
                Environment.Exit(-1);
            }

            /* quit and restore flags are not compatible */
            if ((has_R_flag == true) && (has_Q_flag == true))
            {
                Console.WriteLine("Cannot use flags -r and -q together, aborting..");
                Environment.Exit(-1);
            }

            /* backup and restore are not compatible flags */
            if ((has_B_flag == true) && (has_R_flag == true))
            {
                Console.WriteLine("Cannot use flags -b and -r together, aborting..");
                Environment.Exit(-1);
            }

            /* destionation and source are not compatible flags */
            if ((has_D_flag == true) && (has_S_flag == true))
            {
                Console.WriteLine("Cannot use flags -d and -s together, aborting..");
                Environment.Exit(-1);
            }

            Run r = new Run();

            if (selectedOpt == "backup")
            {
                if (destDir == null)
                {
                    Console.WriteLine("using backup mode, -d must be specified");
                    Console.WriteLine("aborting...");
                    Environment.Exit(-1);
                }
                r.backupRHEV(sqlServerName, destDir, has_Q_flag, inetpubDir, rhevpath);
            }
            else if (selectedOpt == "restore")
            {
                if (sourceDir == null)
                {
                    Console.WriteLine("using restore mode, -s must be specified");
                    Console.WriteLine("aborting...");
                    Environment.Exit(-1);
                }

                r.restoreRHEV(sqlServerName, sourceDir, inetpubDir, rhevpath);
            }
            else
            {
                Console.WriteLine("You must select restore or backup option");
                Environment.Exit(0);
            }
        }
    }
}