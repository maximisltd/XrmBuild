using System.Collections.Generic;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Config
{
    public class TfsConfig
    {
        public List<CheckInNote> CheckInNotes { get; set; }

        public List<TfsProjectCollectionConfig> ProjectCollections { get; set; }
    }
}