using S1API.GameTime;      // For day tracking
using S1API.Messaging;     // For Response objects
using S1API.Entities;      // For NPC logic

namespace PaxDrops
{
    /// <summary>
    /// Handles the logic related to Mr. Stacks — your contact for ordering drops.
    /// Sets up the phone dialogue and schedules future drop packets.
    /// </summary>
    public static class MrStacks
    {
        /// <summary>
        /// Called from InitMain after scene load.
        /// </summary>
        public static void Init()
        {
            Logger.Msg("[MrStacks] Initialized and ready.");
            RegisterPhoneDropOption();
        }

        /// <summary>
        /// Adds a message + response to Mr. Stacks' contact.
        /// If no such NPC exists, one will be created automatically.
        /// </summary>
        private static void RegisterPhoneDropOption()
        {
            // Try to find existing contact by ID
            var npc = NPC.All.Find(n => n.ID == "MrStacks");

            // Create fallback contact if none found
            if (npc == null)
            {
                npc = new MrStacksContact("MrStacks", "Mr.", "Stacks");
                Logger.Msg("[MrStacks] Created fallback NPC 'MrStacks'.");
            }

            // Define a player response option
            var orderDrop = new Response
            {
                Label = "ORDER_DROP",
                Text = "Can I get a drop?",
                OnTriggered = () =>
                {
                    // Schedule drop for tomorrow (elapsed day + 1)
                    int tomorrow = TimeManager.ElapsedDays + 1;
                    var packet = TierLevel.GetDropPacket(tomorrow);
                    DataBase.SaveDrop(tomorrow, packet);

                    npc.SendTextMessage("I'll send you a drop tomorrow morning.");
                    Logger.Msg($"[MrStacks] Drop scheduled for Day {tomorrow}.");
                }
            };

            // Send intro a text and response option
            npc.SendTextMessage("Need something?", new[] { orderDrop });
        }

        /// <summary>
        /// Fallback contact if none is found at the scene.
        /// This is auto-created during runtime if needed.
        /// </summary>
        private class MrStacksContact : NPC
        {
            public MrStacksContact(string id, string first, string last)
                : base(id, first, last) { }
        }
    }
}
