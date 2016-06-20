using Maximis.Toolkit.Xml;
using Maximis.Toolkit.Xrm.Development.Customisation;
using System.Collections.Generic;
using System.IO;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Config
{
    public static class ConfigSerialiser
    {
        public static XrmBuildConfig Load(string path)
        {
            if (path.EndsWith(".xml"))
            {
                return DeserialiseIfExists<XrmBuildConfig>(path);
            }
            else
            {
                XrmBuildConfig config = new XrmBuildConfig();
                config.Publisher = DeserialiseIfExists<PublisherDefinition>(Path.Combine(path, "Publisher.xml"));
                config.SourceControl = DeserialiseIfExists<SourceControlConfig>(Path.Combine(path, "SourceControl.xml"));
                config.DataImportExport = DeserialiseIfExists<List<DataConfig>>(Path.Combine(path, "DataImportExport.xml"));
                config.Environments = new List<EnvironmentConfig>();
                foreach (string envPath in Directory.EnumerateFiles(Path.Combine(path, "Environments")))
                {
                    config.Environments.Add(SerialisationHelper.DeserialiseFromFile<EnvironmentConfig>(envPath));
                }
                return config;
            }
        }

        public static void Save(XrmBuildConfig config, string path)
        {
            if (path.EndsWith(".xml"))
            {
                SerialisationHelper.SerialiseToFile<XrmBuildConfig>(config, path);
            }
            else
            {
                SerialisationHelper.SerialiseToFile<PublisherDefinition>(config.Publisher, Path.Combine(path, "Publisher.xml"));
                SerialisationHelper.SerialiseToFile<SourceControlConfig>(config.SourceControl, Path.Combine(path, "SourceControl.xml"));
                SerialisationHelper.SerialiseToFile<List<DataConfig>>(config.DataImportExport, Path.Combine(path, "DataImportExport.xml"));
                foreach (EnvironmentConfig environment in config.Environments)
                {
                    SerialisationHelper.SerialiseToFile<EnvironmentConfig>(environment, Path.Combine(path, "Environments", string.Format("{0}.xml", environment.UniqueName)));
                }
            }
        }

        private static T DeserialiseIfExists<T>(string path)
        {
            if (File.Exists(path)) return SerialisationHelper.DeserialiseFromFile<T>(path);
            else return (default(T));
        }
    }
}