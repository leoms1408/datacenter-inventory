using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace InventoryMod
{
    public static class Inventory
    {
        public const int MaxSlots = 9;

        public static readonly InventorySlot[] Slots = new InventorySlot[MaxSlots];
        public static int ActiveSlot = 0;

        /// <summary>
        /// Switch to a different hotbar slot. Automatically stashes current hand
        /// items and restores the target slot's items.
        /// </summary>
        public static void SwitchToSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MaxSlots) return;
            if (slotIndex == ActiveSlot) return;

            var pm = PlayerManager.instance;
            if (pm == null) return;

            // 1) Stash whatever is currently in hand into the old active slot
            StashHandItems(ActiveSlot);

            // 2) Switch
            ActiveSlot = slotIndex;

            // 3) Restore the new slot's items into hand
            RestoreSlotItems(ActiveSlot);
        }

        /// <summary>
        /// Cycle to the next/previous slot (direction +1 or -1).
        /// </summary>
        public static void CycleSlot(int direction)
        {
            int next = ((ActiveSlot + direction) % MaxSlots + MaxSlots) % MaxSlots;
            SwitchToSlot(next);
        }

        /// <summary>
        /// Take whatever the player is holding and stash it into the given slot.
        /// </summary>
        private static void StashHandItems(int slotIndex)
        {
            var pm = PlayerManager.instance;
            if (pm == null) return;

            // Clean up slot if its objects were destroyed (placed in world etc.)
            if (Slots[slotIndex] != null && Slots[slotIndex].IsEmpty())
                Slots[slotIndex] = null;

            if (pm.objectInHand == PlayerManager.ObjectInHand.None) return;

            var handArray = pm.objectInHandGO;
            if (handArray == null) return;

            // Collect alive objects from hand
            var objects = new System.Collections.Generic.List<GameObject>();
            string displayName = "Item";
            int prefabID = -1;

            for (int i = 0; i < handArray.Length; i++)
            {
                var go = handArray[i];
                if (go == null) continue;
                objects.Add(go);

                if (prefabID < 0)
                {
                    var usable = go.GetComponent<UsableObject>();
                    if (usable != null)
                    {
                        prefabID = usable.prefabID;
                        if (usable.item != null && !string.IsNullOrEmpty(usable.item.itemName))
                            displayName = usable.item.itemName;
                        else
                            displayName = pm.objectInHand.ToString();

                        // Cache InputController for drop detection
                        if (usable.inputctrl != null)
                            Core.CachedInputCtrl = usable.inputctrl;
                    }
                }
            }

            if (objects.Count == 0) return;

            var slot = new InventorySlot(pm.objectInHand, objects.ToArray(), displayName, prefabID);

            // Teleport objects far away (no SetActive toggling)
            slot.Stash();

            // Clear hand state
            for (int i = 0; i < handArray.Length; i++)
                handArray[i] = null;
            pm.objectInHand = PlayerManager.ObjectInHand.None;
            pm.numberOfObjectsInHand = 0;
            Core.HandItemsFromInventory = false;

            Slots[slotIndex] = slot;
        }

        /// <summary>
        /// Restore a slot's items into the player's hand.
        /// </summary>
        private static void RestoreSlotItems(int slotIndex)
        {
            var pm = PlayerManager.instance;
            if (pm == null) return;

            var slot = Slots[slotIndex];
            if (slot == null || slot.IsEmpty())
            {
                Slots[slotIndex] = null;
                return;
            }

            // Collect alive objects
            var alive = new System.Collections.Generic.List<GameObject>();
            foreach (var go in slot.StoredObjects)
                if (go != null) alive.Add(go);

            if (alive.Count == 0)
            {
                Slots[slotIndex] = null;
                return;
            }

            // Manual restore: parent to hand, set saved local transform, rigidbody.
            var handPos = pm.objectInHandPositionGO != null
                ? pm.objectInHandPositionGO.transform
                : null;
            slot.RestoreToHand(handPos);

            // Update PlayerManager hand state
            var handArray = pm.objectInHandGO;
            if (handArray != null)
            {
                for (int i = 0; i < handArray.Length; i++)
                    handArray[i] = i < alive.Count ? alive[i] : null;
            }
            pm.objectInHand = slot.ItemType;
            pm.numberOfObjectsInHand = alive.Count;

            // Mark that current hand items came from inventory (need custom drop handling)
            Core.HandItemsFromInventory = true;

            // Ensure Drop InputAction is enabled (DropObject disables it)
            Core.EnsureDropActionEnabled();


            // Slot is now in hand, clear it
            Slots[slotIndex] = null;
        }

        /// <summary>
        /// Clean up destroyed objects from all slots.
        /// </summary>
        public static void CleanupSlots()
        {
            for (int i = 0; i < MaxSlots; i++)
            {
                if (Slots[i] != null && Slots[i].IsEmpty())
                    Slots[i] = null;
            }
        }
    }
}
