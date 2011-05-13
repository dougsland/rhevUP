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
using System.Security.AccessControl;
using System.Threading;

namespace rhevUP
{
    class Run
    {
        const string VERSION_RHEV_UPDATE = "1.0.0";

        public int backupRHEV(string sqlServerName, string destDir, bool quitFlag, string inetpubStr, string rhevpath)
        {
            fileOperations fop = new fileOperations();
            string PATH_BACKUP_DIR = destDir;


            DirectoryInfo DirRhevPath = new DirectoryInfo(rhevpath);
            if (DirRhevPath.Exists == false)
            {
                Console.WriteLine("Cannot proceed, RHEV Path " + rhevpath + " doesn't exist!");
                Environment.Exit(-1);
            }
            
            DirectoryInfo DirInetPub = new DirectoryInfo(inetpubStr);
            if (DirInetPub.Exists == false)
            {
                Console.WriteLine("Cannot proceed, inetpub path " + inetpubStr + " doesn't exist!");
                Environment.Exit(-1);
            }

            /* Check if the backup directory already exists, if yes add to the path Date */
            DirectoryInfo DirInfo = new DirectoryInfo(PATH_BACKUP_DIR);
            if (DirInfo.Exists == true)
            {
                DateTime datenow = DateTime.Now;
                PATH_BACKUP_DIR += datenow.ToString("yyyy-MM-dd_HHmmss");   
            }

            string PATH_BACKUP_DB = PATH_BACKUP_DIR + @"\sqlBackup";

            // service
            string PATH_BACKUP_SERVICE = PATH_BACKUP_DIR + @"\Service\";
            string PATH_BACKUP_SERVICE_CA = PATH_BACKUP_DIR + @"\Service\ca\";

            // inetpub
            string PATH_INETPUB = inetpubStr;
            string PATH_BACKUP_INETPUB = PATH_BACKUP_DIR + @"\inetpub";

            // certs
            string PATH_BACKUP_CERTS_AUTH = PATH_BACKUP_DIR + @"\Certificates\TrustedRootCertificatesAuthorities\";
            string PATH_BACKUP_CERTS_PUB = PATH_BACKUP_DIR + @"\Certificates\TrustedPublishers\";
            string PATH_BACKUP_CERTS_PERSONAL = PATH_BACKUP_DIR + @"\Certificates\Personal\";

            string PATH_SERVICE_CA_PROGRAM_FILES = "";
            string PATH_SERVICE_PROGRAM_FILES = "";
            int ret;

            PATH_SERVICE_CA_PROGRAM_FILES = rhevpath + @"\Service\ca\";
            DirectoryInfo DirCA = new DirectoryInfo(rhevpath);
            if (DirCA.Exists == false)
            {
                Console.WriteLine("Cannot locate path: " + PATH_SERVICE_CA_PROGRAM_FILES + ", aborting..");
                Environment.Exit(-1);
            }

            PATH_SERVICE_PROGRAM_FILES = rhevpath + @"\Service\";
            DirectoryInfo DirPFILES = new DirectoryInfo(rhevpath);
            if (DirPFILES.Exists == false)
            {
                Console.WriteLine("Cannot locate path: " + PATH_SERVICE_PROGRAM_FILES + ", aborting..");
                Environment.Exit(-1);
            }
        
            Console.WriteLine("========================================");
            Console.WriteLine("RHEVUP - " + VERSION_RHEV_UPDATE);
            Console.WriteLine("========================================\n");

            /* Creating initial dirs */
            fop.createDir(PATH_BACKUP_DIR);
            fop.createDir(PATH_BACKUP_DB);
            fop.createDir(PATH_BACKUP_SERVICE_CA);
            fop.createDir(PATH_BACKUP_CERTS_AUTH);
            fop.createDir(PATH_BACKUP_CERTS_PUB);
            fop.createDir(PATH_BACKUP_CERTS_PERSONAL);
            fop.createDir(PATH_BACKUP_INETPUB);

            ///////////////////////////////// STEP 1 /////////////////////////////////////////////
            //////////////////////// SHUTDOWN WINDOWS SERVICES ///////////////////////////////////
            //////////////////////////////////////////////////////////////////////////////////////

            /* Shutdown all RHEV Windows Services */
            Console.WriteLine("Phase 1: Stopping services");
            serviceOperations services = new serviceOperations();
            services.StopService("RHEV Manager", 15000);
            services.StopService("RHEVM History Service", 15000);
            services.StopService("RHEVM Net Console", 15000);
            services.StopService("RHEVM Notification Service", 15000);
            Console.WriteLine("Phase 1: Done\n");

            ///////////////////////////////// STEP 2 /////////////////////////////////////////////
            //////////////////////// BACKUP DATABASES ////////////////////////////////////////////
            //////////////////////////////////////////////////////////////////////////////////////
            /* backup rhevm databases */
            Console.WriteLine("Phase 2: backup rhevm databases");
            SqlOperations sql = new SqlOperations();
            ret = sql.backupDatabases(PATH_BACKUP_DB, sqlServerName);
            if (ret == -1)
            {
                Console.WriteLine("Phase 2: Failed\n");
                Environment.Exit(-1);
            }
            Console.WriteLine("Phase 2: Done\n");

            ///////////////////////////////// STEP 3 /////////////////////////////////////////////
            //////////////////////////////////////////////////////////////////////////////////////

            /* STEP 3 */
            /* backup service - CA - directory */
            Console.WriteLine("Phase 3: backup service CA directory");
            DirectoryInfo src = new DirectoryInfo(PATH_SERVICE_CA_PROGRAM_FILES);
            DirectoryInfo dest = new DirectoryInfo(PATH_BACKUP_SERVICE_CA);
            ret = fop.backupServiceCA_DIR(src, dest);
            if (ret == -1)
            {
                Console.WriteLine("Phase 3: Failed\n");
                Environment.Exit(-1);
            }
            Console.WriteLine("Phase 3: Done\n");

            ///////////////////////////////// STEP 4 /////////////////////////////////////////////
            //////////////////////////////////////////////////////////////////////////////////////
            /* backup - rhevm.pfx and rhevm.ssh.key files*/
            Console.WriteLine("Phase 4: backup rhevm.pfx and rhevm.ssh.key");
            ret = fop.copyFile(PATH_SERVICE_PROGRAM_FILES + @"rhevm.pfx", PATH_BACKUP_SERVICE + @"rhevm.pfx");
            if (ret == -1)
            {
                Console.WriteLine("From:" + PATH_SERVICE_PROGRAM_FILES + @"rhevm.pfx");
                Console.WriteLine("To:" + PATH_BACKUP_SERVICE + @"rhevm.pfx");
                Console.WriteLine("Phase 4: Failed\n");
                Environment.Exit(-1);
            }
            Console.WriteLine("Copied rhevm.pfx");

            /* rhevm.ssh.key */
            ret = fop.copyFile(PATH_SERVICE_PROGRAM_FILES + @"rhevm.ssh.key", PATH_BACKUP_SERVICE + @"rhevm.ssh.key");
            if (ret == -1)
            {
                Console.WriteLine("From:" + PATH_SERVICE_PROGRAM_FILES + @"rhevm.ssh.key");
                Console.WriteLine("To:" + PATH_BACKUP_SERVICE + @"rhevm.ssh.key");
                Console.WriteLine("Phase 4: Failed\n");
                Environment.Exit(-1);
            }
            Console.WriteLine("Copied rhevm.ssh.key");
            Console.WriteLine("Phase 4: Done\n");

            ///////////////////////////////// STEP 5 /////////////////////////////////////////////
            //////////////////////////////////////////////////////////////////////////////////////
            /* backup - RHEVManager.exe.config RHEVMHistoryService.exe.config RHEVMNotificationService.exe.config*/

            Console.WriteLine("Phase 5: backup RHEVM*.config files");


            ret = fop.copyFile(PATH_SERVICE_PROGRAM_FILES + @"RHEVManager.exe.config", PATH_BACKUP_SERVICE + @"RHEVManager.exe.config");
            if (ret == -1)
            {
                Console.WriteLine("Phase 5: Failed - File: RHEVManager.exe.config\n");
                Environment.Exit(-1);
            }
            Console.WriteLine("Copied RHEVManager.exe.config");

            // Backup FieldsInVDCConfig.xml file
            ret = fop.copyFile(PATH_SERVICE_PROGRAM_FILES + @"FieldsInVDCConfig.xml", PATH_BACKUP_SERVICE + @"FieldsInVDCConfig.xml");
            if (ret == -1)
            {
                Console.WriteLine("Phase 5: Failed - File: FieldsInVDCConfig.xml\n");
                Environment.Exit(-1);
            }
            Console.WriteLine("Copied FieldsInVDCConfig.xml");

            ret = fop.copyFile(PATH_SERVICE_PROGRAM_FILES + @"RHEVMHistoryService.exe.config", PATH_BACKUP_SERVICE + @"RHEVMHistoryService.exe.config");
            if (ret == -1)
            {
                Console.WriteLine("Phase 5: Failed - File: RHEVMHistoryService.exe.config\n");
                Environment.Exit(-1);
            }
            Console.WriteLine("Copied RHEVMHistoryService.exe.config");

            ret = fop.copyFile(PATH_SERVICE_PROGRAM_FILES + @"RHEVMNotificationService.exe.config", PATH_BACKUP_SERVICE + @"RHEVMNotificationService.exe.config");
            if (ret == -1)
            {
                Console.WriteLine("Phase 5: Failed - File: RHEVMNotificationService.exe.config\n");
                Environment.Exit(-1);
            }
            Console.WriteLine("Copied RHEVMNotificationService.exe.config");
            Console.WriteLine("Phase 5: Done\n");

            ///////////////////////////////// STEP 6 /////////////////////////////////////////////
            //////////////////////////////////////////////////////////////////////////////////////

            /* backup inetpub files */
            Console.WriteLine("Phase 6: backup inetpub files");

            ret = fop.copyFile(PATH_INETPUB + @"\ca.crt", PATH_BACKUP_INETPUB + @"\ca.crt");
            if (ret == -1)
            {
                Console.WriteLine("Phase 6: Failed - cannot copy ca.crt from" + PATH_INETPUB + "\n");
                Environment.Exit(-1);
            }

            ret = fop.copyFile(PATH_INETPUB + @"\rhevm.ssh.key.txt", PATH_BACKUP_INETPUB + @"\rhevm.ssh.key.txt");
            if (ret == -1)
            {
                Console.WriteLine("Phase 6: Failed - cannot copy rhevm.ssh.key.txt from" + PATH_INETPUB + "\n");
                Environment.Exit(-1);
            }
            Console.WriteLine("Copied ca.crt rhevm.ssh.key.txt from inetpub");
            Console.WriteLine("Phase 6: Done\n");

            ///////////////////////////////// STEP 7 /////////////////////////////////////////////

            /* Copy .certs from Red Hat */
            Console.WriteLine("Phase 7: Getting certificates");
            certOperations cert = new certOperations();
            cert.get_Trusted_Root_Certificate_Authorities(PATH_BACKUP_CERTS_AUTH);
            cert.get_Trusted_Publishers(PATH_BACKUP_CERTS_PUB);
            cert.get_Personal_certs(PATH_BACKUP_CERTS_PERSONAL);
            Console.WriteLine("Phase 7: Done\n");



            ///////////////////////////////// STEP 7 /////////////////////////////////////////////
            //////////////////////////////////////////////////////////////////////////////////////          
            /* shutdown the system */
            systemOperations sys = new systemOperations();
            serviceOperations serv = new serviceOperations();

            if (quitFlag == false)
            {
                Console.WriteLine("Please copy the path " + PATH_BACKUP_DIR + " to a CDROM/USB device to move to the new RHEV Server.");
                Console.WriteLine("\nAfter that, we need to *HALT* THIS COMPUTER to avoid any conflicts like DNS.");
                Console.WriteLine("Press y when you get ready to TURN OFF the entire system");
                char character = (char)Console.Read();

                if (character.Equals('y'))
                {
                    sys.Shutdown();
                }
                else
                {
                    Console.WriteLine("\nOk, I will *NOT* halt this machine now, aborting halt..");
                    Console.WriteLine("SUCCESS!");
                    return 0;
                }
            }
            serv.StartService("RHEV Manager");
            serv.StartService("RHEVM History Service");
            serv.StartService("RHEVM Net Console");
            serv.StartService("RHEVM Notification Service");
            Console.WriteLine("SUCCESS!");
            return 0;
        }

        public void restoreRHEV(string sqlServerName, string sourceDir, string inetpubDir, string rhevpath)
        {

            string PATH_BACKUP_DIR = sourceDir;

            string PATH_BACKUP_DB = PATH_BACKUP_DIR + @"\sqlBackup";

            string PATH_BACKUP_SERVICE = PATH_BACKUP_DIR + @"\Service\";
            string PATH_BACKUP_SERVICE_CA = PATH_BACKUP_DIR + @"\Service\ca\";
            string PATH_BACKUP_SERVICE_PRIVATE_CA = PATH_BACKUP_DIR + @"\Service\ca\private\";

            // inetpub
            string PATH_INETPUB = inetpubDir;
            string PATH_BACKUP_INETPUB = PATH_BACKUP_DIR + @"\inetpub\";

            // CERTS
            string PATH_BACKUP_CERTS_AUTH = PATH_BACKUP_DIR + @"\Certificates\TrustedRootCertificatesAuthorities\";
            string PATH_BACKUP_CERTS_PUB = PATH_BACKUP_DIR + @"\Certificates\TrustedPublishers\";
            string PATH_BACKUP_CERTS_PERSONAL = PATH_BACKUP_DIR + @"\Certificates\Personal\";

            // Service
            string PATH_SERVICE_CA_PROGRAM_FILES = "";
            string PATH_SERVICE_CA_PRIVATE_PROGRAM_FILES = "";
            string PATH_SERVICE_PROGRAM_FILES = "";
            int ret;

            PATH_SERVICE_CA_PROGRAM_FILES = rhevpath + @"\Service\ca\";
            DirectoryInfo DirCA = new DirectoryInfo(rhevpath);
            if (DirCA.Exists == false)
            {
                Console.WriteLine("Cannot locate path: " + PATH_SERVICE_CA_PROGRAM_FILES + ", aborting..");
                Environment.Exit(-1);
            }

            PATH_SERVICE_PROGRAM_FILES = rhevpath + @"\Service\";
            DirectoryInfo DirPFILES = new DirectoryInfo(rhevpath);
            if (DirPFILES.Exists == false)
            {
                Console.WriteLine("Cannot locate path: " + PATH_SERVICE_PROGRAM_FILES + ", aborting..");
                Environment.Exit(-1);
            }
        
            PATH_SERVICE_CA_PRIVATE_PROGRAM_FILES = PATH_SERVICE_CA_PROGRAM_FILES + @"\private\";

            Console.WriteLine("========================================");
            Console.WriteLine("RHEVUP - " + VERSION_RHEV_UPDATE);
            Console.WriteLine("========================================\n");

            if (!Directory.Exists(sourceDir))
            {
                Console.WriteLine("source dir " + sourceDir + " doesn't exit!");
                Console.WriteLine("Aborting...");
                Environment.Exit(-1);
            }

            ///////////////////////////////// STEP 1 /////////////////////////////////////////////
            //////////////////////// SHUTDOWN WINDOWS SERVICES ///////////////////////////////////
            //////////////////////////////////////////////////////////////////////////////////////
            /* Shutdown all RHEV Windows Services */
            Console.WriteLine("Phase 1: Stopping services");
            serviceOperations services = new serviceOperations();
            services.StopService("RHEV Manager", 15000);
            services.StopService("RHEVM History Service", 15000);
            services.StopService("RHEVM Net Console", 15000);
            services.StopService("RHEVM Notification Service", 15000);
            Console.WriteLine("Phase 1: Done\n");

            ///////////////////////////////// STEP 2 /////////////////////////////////////////////
            //////////////////////// RESTORE DATABASES ///////////////////////////////////////////
            //////////////////////////////////////////////////////////////////////////////////////    
            Console.WriteLine("Phase 2: Restore rhev databases");
            SqlOperations sql = new SqlOperations();
            ret = sql.restoreDatabases(PATH_BACKUP_DB, sqlServerName);
            if (ret == -1)
            {
                Console.WriteLine("Phase 2: Failed\n");
                Console.ReadLine();
                Environment.Exit(-1);
            }

            sql.setUserPermissionLogin(sqlServerName);
            //Console.ReadLine();
            Console.WriteLine("Phase 2: Done\n");

            ///////////////////////////////// STEP 3 /////////////////////////////////////////////
            ////////////////// Add FULL CONTROL PERMISSION TO Service\ca\rhevm.pfx  //////////////
            //////////////////////////////////////////////////////////////////////////////////////

            //File.Copy((PATH_BACKUP_SERVICE + @"rhevm.pfx"), (PATH_SERVICE_PROGRAM_FILES + @"rhevm.pfx"), true);
            string currTime = DateTime.Now.ToString("MM-dd-yyyy-hh-mm-ss");

            Console.WriteLine("Phase 3: backup current rhevm.pfx, ca.pem and restore the original");

            // restore c:\Program Files (x86)\RHEVManager\Service\ca\ca.pem
            if (File.Exists(PATH_SERVICE_CA_PROGRAM_FILES + @"ca.pem"))
            {
                Console.WriteLine("Backuping " + (PATH_SERVICE_CA_PROGRAM_FILES + "ca.pem"));
                File.Copy((PATH_SERVICE_CA_PROGRAM_FILES + @"ca.pem"), (PATH_SERVICE_CA_PROGRAM_FILES + @"ca.pem" + currTime));
            }

            if (!File.Exists(PATH_BACKUP_SERVICE_CA + @"ca.pem"))
            {
                Console.WriteLine("Unable to locate original " + PATH_BACKUP_SERVICE_CA + @"ca.pem");
                Console.WriteLine("Phase 3: Failed\n");
                Console.WriteLine("Aborting...");
                Environment.Exit(-1);
            }
            File.Delete(PATH_SERVICE_CA_PROGRAM_FILES + @"ca.pem");
            File.Copy((PATH_BACKUP_SERVICE_CA + @"ca.pem"), (PATH_SERVICE_CA_PROGRAM_FILES + @"ca.pem"));

            // restore c:\Program Files (x86)\RHEVManager\Service\ca\private\ca.pem
            if (File.Exists(PATH_SERVICE_CA_PRIVATE_PROGRAM_FILES + @"ca.pem"))
            {
                Console.WriteLine("Backuping " + (PATH_SERVICE_CA_PRIVATE_PROGRAM_FILES + "ca.pem"));
                File.Copy((PATH_SERVICE_CA_PRIVATE_PROGRAM_FILES + @"ca.pem"), (PATH_SERVICE_CA_PRIVATE_PROGRAM_FILES + @"ca.pem" + currTime));
            }

            if (!File.Exists(PATH_BACKUP_SERVICE_PRIVATE_CA + @"ca.pem"))
            {
                Console.WriteLine("Unable to locate original " + PATH_BACKUP_SERVICE_PRIVATE_CA + @"ca.pem");
                Console.WriteLine("Phase 3: Failed\n");
                Console.WriteLine("Aborting...");
                Environment.Exit(-1);
            }
            File.Delete(PATH_SERVICE_CA_PRIVATE_PROGRAM_FILES + @"ca.pem");
            File.Copy((PATH_BACKUP_SERVICE_PRIVATE_CA + @"ca.pem"), (PATH_SERVICE_CA_PRIVATE_PROGRAM_FILES + @"ca.pem"));

            // restore inetpub - ca.crt
            // PATH_INETPUB = @"c:\inetpub\wwwroot\";
            // PATH_BACKUP_INETPUB = PATH_BACKUP_DIR + @"\inetpub";

            if (File.Exists(PATH_INETPUB + @"ca.crt"))
            {
                Console.WriteLine("Backuping " + (PATH_INETPUB + "ca.crt"));
                File.Copy((PATH_INETPUB + @"ca.crt"), (PATH_INETPUB + @"ca.crt" + currTime));
            }

            if (!File.Exists(PATH_BACKUP_INETPUB + @"ca.crt"))
            {
                Console.WriteLine("Unable to locate original " + PATH_BACKUP_INETPUB + @"ca.crt");
                Console.WriteLine("Phase 3: Failed\n");
                Console.WriteLine("Aborting...");
                Environment.Exit(-1);
            }
            File.Delete(PATH_INETPUB + @"ca.crt");
            File.Copy((PATH_BACKUP_INETPUB + @"ca.crt"), (PATH_INETPUB + @"ca.crt"));

            // rhevm.ssh.key.txt
            if (File.Exists(PATH_INETPUB + @"rhevm.ssh.key.txt"))
            {
                Console.WriteLine("Backuping " + (PATH_INETPUB + "rhevm.ssh.key.txt"));
                File.Copy((PATH_INETPUB + @"rhevm.ssh.key.txt"), (PATH_INETPUB + @"rhevm.ssh.key.txt" + currTime));
            }

            if (!File.Exists(PATH_BACKUP_INETPUB + @"rhevm.ssh.key.txt"))
            {
                Console.WriteLine("Unable to locate original " + PATH_BACKUP_INETPUB + @"rhevm.ssh.key.txt");
                Console.WriteLine("Phase 3: Failed\n");
                Console.WriteLine("Aborting...");
                Environment.Exit(-1);
            }
            File.Delete(PATH_INETPUB + @"rhevm.ssh.key.txt");
            File.Copy((PATH_BACKUP_INETPUB + @"rhevm.ssh.key.txt"), (PATH_INETPUB + @"rhevm.ssh.key.txt"));

            // backup current .pfx file
            if (File.Exists(PATH_SERVICE_PROGRAM_FILES + @"rhevm.pfx"))
            {
                Console.WriteLine("\nBackuping current rhevm.pfx");
                Console.WriteLine("Path: " + (PATH_SERVICE_PROGRAM_FILES + @"rhevm.pfx"));
                File.Copy((PATH_SERVICE_PROGRAM_FILES + @"rhevm.pfx"), (PATH_SERVICE_PROGRAM_FILES + @"bkp.rhevm.pfx-" + currTime));
            }

            // copying the "new (previous/old)" rhevm.pfx to Services directory
            // first, verify...
            if (!File.Exists(PATH_BACKUP_SERVICE + @"rhevm.pfx"))
            {
                Console.WriteLine("Unable to locate original rhevm.pfx");
                Console.WriteLine("Phase 3: Failed\n");
                Console.WriteLine("Aborting...");
                Environment.Exit(-1);
            }

            File.Delete(PATH_SERVICE_PROGRAM_FILES + @"rhevm.pfx");
            Console.WriteLine("\nCopying original rhevm.pfx to " + PATH_SERVICE_PROGRAM_FILES);
            File.Copy((PATH_BACKUP_SERVICE + @"rhevm.pfx"), (PATH_SERVICE_PROGRAM_FILES + @"rhevm.pfx"), true);
            
            // Setting perrmission to new .pfx
            Console.WriteLine("Setting permission (NETWORK SERVICE - FULL CONTROL) to rhevm.pfx");
            string setPerFile = (PATH_SERVICE_PROGRAM_FILES + @"rhevm.pfx");
            FileSecurity fileSecurity = File.GetAccessControl(setPerFile);
            fileSecurity.AddAccessRule(new FileSystemAccessRule("NETWORK SERVICE", FileSystemRights.FullControl, AccessControlType.Allow));
            File.SetAccessControl(setPerFile, fileSecurity);

            //FileSecurity fileSecurity = File.GetAccessControl(setPerFile);
            fileSecurity.AddAccessRule(new FileSystemAccessRule("Everyone", FileSystemRights.FullControl, AccessControlType.Allow));
            fileSecurity.AddAccessRule(new FileSystemAccessRule("Users", FileSystemRights.FullControl, AccessControlType.Allow));
            File.SetAccessControl(setPerFile, fileSecurity);

            Console.WriteLine("\nPhase 3: Done\n");

            ///////////////////////////////// STEP 4 ////////////////////////////////////////////
            ////////////////////////////// RESTORE CERTS   //////////////////////////////////////
            //////////;//////////////// CERTS Folders backup ////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////////
            ///////////////////// Personal - LocalMachine               /////////////////////////
            ///////////////////// Trusted Publishers                    /////////////////////////
            ///////////////////// Trusted Root Certificates Authorities /////////////////////////
            /////////////////////////////////////////////////////////////////////////////////////    
            Console.WriteLine("Phase 4: Restore rhev certificates");
            certOperations cert = new certOperations();

            /* restore Trusted Publishers */
            DirectoryInfo dirPub = new DirectoryInfo(PATH_BACKUP_CERTS_PUB);
            FileInfo[] FilesPub = dirPub.GetFiles("*.cer");
            Console.WriteLine("Trusted Publishers - LocalMachine:");
            foreach (FileInfo fi in FilesPub)
            {
                Console.WriteLine("Adding cert " + fi.Name);
                cert.addCertificateTrustedPublishers(PATH_BACKUP_CERTS_PUB + fi.Name);
                
            }
            Console.WriteLine("done\n");

            /* restore Trusted Root Certificates Authorities */
            DirectoryInfo dirAuth = new DirectoryInfo(PATH_BACKUP_CERTS_AUTH);
            FileInfo[] FilesAuth = dirAuth.GetFiles("*.cer");
            Console.WriteLine("Trusted Root Certificates Authorities - LocalMachine");
            foreach (FileInfo fi in FilesAuth)
            {
                Console.WriteLine("Adding cert " + fi.Name);
                cert.addCertificateTrustedRootCertificateAuthorities(PATH_BACKUP_CERTS_AUTH + fi.Name);
                
            }
            Console.WriteLine("done\n");

            /* Restore .pfx */
            Console.WriteLine("\nPFX - Personal - LocalMachine:");
            Console.WriteLine("Adding rhevm.pfx cert");
            cert.addPfxCertificate(PATH_SERVICE_PROGRAM_FILES + @"rhevm.pfx", "mypass");
            Console.WriteLine("done\n");

            Console.WriteLine("Phase 4: Done\n");
                            
            ///////////////////////////////// STEP 5 /////////////////////////////////////////////
            ////////////////// Start RHEVM Services  /////////////////////////////////////////////
            //////////////////////////////////////////////////////////////////////////////////////

            serviceOperations sOp = new serviceOperations();
            /* Start all RHEV Windows Services */
            Console.WriteLine("Phase 5: Starting services");
            serviceOperations servStart = new serviceOperations();
            servStart.StartService("RHEV Manager");
            servStart.StartService("RHEVM History Service");
            servStart.StartService("RHEVM Net Console");
            servStart.StartService("RHEVM Notification Service");
            Console.WriteLine("Phase 5: Done\n");
            Console.WriteLine("Finished!");
        }
    }
}