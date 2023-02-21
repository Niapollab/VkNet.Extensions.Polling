using System;
using System.Collections.Generic;
using VkNet.Enums.SafetyEnums;

namespace VkNet.Extensions.Polling.Models.Configuration
{
    public class GroupLongPollConfiguration : ILongPollConfiguration
    {
        public static GroupLongPollConfiguration Default => new GroupLongPollConfiguration()
        {
            RequestDelay = TimeSpan.FromMilliseconds(333),
            IgnorePreviousUpdates = true
        };

        public TimeSpan RequestDelay { get; set; }

        public bool IgnorePreviousUpdates { get; set; }

        [Obsolete]
        private GroupUpdateType[] _allowedUpdateTypes;

        [Obsolete("Используйте свойство \"" + nameof(AllowedTypes) + "\" вместо этого.")]
        public GroupUpdateType[] AllowedUpdateTypes
        {
            get => _allowedUpdateTypes;
            set
            {
                _allowedUpdateTypes = value == null || _allowedTypes == null
                    ? value
                    : throw new ArgumentException(string.Format("Невозможно использовать свойства \"{0}\" и \"{1}\" одновременно. Присвойте \"{1}\" значение null.", nameof(AllowedUpdateTypes), nameof(AllowedTypes)), nameof(value));
            }
        }

        private ISet<Type> _allowedTypes;

        public ISet<Type> AllowedTypes
        {
            get => _allowedTypes;
            set
            {
#pragma warning disable 612, 618 // Ignore Obsolete attribute
                _allowedTypes = value == null || _allowedUpdateTypes == null
                    ? value
                    : throw new ArgumentException(string.Format("Невозможно использовать свойства \"{0}\" и \"{1}\" одновременно. Присвойте \"{1}\" значение null.", nameof(AllowedTypes), nameof(AllowedUpdateTypes)), nameof(value));
#pragma warning restore 612, 618 // Ignore Obsolete attribute
            }
        }
    }
}