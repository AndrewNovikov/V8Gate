using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace V8Gate {
  [Serializable]
  public class ConnectionsInfo: ISerializable {
    const string LOCKED_MARKER = "l";
    const string UNLOCKED_MARKER = "ul";
    const string T_LOCKED_MARKER = "tl";
    const string T_UNLOCKED_MARKER = "tu";

    public int LockedConnectionsCount { get; private set; }
    public int UnlockedConnectionsCount { get; private set; }
    public int tLockedConnectionsCount { get; private set; }
    public int tUnlockedConnectionsCount { get; private set; }

    #region "constructors
    public ConnectionsInfo(int locked, int unlocked, int tLocked, int tUnlocked) {
      this.LockedConnectionsCount = locked;
      this.UnlockedConnectionsCount = unlocked;
      this.tLockedConnectionsCount = tLocked;
      this.tUnlockedConnectionsCount = tUnlocked;
    }
    #endregion

    #region ISerializable implementation
    public ConnectionsInfo() { }
    public ConnectionsInfo(SerializationInfo info, StreamingContext context) {
      this.LockedConnectionsCount = info.GetInt32(LOCKED_MARKER);
      this.UnlockedConnectionsCount = info.GetInt32(UNLOCKED_MARKER);
      this.tLockedConnectionsCount = info.GetInt32(T_LOCKED_MARKER);
      this.tUnlockedConnectionsCount = info.GetInt32(T_UNLOCKED_MARKER);
    }

    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) {
      info.AddValue(LOCKED_MARKER, this.LockedConnectionsCount);
      info.AddValue(UNLOCKED_MARKER, this.UnlockedConnectionsCount);
      info.AddValue(T_LOCKED_MARKER, this.tLockedConnectionsCount);
      info.AddValue(T_UNLOCKED_MARKER, this.tUnlockedConnectionsCount);
    }
    #endregion
  }
}
