using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapLoader : MonoBehaviour
{
    [SerializeField] private TracksManager manager;
    [SerializeField] private NoteLanesController noteLanesController;

    [Space] [SerializeField] private Transform containerCollectionsContainer;

    private BeatSaberMap map;
    private int noteLaneSize = 2;
    private int noteLayerSize = 3;

    public static string heckUnderscore = "";
    public static string heckPosition = "";
    public static string heckRotation = "";
    public static string heckInteractable = "";
    public static string heckScale = "";
    public static string heckLockPosition = "";
    
    public void UpdateMapData(BeatSaberMap map)
    {
        if (map is BeatSaberMapV3)
        {
            heckUnderscore = "";
            heckPosition = "coordinates";
            heckRotation = "worldRotation";
            heckInteractable = "uninteractable";
            heckScale = "size";
            heckLockPosition = "lockRotation";

            var copy = new BeatSaberMapV3
            {
                CustomData = map.CustomData.Clone(),
                Notes = new List<BeatmapNote>(map.Notes),
                Obstacles = new List<BeatmapObstacle>(map.Obstacles),
                Arcs = new List<BeatmapArc>((map as BeatSaberMapV3).Arcs),
                Chains = new List<BeatmapChain>((map as BeatSaberMapV3).Chains),
                Events = new List<MapEvent>(map.Events),
                BpmChanges = new List<BeatmapBPMChange>(map.BpmChanges),
                ColorBoostBeatmapEvents = new List<ColorBoostEvent>((map as BeatSaberMapV3).ColorBoostBeatmapEvents),
                LightColorEventBoxGroups = new List<BeatmapLightColorEvent>((map as BeatSaberMapV3).LightColorEventBoxGroups),
                LightRotationEventBoxGroups = new List<BeatmapLightRotationEvent>((map as BeatSaberMapV3).LightRotationEventBoxGroups),
                CustomEvents = new List<BeatmapCustomEvent>(map.CustomEvents)
            };
            this.map = copy;
        }
        else
        {
            heckUnderscore = "_";
            heckPosition = "_position";
            heckRotation = "_rotation";
            heckInteractable = "_interactable";
            heckScale = "_scale";
            heckLockPosition = "_lockPosition";

            var copy = new BeatSaberMap
            {
                CustomData = map.CustomData.Clone(),
                Notes = new List<BeatmapNote>(map.Notes),
                Obstacles = new List<BeatmapObstacle>(map.Obstacles),
                Events = new List<MapEvent>(map.Events),
                BpmChanges = new List<BeatmapBPMChange>(map.BpmChanges),
                CustomEvents = new List<BeatmapCustomEvent>(map.CustomEvents)
            };
            this.map = copy;
        }

    }

    public IEnumerator HardRefresh()
    {
        if (Settings.Instance.Load_Notes) yield return StartCoroutine(LoadObjects(map.Notes));
        if (Settings.Instance.Load_Obstacles) yield return StartCoroutine(LoadObjects(map.Obstacles));
        if (Settings.Instance.Load_Events) yield return StartCoroutine(LoadObjects(map.Events));
        if (Settings.Instance.Load_Others)
        {
            yield return StartCoroutine(LoadObjects(map.BpmChanges));
            yield return StartCoroutine(LoadObjects(map.CustomEvents));
        }
        if (Settings.Instance.Load_MapV3)
        {
            yield return StartCoroutine(LoadObjects((map as BeatSaberMapV3).Arcs));
            yield return StartCoroutine(LoadObjects((map as BeatSaberMapV3).Chains));
            yield return StartCoroutine(LoadObjects((map as BeatSaberMapV3).LightColorEventBoxGroups));
            yield return StartCoroutine(LoadObjects((map as BeatSaberMapV3).LightRotationEventBoxGroups));
        }

        PersistentUI.Instance.LevelLoadSliderLabel.text = "Finishing up...";
        manager.RefreshTracks();
        SelectionController.RefreshMap();
        PersistentUI.Instance.LevelLoadSlider.gameObject.SetActive(false);
    }

    public IEnumerator LoadObjects<T>(IEnumerable<T> objects) where T : BeatmapObject
    {
        if (!objects.Any()) yield break;
        var collection = BeatmapObjectContainerCollection.GetCollectionForType(objects.First().BeatmapType);
        if (collection == null) yield break;
        foreach (var obj in collection.LoadedObjects.ToArray()) collection.DeleteObject(obj, false, false);
        PersistentUI.Instance.LevelLoadSlider.gameObject.SetActive(true);
        collection.LoadedObjects = new SortedSet<BeatmapObject>(objects, new BeatmapObjectComparer());
        collection.UnsortedObjects = collection.LoadedObjects.ToList();
        UpdateSlider<T>();
        if (typeof(T) == typeof(BeatmapNote) || typeof(T) == typeof(BeatmapObstacle))
        {
            for (var i = 0; i < objects.Count(); i++)
            {
                BeatmapObject data = objects.ElementAt(i);
                if (data is BeatmapNote noteData)
                {
                    if (noteData.LineIndex >= 1000 || noteData.LineIndex <= -1000 || noteData.LineLayer >= 1000 ||
                        noteData.LineLayer <= -1000)
                    {
                        continue;
                    }

                    if (2 - noteData.LineIndex > noteLaneSize) noteLaneSize = 2 - noteData.LineIndex;
                    if (noteData.LineIndex - 1 > noteLaneSize) noteLaneSize = noteData.LineIndex - 1;
                    if (noteData.LineLayer + 1 > noteLayerSize) noteLayerSize = noteData.LineLayer + 1;
                }
                else if (data is BeatmapObstacle obstacleData)
                {
                    if (data is BeatmapObstacleV3) continue;
                    if (obstacleData.LineIndex >= 1000 || obstacleData.LineIndex <= -1000) continue;
                    if (2 - obstacleData.LineIndex > noteLaneSize) noteLaneSize = 2 - obstacleData.LineIndex;
                    if (obstacleData.LineIndex - 1 > noteLaneSize) noteLaneSize = obstacleData.LineIndex - 1;
                }
            }

            if (Settings.NonPersistentSettings.ContainsKey("NoteLanes"))
                Settings.NonPersistentSettings["NoteLanes"] = (noteLaneSize * 2).ToString();
            else
                Settings.NonPersistentSettings.Add("NoteLanes", (noteLaneSize * 2).ToString());
            noteLanesController.UpdateNoteLanes((noteLaneSize * 2).ToString());
        }

        if (typeof(T) == typeof(MapEvent))
        {
            manager.RefreshTracks();
            var events = collection as EventsContainer;
            events.AllRotationEvents = objects.Cast<MapEvent>().Where(x => x.IsRotationEvent).ToList();
            events.AllBoostEvents = objects.Cast<MapEvent>().Where(x => x.Type == MapEvent.EventTypeBoostLights)
                .ToList();

            if (Settings.Instance.Load_MapV3)
            {
                events.LinkAllLightEvents();
            }
        }

        collection.RefreshPool(true);
    }

    private void UpdateSlider<T>() where T : BeatmapObject
    {
        PersistentUI.Instance.LevelLoadSliderLabel.text = $"Loading {typeof(T).Name}s... ";
        PersistentUI.Instance.LevelLoadSlider.value = 1;
    }
}
