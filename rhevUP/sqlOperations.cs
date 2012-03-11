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
using System.Collections;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.IO;

namespace rhevUP
{
    class SqlOperations
    {
        public int backupDatabases(string pathBackupDB, string sqlServerName)
        {
            SqlConnection thisConnection = new SqlConnection(@"Server=" + sqlServerName + ";Integrated Security=True");

            try
            {
                thisConnection.Open();
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to connect to SQL Server!!");
                Console.WriteLine("Are you using the correct SQL ServerName?");
                Console.WriteLine("{0} Exception caught.", e);
                Environment.Exit(-1);
            }

            string DB1 = pathBackupDB + @"\rhevm.bak";
            string DB2 = pathBackupDB + @"\rhevm_history.bak";

            SqlCommand thisCommand = thisConnection.CreateCommand();

            thisCommand.CommandText = @"backup database rhevm to disk='" + DB1 + @"'";
            try
            {
                thisCommand.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot proceed with backup rhevm database, please check the following:");
                Console.WriteLine("* Are you using the correct SQL ServerName?");
                Console.WriteLine("* Any problem/timeout with SQL Server?");
                Console.WriteLine("* Make sure no application are using rhevm database at this moment");
                Console.WriteLine("Example: SQL Server Management software, RHEV Configurator, etc.");
                Console.WriteLine("{0} Exception caught.", e);
                Environment.Exit(-1);
            }

            /* if file exists, backup OK */
            if (!File.Exists(DB1))
            {
                Console.WriteLine("Unable to backup rhevm_history cannot continue!");
                return -1;
            }

            /* rhevm_history */            
            thisCommand.CommandText = @"backup database rhevm_history to disk='" + DB2 + @"'";
            try
            {
                thisCommand.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot proceed with backup rhevm database, please check the following:");
                Console.WriteLine("* Are you using the correct SQL ServerName?");
                Console.WriteLine("* Any problem/timeout with SQL Server?");
                Console.WriteLine("* Make sure no application are using rhevm database at this moment");
                Console.WriteLine("Example: SQL Server Management software, RHEV Configurator, etc.");
                Console.WriteLine("{0} Exception caught.", e);
                Environment.Exit(-1);
            }
            thisConnection.Close();

            /* if file exists, backup OK */
            if (!File.Exists(DB2))
            {
                Console.WriteLine("Unable to backup rhevm_history, cannot continue!");
                return -1;
            }
            return 0;
        }

        public int restoreDatabases(string pathBackupDB, string sqlServerName)
        {
            SqlConnection thisConnection = new SqlConnection(@"Server=" + sqlServerName + ";Integrated Security=True");
            try
            {
                thisConnection.Open();
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to connect to SQL Server!!");
                Console.WriteLine("Are you using the correct SQL ServerName?");
                Console.WriteLine("{0} Exception caught.", e);
                Environment.Exit(-1);
            }

            string DB_DISK1 = pathBackupDB + @"\rhevm_history.bak";
            
            /* rhevm_history DB */
            if (!File.Exists(DB_DISK1))
            {
                Console.WriteLine("Unable to verify rhevm_history.bak file, cannot continue!");
                return -1;
            }

            SqlCommand thisCommand = thisConnection.CreateCommand();

            thisCommand.CommandText = "restore database rhevm_history from disk='" + DB_DISK1 + @"' WITH REPLACE";
            try
            {
                thisCommand.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot proceed with restore rhevm_history database, any problem/timeout with SQL Server?");
                Console.WriteLine("tip: Make sure SQL Server Management software is closed.");
                Console.WriteLine("{0} Exception caught.", e);
                Environment.Exit(-1);
            }
        
            /* rhevm DB */
            string DB_DISK2 = pathBackupDB + @"\rhevm.bak";

            if (!File.Exists(DB_DISK1))
            {
                Console.WriteLine("Unable to verify rhevm.bak file, cannot continue!");
                return -1;
            }
            
            thisCommand.CommandText = "restore database rhevm from disk='" + DB_DISK2 + @"' WITH REPLACE";
            try
            {
                thisCommand.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot proceed with restore rhevm database, any problem/timeout with SQL Server?");
                Console.WriteLine("tip: Make sure SQL Server Management software is closed.");
                Console.WriteLine("{0} Exception caught.", e);
                Environment.Exit(-1);
            }
            thisConnection.Close();
            return 0;

        }

        public void setUserPermissionLogin(string sqlServerName)
        {
            /* SQL - example */

            /* use rhevm;
             * select option_value from dbo.vdc_options where option_name='AdUserName';
             * select user_id from users where username='rhevm';
             * update permissions SET role_id='00000000-0000-0000-0000-000000000001' where ad_element_id='7C539955-2485-715B-22DA-9258ED030000'
             */
            SqlConnection thisConnection = new SqlConnection(@"Server=" + sqlServerName + ";database=rhevm;Integrated Security=True");
            
            string option_value = "";
            string user_id = "";
            string user_id_with_domain = "";

            try
            {
                thisConnection.Open();
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to connect to SQL Server!!");
                Console.WriteLine("Are you using the correct SQL ServerName?");
                Console.WriteLine("{0} Exception caught.", e);
                Environment.Exit(-1);
            }

            ////////// option_value /////////////////////
            SqlCommand sqlComm = new SqlCommand("select option_value from dbo.vdc_options where option_name='AdUserName'", thisConnection);
            SqlDataReader r = sqlComm.ExecuteReader();
            
            while (r.Read())
            {
                option_value = (string)r["option_value"];
            }
            r.Close();

            if (option_value == "")
            {
                Console.WriteLine("Unable to locate option_value field!!");
                Environment.Exit(-1);
            }
            /* Console.WriteLine(@"option_value = " + option_value); */

            //////// user_id ////////////////////////
            sqlComm = new SqlCommand("select user_id from users where username='" + option_value + "'", thisConnection);
            r = sqlComm.ExecuteReader();

            while (r.Read())
            {
                user_id = r["user_id"].ToString();
            }
            r.Close();
            
            /* Console.WriteLine(@"user_id = " + user_id); */

            //////////////// Collecting user_id_with_domain //////////////////

            sqlComm = new SqlCommand("select user_id from users where username LIKE '" + option_value + "%' or username LIKE '" + option_value + "@%'", thisConnection);
            r = sqlComm.ExecuteReader();

            while (r.Read())
            {
                user_id_with_domain = r["user_id"].ToString();
            }
            r.Close();

            /* Console.WriteLine(@"user_id_with_domain = " + user_id_with_domain); */
        
            /////////// update permission //////////////
            SqlCommand thisCommand = thisConnection.CreateCommand();

            if (user_id != "")
            {
                thisCommand = thisConnection.CreateCommand();
                thisCommand.CommandText = @"update permissions SET role_id='00000000-0000-0000-0000-000000000001' where ad_element_id='" + user_id + "'";
                thisCommand.ExecuteNonQuery();
            }

            if (user_id_with_domain != "")
            {
                thisCommand = thisConnection.CreateCommand();
                thisCommand.CommandText = @"update permissions SET role_id='00000000-0000-0000-0000-000000000001' where ad_element_id='" + user_id_with_domain + "'";
                thisCommand.ExecuteNonQuery();
            }
            thisConnection.Close();
        }
    }
}