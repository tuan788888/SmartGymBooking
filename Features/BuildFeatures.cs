namespace SmartGymBooking.Features;

public static class BuildFeatures
{
    public static bool AIEnabled =>
#if AI_FEATURES
        true;
#else
        false;
#endif
}
