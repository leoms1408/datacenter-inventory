using Il2Cpp;
using UnityEngine;

namespace InventoryMod
{
    public class InventorySlot
    {
        public PlayerManager.ObjectInHand ItemType { get; }
        public GameObject[] StoredObjects { get; }

        public string DisplayName { get; }
        public int PrefabID { get; }

        public Vector3[] SavedLocalPositions { get; }
        public Quaternion[] SavedLocalRotations { get; }

        private static readonly Vector3 StashPosition = new Vector3(0, -5000, 0);

        public InventorySlot(PlayerManager.ObjectInHand itemType, GameObject[] objects,
                             string displayName, int prefabID)
        {
            ItemType = itemType;
            StoredObjects = objects;
            DisplayName = displayName;
            PrefabID = prefabID;

            SavedLocalPositions = new Vector3[objects.Length];
            SavedLocalRotations = new Quaternion[objects.Length];
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] == null) continue;
                SavedLocalPositions[i] = objects[i].transform.localPosition;
                SavedLocalRotations[i] = objects[i].transform.localRotation;
            }
        }

        public void Stash()
        {
            foreach (var go in StoredObjects)
            {
                if (go == null) continue;

                var usable = go.GetComponent<UsableObject>();
                if (usable != null)
                {
                    // Unsubscribe native drop/action callbacks so the game's
                    // own DropObject() can't fire for stashed items.
                    // This prevents interference with our ManualDrop.
                    UnsubscribeNativeCallbacks(usable);

                    usable.objectInHands = false;
                    if (usable.rb != null)
                    {
                        usable.rb.isKinematic = true;
                        usable.rb.velocity = Vector3.zero;
                        usable.rb.angularVelocity = Vector3.zero;
                    }
                }

                go.transform.SetParent(null, false);
                go.transform.position = StashPosition;
            }
        }

        public void RestoreToHand(Transform handParent)
        {
            for (int i = 0; i < StoredObjects.Length; i++)
            {
                var go = StoredObjects[i];
                if (go == null) continue;

                go.transform.SetParent(handParent, false);
                go.transform.localPosition = SavedLocalPositions[i];
                go.transform.localRotation = SavedLocalRotations[i];

                var usable = go.GetComponent<UsableObject>();
                if (usable != null)
                {
                    usable.objectInHands = true;
                    if (usable.rb != null)
                    {
                        usable.rb.isKinematic = true;
                        usable.rb.velocity = Vector3.zero;
                        usable.rb.angularVelocity = Vector3.zero;
                    }
                }
            }
        }

        private static void UnsubscribeNativeCallbacks(UsableObject usable)
        {
            try
            {
                if (usable.inputctrl == null) return;
                var player = usable.inputctrl.Player;

                if (usable.dropStarted != null)
                    player.Drop.remove_started(usable.dropStarted);
                if (usable.actionInHandStarted != null)
                    player.Interact.remove_started(usable.actionInHandStarted);
                if (usable.secondActionStarted != null)
                    player.SecondAction.remove_started(usable.secondActionStarted);
            }
            catch { }
        }

        public bool IsEmpty()
        {
            foreach (var go in StoredObjects)
                if (go != null) return false;
            return true;
        }

        public int AliveCount()
        {
            int count = 0;
            foreach (var go in StoredObjects)
                if (go != null) count++;
            return count;
        }
    }
}
