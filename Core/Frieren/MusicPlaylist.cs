namespace FrierenMod.Core;

// 音乐使用独立随机源，不消耗地图、战斗或奖励的随机序列。
public sealed class MusicPlaylist(uint seed)
{
    private readonly Random _random = new(unchecked((int)(seed ^ 0x5A17B6C3)));
    private readonly Dictionary<string,int> _last = [];
    public int Next(string pool, int count)
    {
        if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
        int index;
        if (_last.TryGetValue(pool,out int previous) && previous < count && count > 1) {
            index = _random.Next(count-1);
            if (index >= previous) index++;
        } else index = _random.Next(count);
        _last[pool]=index;
        return index;
    }
}
