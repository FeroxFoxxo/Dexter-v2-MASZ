using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using masz.Dtos.DiscordAPIResponses;

namespace masz.Models
{
    public class AutoModerationTypeSplit
    {
        public AutoModerationType Type { get; set; }
        public int Count { get; set; }
    }
}