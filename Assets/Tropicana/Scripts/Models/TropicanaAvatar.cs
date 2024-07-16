using System.Collections.Generic;
using UnityEngine;

namespace Tropicana.Models
{
    [System.Serializable]
    public class TropicanaAvatar
    {
        public string Uri;
        public float PositionX;
        public float PositionZ;
        public float Orientation;
        public List<TropicanaAvatarState> States;
    }

    public enum StateAnimation {
        Idle,
        Wave,
        Talk
    }

    [System.Serializable]
    public class TropicanaAvatarState
    {
        public string Name;
        // initial state and state to return to when leaving all trigger distances
        public bool IsDefaultState;
        // 0 if not triggered by distance
        public float TriggerDistance;
        public string AudioUri;
        public string MediaUri;
        public MediaType MediaType;
        public string Text;
        public StateAnimation Animation;
        public bool TurnTowardsUser;
        public List<TropicanaAvatarButton> Buttons;
    }

    public enum AvatarButtonAction {
        TriggerState,
        Information,
        PlayMedia,
        InteractWithShelfUnit,
        WalkToPointAndTriggerState
    }

    // For PlayMedia AvatarButtonAction
    public enum MediaType {
        Image,
        Image360,
        Video,
        VideoFullScreen,
        Video360,
        ImageSlideshow
    }

    // For TriggerState AvatarButtonAction
    public enum PlayStateAudio {
        Play,
        PlayOnce,
        DoNotPlay
    }

    // For InteractWithShelfUnit AvatarButtonAction
    public enum ShelfInteraction {
        ToggleSets,
        ToggleBanners,
        ToggleOverlay,
        ToggleTopProducts,
        ToggleInnovations
    }

    [System.Serializable]
    public class TropicanaAvatarButton
    {
        public string Text;
        public AvatarButtonAction Action;
        public string ActionData;
        public MediaType MediaType;
        public ShelfInteraction ShelfInteraction;
        public PlayStateAudio PlayStateAudio;
        public string OpenLinkUri;
        public string DownloadFileUri;
        public string PathName;
        public int WayPointNumber;
    }
}