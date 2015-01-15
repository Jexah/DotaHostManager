
namespace DotaHostClientLibrary
{
    public class Options : Kv
    {
        public void SetOption(string key, string value)
        {
            SetValue(key, value);
        }

        public Options()
        {
            InitObject();
        }


        public Options(Kv source)
        {
            InheritSource(source);
        }

    }
}
