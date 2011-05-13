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


namespace rhevUP
{
    class fileOperations
    {
        public void createDir(string targetPath)
        {
            /* Create a new target folder, if necessary. */
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }
        }

        public int copyFile(string src, string dest)
        {
            /* Create a new target folder */
            if (!File.Exists(src))
            {
                Console.WriteLine("Unable to find src from copy operation!");
                return -1;
            }
            File.Copy(src, dest, true);
            return 0;
        }

        public int backupServiceCA_DIR(DirectoryInfo source, DirectoryInfo target)
        {
            /* Check if the target directory exists */
            if (Directory.Exists(target.FullName) == false)
            {
                Console.WriteLine("Unable to find *dest* path to copy Service CA dir!");
                return -1;
            }

            /* Check if the source directory exists */
            if (Directory.Exists(source.FullName) == false)
            {
                Console.WriteLine("Unable to find *source* path to copy Service CA dir!");
                return -1;
            }

            /* Copy each file into it’s new directory. */
            foreach (FileInfo fi in source.GetFiles())
            {
                Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
                fi.CopyTo(Path.Combine(target.ToString(), fi.Name), true);
            }

            /* Copy each subdirectory using recursion. */
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
                backupServiceCA_DIR(diSourceSubDir, nextTargetSubDir);
            }

            return 0;
        }   
    }
}