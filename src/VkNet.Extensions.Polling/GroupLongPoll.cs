using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VkNet.Abstractions;
using VkNet.Extensions.Polling.Models.Configuration;
using VkNet.Extensions.Polling.Models.State;
using VkNet.Model;
using VkNet.Model.GroupUpdate;
using VkNet.Model.RequestParams;

namespace VkNet.Extensions.Polling
{
    public class GroupLongPoll :
        LongPollBase<BotsLongPollHistoryResponse, GroupUpdate, GroupLongPollServerState,
            GroupLongPollConfiguration>
    {
        private ulong _groupId;

        public GroupLongPoll(IVkApi vkApi) : base(vkApi)
        {
        }

        protected override async Task<bool> ValidateAsync(IVkApi vkApi)
        {
            var groups = await vkApi
                .Groups
                .GetByIdAsync(null, null, null)
                .ConfigureAwait(false);

            var groupOwner = groups.FirstOrDefault();

            if (groupOwner != null)
            {
                _groupId = (ulong)groupOwner.Id;

                return true;
            }

            return false;
        }

        protected override async Task<GroupLongPollServerState> GetServerInformationAsync(IVkApi vkApi,
            GroupLongPollConfiguration longPollConfiguration, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var longPollServerResponse = await vkApi
                .Groups
                .GetLongPollServerAsync(_groupId)
                .ConfigureAwait(false);

            return new GroupLongPollServerState(
                Convert.ToUInt64(longPollServerResponse.Ts),
                longPollServerResponse.Key,
                longPollServerResponse.Server
            );
        }

        protected override async Task<BotsLongPollHistoryResponse> GetUpdatesAsync(IVkApi vkApi,
            GroupLongPollConfiguration longPollConfiguration,
            GroupLongPollServerState longPollServerInformation,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var longPollHistory = await vkApi
                .Groups
                .GetBotsLongPollHistoryAsync(new BotsLongPollHistoryParams
                {
                    Key = longPollServerInformation.Key,
                    Server = longPollServerInformation.Server,
                    Ts = longPollServerInformation.Ts.ToString()
                })
                .ConfigureAwait(false);

            longPollServerInformation.Update(Convert.ToUInt64(longPollHistory.Ts));

            return longPollHistory;
        }

        protected override IEnumerable<GroupUpdate> ConvertLongPollResponse(
            BotsLongPollHistoryResponse longPollResponse)
        {
            foreach (GroupUpdate groupUpdate in longPollResponse.Updates)
            {
#pragma warning disable 612, 618 // Ignore Obsolete attribute
                if ((Configuration.AllowedUpdateTypes != null
                    && Array.IndexOf(Configuration.AllowedUpdateTypes, groupUpdate.Type) == -1)
                    || (Configuration.AllowedTypes != null
                        && !Configuration.AllowedTypes.Contains(groupUpdate.Instance.GetType())))
                    continue;
#pragma warning restore 612, 618 // Ignore Obsolete attribute

                yield return groupUpdate;
            }
        }
    }
}