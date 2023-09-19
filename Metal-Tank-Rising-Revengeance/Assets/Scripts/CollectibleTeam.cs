/*  This file is part of the "Tanks Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using UnityEngine;
using Mirror;

namespace TanksMP
{
    /// <summary>
    /// Custom Collectible implementation for scene owned (unassigned) or team owned items.
    /// E.g. allowing for 'Rambo' pickups, Capture the Flag items etc.
    /// </summary>
	public class CollectibleTeam : Collectible
    {
        /// <summary>
        /// Team index this Collectible belongs to, or -1 if unassigned.
        /// Teams are defined in the GameManager script inspector.
        /// </summary>
        public int teamIndex = -1;

        /// <summary>
        /// Optional: Material that should be re-assigned if this Collectible is dropped or returned.
        /// </summary>
        public Material baseMaterial;

        /// <summary>
        /// Optional: Renderer on which the material should be modified depending on carrier team.
        /// </summary>
        public MeshRenderer targetRenderer;


        /// <summary>
        /// Server only: check for players colliding with the powerup.
        /// Possible collision are defined in the Physics Matrix.
        /// </summary>
        public override void OnTriggerEnter(Collider col)
        {
            if (!isServer)
                return;

            GameObject obj = col.gameObject;
            Player player = obj.GetComponent<Player>();

            //try to apply collectible to player, the result should be true
            if (Apply(player))
            {
                //check if colliding player belongs to the same team as the item
                if (teamIndex == player.teamIndex)
                {
                    //player collected team item, return it to team home base
                    //loop over the synced list of Collectibles to find the corresponding item
                    for (int i = 0; i < GameManager.GetInstance().collects.Count; i++)
                    {
                        //found it locally via its unique network ID
                        if (NetworkIdentity.spawned[GameManager.GetInstance().collects[i].objId].gameObject == gameObject)
                        {
                            //reset entry back to its spawner
                            int changedIndex = GameManager.GetInstance().AddBufferedCollectible(netId, spawner.transform.position, 0);
                            GameManager.GetInstance().OnCollectibleStateChanged(SyncListCollectible.Operation.OP_SET, changedIndex);
                            break;
                        }
                    }
                }
                else
                {
                    //player picked up item from other team, add synced list entry with target player for it to be remembered
                    int changedIndex = GameManager.GetInstance().AddBufferedCollectible(netId, spawner.transform.position, player.netId);
                    GameManager.GetInstance().OnCollectibleStateChanged(SyncListCollectible.Operation.OP_SET, changedIndex);
                }
            }
        }


        /// <summary>
        /// Overrides the default behavior with a custom implementation.
        /// Check for the carrier and item position to decide valid pickup.
        /// </summary>
        public override bool Apply(Player p)
        {
            //do not allow collection if the item is already carried around
            //but also skip any processing if our flag is on the home base already
            if (p == null || carrierId > 0 ||
                teamIndex == p.teamIndex && transform.position == spawner.transform.position)
                return false;

            //if a target renderer is set, assign team material
            Colorize(p.teamIndex);

            //return successful collection
            return true;
        }


        /// <summary>
        /// Overrides the default behavior with a custom implementation.
        /// </summary>
        public override void OnDrop()
        {
            Colorize(this.teamIndex);
        }


        /// <summary>
        /// Overrides the default behavior with a custom implementation.
        /// </summary>
        public override void OnReturn()
        {
            Colorize(this.teamIndex);
        }


        //assign material based on team index passed in
        void Colorize(int teamIndex)
        {
            if (targetRenderer != null)
            {
                if (teamIndex >= 0)
                    targetRenderer.material = GameManager.GetInstance().teams[teamIndex].material;
                else
                    targetRenderer.material = baseMaterial;
            }
        }
    }
}