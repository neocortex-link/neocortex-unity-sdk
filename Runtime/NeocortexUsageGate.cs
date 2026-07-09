using System;
using UnityEngine;
using Neocortex.API;
using Neocortex.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Neocortex
{
    /// <summary>
    ///     Turns the read-only account/usage endpoints into simple flags and events so a game can
    ///     gate smart NPC features instead of failing mid-conversation. Results are cached; usage is
    ///     only re-fetched when the cache is older than <see cref="MinRefreshInterval"/>, on an
    ///     explicit Refresh call, or via <see cref="StartAutoRefresh"/>. These endpoints never cost
    ///     a credit.
    /// </summary>
    public class NeocortexUsageGate
    {
        private readonly ApiRequest apiRequest = new();
        private CancellationTokenSource autoRefreshCts;

        private float lastFetchTime = float.MinValue;
        private string lastQueryKey;

        private bool wasLow;
        private bool wasEmpty;
        private bool wasPlayerOverLimit;
        private bool wasCharacterOverLimit;

        /// <summary>Seconds a cached usage result is served before a new request is made.</summary>
        public float MinRefreshInterval { get; set; } = 30f;

        public ApiAccountResponse LastAccount { get; private set; }
        public ApiUsageResponse LastUsage { get; private set; }

        /// <summary>Raised after every successful usage fetch.</summary>
        public event Action<ApiUsageResponse> OnUsageUpdated;
        /// <summary>Raised once when team credits drop below the dashboard low-credit threshold.</summary>
        public event Action<ApiUsageResponse> OnLowCredits;
        /// <summary>Raised once when team credits run out.</summary>
        public event Action<ApiUsageResponse> OnCreditsEmpty;
        /// <summary>Raised once when the queried player goes over a developer-configured cap.</summary>
        public event Action<ApiUsageResponse> OnPlayerOverLimit;
        /// <summary>Raised once when the queried character goes over a developer-configured cap.</summary>
        public event Action<ApiUsageResponse> OnCharacterOverLimit;
        /// <summary>Raised when a request fails (offline, invalid key, ...). The game keeps running.</summary>
        public event Action<string> OnRequestFailed;

        public NeocortexUsageGate()
        {
            apiRequest.OnRequestFailed += error => OnRequestFailed?.Invoke(error);
        }

        /// <summary>
        ///     Fetches usage and raises the gating events. When playerId is null, the device unique
        ///     identifier is used, which is the playerId the SDK sends on every chat request.
        ///     Returns null on failure (the last cached value stays in <see cref="LastUsage"/>).
        /// </summary>
        public async Task<ApiUsageResponse> RefreshUsage(string playerId = null, string characterId = null)
        {
            playerId ??= SystemInfo.deviceUniqueIdentifier;

            ApiUsageResponse usage = await apiRequest.GetUsage(playerId, characterId);
            if (usage == null)
            {
                return null;
            }

            lastFetchTime = Time.realtimeSinceStartup;
            lastQueryKey = GetQueryKey(playerId, characterId);
            LastUsage = usage;

            RaiseUsageEvents(usage);
            return usage;
        }

        /// <summary>
        ///     Fetches the developer account info. Returns null on failure
        ///     (the last cached value stays in <see cref="LastAccount"/>).
        /// </summary>
        public async Task<ApiAccountResponse> RefreshAccount()
        {
            ApiAccountResponse account = await apiRequest.GetAccount();
            if (account != null)
            {
                LastAccount = account;
            }

            return account;
        }

        /// <summary>
        ///     Returns false when team credits are empty or the player/character is over a
        ///     developer-configured cap. Uses the cached result when it is fresh, so it is safe to
        ///     call before every chat message. Fails open: when no usage data is available at all
        ///     (e.g. offline before the first fetch), it returns true rather than blocking the game.
        /// </summary>
        public async Task<bool> CanUseSmartNPC(string playerId = null, string characterId = null)
        {
            playerId ??= SystemInfo.deviceUniqueIdentifier;

            ApiUsageResponse usage = LastUsage;
            bool cacheIsFresh = usage != null
                                && lastQueryKey == GetQueryKey(playerId, characterId)
                                && Time.realtimeSinceStartup - lastFetchTime < MinRefreshInterval;

            if (!cacheIsFresh)
            {
                usage = await RefreshUsage(playerId, characterId) ?? LastUsage;
            }

            if (usage == null)
            {
                return true;
            }

            return usage.status != UsageStatus.Empty
                   && !(usage.player?.overLimit ?? false)
                   && !(usage.character?.overLimit ?? false);
        }

        /// <summary>
        ///     Refreshes usage on a low-frequency interval until <see cref="StopAutoRefresh"/> is
        ///     called or the application quits. Keep the interval high; these values change slowly.
        /// </summary>
        public void StartAutoRefresh(float intervalSeconds = 300f, string playerId = null, string characterId = null)
        {
            StopAutoRefresh();
            autoRefreshCts = CancellationTokenSource.CreateLinkedTokenSource(Application.exitCancellationToken);
            AutoRefreshLoop(intervalSeconds, playerId, characterId, autoRefreshCts.Token);
        }

        public void StopAutoRefresh()
        {
            autoRefreshCts?.Cancel();
            autoRefreshCts = null;
        }

        private async void AutoRefreshLoop(float intervalSeconds, string playerId, string characterId, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await RefreshUsage(playerId, characterId);
                    await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), token);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void RaiseUsageEvents(ApiUsageResponse usage)
        {
            OnUsageUpdated?.Invoke(usage);

            bool isLow = usage.status == UsageStatus.Low;
            bool isEmpty = usage.status == UsageStatus.Empty;
            bool playerOverLimit = usage.player?.overLimit ?? false;
            bool characterOverLimit = usage.character?.overLimit ?? false;

            // Threshold events fire on the transition only, so a popup hooked to them
            // shows once instead of on every refresh.
            if (isLow && !wasLow) OnLowCredits?.Invoke(usage);
            if (isEmpty && !wasEmpty) OnCreditsEmpty?.Invoke(usage);
            if (playerOverLimit && !wasPlayerOverLimit) OnPlayerOverLimit?.Invoke(usage);
            if (characterOverLimit && !wasCharacterOverLimit) OnCharacterOverLimit?.Invoke(usage);

            wasLow = isLow;
            wasEmpty = isEmpty;
            wasPlayerOverLimit = playerOverLimit;
            wasCharacterOverLimit = characterOverLimit;
        }

        private static string GetQueryKey(string playerId, string characterId)
        {
            return $"{playerId}|{characterId}";
        }
    }
}
