using AmorLib.Utils.JsonElementConverters;
using AWO.Sessions;
using Dissonance;
using GameData;
using GTFO.API;
using LevelGeneration;

namespace AWO.Modules.WEE.Events;

internal sealed class SetTerminalLog : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.SetTerminalLog;
    public override bool AllowArrayableGlobalIndex => true;

    public static readonly Dictionary<(uint, string), Queue<WardenObjectiveEventData>> LogEventQueue = new();

    protected override void OnSetup()
    {
        LevelAPI.OnLevelCleanup += LogEventQueue.Clear;
    }

    protected override void TriggerCommon(WEE_EventData e)
    {
        foreach (var eLog in e.SetTerminalLog.Values)
        {
            if (!TryGetTerminalFromZone(e, eLog.TerminalIndex, out var term)) 
                continue;

            var events = ResolveFieldsFallback(e.Events, eLog.EventsOnFileRead);
            var filename = eLog.FileName.ToUpper();
            switch (eLog.Type)
            {
                case WEE_SetTerminalLog.LogEventType.Add:
                    if (!LogAbsentOn(term))
                    {
                        continue;
                    }
                    else if (eLog.FileContent == LocaleText.Empty)
                    {
                        LogError("Terminal log's FileContent cannot be empty.");
                        continue;
                    }

                    if (eLog.FileNameReferenceValueID != 0)
                    {
                        eLog.FileName = AWO_ReferenceValues.GetStringReference(eLog.FileNameReferenceValueID);
                    }
                    if (eLog.FileContentReferenceValueID != 0)
                    {
                        LocaleText text = new LocaleText(AWO_ReferenceValues.GetStringReference(eLog.FileContentReferenceValueID));
                        eLog.FileContent = text; //I fucking hate TextDB
                    }

                    term.AddLocalLog(new()
                    {
                        FileName = filename,
                        FileContent = eLog.FileContent,
                        FileContentOriginalLanguage = eLog.FileContentOriginalLanguage,
                        AttachedAudioFile = eLog.AttachedAudioFile,
                        AttachedAudioByteSize = eLog.AttachedAudioByteSize,
                        PlayerDialogToTriggerAfterAudio = eLog.PlayerDialogToTriggerAfterAudio
                    });
                    if (events.Any())
                    {
                        LogEventQueue[(term.SyncID, filename)] = new(events);
                    }
                    break;

                case WEE_SetTerminalLog.LogEventType.Remove:
                    if (LogPresentOnSrc())
                    {
                        term.RemoveLocalLog(filename);
                    }
                    break;

                case WEE_SetTerminalLog.LogEventType.Move:
                    if (!LogPresentOnSrc())
                    {
                        continue;
                    }
                    if (!eLog.TryGetTargetTerminal(out var targetTerm))
                    {
                        LogError("Failed to find target terminal");
                        continue;
                    }
                    else if (!LogAbsentOn(targetTerm))
                    {
                        continue;
                    }

                    var logData = term.GetLocalLogs()[filename];
                    targetTerm.AddLocalLog(logData);
                    term.RemoveLocalLog(filename);
                    if (LogEventQueue.TryGetValue((term.SyncID, filename), out var eventQueue))
                    {
                        LogEventQueue.Remove((term.SyncID, filename));
                        LogEventQueue[(targetTerm.SyncID, filename)] = eventQueue;
                    }
                    break;
            }

            bool LogPresentOnSrc()
            {
                if (term.GetLocalLogs().ContainsKey(filename)) return true;
                LogError($"Source terminal does not contain log file with filename {filename}!");
                return false;
            }

            bool LogAbsentOn(LG_ComputerTerminal t)
            {
                if (!t.GetLocalLogs().ContainsKey(filename)) return true;
                LogError($"A log file with filename {filename} is already present on terminal!");
                return false;
            }
        }
    }
}