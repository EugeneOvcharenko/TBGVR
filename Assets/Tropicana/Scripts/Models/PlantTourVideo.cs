using System.Collections.Generic;

namespace Tropicana.Models
{

    [System.Serializable]
    public class PlantTourVideoGroup
    {
        public int Id;
        public string Name;
    }

    [System.Serializable]
    public class PlantTourVideo
    {
      public string FileName;
      public string Title;
      public string FileNameLinkA;
      public string TeaserLinkA;
      public string FileNameLinkB;
      public string TeaserLinkB;
      public string PopUpTitle;
      public string PopUpBody;
      public float XPosition;
      public float YPosition;
      public PlantTourVideoGroup VideoGroup;
    }

    public struct InfoNode
    {
      public string PopUpTitle;
      public string PopUpBody;
      public float XPosition;
      public float YPosition;
    }

}