using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VkNet.Abstractions;
using VkNet.Extensions.Polling.Models.Configuration;
using VkNet.Extensions.Polling.Models.State;
using VkNet.Extensions.Polling.Models.Update;
using VkNet.Model;
using VkNet.Model.RequestParams;

namespace VkNet.Extensions.Polling
{
    public class UserLongPoll :
        LongPollBase<LongPollHistoryResponse, UserUpdate, UserLongPollServerState, UserLongPollConfiguration>
    {
        public UserLongPoll(IVkApi vkApi) : base(vkApi)
        {
        }

        protected override async Task<bool> ValidateAsync(IVkApi vkApi)
        {
            var users = await vkApi
                .Users
                .GetAsync(Array.Empty<long>())
                .ConfigureAwait(false);

            return users.Any();
        }

        protected override async Task<UserLongPollServerState> GetServerInformationAsync(IVkApi vkApi,
            UserLongPollConfiguration longPollConfiguration, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var longPollServer = await vkApi
                .Messages
                .GetLongPollServerAsync(true)
                .ConfigureAwait(false);

            ulong pts = longPollServer.Pts ?? throw new InvalidOperationException("Не удалось получить Pts. Проблема при получении информации о сервере.");

            return new UserLongPollServerState(Convert.ToUInt64(longPollServer.Ts), pts);
        }

        protected override async Task<LongPollHistoryResponse> GetUpdatesAsync(IVkApi vkApi,
            UserLongPollConfiguration userLongPollConfiguration,
            UserLongPollServerState longPollServerInformation,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var longPollHistory = await vkApi
                .Messages
                .GetLongPollHistoryAsync(new MessagesGetLongPollHistoryParams
                {
                    Pts = longPollServerInformation.Pts,
                    Ts = longPollServerInformation.Ts,
                    Fields = userLongPollConfiguration.Fields
                })
                .ConfigureAwait(false);

            longPollServerInformation.Update(longPollHistory.NewPts);

            return longPollHistory;
        }

        protected override IEnumerable<UserUpdate> ConvertLongPollResponse(
            LongPollHistoryResponse longPollResponse)
        {
            foreach (var message in longPollResponse.Messages)
            {
                UserUpdateSender updateSender;

                if (message.FromId < 0)
                    updateSender = new UserUpdateSender(longPollResponse.Groups.First(_ => _.Id == message.FromId));
                else
                    updateSender = new UserUpdateSender(longPollResponse.Profiles.First(_ => _.Id == message.FromId));

                var userUpdate = new UserUpdate(message, updateSender);

                yield return userUpdate;
            }
        }
    }
}