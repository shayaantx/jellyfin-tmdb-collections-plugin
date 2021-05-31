using MediaBrowser.Model.Plugins;

namespace Jellyfin.Tv.Network.Collections.Plugin.Configuration
{

    public class PluginConfiguration : BasePluginConfiguration
    {
        public string TmdbNetworks { get; set; }

        public PluginConfiguration()
        {
            // set default options here
            TmdbNetworks = "";
        }
    }
}
