# jellyfin-tmdb-collections-plugin
Creates jellyfin collections based on tmdb networks. See https://www.themoviedb.org for networks

## How it works
- The plugin will run once a week and check for any shows that exist in the configured list of tmdb network ids.
- If the show exists it will try to add it to a collection for the network.
- If the collection doesn't exist, the plugin will create the collection automatically.
- To access tmdb we rely on Jellyfins api key which is not exposed in any public apis, so we get it via reflection. NOTE - The existing DI tmdb client in jellyfin doesn't have a method exposed for accessing networks, hence why we just create another one here.

## Installation

1. Create a plugin folder in your jellyfin plugins folder called "tmdb-collections-plugin"
2. Take the latest dll from releases (https://github.com/shayaantx/jellyfin-tmdb-collections-plugin/releases) and place it in the new plugin folder
3. Restart jellyfin
4. Go to Admin Dashboard -> Plugins -> Click the "TMDB Network Collections Generator" plugin.
5. Enter your comma delimted list of tmdb networks. The easiest way to obtain these integers is to search a show on tmdb and get the network id from the show (the network is usually somewhere on the page)
6. You can either wait a week or run it manually via the "Scheduled Tasks" page, it should be under "Collections Generation"
