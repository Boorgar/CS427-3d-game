/*  This file is part of the "Tanks Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using System.Collections;
using UnityEngine;
using Mirror;

namespace TanksMP
{
    /// <summary>
    /// Manages network-synced spawning of prefabs, in this case collectibles and powerups.
    /// With the respawn time synced on all clients it supports host migration too.
    /// </summary>
	public class ObjectSpawner : NetworkBehaviour
	{
        /// <summary>
        /// Prefab to sync the instantiation for over the network.
        /// </summary>
		public GameObject prefab;
        
        /// <summary>
        /// Checkbox whether the object should be respawned after being despawned.
        /// </summary>
        public bool respawn;
        
        /// <summary>
        /// Delay until respawning the object again after it got despawned.
        /// </summary>
		public int respawnTime;
        
        /// <summary>
        /// Reference to the spawned prefab gameobject instance in the scene.
        /// </summary>
        [HideInInspector]
        public GameObject obj;

        /// <summary>
        /// Type of Collectible this spawner should utilize. This is set automatically,
        /// thus hidden in the inspector. Fires network messages depending on type.
        /// </summary>
        [HideInInspector]
        public CollectionType colType = CollectionType.Use;

        //time value when the next respawn should happen measured in game time
        private float nextSpawn;
        

        /// <summary>
        /// Server only: check if the object is active in the scene already, then the
        /// host or migrated host does not have to do anything. If the object is not
        /// active, the host has to continue with the SpawnCoroutine where it left off
        /// </summary>
		public override void OnStartServer()
		{         
            if(obj != null && obj.activeInHierarchy)
                return;
                           
            StartCoroutine(SpawnRoutine());
		}


        //calculates the remaining time until the next respawn,
        //waits for the delay to have passed and then instantiates the object
        IEnumerator SpawnRoutine()
		{
            yield return new WaitForEndOfFrame();
            float delay = Mathf.Clamp(nextSpawn - Time.time, 0, respawnTime);
			yield return new WaitForSeconds(delay);

            //differ between CollectionType
            if (colType == CollectionType.Pickup && obj != null)
            {
                //if the item is of type Pickup, it should not be destroyed after
                //the routine is over but returned to its original position again
                for(int i = 0; i < GameManager.GetInstance().collects.Count; i++)
                {
                    //check if the collectible instance is managed by this spawner
                    if (NetworkIdentity.spawned[GameManager.GetInstance().collects[i].objId].gameObject == obj)
                    {
                        //found it, reset its position to the original position
                        GameManager.GetInstance().collects[i] = new CollectibleState
                        {
                            objId = obj.GetComponent<NetworkIdentity>().netId,
                            pos = transform.position
                        };
                        GameManager.GetInstance().OnCollectibleStateChanged(SyncListCollectible.Operation.OP_SET, i);
                        break;
                    }
                }
            }
            else
            {
                //instantiate a new copy on all clients
                Instantiate();
            }
		}
		
        
        /// <summary>
        /// Server only: spawns the prefab over the network, making use of object pooling.
        /// Keeps the parent-child tracking by setting the network id to this managing gameobject.
        /// </summary>
		[Server]
		public void Instantiate()
		{
            //sanity check in case there already is an object active
            if (obj != null)
                return;

            obj = PoolManager.Spawn(prefab, transform.position, transform.rotation);
            //set the reference on the instantiated object for cross-referencing
            Collectible colItem = obj.GetComponent<Collectible>();
            if(colItem != null)
            {
                //set cross-reference
                colItem.parentId = GetComponent<NetworkIdentity>().netId;
                //set internal item type automatically
                if (colItem is CollectibleTeam) colType = CollectionType.Pickup;
                else colType = CollectionType.Use;
            }

            NetworkServer.Spawn(obj, prefab.GetComponent<NetworkIdentity>().assetId);
		}


        /// <summary>
        /// Collects the object and assigns it to the player with the corresponding view.
        /// </summary>
        public void Pickup(uint viewId)
        {
            //in case this method call is received over the network earlier than the
            //spawner instantiation, here we make sure to catch up and instantiate it directly
            if (obj == null)
                Instantiate();

            //get target view transform to parent to
            GameObject view = NetworkIdentity.spawned[viewId].gameObject;
            obj.transform.parent = view.transform;
            obj.transform.localPosition = Vector3.zero + new Vector3(0, 2, 0);

            //assign carrier to Collectible
            Collectible colItem = obj.GetComponent<Collectible>();
            if (colItem != null)
            {
                colItem.carrierId = viewId;
                colItem.OnPickup();
            }

            //cancel return timer as this object is now being carried around
            if (isServer)
                StopAllCoroutines();
        }


        /// <summary>
        /// Unparents the object from any carrier and drops it at the targeted position.
        /// </summary>
        public void Drop(Vector3 position)
        {
            //in case this method call is received over the network earlier than the
            //spawner instantiation, here we make sure to catch up and instantiate it directly
            if (obj == null)
                Instantiate();

            //re-parent object to this spawner
            obj.transform.parent = PoolManager.GetPool(obj).transform;
            obj.transform.position = position;
            obj.transform.rotation = Quaternion.identity;

            //reset carrier
            Collectible colItem = obj.GetComponent<Collectible>();
            if (colItem != null)
            {
                colItem.carrierId = 0;
                colItem.OnDrop();
            }

            //update respawn counter for a future point in time
            SetRespawn();
            //if the respawn mechanic is selected, trigger a new coroutine
            if (isServer && respawn)
            {
                StopAllCoroutines();
                StartCoroutine(SpawnRoutine());
            }
        }


        /// <summary>
        /// Returns the object back to this spawner's position. E.g. in Capture The Flag mode this
        /// can occur if a team collects its own flag, or a flag timed out after being dropped. 
        /// </summary>
        public void Return()
        {
            //re-parent object to this spawner
            obj.transform.parent = PoolManager.GetPool(obj).transform;
            obj.transform.position = transform.position;
            obj.transform.rotation = Quaternion.identity;

            //reset carrier
            Collectible colItem = obj.GetComponent<Collectible>();
            if (colItem != null)
            {
                colItem.carrierId = 0;
                colItem.OnReturn();
            }

            //cancel return timer as the object is now back at its base position
            if(isServer)
                StopAllCoroutines();
        }


        /// <summary>
        /// Server only: Called by the spawned object to destroy itself on this managing component.
        /// This could be the case when it has been collected by players.
        /// </summary>
        [Server]
		public void Destroy()
		{
            //despawn object and clear references
            PoolManager.Despawn(obj);
			NetworkServer.UnSpawn(obj);
 			obj = null;

            //if it should respawn again, trigger a new coroutine
            if (respawn)
            {
                StartCoroutine(SpawnRoutine());
            }
		}
        
        
        /// <summary>
        /// Sets the next spawn time in 'respawnTime' seconds from now on, measured in game time.
        /// This is done on all clients too, so that everyone can continue where the old host has
        /// left off to not disrupt the timed spawn intervals of collectibles.
        /// </summary>
        public void SetRespawn()
        {
            nextSpawn = Time.time + respawnTime;
        }
	}


    /// <summary>
    /// Collectible type used on the ObjectSpawner, to define whether the item is consumed or picked up.
    /// </summary>
    public enum CollectionType
    {
        Use,
        Pickup
    }


    /// <summary>
    /// Custom class derived from SyncListStruct for our buffered list of CollectibleState values.
    /// </summary>
    public class SyncListCollectible : SyncList<CollectibleState> { }

    /// <summary>
    /// Custom class used for storing Collectible states across the network in a SyncListStruct.
    /// </summary>
    [System.Serializable]
    public struct CollectibleState
    {
        /// <summary>
        /// The network ID of the Collectible referenced.
        /// </summary>
        public uint objId;

        /// <summary>
        /// The network ID of the Player this Collectible is assigned to.
        /// </summary>
        public uint targetId;

        /// <summary>
        /// The position in the scene the Collectible has been dropped at.
        /// </summary>
        public Vector3 pos;
    }
}
