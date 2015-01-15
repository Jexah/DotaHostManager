
namespace DotaHostClientLibrary
{
    public class GenericKv : Kv
    {
        public GenericKv()
        {
            InitObject();
        }

        public void SetGenericKey(string key, Kv kv)
        {
            SetKey(key, kv);
        }

        public void SetGenericValue(string key, string value)
        {
            SetValue(key, value);
        }
    }
}
