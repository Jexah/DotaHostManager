
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
            if (source == null)
            {
                this.sort = 1;
                this.keys = null;
                this.values = null;
                return;
            }
            this.sort = source.getSort();
            this.keys = source.getKeys();
            this.values = source.getValues();
        }
    }
}
