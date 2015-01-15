
namespace DotaHostClientLibrary
{
    public class Addon : Kv
    {
        public string Id
        {
            get
            {
                return GetValue("0");
            }
            set
            {
                SetValue("0", value);
            }

        }

        public Options Options
        {
            get
            {
                return new Options(GetKv("1"));
            }
            set
            {
                SetKey("1", value);
            }
        }

        public Addon()
        {
            InitObject();
        }

        public Addon(Kv source)
        {
            InheritSource(source);
        }
    }
}
