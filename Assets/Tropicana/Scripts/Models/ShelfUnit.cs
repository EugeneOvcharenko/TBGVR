using System.Collections.Generic;

namespace Tropicana.Models
{

    [System.Serializable]
    public class ShelfUnit
    {
        public int Id;
        public List<Planogram> Planograms;
        public List<ShelfUnitMenuButton> MenuButtons;
    }

    [System.Serializable]
    public class ShelfUnitMenuButton
    {
        public string Text;
        public ShelfUnitButtonAction Action;
        public string ActionData;
        public MediaType MediaType;
        public string OpenLinkUri;
        public string DownloadFileUri;

        public List<ShelfUnitMenuSecondaryButton> SecondaryButtons;
    }

    [System.Serializable]
    public class ShelfUnitMenuSecondaryButton
    {
        public string Text;
        public ShelfUnitButtonAction Action;
        public string ActionData;
        public MediaType MediaType;
        public string OpenLinkUri;
        public string DownloadFileUri;
    }

    public enum ShelfUnitButtonAction {
        PlayMedia,
        ToggleButtonsAbove,
        TogglePlanograms,
        ToggleBanners,
        ToggleOverlays,
        ToggleTopProducts,
        ToggleInnovations,
    }

    [System.Serializable]
    public class Planogram
    {
        public string Name;
        public List<PlanogramShelf> Shelves;
        public List<PlanogramBanner> Banners;
        public List<PlanogramOverlay> Overlays;
        public List<PlanogramTopBlock> TopBlocks;
        public List<PlanogramOverlay> TopProducts;
        public List<PlanogramOverlay> Innovations;
        public float ProductSpacing;
    }

    [System.Serializable]
    public class PlanogramShelf
    {
        public List<PlanogramShelfRow> Rows;
    }

    [System.Serializable]
    public class PlanogramShelfRow
    {
        public List<string> Products;
    }

    [System.Serializable]
    public class PlanogramBanner
    {
        public string Image;
        public float Width;
        public float Height;
        public float HorizontalPosition;
        public float VerticalPosition;
        public bool IsParallel;
    }

    [System.Serializable]
    public class PlanogramOverlay
    {
        public string Color;
        public List<string> Prefabs;
        public string Text;
        public string FontColor;
        public string TextBoxBackgroundColor;
        public float TextBoxWidth;
        public float TextBoxHeight;
        public float TextBoxHorizontalPosition;
        public float TextBoxVerticalPosition;
        public string MediaUri;
        public MediaType MediaType;
        public string OpenLinkUri;
        public string DownloadFileUri;
    }

    [System.Serializable]
    public class PlanogramTopBlock
    {
        public string Image;
        public string Text;
        public string FontColor;
        public string BackgroundColor;
    }
}