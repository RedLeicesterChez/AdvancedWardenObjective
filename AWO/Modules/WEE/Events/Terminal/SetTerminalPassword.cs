using AWO.Sessions;
using LevelGeneration;
using SNetwork;

namespace AWO.Modules.WEE.Events;

internal sealed class SetTerminalPassword : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.SetTerminalPassword;
    public override bool AllowArrayableGlobalIndex => true;

    protected override void TriggerCommon(WEE_EventData e)
    {
        foreach (var ePassword in e.SetTerminalPassword.Values)
        {
            if (!TryGetTerminalFromZone(e, ePassword.TerminalIndex, out var term)) 
                continue;

            switch (ePassword.Type)
            {
                case WEE_SetTerminalPassword.PasswordEventType.Add:
                    var password = ePassword.Password;

                    if (ePassword.MakePasswordRandom)
                    {
                        //Untested if it syncs with clients but should be fine (I hope)
                        uint seed = EntryPoint.SessionRand.Next();
                        if (ePassword.UseComplexPassword)
                        {
                            password = SerialGenerator.GenerateComplexPassword(seed, ePassword.PasswordLength);
                        }
                        else
                        {
                            //I don't know if this works with brandons LongerCodes mod
                            password = SerialGenerator.GetCodeWord();
                        }
                        Logger.Debug($"Generated random password: {password}");
                    }

                    List<string> passwordParts = new();

                    if (ePassword.ReferenceValueID.Values.Count > 1 && password.Length % ePassword.ReferenceValueID.Values.Count == 0)
                    {
                        //I am not good at keeping track of all of this bullshit
                        char[] activeSplitPassword = new string('-', password.Length).ToCharArray();
                         
                        int splitPartLength = password.Length / ePassword.ReferenceValueID.Values.Count;

                        //Remind me to never touch for loops again I fucking hate this
                        for (int i = 0; i < splitPartLength * ePassword.ReferenceValueID.Values.Count; i += splitPartLength)
                        {
                            string passwordPart = password.Substring(i, splitPartLength);
                            //Logger.Debug($"Separating password into parts current part: {passwordPart}");
                            
                            for (int i2 = 0; i2 < passwordPart.Length; i2++)
                            {
                                activeSplitPassword[i + i2] = passwordPart[i2];
                            }
                            
                            //Logger.Debug($"Split password current split password: {new string (activeSplitPassword)}");
                            passwordParts.Add(new string(activeSplitPassword));
                            activeSplitPassword = new string('-', password.Length).ToCharArray();
                        }

                        foreach (string passwordPart in passwordParts)
                        {
                            Logger.Debug($"Got password part: {passwordPart}");
                        }
                    }

                    int count = 0;
                    foreach (var refValue in ePassword.ReferenceValueID)
                    {
                        if (refValue == 0)
                        {
                            Logger.Debug("RefValue was 0 so it will be ignored");
                        }
                        else if (ePassword.UseReferenceValueAsPassword)
                        {
                            password = "";
                            Logger.Debug($"Setting terminal password from RefernceValue {refValue} Current Password: {password}");
                            password += AWO_ReferenceValues.GetStringReference(refValue);
                        }
                        else if (passwordParts.Count > 1)
                        {
                            AWO_ReferenceValues.SetStringReference(refValue, passwordParts[count]);
                        }
                        else
                        {
                            Logger.Debug("Password couldn't be split so will be set as one password");
                            AWO_ReferenceValues.SetStringReference(refValue, password);
                        }
                        count++;
                    }
                    
                    if (password.Length > 25)
                    {
                        LogWarning($"Password {password} exceeds 25 character limit, trimming to fit!");
                        password = password[..25];
                    }
                    else if (password.Length == 0)
                    {
                        LogError($"Password is empty, cannot add to terminal!");
                        return;
                    }

                    // If already locked, only the answer needs to be changed
                    if (term.IsPasswordProtected)
                    {
                        term.m_password = password;
                        LogDebug($"Updated terminal {ePassword.TerminalIndex} in {e.DimensionIndex}, {e.Layer}, {e.LocalIndex} to password {password}");
                        return;
                    }

                    term.LockWithPassword(password, ePassword.LockedHelpText);
                    // If term has been unlocked previously, input is no longer allowed
                    if (term.CurrentStateName != TERM_State.EnterPassword)
                        term.GetState((int)TERM_State.EnterPassword).Cast<LG_TERM_EnterPassword>().EnableInput();

                    if (SNet.IsMaster)
                    {
                        switch (term.CurrentStateName)
                        {
                            case TERM_State.Sleeping:
                            case TERM_State.Awake:
                            case TERM_State.PasswordProtected:
                            case TERM_State.EnterPassword:
                                break;
                            // I sure hope nothing goes wrong with this
                            default:
                                var interactionSource = term.m_localInteractionSource ?? term.m_syncedInteractionSource;
                                if (interactionSource != null)
                                    term.ChangeState(TERM_State.EnterPassword);
                                else
                                    term.ChangeState(TERM_State.PasswordProtected);
                                break;
                        }
                    }
                    LogDebug($"Updated terminal {ePassword.TerminalIndex} in {e.DimensionIndex}, {e.Layer}, {e.LocalIndex} to password {password}");
                    break;

                case WEE_SetTerminalPassword.PasswordEventType.Remove:
                    if (!term.IsPasswordProtected)
                        return;

                    term.IsPasswordProtected = false;
                    var cmd = term.m_command;
                    cmd.AddOutput("");
                    cmd.AddOutput(TerminalLineType.Normal, "<color=green>Security protocol deactivated.</color>");
                    cmd.AddOutput(TerminalLineType.Normal, "<color=green>Terminal unlocked.</color>");
                    cmd.AddOutput("");
                    if (SNet.IsMaster)
                    {
                        switch (term.CurrentStateName)
                        {
                            case TERM_State.PasswordProtected:
                                term.ChangeState(TERM_State.Awake);
                                break;
                            case TERM_State.EnterPassword:
                                term.ChangeState(TERM_State.PlayerInteracting);
                                break;
                        }
                    }
                    LogDebug($"Removed password from terminal {ePassword.TerminalIndex} in {e.DimensionIndex}, {e.Layer}, {e.LocalIndex}");
                    break;
            }
        }
    }
}