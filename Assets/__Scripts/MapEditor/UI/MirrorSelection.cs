﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MirrorSelection : MonoBehaviour
{
    [SerializeField] private NoteAppearanceSO noteAppearance;
    [SerializeField] private EventAppearanceSO eventAppearance;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private CreateEventTypeLabels labels;

    private readonly Dictionary<int, int> cutDirectionToMirrored = new Dictionary<int, int>
    {
        {BeatmapNote.NoteCutDirectionDownLeft, BeatmapNote.NoteCutDirectionDownRight},
        {BeatmapNote.NoteCutDirectionDownRight, BeatmapNote.NoteCutDirectionDownLeft},
        {BeatmapNote.NoteCutDirectionUpLeft, BeatmapNote.NoteCutDirectionUpRight},
        {BeatmapNote.NoteCutDirectionUpRight, BeatmapNote.NoteCutDirectionUpLeft},
        {BeatmapNote.NoteCutDirectionRight, BeatmapNote.NoteCutDirectionLeft},
        {BeatmapNote.NoteCutDirectionLeft, BeatmapNote.NoteCutDirectionRight}
    };

    public void MirrorTime()
    {
        if (!SelectionController.HasSelectedObjects())
        {
            PersistentUI.Instance.DisplayMessage("Mapper", "mirror.error", PersistentUI.DisplayMessageType.Bottom);
            return;
        }

        var ordered = SelectionController.SelectedObjects.OrderByDescending(x => x.Time);
        var end = ordered.First().Time;
        var start = ordered.Last().Time;
        var allActions = new List<BeatmapAction>();
        foreach (var con in SelectionController.SelectedObjects)
        {
            var edited = BeatmapObject.GenerateCopy(con);
            edited.Time = start + (end - con.Time);
            allActions.Add(new BeatmapObjectModifiedAction(edited, con, con, "e", true));
        }

        var actionCollection =
            new ActionCollectionAction(allActions, true, true, "Mirrored a selection of objects in time.");
        BeatmapActionContainer.AddAction(actionCollection, true);
    }

    public void Mirror(bool moveNotes = true)
    {
        if (!SelectionController.HasSelectedObjects())
        {
            PersistentUI.Instance.DisplayMessage("Mapper", "mirror.error", PersistentUI.DisplayMessageType.Bottom);
            return;
        }

        var events = BeatmapObjectContainerCollection.GetCollectionForType<EventsContainer>(BeatmapObject.ObjectType.Event);
        var allActions = new List<BeatmapAction>();
        foreach (var con in SelectionController.SelectedObjects)
        {
            var original = BeatmapObject.GenerateCopy(con);
            if (con is BeatmapObstacle obstacle && moveNotes)
            {
                var precisionWidth = obstacle.Width >= 1000;
                var state = obstacle.LineIndex;
                if (obstacle.CustomData != null) //Noodle Extensions
                {
                    if (obstacle.CustomData.HasKey(MapLoader.heckPosition))
                    {
                        Vector2 oldPosition = obstacle.CustomData[MapLoader.heckPosition];
                        
                        var flipped = new Vector2(oldPosition.x * -1, oldPosition.y);

                        if (obstacle.CustomData.HasKey(MapLoader.heckScale))
                        {
                            Vector2 scale = obstacle.CustomData[MapLoader.heckScale];
                            flipped.x -= scale.x;
                        }
                        else
                        {
                            flipped.x -= obstacle.Width;
                        }

                        obstacle.CustomData[MapLoader.heckPosition] = flipped;
                    }
                }

                if (state >= 1000 || state <= -1000 || precisionWidth) // precision lineIndex
                {
                    var newIndex = state;
                    if (newIndex <= -1000) // normalize index values, we'll fix them later
                        newIndex += 1000;
                    else if (newIndex >= 1000)
                        newIndex -= 1000;
                    else
                        newIndex *= 1000; //convert lineIndex to precision if not already
                    newIndex = ((newIndex - 2000) * -1) + 2000; //flip lineIndex

                    var newWidth = obstacle.Width; //normalize wall width
                    if (newWidth < 1000)
                        newWidth *= 1000;
                    else
                        newWidth -= 1000;
                    newIndex -= newWidth;

                    if (newIndex < 0)
                        //this is where we fix them
                        newIndex -= 1000;
                    else
                        newIndex += 1000;
                    obstacle.LineIndex = newIndex;
                }
                else // state > -1000 || state < 1000 assumes no precision width
                {
                    var mirrorLane = ((state - 2) * -1) + 2; //flip lineIndex
                    obstacle.LineIndex = mirrorLane - obstacle.Width; //adjust for wall width
                }
            }
            else if (con is BeatmapNote note)
            {
                if (note is BeatmapColorNote cnote) cnote.AngleOffset *= -1;
                if (moveNotes)
                {
                    if (note.CustomData != null)
                    {
                        // NE Precision rotation
                        if (note.CustomData.HasKey(MapLoader.heckPosition))
                        {
                            Vector2 oldPosition = note.CustomData[MapLoader.heckPosition];
                            var flipped = new Vector2(((oldPosition.x + 0.5f) * -1) - 0.5f, oldPosition.y);
                            note.CustomData[MapLoader.heckPosition] = flipped;
                        }
                        
                        // NE precision cut direction
                        if (note.CustomData.HasKey("_cutDirection"))
                        {
                            var cutDirection = note.CustomData["_cutDirection"].AsFloat;
                            note.CustomData["_cutDirection"] = cutDirection * -1;
                        }
                    }
                    else
                    {
                        var state = note.LineIndex; // flip line index
                        if (state > 3 || state < 0) // precision case
                        {
                            var newIndex = state;
                            if (newIndex <= -1000) // normalize index values, we'll fix them later
                                newIndex += 1000;
                            else if (newIndex >= 1000) newIndex -= 1000;

                            newIndex = ((newIndex - 1500) * -1) + 1500; //flip lineIndex

                            if (newIndex < 0) //this is where we fix them
                                newIndex -= 1000;
                            else
                                newIndex += 1000;

                            note.LineIndex = newIndex;
                        }
                        else
                        {
                            var mirrorLane = (int)(((state - 1.5f) * -1) + 1.5f);
                            note.LineIndex = mirrorLane;
                        }
                    }
                }

                //flip colors
                if (note.Type != BeatmapNote.NoteTypeBomb)
                {
                    note.Type = note.Type == BeatmapNote.NoteTypeA
                        ? BeatmapNote.NoteTypeB
                        : BeatmapNote.NoteTypeA;

                    //flip cut direction horizontally
                    if (moveNotes && cutDirectionToMirrored.ContainsKey(note.CutDirection))
                        note.CutDirection = cutDirectionToMirrored[note.CutDirection];
                }
            }
            else if (con is MapEvent e)
            {
                if (e.IsRotationEvent)
                {
                    if (e.CustomData != null && e.CustomData.HasKey(MapLoader.heckRotation))
                        e.CustomData[MapLoader.heckRotation] = e.CustomData[MapLoader.heckRotation].AsFloat * -1;

                    var rotation = e.GetRotationDegreeFromValue();
                    if (rotation != null)
                    {
                        if (e.Value >= 0 && e.Value < MapEvent.LightValueToRotationDegrees.Length)
                            e.Value = MapEvent.LightValueToRotationDegrees.ToList().IndexOf((rotation ?? 0) * -1);
                        else if (e.Value >= 1000 && e.Value <= 1720) //Invert Mapping Extensions rotation
                            e.Value = 1720 - (e.Value - 1000);
                    }

                    tracksManager.RefreshTracks();
                }
                else
                {
                    if (e.LightGradient != null)
                    {
                        var startColor = e.LightGradient.StartColor;
                        e.LightGradient.StartColor = e.LightGradient.EndColor;
                        e.LightGradient.EndColor = startColor;
                    }

                    if (e.IsUtilityEvent) continue;
                    if (moveNotes && e.IsPropogationEvent && events.EventTypeToPropagate == e.Type &&
                        events.PropagationEditing == EventsContainer.PropMode.Prop)
                    {
                        var mirroredIdx = events.EventTypePropagationSize - e.PropId - 1;
                        e.CustomData[MapLoader.heckUnderscore + "lightID"] = labels.PropIdToLightIdsJ(e.Type, mirroredIdx);
                    }
                    else if (moveNotes && e.IsLightIdEvent && events.EventTypeToPropagate == e.Type &&
                             events.PropagationEditing == EventsContainer.PropMode.Light)
                    {
                        var idx = labels.LightIDToEditor(e.Type, e.LightId[0]);
                        var mirroredIdx = events.EventTypePropagationSize - idx - 1;
                        e.CustomData[MapLoader.heckUnderscore + "lightID"] = labels.EditorToLightID(e.Type, mirroredIdx);
                    }

                    if (e.Value > 4 && e.Value <= 8) e.Value -= 4;
                    else if (e.Value > 0 && e.Value <= 4) e.Value += 4;
                    else if (e.Value > 8 && e.Value <= 12) e.Value -= 4; // white to red
                }
            }
            else if (Settings.Instance.Load_MapV3)
            {
                if (con is BeatmapArc arc)
                {
                    if (moveNotes)
                    {
                        arc.X = Mathf.RoundToInt(((arc.X - 1.5f) * -1) + 1.5f);
                        if (cutDirectionToMirrored.ContainsKey(arc.Direction))
                            arc.Direction = cutDirectionToMirrored[arc.Direction];

                        arc.TailX = Mathf.RoundToInt(((arc.TailX - 1.5f) * -1) + 1.5f);
                        if (cutDirectionToMirrored.ContainsKey(arc.TailCutDirection))
                            arc.TailCutDirection = cutDirectionToMirrored[arc.TailCutDirection];
                    }
                    arc.Color = arc.Color == BeatmapNote.NoteTypeA
                        ? BeatmapNote.NoteTypeB
                        : BeatmapNote.NoteTypeA;

                }
                else if (con is BeatmapChain chain)
                {
                    if (moveNotes)
                    {
                        chain.X = Mathf.RoundToInt(((chain.X - 1.5f) * -1) + 1.5f);
                        if (cutDirectionToMirrored.ContainsKey(chain.Direction))
                            chain.Direction = cutDirectionToMirrored[chain.Direction];

                        chain.TailX = Mathf.RoundToInt(((chain.TailX - 1.5f) * -1) + 1.5f);
                    }
                    chain.Color = chain.Color == BeatmapNote.NoteTypeA
                        ? BeatmapNote.NoteTypeB
                        : BeatmapNote.NoteTypeA;
                }
                else if (con is BeatmapLightColorEvent colorEvent)
                {
                    foreach (var data in colorEvent.EventBoxes[0].EventDatas)
                    {
                        // white / blue -> red
                        // red -> blue
                        data.Color = data.Color == 0 ? 1 : 0;
                    }
                }
            }

            allActions.Add(new BeatmapObjectModifiedAction(con, con, original, "e", true));
        }

        foreach (var unique in SelectionController.SelectedObjects.DistinctBy(x => x.BeatmapType))
            BeatmapObjectContainerCollection.GetCollectionForType(unique.BeatmapType).RefreshPool(true);
        BeatmapActionContainer.AddAction(new ActionCollectionAction(allActions, true, true,
            "Mirrored a selection of objects."));
    }
}
