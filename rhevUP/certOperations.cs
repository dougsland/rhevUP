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
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Security.Cryptography;
using System.Security.Permissions;

namespace rhevUP
{
    class certOperations
    {
        /* Method to collect certs from Personal Certificates */
        public void get_Personal_certs(string pathPERSONAL)
        {
            try
            {
                X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                X509Certificate2Collection certificates = (X509Certificate2Collection)store.Certificates;
                Int32 counter1 = 1;
                try
                {
                    foreach (X509Certificate2 certificate in certificates)
                    {
                        string cert = "cert_personal";
                        byte[] bytes = null;

                        try
                        {
                            bytes = certificate.Export(X509ContentType.Cert);
                            cert += counter1 + ".cer";
                            File.WriteAllBytes(pathPERSONAL + cert, bytes);
                            counter1 += 1;
                        }
                        catch (Exception e)
                        {
                            Console.Write("Exception " + e);
                        }
                    }
                }
                finally
                {
                    foreach (X509Certificate2 certificate in certificates)
                    {
                        certificate.Reset();
                    }
                    store.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot collect the certificate - personal.");
            }
        }

        /* Method to collect certs from Trusted Root Certificate Authorities */
        public void get_Trusted_Root_Certificate_Authorities(string pathAUTH)
        {
            try
            {
                X509Store store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                X509Certificate2Collection certificates = (X509Certificate2Collection)store.Certificates;
                Int32 counter1 = 1, counter2 = 1;
                try
                {
                    foreach (X509Certificate2 certificate in certificates)
                    {
                        string cert1 = "rhev_CA_O_rh";
                        string cert2 = "rhev_CA_O_redhat";
                        byte[] bytes = null;

                        try
                        {
                            /* pfx file */
                            /* We don't need for now to collect the .pfx file, however, here the code. */
                           
                            // if (certificate.SubjectName.Name == "CN=RHEVM CA, O=rh, C=US")
                            //{
                            //
                            //    bytes = certificate.Export(X509ContentType.Pkcs12);
                            //    filename = certificate.SubjectName.Name + ".pfx";
                            //    Console.WriteLine(certificate.Issuer);
                            //    Console.WriteLine(certificate.SubjectName.Name);
                            //    string stringInp1ut = Console.ReadLine();
                            //    File.WriteAllBytes("c:\\" + filename, bytes);
                            //
                            //}

                            /* .cert file */
                            if (certificate.SubjectName.Name.Contains("CN=RHEVM CA"))
                            {
                                bytes = certificate.Export(X509ContentType.Cert);
                                cert1 += counter1 + ".cer";
                                File.WriteAllBytes(pathAUTH + cert1, bytes);
                                counter1 += 1;
                            }
                        }
                        catch (Exception e)
                        {
                            Console.Write("Exception " + e);
                        }
                    }
                }
                finally
                {
                    foreach (X509Certificate2 certificate in certificates)
                    {
                        certificate.Reset();
                    }
                    store.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot collect the certificate.");
            }
        }

        /* Method to collect Trusted Publishers certificate - Red Hat */
        public void get_Trusted_Publishers(string pathPUB)
        {
            try
            {
                X509Store store = new X509Store(StoreName.TrustedPublisher, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                X509Certificate2Collection certificates = (X509Certificate2Collection)store.Certificates;

                try
                {
                    foreach (X509Certificate2 certificate in certificates)
                    {
                        string cert = "rhev_CA_Trusted_Publishers";
                        byte[] bytes = null;

                        try
                        {
                            string s = certificate.SubjectName.Name;
                            /* Getting only Red Hat cert */
                            if (s.IndexOf("Red Hat") != -1)
                            {
                                /* .cert file */
                                bytes = certificate.Export(X509ContentType.Cert);
                                cert += ".cer";
                                File.WriteAllBytes(pathPUB + cert, bytes);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.Write("Exception " + e);
                        }
                    }
                }
                finally
                {
                    foreach (X509Certificate2 certificate in certificates)
                    {
                        certificate.Reset();
                    }
                    store.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot collect the certificate, get_Trusted_Publishers().");
            }    
        }

        public void addCertificateTrustedPublishers(string path)
        {
            /* Load certificate */
            X509Certificate2 cert = new X509Certificate2(path);
            
            /* Place to store cert */
            X509Store store = new X509Store(StoreName.TrustedPublisher, StoreLocation.LocalMachine);

            /* Add cert to the store */
            store.Open(OpenFlags.ReadWrite);
            store.Add(cert);
            store.Close();
        }

        public void addCertificateTrustedRootCertificateAuthorities(string path)
        {
            /* Load certificate */
            X509Certificate2 cert = new X509Certificate2(path);

            /* Place to store cert */
            X509Store store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);

            /* Add cert to the store */
            store.Open(OpenFlags.ReadWrite);
            store.Add(cert);
            store.Close();
        }

        public void addCertificatePersonal(string path)
        {
            /* Load certificate */
            X509Certificate2 cert = new X509Certificate2(path);
            
            /* Place to store cert */
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);

            /* Add cert to the store */
            store.Open(OpenFlags.ReadWrite);
            store.Add(cert);
            store.Close();
        }

        public void addPfxCertificate(string path, string password)
        {
            X509Certificate2 cert = new X509Certificate2(path, password, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            
            store.Open(OpenFlags.MaxAllowed);
            store.Add(cert);
            AddPermissionToCertificate(cert);
            store.Close();
            
        }
        private static void AddPermissionToCertificate(X509Certificate2 cert)
        {
            RSACryptoServiceProvider rsa = cert.PrivateKey as RSACryptoServiceProvider;
            if (rsa == null)
            {
                return;
            }

            string keyfilepath = FindKey(rsa.CspKeyContainerInfo.UniqueKeyContainerName);

            FileInfo file = new FileInfo(System.IO.Path.Combine(keyfilepath, rsa.CspKeyContainerInfo.UniqueKeyContainerName));

            FileSecurity fs = file.GetAccessControl();

            SecurityIdentifier sid = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
            fs.AddAccessRule(new FileSystemAccessRule("NETWORK SERVICE", FileSystemRights.FullControl, AccessControlType.Allow));
            fs.AddAccessRule(new FileSystemAccessRule("Everyone", FileSystemRights.Read, AccessControlType.Allow));
            fs.AddAccessRule(new FileSystemAccessRule("RHEV-M Admins", FileSystemRights.FullControl, AccessControlType.Allow));
            fs.AddAccessRule(new FileSystemAccessRule("Users", FileSystemRights.Read, AccessControlType.Allow));
            
            file.SetAccessControl(fs);
            
        }

        private static string FindKey(string keyFileName)
        {
            string pathCommAppData = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Crypto\RSA\MachineKeys");
            string[] textArray = Directory.GetFiles(pathCommAppData, keyFileName);
            if (textArray.Length > 0)
            {
                return pathCommAppData;
            }

            string pathAppData = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Crypto\RSA\");
            textArray = Directory.GetDirectories(pathAppData);
            if (textArray.Length > 0)
            {
                foreach (string str in textArray)
                {
                    textArray = Directory.GetFiles(str, keyFileName);
                    if (textArray.Length != 0) return str;
                }
            }
            return null;
        }

    }
}