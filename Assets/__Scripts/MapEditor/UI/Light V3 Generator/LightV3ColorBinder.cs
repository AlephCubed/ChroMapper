using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class LightV3ColorBinder : MetaLightV3Binder<BeatmapLightColorEvent>, CMInput.IEventUIActions, CMInput.IWorkflowsActions
{
    public int DataIdx = 0;
    [SerializeField] private LightColorEventPlacement lightColorEventPlacement;
    protected override void InitBindings()
    {
        ObjectData = new BeatmapLightColorEvent();

        InputDumpFn.Add(x => (x.EventBoxes[0].Filter.FilterType == 1 ? x.EventBoxes[0].Filter.Section + 1 : x.EventBoxes[0].Filter.Section).ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].Filter.Partition.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].Distribution.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].BrightnessDistribution.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].EventDatas[DataIdx].AddedBeat.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].EventDatas[DataIdx].Color.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].EventDatas[DataIdx].Brightness.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].EventDatas[DataIdx].FlickerFrequency.ToString());

        DropdownDumpFn.Add(x => x.EventBoxes[0].Filter.FilterType - 1);
        DropdownDumpFn.Add(x => x.EventBoxes[0].DistributionType - 1);
        DropdownDumpFn.Add(x => x.EventBoxes[0].BrightnessDistributionType - 1);
        DropdownDumpFn.Add(x => x.EventBoxes[0].EventDatas[DataIdx].TransitionType);

        TextsDumpFn.Add(x => x.EventBoxes[0].Filter.FilterType == 1 ? "Section" : "Step");
        TextsDumpFn.Add(x => x.EventBoxes[0].Filter.FilterType == 1 ? "Partition" : "Start");
        TextsDumpFn.Add(x => $"{DataIdx + 1}/{x.EventBoxes[0].EventDatas.Count}");

        ToggleDumpFn.Add(x => x.EventBoxes[0].Filter.Reverse == 1);
        ToggleDumpFn.Add(x => x.EventBoxes[0].BrightnessAffectFirst == 1);

        InputLoadFn.Add((x, s) => x.EventBoxes[0].Filter.Section = x.EventBoxes[0].Filter.FilterType == 1 ? int.Parse(s) - 1 : int.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].Filter.Partition = int.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].Distribution = float.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].BrightnessDistribution = float.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].EventDatas[DataIdx].AddedBeat = float.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].EventDatas[DataIdx].Color = int.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].EventDatas[DataIdx].Brightness = float.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].EventDatas[DataIdx].FlickerFrequency = int.Parse(s));

        DropdownLoadFn.Add((x, i) => x.EventBoxes[0].Filter.FilterType = i + 1);
        DropdownLoadFn.Add((x, i) => x.EventBoxes[0].DistributionType = i + 1);
        DropdownLoadFn.Add((x, i) => x.EventBoxes[0].BrightnessDistributionType = i + 1);
        DropdownLoadFn.Add((x, i) => x.EventBoxes[0].EventDatas[DataIdx].TransitionType = i);

        ToggleLoadFn.Add((x, b) => x.EventBoxes[0].Filter.Reverse = b ? 1 : 0);
        ToggleLoadFn.Add((x, b) => x.EventBoxes[0].BrightnessAffectFirst = b ? 1 : 0);

        for (int i = 0; i < InputFields.Length; ++i)
        {
            var currentIdx = new int();
            currentIdx = i;
            InputFields[currentIdx].onEndEdit.AddListener((t) => {
                if (DisplayingSelectedObject) return;
                InputLoadFn[currentIdx](ObjectData, t);
                UpdateToPlacement();
            });
        }

        for (int i = 0; i < Dropdowns.Length; ++i)
        {
            var currentIdx = new int();
            currentIdx = i;
            Dropdowns[currentIdx].onValueChanged.AddListener((t) => { 
                if (DisplayingSelectedObject) return;
                DropdownLoadFn[currentIdx](ObjectData, t); 
                UpdateToPlacement(); 
            });
        }
        Dropdowns[0].onValueChanged.AddListener((t) =>
        {
            Texts[0].text = t == 0 ? "Section" : "Step";
            Texts[1].text = t == 0 ? "Partition" : "Start";
        });

        for (int i = 0; i < Toggles.Length; ++i)
        {
            var currentIdx = new int();
            currentIdx = i;
            Toggles[currentIdx].onValueChanged.AddListener((t) => { 
                if (DisplayingSelectedObject) return;
                ToggleLoadFn[currentIdx](ObjectData, t); 
                UpdateToPlacement(); 
            });
        }
    }

    public override void Dump(BeatmapLightColorEvent obj)
    {
        var col = BeatmapObjectContainerCollection.GetCollectionForType<LightColorEventsContainer>(obj.BeatmapType);
        if (col.LoadedContainers.TryGetValue(obj, out var con))
        {
            var colorCon = con as BeatmapLightColorEventContainer;
            DataIdx = colorCon.GetRaycastedIdx();
        }
        else
        {
            DataIdx = 0;
        }
        base.Dump(obj);
    }

    public void UpdateToPlacement()
    {
        lightColorEventPlacement.UpdateData(ObjectData);
    }

    private void DataidxSwitchDecorator(Action callback)
    {
        var cur = DataIdx;
        DataIdx = 0;
        callback();
        DataIdx = cur;
    }

    #region Input Hook
    public void OnTypeOn(InputAction.CallbackContext context)
    {
        if (!context.performed || !Settings.Instance.Load_MapV3) return;
        DataidxSwitchDecorator(() =>
        {
            DropdownLoadFn[3](ObjectData, 0);
            InputLoadFn[6](ObjectData, "1");
        });
        if (!DisplayingSelectedObject) Dump(ObjectData);
        UpdateToPlacement();
    }
    public void OnTypeFlash(InputAction.CallbackContext context) { }
    public void OnTypeOff(InputAction.CallbackContext context)
    {
        if (!context.performed || !Settings.Instance.Load_MapV3) return;
        DataidxSwitchDecorator(() =>
        {
            DropdownLoadFn[3](ObjectData, 0);
            InputLoadFn[6](ObjectData, "0");
        });
        if (!DisplayingSelectedObject) Dump(ObjectData);
        UpdateToPlacement();
    }
    public void OnTypeFade(InputAction.CallbackContext context) { }
    public void OnTogglePrecisionRotation(InputAction.CallbackContext context) { }
    public void OnSwapCursorInterval(InputAction.CallbackContext context) { }
    public void OnTypeTransition(InputAction.CallbackContext context)
    {
        if (!context.performed || !Settings.Instance.Load_MapV3) return;
        DataidxSwitchDecorator(() =>
        {
            DropdownLoadFn[3](ObjectData, 1);
        });
        if (!DisplayingSelectedObject) Dump(ObjectData);
        UpdateToPlacement();
    }


    public void OnToggleRightButtonPanel(InputAction.CallbackContext context) { }
    public void OnUpdateSwingArcVisualizer(InputAction.CallbackContext context) { }
    public void OnToggleNoteorEvent(InputAction.CallbackContext context) { }
    public void OnPlaceRedNoteorEvent(InputAction.CallbackContext context) 
    {
        if (!context.performed || !Settings.Instance.Load_MapV3) return;
        DataidxSwitchDecorator(() =>
            InputLoadFn[5](ObjectData, "0")
        );
        if (!DisplayingSelectedObject) Dump(ObjectData);
        UpdateToPlacement();
    }
    public void OnPlaceBlueNoteorEvent(InputAction.CallbackContext context) 
    {
        if (!context.performed || !Settings.Instance.Load_MapV3) return;
        DataidxSwitchDecorator(() =>
            InputLoadFn[5](ObjectData, "1")
        );
        if (!DisplayingSelectedObject) Dump(ObjectData);
        UpdateToPlacement();
    }
    public void OnPlaceBomb(InputAction.CallbackContext context)
    {
        if (!context.performed || !Settings.Instance.Load_MapV3) return;
        DataidxSwitchDecorator(() =>
            InputLoadFn[5](ObjectData, "2")
        );
        if (!DisplayingSelectedObject) Dump(ObjectData);
        UpdateToPlacement();
    }
    public void OnPlaceObstacle(InputAction.CallbackContext context) { }
    public void OnToggleDeleteTool(InputAction.CallbackContext context) { }
    public void OnMirror(InputAction.CallbackContext context) { }
    public void OnMirrorinTime(InputAction.CallbackContext context) { }
    public void OnMirrorColoursOnly(InputAction.CallbackContext context) { }
    #endregion
}
