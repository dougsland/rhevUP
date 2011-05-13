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
using System.Xml;

namespace rhevUP
{
    class xmlOperations
    {
        public string getEntry(string pathToXML, string entryToLook)
        {
            XmlTextReader reader = new XmlTextReader (pathToXML);
            string tmpStr = "";

            while (reader.Read()) 
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element: 
                        tmpStr = reader.Name;
                        break;
                    case XmlNodeType.Text:      /* Display the text in each element. */
                        if (tmpStr == entryToLook)
                        {
                            tmpStr = reader.Value;
                            reader.Close();
                            return tmpStr;
                        }
                        break;
                    /* No needed, just example */
                    /* case XmlNodeType.EndElement: //Display the end of the element.
                     *   Console.Write("</" + reader.Name);
                     *   Console.WriteLine(">");
                     *   break; 
                     */
                }
            }
            reader.Close();
            return null;
            /* Console.ReadLine(); */
        }

        public void setEntry(string path, string entry)
        {
            XmlNode node;
            XmlDocument myXmlDocument = new XmlDocument(); myXmlDocument.Load(path);
            node = myXmlDocument.DocumentElement;

            foreach (XmlNode node1 in node.ChildNodes) {
                foreach (XmlNode node2 in node1.ChildNodes)
                {
                    if (node2.FirstChild != null)
                    {
                        if (node2.FirstChild.Name == "CipherData")
                        {
                            /* updating CipherValue */
                            node2.FirstChild.ChildNodes[0].InnerText = entry;
                            myXmlDocument.Save(path);
                        }
                    }
                }
            }
        }

    }
}