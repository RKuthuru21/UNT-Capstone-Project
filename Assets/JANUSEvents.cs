using System;

public static class JANUSEvents
{
    // Menu Events
    public static Action OnMenuOpened;
    public static Action OnMenuClosed;

    // Selection Events
    public static Action<FloorPlanData> OnFloorPlanSelected;
    public static Action<string> OnModuleSelected;

    // Session Logic Events
    public static Action<string> OnModuleBegin; 
    public static Action OnSessionPaused;
    public static Action OnSessionResumed;
    public static Action<string, float> OnSessionEnded;

    // --- ADD THIS LINE BELOW ---
    public static Action<string> OnHardwareWarning; 
    // ---------------------------
}