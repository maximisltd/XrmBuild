using Maximis.Toolkit.Logging;
using Maximis.Toolkit.Xml;
using Maximis.Toolkit.Xrm.Development.BuildManagement.Actions;
using Maximis.Toolkit.Xrm.Development.BuildManagement.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace BuildManager
{
    internal class Program
    {
        private static void ConvertDataXml(string source, string target)
        {
            // Load Source XML (Schema File from SDK Data Migration Utility)
            XmlDocument xml = new XmlDocument();
            xml.Load(source);

            // Create List of DataConfig objects
            List<DataConfig> dataConfigs = new List<DataConfig>();

            // Populate from XML
            foreach (XmlElement entityNode in xml.SelectNodes("//entity"))
            {
                DataConfig dataConfig = new DataConfig { EntityName = entityNode.GetAttribute("name"), Attributes = new List<string>() };
                foreach (XmlElement fieldNode in entityNode.SelectNodes("fields/field"))
                {
                    if (fieldNode.GetAttribute("primaryKey") == "true") continue;
                    dataConfig.Attributes.Add(fieldNode.GetAttribute("name"));
                }
                dataConfig.Attributes.Sort();
                dataConfigs.Add(dataConfig);
            }

            // Save to file
            dataConfigs.Sort(delegate(DataConfig left, DataConfig right) { return left.EntityName.CompareTo(right.EntityName); });
            SerialisationHelper.SerialiseToFile<List<DataConfig>>(dataConfigs, target);
        }

        private static void Main(string[] args)
        {
            // Add Console  Trace Listener
            Trace.Listeners.Add(new ConsoleTraceListener());

            // Simple check for at least 3 arguments
            if (args.Length < 3)
            {
                Trace.WriteLine("Usage: BuildManager.exe ConfigFolderPath EnvironmentName ActionName [OrgName1] [OrgName2] [OrgName3] etc.");
                return;
            }

            // Add File Trace Listener
            Trace.Listeners.Add(new LogFileTraceListener(Path.Combine(Environment.CurrentDirectory, string.Format("Trace\\{0:yyyyMMddHHmmss}_{1}.txt", DateTime.Now, args[2]))));

            // Special case - convert SDK Data Migration Utility XML to Static Data Export XML
            if (args[0] == "ConvertDataXml")
            {
                ConvertDataXml(args[1], args[2]);
                return;
            }

            // Get additional arguments
            List<string> orgUniqueNames = new List<string>();
            if (args.Length > 3)
            {
                for (int i = 3; i < args.Length; i++)
                    orgUniqueNames.Add(args[i]);
            }

            // Read Config from File (Deserialise XML)
            XrmBuildConfig config = ConfigSerialiser.Load(args[0]);
            if (config == null)
            {
                Trace.WriteLine("Could not read config!");
                return;
            }

            // Create Instance of Action
            string actionTypeName = string.Format("{0}Action", args[2]);
            Assembly actionAssembly = Assembly.GetAssembly(typeof(BaseAction));
            Type actionType = actionAssembly.GetTypes().FirstOrDefault(q => q.Name.Contains(actionTypeName));
            if (actionType == null)
            {
                Trace.WriteLine(string.Format("Invalid action type: '{0}'", args[2]));
                return;
            }
            BaseAction action = (BaseAction)Activator.CreateInstance(actionType);

            // Perform Action
            action.PerformAction(config, args[1], orgUniqueNames.ToArray());

            Trace.WriteLine(string.Empty);
            Trace.WriteLine(string.Empty);
            Trace.WriteLine("OPERATION COMPLETE.");
        }
    }
}