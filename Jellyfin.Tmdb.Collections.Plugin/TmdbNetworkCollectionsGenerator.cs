using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Tv.Network.Collections.Plugin;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.TvShows;

namespace Emby.Server.Implementations.ScheduledTasks.Tasks
{
    /// <summary>
    /// Generates Collections for shows based on TMDB networks.
    /// </summary>
    public class TmdbNetworkCollectionsGenerator : IScheduledTask, IConfigurableScheduledTask
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ICollectionManager _collectionManager;
        private readonly TMDbClient _tmDbClient;
        private readonly ILogger<TmdbNetworkCollectionsGenerator> _logger;
        /// <inheritdoc />
        public TmdbNetworkCollectionsGenerator(ILibraryManager libraryManager, ICollectionManager collectionManager, ILogger<TmdbNetworkCollectionsGenerator> logger)
        {
            _collectionManager = collectionManager;
            _libraryManager = libraryManager;
            // jellyfin source api key
            Type type = Type.GetType("MediaBrowser.Providers.Plugins.Tmdb.TmdbUtils, MediaBrowser.Providers");
            FieldInfo fieldInfo = type.GetField("ApiKey");
            string apiKey = (string) fieldInfo.GetValue(null);
            _tmDbClient = new TMDbClient(apiKey);
            _logger = logger;
        }

        /// <inheritdoc />
        public bool IsHidden => false;

        /// <inheritdoc />
        public bool IsEnabled => true;

        /// <inheritdoc />
        public bool IsLogged => true;

        /// <inheritdoc />
        public string Name => "TMDB Network Collections Generator Task";

        /// <inheritdoc />
        public string Key => "CollectionsGenerator";

        /// <inheritdoc />
        public string Description => "Generates Collections based on TMDB networks for shows";

        /// <inheritdoc />
        public string Category => "Collections Generation";

        /// <inheritdoc />
        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            string tmdbNetworksInput = Plugin.Instance.Configuration.TmdbNetworks;
            if (String.IsNullOrEmpty(tmdbNetworksInput)) {
                this._logger.LogWarning("Did not find any Tmdb Network ids in plugin configuration");
                return;
            }
            List<string> tmdbNetworks = tmdbNetworksInput.Split(',').ToList();
            foreach(string tmdbNetworkStr in tmdbNetworks) {
                cancellationToken.ThrowIfCancellationRequested();
                if (!int.TryParse(tmdbNetworkStr, out int tmdbNetwork))
                {
                    this._logger.LogError("Found non integer Tmdb Network id", new string[] {tmdbNetworkStr});
                    continue;
                }
                Network network = await _tmDbClient.GetNetworkAsync(tmdbNetwork);
                // get all shows per network
                ICollection<int> networkItemTmdbIds = this.GetNetworkItems(network);

                // get existing boxset or create it
                BoxSet existingCollection = this.GetExistingCollection(network.Name);
                if (existingCollection == null) {
                    existingCollection = this.CreateCollection(network.Name);
                }

                var shows = this.GetShows();
                List<Guid> showGuids = this.GetTmDbApplicableShows(shows, networkItemTmdbIds);
                // add shows to collection
                await this._collectionManager.AddToCollectionAsync(existingCollection.Id, showGuids);
            }
        }

        private List<Guid> GetTmDbApplicableShows(IEnumerable<MediaBrowser.Controller.Entities.TV.Series> shows,
                                                  ICollection<int> networkItemTmdbIds)
        {
            List<Guid> showGuids = new List<Guid>();
            foreach (var show in shows)
            {
                if (!show.ProviderIds.ContainsKey("Tmdb"))
                {
                    // if the show doesn't have a Tmdb provider, we can't match it
                    // against a Tmdb network
                    this._logger.LogError("Show doesn't have a tmdb id", new string[] {show.Name});
                    continue;
                }
                var value = show.ProviderIds["Tmdb"];
                if (!networkItemTmdbIds.Contains(Int32.Parse(value)))
                {
                    // if the show doesn't match the networks tmdb item ids
                    // skip it
                    this._logger.LogError("Mismatch between tmdb network item tmdb id and jellyfin show tmdbid", new string[] {show.Name, value});
                    continue;
                }
                showGuids.Add(show.Id);
            }
            return showGuids;
        }

        private IEnumerable<MediaBrowser.Controller.Entities.TV.Series> GetShows()
        {
            return _libraryManager.GetItemList(new MediaBrowser.Controller.Entities.InternalItemsQuery
            {
                IncludeItemTypes = new[] { nameof(MediaBrowser.Controller.Entities.TV.Series) },
                IsVirtualItem = false,
                Recursive = true,
                HasTmdbId = true
            }).Select(tvshow => tvshow as MediaBrowser.Controller.Entities.TV.Series).ToList();
        }

        private ICollection<int> GetNetworkItems(Network network)
        {
            ICollection<int> tmdbIds = new HashSet<int>();
            List<Network> networks = new List<Network>() { network };
            SearchContainer<TMDbLib.Objects.Search.SearchTv> result = null;
            int page = 0;
            // page through all the results
            do {
                result = _tmDbClient.DiscoverTvShowsAsync().WhereNetworksInclude(networks).Query(page).Result;
                page = result.Page;

                foreach (var item in result.Results) {
                    tmdbIds.Add(item.Id);
                }

                if (result.Page == result.TotalPages)
                {
                    result = null;
                }
                page++;
            } while (result != null);
            return tmdbIds;
        }

        private BoxSet CreateCollection(string networkName)
        {
            CollectionCreationOptions options = new CollectionCreationOptions();
            options.Name = networkName;
            return _collectionManager.CreateCollectionAsync(options).Result;
        }

        private BoxSet GetExistingCollection(string networkName)
        {
            return _libraryManager.GetItemList(new MediaBrowser.Controller.Entities.InternalItemsQuery
            {
                IncludeItemTypes = new[] { nameof(BoxSet) },
                CollapseBoxSetItems = false,
                Recursive = true,
            }).Select(boxSet => boxSet as BoxSet).Where(boxSet => boxSet.Name == networkName).FirstOrDefault();
        }

        /// <inheritdoc />
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[]
            {
                // run every sunday at 1am
                new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerWeekly, TimeOfDayTicks = TimeSpan.FromHours(1).Ticks, DayOfWeek = DayOfWeek.Sunday }
            };
        }
    }
}
