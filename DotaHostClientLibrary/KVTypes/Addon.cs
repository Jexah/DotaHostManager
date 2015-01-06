
namespace DotaHostClientLibrary
{
    public class Addon : KV
    {
        public string Id
        {
            get
            {
                return getValue("0");
            }
            set
            {
                setValue("0", value);
            }

        }

        public Options Options
        {
            get
            {
                return new Options(getKV("1"));
            }
            set
            {
                setKey("1", value);
            }
        }

        public Addon()
        {
            initObject();
        }

        public Addon(KV source)
        {
            inheritSource(source);
        }
    }
}
