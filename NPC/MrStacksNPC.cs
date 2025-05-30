using UnityEngine;
using S1API.Entities;
using ScheduleOne.UI.Phone.ContactsApp;

namespace PaxDrops.NPC
{
    /// <summary>
    /// Static helper for creating the Mrs. Stacks NPC using internal API-friendly pattern.
    /// </summary>
    public static class MrStacksNpc
    {
        /// <summary>
        /// Attempts to create and return a properly initialized NPC instance.
        /// </summary>
        public static NPC Create()
        {
            Logger.Msg("[MrStacksNpc] 🛠️ Creating new NPC 'Mrs. Stacks'...");

            // Construct using Schedule I-compatible NPC constructor (ID, firstName, lastName, icon)
            return new NPC(
                id: "MrStacks",
                firstName: "Mrs.",
                lastName: "Stacks",
                icon: GetAppIcon()
            );
        }

        /// <summary>
        /// Tries to fetch the ContactsApp icon used for NPCs.
        /// </summary>
        private static Sprite GetAppIcon()
        {
            try
            {
                var app = GameObject.FindObjectOfType<ContactsApp>();
                return app?.AppIcon;
            }
            catch
            {
                Logger.Warn("[MrStacksNpc] ⚠️ Couldn't fetch ContactsApp icon.");
                return null;
            }
        }
    }
}