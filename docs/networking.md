# Networking and authority

Builds on the R2Wiki's [UNet](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/C%23-Programming/Networking/UNet/), [R2API.NetworkingAPI](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/C%23-Programming/Networking/R2API.NetworkingAPI/) and [Server side and client side mods](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/C%23-Programming/Networking/Server-side-and-client-side-mods/) pages. File and member names below refer to the decompiled game 1.4.1.

Networking is where AI-written mods go wrong most, and no solo test shows it. Use the paths the game already has; if you think you need something new, write down why and ask the human, who can ask the [modding Discord](https://discord.gg/5MbXZvd).

## Who decides what

The game uses Unity's UNet. The host is the server; every machine has a copy of every networked object.

- **The server decides game state**: damage, deaths, items, money, spawns. Methods marked `[Server]` log a warning and do nothing on clients (e.g. `HealthComponent.TakeDamage`).
- **A player's client decides its own body**: movement, input, skills (entity states) and sprint. Player bodies are spawned with that client as authority (`CharacterMaster` uses `NetworkServer.SpawnWithClientAuthority`).
- **Effective authority** (`Util.HasEffectiveAuthority`, `CharacterBody.hasEffectiveAuthority`, `EntityState.isAuthority`) is true on the owning client, or on the server for objects no client owns (monsters, drones).
- Component code (`FixedUpdate`, entity states, skills) runs on **every copy** unless it checks `NetworkServer.active` or authority.

Before changing a value, find the machine that decides it, and change it there, through the path the game already networks: the owner's input, a command the game already sends, an entity state (replicated by `NetworkStateMachine`), or server state the game already syncs. Then the game replicates it for you.

## Who needs the mod

Decide this early and state it in the README:

- **Host only**: the mod changes server-decided things (the template: fall damage). Clients can join with or without it.
- **Client only**: it changes what the local player decides or sees (input, camera, UI).
- **Everyone, same version**: it adds catalog content or its own network messages. It must be on the network mod list ([content.md](content.md)).

## Case study: sprint

Verified in 1.4.1:

1. The `CharacterBody.isSprinting` setter replicates in two directions: on the server it sets dirty bit 16 (sent to clients); on the authority client it calls `CallCmdUpdateSprint` (sent to the server).
2. `CharacterBody.OnDeserialize` ignores the server's sprint value where `hasEffectiveAuthority`: the owner decides its own sprint and never takes the server's.
3. The owner's `PlayerCharacterMasterController.PollButtonInput` starts from `body.isSprinting`, flips it when `sprintInputPressReceived` is set (the sprint key), drops it unless you move within 60° of where you aim (or the body has `SprintAnyDirection`), and pushes the result into `bodyInputs.sprint`.
4. Skills also write it. `SkillDef.OnExecute` clears it for skills with `cancelSprintingOnActivation`. `SkillDef.OnFixedUpdate`, called from `GenericSkill.FixedUpdate` on **every copy**, sets `isSprinting = true` while a `forceSprintDuringState` skill is in its activation state, and entity states are replicated from the owner (`NetworkStateMachine`), so this runs on the server's copy too. Entity states differ: `EntityStates.Croco.BaseLeap` sets sprint only `if (isAuthority)`, while `EntityStates.Chef.OilSpillBase` and `EntityStates.Drifter.TornadoSlam` set it on every copy.
5. `CallCmdUpdateSprint` and `NetworkStateMachine.SendSetEntityState` both send through the client's `ClientScene.readyConnection` on channel 0, `QosChannelIndex.defaultReliable`.

What follows for a mod:

- **Prefer acting through input.** AutoSprint sprints for the player by setting `sprintInputPressReceived` in an IL hook on `PollButtonInput` when its conditions hold; the game then treats it exactly like a key press, on the owner, and tells the server the usual way. Which entity states block or delay sprint is worked out once at startup from the `SkillCatalog` (`[SystemInitializer(typeof(SkillCatalog))]`), plus a short list of exceptions.
- **If a feature must override a game write** (a last resort; ask the community first), e.g. keep walking speed during a dash that forces sprint, first find out which copies run that write (fact 4). Blocking it on the owner doesn't stop the server's copy, and since the owner ignores the server's value (fact 2), the server stays wrong until the owner sends a change. Correct it once through the game's own command (`CallCmdUpdateSprint`) at a point you can justify from the message order, e.g. once the owner has left the state, since the state change and the command go through the same connection and channel. Never with timers or repeated resends.
- Order caveat: the EOS transport sends reliable channels as `ReliableOrdered` (`EOSNetworkConnection`); on Steam P2P the send is `SendType.Reliable` (`SteamNetworkConnection.TransportSend`), and in-order delivery there is assumed, not verified here.

## Custom messages

Only when the game has no path for what you need to send. Two established options (ask the human which):

- **R2API.Networking** ([wiki page](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/C%23-Programming/Networking/R2API.NetworkingAPI/)): implement `INetMessage`, register it with `NetworkingAPI.RegisterMessageType<T>()`. It carries every mod's messages over its own four message ids (2048, 4096, 6144, 8192), so you don't pick an id. Depending on it puts your mod on the network mod list.
- **The game's own `[NetworkMessageHandler]`**, no dependency. The game collects these once during startup (`NetworkMessageHandlerAttribute.CollectHandlers`) and registers them on every server and client it starts. It only scans assemblies marked `[assembly: HG.Reflection.SearchableAttribute.OptIn]`.

```csharp
using RoR2.Networking;
using UnityEngine.Networking;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

internal static class BodyRequest
{
    private const short MsgType = 17400; // see "Choosing a message id"

    // Called on the client that owns the body.
    internal static void Send(CharacterBody body)
    {
        if (NetworkServer.active)
        {
            Handle(body.gameObject);
            return;
        }
        var writer = new NetworkWriter();
        writer.StartMessage(MsgType);
        writer.Write(body.gameObject);
        writer.FinishMessage();
        ClientScene.readyConnection.SendWriter(writer, QosChannelIndex.defaultReliable.intVal);
    }

    [NetworkMessageHandler(msgType = MsgType, server = true)]
    private static void HandleMessage(NetworkMessage netMsg)
    {
        GameObject bodyObject = netMsg.reader.ReadGameObject();
        // Accept requests only for objects the sender owns, as NetworkStateMachine.HandleSetEntityState does.
        if (!bodyObject || netMsg.conn.clientOwnedObjects == null
            || !netMsg.conn.clientOwnedObjects.Contains(bodyObject.GetComponent<NetworkIdentity>().netId))
        {
            return;
        }
        Handle(bodyObject);
    }

    private static void Handle(GameObject bodyObject)
    {
        // Server-side work. Re-check any state the request depends on: it may have changed in transit.
    }
}
```

**Choosing a message id.** UNet reserves up to 47 and RoR2 registers 48 to 87; R2API.Networking uses the four ids above. There is no registry of mod ids that we know of (unverified: ask the Discord), and registering an id that is taken silently replaces the other handler (UNet's `NetworkMessageHandlers.RegisterHandler`). When in doubt, use R2API.Networking or ask the Discord; otherwise pick an unusual id and list it in the README.

Either way, a mod with its own messages must be on every player's machine in the same version (network mod list), and the server must validate every request: who sent it, and whether the object it names is theirs and still in the right state.

For networked components with `[Command]`, `[ClientRpc]` and `[SyncVar]`, see the UNet page (it needs the UNetWeaver as a post-build step).

## Testing

Solo play runs the host path only. See [testing.md](testing.md) for testing as a client.
