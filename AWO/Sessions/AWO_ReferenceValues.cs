namespace AWO.Sessions;

/*
 * Rather long and windy comment but bear with me please
 * This exists because I wanted to create random terminal passwords and set logs with those random passwords
 * Because Dino didn't add a random option to the SetTerminal password event (OR DOCUMENTATION)
 * I added this myself
 * In the future this can be expanded upon to add RichText to vanilla warden objective datablock or other events
 * hopefully this won't get me killed - RLC
 */
public static class AWO_ReferenceValues
{
    internal static void Cleanup()
    {
        Logger.Debug("Cleaning up all reference values");
        StringReferences.Clear();
        FloatReferences.Clear();
    }
    
    internal static void SetStringReference(int referenceKey, string value)
    {
        if (referenceKey != 0)
        {
            if (!AWO_ReferenceValues.StringReferences.TryAdd(referenceKey, value))
            {
                Logger.Debug($"String reference with id: {referenceKey} already exists setting String to: {value}");
                AWO_ReferenceValues.StringReferences[referenceKey] = value;
            }
            else
            {
                Logger.Debug($"Set String reference with id: {referenceKey} and String of: {value}");
            }

            return;
        }
        
        Logger.Debug("String reference key was 0 so no reference will be changed");
    }
    
    internal static void SetNumberReference(int referenceKey, float value)
    {
        if (referenceKey != 0)
        {
            if (!AWO_ReferenceValues.FloatReferences.TryAdd(referenceKey, value))
            {
                Logger.Debug($"Number reference with id: {referenceKey} already exists setting value to: {value}");
                AWO_ReferenceValues.FloatReferences[referenceKey] = value;
            }
            else
            {
                Logger.Debug($"Set number reference with id: {referenceKey} and value of: {value}");
            }

            return;
        }
        
        Logger.Debug("Number reference key was 0 so no reference will be changed");
    }

    public static string GetStringReference(int referenceKey)
    {
        if (referenceKey != 0)
        {
            if (StringReferences.TryGetValue(referenceKey, out string password))
            {
                return password;
            }
            Logger.Error($"Failed to get String with reference key: {referenceKey} returning an empty string");
            return "";
        }

        Logger.Error("Tried getting a String reference but the key was 0 will return an empty string");
        return "";
    }
    
    public static float GetFloatReference(int referenceKey)
    {
        if (referenceKey != 0)
        {
            if (FloatReferences.TryGetValue(referenceKey, out float value))
            {
                return value;
            }
            Logger.Error($"Failed to get number with reference key: {referenceKey} returning as 0");
            return 0;
        }

        Logger.Error("Tried getting a number reference but the key was 0 will return as 0");
        return 0;
    }
    
    //Usage: int = ReferenceKey, string = password
    //Setting a password with an existing key will just replace it (proper usage is on rundown devs) put this in the non existent documentation
    //We will only create a reference key if the key itself is NOT "0"
    public static Dictionary<int, string> StringReferences = new();
    
    //Usage: int = ReferenceKey, float = Value dunno if this will be used elsewhere yet
    public static Dictionary<int, float> FloatReferences = new();
}