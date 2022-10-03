﻿using QModManager.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Logger = QModManager.Utility.Logger;

namespace AutoStorageTransfer.Monobehaviours
{
    public class StorageTransfer : MonoBehaviour
    {
        public const float THOROUGHSORTCOOLDOWN = 5f;

        private static List<StorageTransfer> storageTransfers = new List<StorageTransfer>();

        private StorageContainer _storageContainer;
        public StorageContainer StorageContainer 
        { 
            get 
            { 
                if( _storageContainer == null )
                    _storageContainer = GetComponent<StorageContainer>();
                return _storageContainer; 
            }
        }

        protected ItemsContainer _itemsContainer;
        public ItemsContainer Container 
        { 
            get 
            { 
                if (_itemsContainer == null) 
                    _itemsContainer = StorageContainer?.container; 
                return _itemsContainer; 
            } 
        }

        private PrefabIdentifier _prefabIdentifier;
        public PrefabIdentifier PrefabIdentifier
        {
            get
            {
                if (_prefabIdentifier == null)
                    _prefabIdentifier = UWE.Utils.GetComponentInHierarchy<PrefabIdentifier>(gameObject);
                return _prefabIdentifier;
            }
        }
        public string StorageID;
        public bool IsReciever { get; set; } = true;


        private Dictionary<InventoryItem, int> SortAttemptsPerItem = new Dictionary<InventoryItem, int>();
        //so that we don't end up with 30 items all doing a bunch of looking around and string comparing constantly, just because they can't find a spot.
        protected float timeLastThoroughSort = 0;


        public void Start()
        {
            if(!storageTransfers.Contains(this)) 
                storageTransfers.Add(this);

            if(TryGetComponent(out SpawnEscapePodSupplies asd))
            {
                Destroy(this);//fuck this. This single container is causing problems all on its own. Fuck it.
                return;
            }

            if (PrefabIdentifier == null) return;//there's not much I can do regarding containers that can't find a prefab identifier. Sucks to suck lul

            QMod.SaveData.OnStartedSaving += OnBeforeSave;
            if(QMod.SaveData.SavedStorages.TryGetValue(PrefabIdentifier.Id, out SaveInfo saveInfo))
            {
                StorageID = saveInfo.StorageID;
                IsReciever = saveInfo.IsReciever;
            }
        }
        public void OnBeforeSave(object sender, EventArgs e)
        {
            try//fuck this shit. I'm not dealing with it
            {
                if (QMod.SaveData.SavedStorages.TryGetValue(PrefabIdentifier.Id, out var saveInfo))
                {
                    saveInfo.IsReciever = IsReciever;
                    saveInfo.StorageID = StorageID;
                }
                else
                {
                    var newSaveInfo = new SaveInfo()
                    {
                        StorageID = StorageID,
                        IsReciever = IsReciever
                    };
                    QMod.SaveData.SavedStorages.Add(PrefabIdentifier.id, newSaveInfo);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(Logger.Level.Error, $"Error caught when saving storage transfer! Saving will continue for everything except this container's transfer settings.", ex);
                try
                {
                    Logger.Log(Logger.Level.Error, $"Double the try, double the... do? idk, prefab identifier: {_prefabIdentifier}");
                }
                catch(Exception)
                {
                    Logger.Log(Logger.Level.Error, "Couldn't even get prefab identifier without error. fuck this", null, true);
                }
            }
        }


        public void OnDisable()
        {
            storageTransfers.Remove(this);
        }
        public void OnDestroy()
        {
            storageTransfers.Remove(this);
        }
        public virtual void FixedUpdate()
        {
            if(Container == null)
            {
                Destroy(this);
                return;
            }
            if (!storageTransfers.Contains(this))
            {
                storageTransfers.Add(this);
            }

            if (IsReciever || string.IsNullOrEmpty(StorageID) || Container.count <= 0) return;

            InventoryItem chosenItem = null;

            foreach(var item in Container)//shouldn't be using foreach for this, but fuckit idc. Doesn't change anything at all anyway
            {
                if (item == null) continue;

                if (SortAttemptsPerItem.TryGetValue(item, out var attempts) && attempts >= 5 && (Time.time < timeLastThoroughSort + THOROUGHSORTCOOLDOWN))
                    continue;

                var size = CraftData.GetItemSize(item.item.GetTechType());

                var reciever = FindTransfer(size, StorageID);

                var usedRecievers = new List<StorageTransfer>();

                while (reciever != null)
                {
                    if(((IItemsContainer)reciever.Container).AddItem(item))
                    {
                        chosenItem = item;
                        break;
                    }
                    else
                    {
                        usedRecievers.Add(reciever);
                        reciever = FindTransfer(size, StorageID, usedRecievers);//if first reciever found can't take item, blacklist it and look again.
                    }
                }

                if (chosenItem != null) break;

                if(SortAttemptsPerItem.TryGetValue(item, out var value))
                    SortAttemptsPerItem[item] = value + 1;
                else
                    SortAttemptsPerItem.Add(item, 1);
            }

            if (chosenItem == null) return;

            if (SortAttemptsPerItem.ContainsKey(chosenItem))
                SortAttemptsPerItem.Remove(chosenItem);
            Container.RemoveItem(chosenItem.item.GetTechType());
        }
        public static StorageTransfer FindTransfer(Vector2int itemSize, string storageID, List<StorageTransfer> ignoreTransfers = null)
        {
            List<StorageTransfer> transfersToRemove = new List<StorageTransfer>();

            if (string.IsNullOrEmpty(storageID)) return null;

            StorageTransfer storageTransfer = null;

            foreach (StorageTransfer reciever in storageTransfers)
            {
                if (reciever == null || reciever.Container == null || !reciever.gameObject.activeInHierarchy)
                {
                    transfersToRemove.Add(reciever);
                    continue;
                }

                if (ignoreTransfers != null && ignoreTransfers.Contains(reciever)) continue;

                if (!reciever.IsReciever) continue;
                if (storageID != reciever.StorageID) continue;

                try
                {
                    if (reciever.Container.HasRoomFor((int)itemSize.x, (int)itemSize.y))
                    {
                        storageTransfer = reciever;
                        break;
                    }
                }
                catch(Exception e)
                {
                    ErrorMessage.AddError($"Error caught! Failed with {reciever.name}. Handled safely");
                    if(storageTransfer == null)
                    {
                        storageTransfer = reciever;
                        break;
                    }
                }
            }
            foreach (var transfer in transfersToRemove)
            {
                storageTransfers.Remove(transfer);
            }
            return storageTransfer;
        }
        public void ToggleRecieverStatus()
        {
            IsReciever = !IsReciever;
        }
        public void SetIDString(string ID)
        {
            StorageID = ID;
        }
    }
}
