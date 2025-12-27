using PurrNet.Packing;

public struct RoomListRequestMessage : IPackedAuto { }
public struct RoomListResponseMessage : IPackedAuto
{
    public string[] roomNames;
    public string[] roomDatas;
    public string[] sceneNames;
    public int[] currentCounts;
    public int[] maxCounts;
}

public struct CreateRoomMessage : IPackedAuto
{
    public string roomName;
    public string roomData;
    public string sceneName;
    public int maxPlayers;
}

public struct JoinRoomMessage : IPackedAuto
{
    public string roomName;
}
