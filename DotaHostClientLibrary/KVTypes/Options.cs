
namespace DotaHostClientLibrary
{
    public class Options : KV
    {
        public void setOption(string key, string value)
        {
            setValue(key, value);
        }

        public Options()
        {
            initObject();
        }


        public Options(KV source)
        {
            inheritSource(source);
        }

    }
}
