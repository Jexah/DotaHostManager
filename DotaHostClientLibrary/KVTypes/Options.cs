
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
