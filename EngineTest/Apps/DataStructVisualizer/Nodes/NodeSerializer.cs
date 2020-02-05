using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Xml;
using System.IO;

using DataStructVisualizer.WinUtils;

namespace DataStructVisualizer.Nodes
{
    public static class NodeSerializer
    {
        public static void SaveNodeList(List<Node> _nodes, string _path)
        {
            if (_nodes == null || _nodes.Count < 1 || _path == null) return;

            NodeCollectionSerializable ncs = new NodeCollectionSerializable();
            ncs.Items = new List<Node>(_nodes);
            DataContractSerializer dcs = new DataContractSerializer(typeof(NodeCollectionSerializable));

            // write the XML file
            using (FileStream fs = File.Create(_path))
            {
                // OLD (msdn example):
                //XmlDictionaryWriter xdw = XmlDictionaryWriter.CreateTextWriter(fs, Encoding.UTF8);
                //dcs.WriteObject(xdw, ncs);
                //xdw.Flush();

                string xml_declaration = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n";
                byte[] xml_declaration_B = System.Text.Encoding.UTF8.GetBytes(xml_declaration);
                fs.Write(xml_declaration_B, 0, xml_declaration_B.Length);

                string xsl_link = "<?xml-stylesheet type=\"text/xsl\" href=\"Export.xsl\"?>\n";
                byte[] xsl_link_B = System.Text.Encoding.UTF8.GetBytes(xsl_link);
                fs.Write(xsl_link_B, 0, xsl_link_B.Length);

                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Encoding = Encoding.UTF8;
                settings.Indent = true;
                settings.IndentChars = ("\t");
                settings.OmitXmlDeclaration = true;
                XmlWriter xdw = XmlWriter.Create(fs, settings);
                
                dcs.WriteObject(xdw, ncs);
                xdw.Flush();   
            }
        
            // copy the XSL, CSS and JS files to the same directory            
            int ind_backslash = _path.LastIndexOf('\\');
            int ind_slash = _path.LastIndexOf('/');
            int ind = Math.Max(ind_backslash, ind_slash);
            if (ind > 0)
            {
                string dirname = _path.Substring(0,ind);
                // XSL
                string filename_xsl = dirname + "\\Export.xsl";
                File.Delete(filename_xsl);
                File.Copy(".\\Data\\xml\\Export.xsl", filename_xsl);
                // CSS
                string filename_css = dirname + "\\export_style.css";
                File.Delete(filename_css);
                File.Copy(".\\Data\\xml\\export_style.css", filename_css);
                // JS
                string filename_js = dirname + "\\sorttable.js";
                File.Delete(filename_js);
                File.Copy(".\\Data\\xml\\sorttable.js", filename_js);
            }
 
        }

        public static List<Node> RetrieveNodeList(string _path)
        {
            List<Node> list = new List<Node>();
            if (_path == null) return list;

            DataContractSerializer dcs = new DataContractSerializer(typeof(NodeCollectionSerializable));
            
            using (FileStream fs = File.OpenRead(_path))
            {
                XmlReaderSettings settings = new XmlReaderSettings();
                XmlReader xr = XmlReader.Create(fs, settings);

                NodeCollectionSerializable result = dcs.ReadObject(xr) as NodeCollectionSerializable;
                if (result != null)
                {
                    if (result.Items != null && result.Items.Count > 0)
                    {
                        list = new List<Node>(result.Items);
                    }
                }
            }

            // adjust the state of the Node type and its instances
            long nrNodes = 0;
            foreach(Node n in list)
            {
                nrNodes = Math.Max(nrNodes, n.InitAppearance());
            }
            Node.AdjustNrNodesAfterDeserialization(nrNodes);

            return list;
        }

    }

    [DataContract(Name="Node_List", Namespace="http://DataContractSerializer.NodeList/")]
    public class NodeCollectionSerializable
    {
        [DataMember]
        public List<Node> Items;
    }
}
