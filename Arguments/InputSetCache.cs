namespace RedisDemo.Arguments;

public class InputSetCache(string key, string value, int secondExpiration = 60)
{
    public string Key { get; private set; } = key;
    public string Value { get; private set; } = value;
    public int SecondExpiration { get; private set; } = secondExpiration;
}