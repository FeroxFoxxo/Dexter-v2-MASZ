using AutoMods.Models;
using Discord;
using Discord.WebSocket;
using System.Text.RegularExpressions;

namespace AutoMods.MessageChecks;

public static class EmoteCheck
{
    private static readonly Regex EmoteRegex = new(
        @"(👨🏿‍❤️‍💋‍👨🏿|👨🏿‍❤️‍💋‍👨🏻|👨🏿‍❤️‍💋‍👨🏾|👨🏿‍❤️‍💋‍👨🏼|👨🏿‍❤️‍💋‍👨🏽|👨🏻‍❤️‍💋‍👨🏻|👨🏻‍❤️‍💋‍👨🏿|👨🏻‍❤️‍💋‍👨🏾|👨🏻‍❤️‍💋‍👨🏼|👨🏻‍❤️‍💋‍👨🏽|👨🏾‍❤️‍💋‍👨🏾|👨🏾‍❤️‍💋‍👨🏿|👨🏾‍❤️‍💋‍👨🏻|👨🏾‍❤️‍💋‍👨🏼|👨🏾‍❤️‍💋‍👨🏽|👨🏼‍❤️‍💋‍👨🏼|👨🏼‍❤️‍💋‍👨🏿|👨🏼‍❤️‍💋‍👨🏻|👨🏼‍❤️‍💋‍👨🏾|👨🏼‍❤️‍💋‍👨🏽|👨🏽‍❤️‍💋‍👨🏽|👨🏽‍❤️‍💋‍👨🏿|👨🏽‍❤️‍💋‍👨🏻|👨🏽‍❤️‍💋‍👨🏾|👨🏽‍❤️‍💋‍👨🏼|🧑🏿‍❤️‍💋‍🧑🏻|🧑🏿‍❤️‍💋‍🧑🏾|🧑🏿‍❤️‍💋‍🧑🏼|🧑🏿‍❤️‍💋‍🧑🏽|🧑🏻‍❤️‍💋‍🧑🏿|🧑🏻‍❤️‍💋‍🧑🏾|🧑🏻‍❤️‍💋‍🧑🏼|🧑🏻‍❤️‍💋‍🧑🏽|🧑🏾‍❤️‍💋‍🧑🏿|🧑🏾‍❤️‍💋‍🧑🏻|🧑🏾‍❤️‍💋‍🧑🏼|🧑🏾‍❤️‍💋‍🧑🏽|🧑🏼‍❤️‍💋‍🧑🏿|🧑🏼‍❤️‍💋‍🧑🏻|🧑🏼‍❤️‍💋‍🧑🏾|🧑🏼‍❤️‍💋‍🧑🏽|🧑🏽‍❤️‍💋‍🧑🏿|🧑🏽‍❤️‍💋‍🧑🏻|🧑🏽‍❤️‍💋‍🧑🏾|🧑🏽‍❤️‍💋‍🧑🏼|👩🏿‍❤️‍💋‍👨🏿|👩🏿‍❤️‍💋‍👨🏻|👩🏿‍❤️‍💋‍👨🏾|👩🏿‍❤️‍💋‍👨🏼|👩🏿‍❤️‍💋‍👨🏽|👩🏻‍❤️‍💋‍👨🏻|👩🏻‍❤️‍💋‍👨🏿|👩🏻‍❤️‍💋‍👨🏾|👩🏻‍❤️‍💋‍👨🏼|👩🏻‍❤️‍💋‍👨🏽|👩🏾‍❤️‍💋‍👨🏾|👩🏾‍❤️‍💋‍👨🏿|👩🏾‍❤️‍💋‍👨🏻|👩🏾‍❤️‍💋‍👨🏼|👩🏾‍❤️‍💋‍👨🏽|👩🏼‍❤️‍💋‍👨🏼|👩🏼‍❤️‍💋‍👨🏿|👩🏼‍❤️‍💋‍👨🏻|👩🏼‍❤️‍💋‍👨🏾|👩🏼‍❤️‍💋‍👨🏽|👩🏽‍❤️‍💋‍👨🏽|👩🏽‍❤️‍💋‍👨🏿|👩🏽‍❤️‍💋‍👨🏻|👩🏽‍❤️‍💋‍👨🏾|👩🏽‍❤️‍💋‍👨🏼|👩🏿‍❤️‍💋‍👩🏿|👩🏿‍❤️‍💋‍👩🏻|👩🏿‍❤️‍💋‍👩🏾|👩🏿‍❤️‍💋‍👩🏼|👩🏿‍❤️‍💋‍👩🏽|👩🏻‍❤️‍💋‍👩🏻|👩🏻‍❤️‍💋‍👩🏿|👩🏻‍❤️‍💋‍👩🏾|👩🏻‍❤️‍💋‍👩🏼|👩🏻‍❤️‍💋‍👩🏽|👩🏾‍❤️‍💋‍👩🏾|👩🏾‍❤️‍💋‍👩🏿|👩🏾‍❤️‍💋‍👩🏻|👩🏾‍❤️‍💋‍👩🏼|👩🏾‍❤️‍💋‍👩🏽|👩🏼‍❤️‍💋‍👩🏼|👩🏼‍❤️‍💋‍👩🏿|👩🏼‍❤️‍💋‍👩🏻|👩🏼‍❤️‍💋‍👩🏾|👩🏼‍❤️‍💋‍👩🏽|👩🏽‍❤️‍💋‍👩🏽|👩🏽‍❤️‍💋‍👩🏿|👩🏽‍❤️‍💋‍👩🏻|👩🏽‍❤️‍💋‍👩🏾|👩🏽‍❤️‍💋‍👩🏼|👨🏿‍❤‍💋‍👨🏿|👨🏿‍❤‍💋‍👨🏻|👨🏿‍❤‍💋‍👨🏾|👨🏿‍❤‍💋‍👨🏼|👨🏿‍❤‍💋‍👨🏽|👨🏻‍❤‍💋‍👨🏻|👨🏻‍❤‍💋‍👨🏿|👨🏻‍❤‍💋‍👨🏾|👨🏻‍❤‍💋‍👨🏼|👨🏻‍❤‍💋‍👨🏽|👨🏾‍❤‍💋‍👨🏾|👨🏾‍❤‍💋‍👨🏿|👨🏾‍❤‍💋‍👨🏻|👨🏾‍❤‍💋‍👨🏼|👨🏾‍❤‍💋‍👨🏽|👨🏼‍❤‍💋‍👨🏼|👨🏼‍❤‍💋‍👨🏿|👨🏼‍❤‍💋‍👨🏻|👨🏼‍❤‍💋‍👨🏾|👨🏼‍❤‍💋‍👨🏽|👨🏽‍❤‍💋‍👨🏽|👨🏽‍❤‍💋‍👨🏿|👨🏽‍❤‍💋‍👨🏻|👨🏽‍❤‍💋‍👨🏾|👨🏽‍❤‍💋‍👨🏼|🧑🏿‍❤‍💋‍🧑🏻|🧑🏿‍❤‍💋‍🧑🏾|🧑🏿‍❤‍💋‍🧑🏼|🧑🏿‍❤‍💋‍🧑🏽|🧑🏻‍❤‍💋‍🧑🏿|🧑🏻‍❤‍💋‍🧑🏾|🧑🏻‍❤‍💋‍🧑🏼|🧑🏻‍❤‍💋‍🧑🏽|🧑🏾‍❤‍💋‍🧑🏿|🧑🏾‍❤‍💋‍🧑🏻|🧑🏾‍❤‍💋‍🧑🏼|🧑🏾‍❤‍💋‍🧑🏽|🧑🏼‍❤‍💋‍🧑🏿|🧑🏼‍❤‍💋‍🧑🏻|🧑🏼‍❤‍💋‍🧑🏾|🧑🏼‍❤‍💋‍🧑🏽|🧑🏽‍❤‍💋‍🧑🏿|🧑🏽‍❤‍💋‍🧑🏻|🧑🏽‍❤‍💋‍🧑🏾|🧑🏽‍❤‍💋‍🧑🏼|👩🏿‍❤‍💋‍👨🏿|👩🏿‍❤‍💋‍👨🏻|👩🏿‍❤‍💋‍👨🏾|👩🏿‍❤‍💋‍👨🏼|👩🏿‍❤‍💋‍👨🏽|👩🏻‍❤‍💋‍👨🏻|👩🏻‍❤‍💋‍👨🏿|👩🏻‍❤‍💋‍👨🏾|👩🏻‍❤‍💋‍👨🏼|👩🏻‍❤‍💋‍👨🏽|👩🏾‍❤‍💋‍👨🏾|👩🏾‍❤‍💋‍👨🏿|👩🏾‍❤‍💋‍👨🏻|👩🏾‍❤‍💋‍👨🏼|👩🏾‍❤‍💋‍👨🏽|👩🏼‍❤‍💋‍👨🏼|👩🏼‍❤‍💋‍👨🏿|👩🏼‍❤‍💋‍👨🏻|👩🏼‍❤‍💋‍👨🏾|👩🏼‍❤‍💋‍👨🏽|👩🏽‍❤‍💋‍👨🏽|👩🏽‍❤‍💋‍👨🏿|👩🏽‍❤‍💋‍👨🏻|👩🏽‍❤‍💋‍👨🏾|👩🏽‍❤‍💋‍👨🏼|👩🏿‍❤‍💋‍👩🏿|👩🏿‍❤‍💋‍👩🏻|👩🏿‍❤‍💋‍👩🏾|👩🏿‍❤‍💋‍👩🏼|👩🏿‍❤‍💋‍👩🏽|👩🏻‍❤‍💋‍👩🏻|👩🏻‍❤‍💋‍👩🏿|👩🏻‍❤‍💋‍👩🏾|👩🏻‍❤‍💋‍👩🏼|👩🏻‍❤‍💋‍👩🏽|👩🏾‍❤‍💋‍👩🏾|👩🏾‍❤‍💋‍👩🏿|👩🏾‍❤‍💋‍👩🏻|👩🏾‍❤‍💋‍👩🏼|👩🏾‍❤‍💋‍👩🏽|👩🏼‍❤‍💋‍👩🏼|👩🏼‍❤‍💋‍👩🏿|👩🏼‍❤‍💋‍👩🏻|👩🏼‍❤‍💋‍👩🏾|👩🏼‍❤‍💋‍👩🏽|👩🏽‍❤‍💋‍👩🏽|👩🏽‍❤‍💋‍👩🏿|👩🏽‍❤‍💋‍👩🏻|👩🏽‍❤‍💋‍👩🏾|👩🏽‍❤‍💋‍👩🏼|👨🏿‍❤️‍👨🏿|👨🏿‍❤️‍👨🏻|👨🏿‍❤️‍👨🏾|👨🏿‍❤️‍👨🏼|👨🏿‍❤️‍👨🏽|👨🏻‍❤️‍👨🏻|👨🏻‍❤️‍👨🏿|👨🏻‍❤️‍👨🏾|👨🏻‍❤️‍👨🏼|👨🏻‍❤️‍👨🏽|👨🏾‍❤️‍👨🏾|👨🏾‍❤️‍👨🏿|👨🏾‍❤️‍👨🏻|👨🏾‍❤️‍👨🏼|👨🏾‍❤️‍👨🏽|👨🏼‍❤️‍👨🏼|👨🏼‍❤️‍👨🏿|👨🏼‍❤️‍👨🏻|👨🏼‍❤️‍👨🏾|👨🏼‍❤️‍👨🏽|👨🏽‍❤️‍👨🏽|👨🏽‍❤️‍👨🏿|👨🏽‍❤️‍👨🏻|👨🏽‍❤️‍👨🏾|👨🏽‍❤️‍👨🏼|🧑🏿‍❤️‍🧑🏻|🧑🏿‍❤️‍🧑🏾|🧑🏿‍❤️‍🧑🏼|🧑🏿‍❤️‍🧑🏽|🧑🏻‍❤️‍🧑🏿|🧑🏻‍❤️‍🧑🏾|🧑🏻‍❤️‍🧑🏼|🧑🏻‍❤️‍🧑🏽|🧑🏾‍❤️‍🧑🏿|🧑🏾‍❤️‍🧑🏻|🧑🏾‍❤️‍🧑🏼|🧑🏾‍❤️‍🧑🏽|🧑🏼‍❤️‍🧑🏿|🧑🏼‍❤️‍🧑🏻|🧑🏼‍❤️‍🧑🏾|🧑🏼‍❤️‍🧑🏽|🧑🏽‍❤️‍🧑🏿|🧑🏽‍❤️‍🧑🏻|🧑🏽‍❤️‍🧑🏾|🧑🏽‍❤️‍🧑🏼|👩🏿‍❤️‍👨🏿|👩🏿‍❤️‍👨🏻|👩🏿‍❤️‍👨🏾|👩🏿‍❤️‍👨🏼|👩🏿‍❤️‍👨🏽|👩🏻‍❤️‍👨🏻|👩🏻‍❤️‍👨🏿|👩🏻‍❤️‍👨🏾|👩🏻‍❤️‍👨🏼|👩🏻‍❤️‍👨🏽|👩🏾‍❤️‍👨🏾|👩🏾‍❤️‍👨🏿|👩🏾‍❤️‍👨🏻|👩🏾‍❤️‍👨🏼|👩🏾‍❤️‍👨🏽|👩🏼‍❤️‍👨🏼|👩🏼‍❤️‍👨🏿|👩🏼‍❤️‍👨🏻|👩🏼‍❤️‍👨🏾|👩🏼‍❤️‍👨🏽|👩🏽‍❤️‍👨🏽|👩🏽‍❤️‍👨🏿|👩🏽‍❤️‍👨🏻|👩🏽‍❤️‍👨🏾|👩🏽‍❤️‍👨🏼|👩🏿‍❤️‍👩🏿|👩🏿‍❤️‍👩🏻|👩🏿‍❤️‍👩🏾|👩🏿‍❤️‍👩🏼|👩🏿‍❤️‍👩🏽|👩🏻‍❤️‍👩🏻|👩🏻‍❤️‍👩🏿|👩🏻‍❤️‍👩🏾|👩🏻‍❤️‍👩🏼|👩🏻‍❤️‍👩🏽|👩🏾‍❤️‍👩🏾|👩🏾‍❤️‍👩🏿|👩🏾‍❤️‍👩🏻|👩🏾‍❤️‍👩🏼|👩🏾‍❤️‍👩🏽|👩🏼‍❤️‍👩🏼|👩🏼‍❤️‍👩🏿|👩🏼‍❤️‍👩🏻|👩🏼‍❤️‍👩🏾|👩🏼‍❤️‍👩🏽|👩🏽‍❤️‍👩🏽|👩🏽‍❤️‍👩🏿|👩🏽‍❤️‍👩🏻|👩🏽‍❤️‍👩🏾|👩🏽‍❤️‍👩🏼|👨‍❤️‍💋‍👨|👩‍❤️‍💋‍👨|👩‍❤️‍💋‍👩|🏴󠁧󠁢󠁥󠁮󠁧󠁿|🏴󠁧󠁢󠁳󠁣󠁴󠁿|🏴󠁧󠁢󠁷󠁬󠁳󠁿|👨🏿‍❤‍👨🏿|👨🏿‍❤‍👨🏻|👨🏿‍❤‍👨🏾|👨🏿‍❤‍👨🏼|👨🏿‍❤‍👨🏽|👨🏻‍❤‍👨🏻|👨🏻‍❤‍👨🏿|👨🏻‍❤‍👨🏾|👨🏻‍❤‍👨🏼|👨🏻‍❤‍👨🏽|👨🏾‍❤‍👨🏾|👨🏾‍❤‍👨🏿|👨🏾‍❤‍👨🏻|👨🏾‍❤‍👨🏼|👨🏾‍❤‍👨🏽|👨🏼‍❤‍👨🏼|👨🏼‍❤‍👨🏿|👨🏼‍❤‍👨🏻|👨🏼‍❤‍👨🏾|👨🏼‍❤‍👨🏽|👨🏽‍❤‍👨🏽|👨🏽‍❤‍👨🏿|👨🏽‍❤‍👨🏻|👨🏽‍❤‍👨🏾|👨🏽‍❤‍👨🏼|🧑🏿‍❤‍🧑🏻|🧑🏿‍❤‍🧑🏾|🧑🏿‍❤‍🧑🏼|🧑🏿‍❤‍🧑🏽|🧑🏻‍❤‍🧑🏿|🧑🏻‍❤‍🧑🏾|🧑🏻‍❤‍🧑🏼|🧑🏻‍❤‍🧑🏽|🧑🏾‍❤‍🧑🏿|🧑🏾‍❤‍🧑🏻|🧑🏾‍❤‍🧑🏼|🧑🏾‍❤‍🧑🏽|🧑🏼‍❤‍🧑🏿|🧑🏼‍❤‍🧑🏻|🧑🏼‍❤‍🧑🏾|🧑🏼‍❤‍🧑🏽|🧑🏽‍❤‍🧑🏿|🧑🏽‍❤‍🧑🏻|🧑🏽‍❤‍🧑🏾|🧑🏽‍❤‍🧑🏼|👩🏿‍❤‍👨🏿|👩🏿‍❤‍👨🏻|👩🏿‍❤‍👨🏾|👩🏿‍❤‍👨🏼|👩🏿‍❤‍👨🏽|👩🏻‍❤‍👨🏻|👩🏻‍❤‍👨🏿|👩🏻‍❤‍👨🏾|👩🏻‍❤‍👨🏼|👩🏻‍❤‍👨🏽|👩🏾‍❤‍👨🏾|👩🏾‍❤‍👨🏿|👩🏾‍❤‍👨🏻|👩🏾‍❤‍👨🏼|👩🏾‍❤‍👨🏽|👩🏼‍❤‍👨🏼|👩🏼‍❤‍👨🏿|👩🏼‍❤‍👨🏻|👩🏼‍❤‍👨🏾|👩🏼‍❤‍👨🏽|👩🏽‍❤‍👨🏽|👩🏽‍❤‍👨🏿|👩🏽‍❤‍👨🏻|👩🏽‍❤‍👨🏾|👩🏽‍❤‍👨🏼|👩🏿‍❤‍👩🏿|👩🏿‍❤‍👩🏻|👩🏿‍❤‍👩🏾|👩🏿‍❤‍👩🏼|👩🏿‍❤‍👩🏽|👩🏻‍❤‍👩🏻|👩🏻‍❤‍👩🏿|👩🏻‍❤‍👩🏾|👩🏻‍❤‍👩🏼|👩🏻‍❤‍👩🏽|👩🏾‍❤‍👩🏾|👩🏾‍❤‍👩🏿|👩🏾‍❤‍👩🏻|👩🏾‍❤‍👩🏼|👩🏾‍❤‍👩🏽|👩🏼‍❤‍👩🏼|👩🏼‍❤‍👩🏿|👩🏼‍❤‍👩🏻|👩🏼‍❤‍👩🏾|👩🏼‍❤‍👩🏽|👩🏽‍❤‍👩🏽|👩🏽‍❤‍👩🏿|👩🏽‍❤‍👩🏻|👩🏽‍❤‍👩🏾|👩🏽‍❤‍👩🏼|👨‍👨‍👦‍👦|👨‍👨‍👧‍👦|👨‍👨‍👧‍👧|👨‍👩‍👦‍👦|👨‍👩‍👧‍👦|👨‍👩‍👧‍👧|👩‍👩‍👦‍👦|👩‍👩‍👧‍👦|👩‍👩‍👧‍👧|👨‍❤‍💋‍👨|👩‍❤‍💋‍👨|👩‍❤‍💋‍👩|👨🏿‍🤝‍👨🏻|👨🏿‍🤝‍👨🏾|👨🏿‍🤝‍👨🏼|👨🏿‍🤝‍👨🏽|👨🏻‍🤝‍👨🏿|👨🏻‍🤝‍👨🏾|👨🏻‍🤝‍👨🏼|👨🏻‍🤝‍👨🏽|👨🏾‍🤝‍👨🏿|👨🏾‍🤝‍👨🏻|👨🏾‍🤝‍👨🏼|👨🏾‍🤝‍👨🏽|👨🏼‍🤝‍👨🏿|👨🏼‍🤝‍👨🏻|👨🏼‍🤝‍👨🏾|👨🏼‍🤝‍👨🏽|👨🏽‍🤝‍👨🏿|👨🏽‍🤝‍👨🏻|👨🏽‍🤝‍👨🏾|👨🏽‍🤝‍👨🏼|🧑🏿‍🤝‍🧑🏿|🧑🏿‍🤝‍🧑🏻|🧑🏿‍🤝‍🧑🏾|🧑🏿‍🤝‍🧑🏼|🧑🏿‍🤝‍🧑🏽|🧑🏻‍🤝‍🧑🏻|🧑🏻‍🤝‍🧑🏿|🧑🏻‍🤝‍🧑🏾|🧑🏻‍🤝‍🧑🏼|🧑🏻‍🤝‍🧑🏽|🧑🏾‍🤝‍🧑🏾|🧑🏾‍🤝‍🧑🏿|🧑🏾‍🤝‍🧑🏻|🧑🏾‍🤝‍🧑🏼|🧑🏾‍🤝‍🧑🏽|🧑🏼‍🤝‍🧑🏼|🧑🏼‍🤝‍🧑🏿|🧑🏼‍🤝‍🧑🏻|🧑🏼‍🤝‍🧑🏾|🧑🏼‍🤝‍🧑🏽|🧑🏽‍🤝‍🧑🏽|🧑🏽‍🤝‍🧑🏿|🧑🏽‍🤝‍🧑🏻|🧑🏽‍🤝‍🧑🏾|🧑🏽‍🤝‍🧑🏼|👩🏿‍🤝‍👨🏻|👩🏿‍🤝‍👨🏾|👩🏿‍🤝‍👨🏼|👩🏿‍🤝‍👨🏽|👩🏻‍🤝‍👨🏿|👩🏻‍🤝‍👨🏾|👩🏻‍🤝‍👨🏼|👩🏻‍🤝‍👨🏽|👩🏾‍🤝‍👨🏿|👩🏾‍🤝‍👨🏻|👩🏾‍🤝‍👨🏼|👩🏾‍🤝‍👨🏽|👩🏼‍🤝‍👨🏿|👩🏼‍🤝‍👨🏻|👩🏼‍🤝‍👨🏾|👩🏼‍🤝‍👨🏽|👩🏽‍🤝‍👨🏿|👩🏽‍🤝‍👨🏻|👩🏽‍🤝‍👨🏾|👩🏽‍🤝‍👨🏼|👩🏿‍🤝‍👩🏻|👩🏿‍🤝‍👩🏾|👩🏿‍🤝‍👩🏼|👩🏿‍🤝‍👩🏽|👩🏻‍🤝‍👩🏿|👩🏻‍🤝‍👩🏾|👩🏻‍🤝‍👩🏼|👩🏻‍🤝‍👩🏽|👩🏾‍🤝‍👩🏿|👩🏾‍🤝‍👩🏻|👩🏾‍🤝‍👩🏼|👩🏾‍🤝‍👩🏽|👩🏼‍🤝‍👩🏿|👩🏼‍🤝‍👩🏻|👩🏼‍🤝‍👩🏾|👩🏼‍🤝‍👩🏽|👩🏽‍🤝‍👩🏿|👩🏽‍🤝‍👩🏻|👩🏽‍🤝‍👩🏾|👩🏽‍🤝‍👩🏼|👨‍❤️‍👨|👩‍❤️‍👨|👩‍❤️‍👩|👨‍❤‍👨|👩‍❤‍👨|👩‍❤‍👩|🧏🏿‍♂️|🧏🏻‍♂️|🧏🏾‍♂️|🧏🏼‍♂️|🧏🏽‍♂️|🧏🏿‍♀️|🧏🏻‍♀️|🧏🏾‍♀️|🧏🏼‍♀️|🧏🏽‍♀️|👁️‍🗨️|👨‍👦‍👦|👨‍👧‍👦|👨‍👧‍👧|👨‍👨‍👦|👨‍👨‍👧|👨‍👩‍👦|👨‍👩‍👧|👩‍👦‍👦|👩‍👧‍👦|👩‍👧‍👧|👩‍👩‍👦|👩‍👩‍👧|🫱🏿‍🫲🏻|🫱🏿‍🫲🏾|🫱🏿‍🫲🏼|🫱🏿‍🫲🏽|🫱🏻‍🫲🏿|🫱🏻‍🫲🏾|🫱🏻‍🫲🏼|🫱🏻‍🫲🏽|🫱🏾‍🫲🏿|🫱🏾‍🫲🏻|🫱🏾‍🫲🏼|🫱🏾‍🫲🏽|🫱🏼‍🫲🏿|🫱🏼‍🫲🏻|🫱🏼‍🫲🏾|🫱🏼‍🫲🏽|🫱🏽‍🫲🏿|🫱🏽‍🫲🏻|🫱🏽‍🫲🏾|🫱🏽‍🫲🏼|🧑🏿‍⚕️|🧑🏻‍⚕️|🧑🏾‍⚕️|🧑🏼‍⚕️|🧑🏽‍⚕️|🧑🏿‍⚖️|🧑🏻‍⚖️|🧑🏾‍⚖️|🧑🏼‍⚖️|🧑🏽‍⚖️|🚴🏿‍♂️|🚴🏻‍♂️|🚴🏾‍♂️|🚴🏼‍♂️|🚴🏽‍♂️|⛹️‍♂️|⛹🏿‍♂️|⛹🏻‍♂️|⛹🏾‍♂️|⛹🏼‍♂️|⛹🏽‍♂️|🙇🏿‍♂️|🙇🏻‍♂️|🙇🏾‍♂️|🙇🏼‍♂️|🙇🏽‍♂️|🤸🏿‍♂️|🤸🏻‍♂️|🤸🏾‍♂️|🤸🏼‍♂️|🤸🏽‍♂️|🧗🏿‍♂️|🧗🏻‍♂️|🧗🏾‍♂️|🧗🏼‍♂️|🧗🏽‍♂️|👷🏿‍♂️|👷🏻‍♂️|👷🏾‍♂️|👷🏼‍♂️|👷🏽‍♂️|🧔🏿‍♂️|👱🏿‍♂️|🕵️‍♂️|🕵🏿‍♂️|🕵🏻‍♂️|🕵🏾‍♂️|🕵🏼‍♂️|🕵🏽‍♂️|🧝🏿‍♂️|🧝🏻‍♂️|🧝🏾‍♂️|🧝🏼‍♂️|🧝🏽‍♂️|🤦🏿‍♂️|🤦🏻‍♂️|🤦🏾‍♂️|🤦🏼‍♂️|🤦🏽‍♂️|🧚🏿‍♂️|🧚🏻‍♂️|🧚🏾‍♂️|🧚🏼‍♂️|🧚🏽‍♂️|🙍🏿‍♂️|🙍🏻‍♂️|🙍🏾‍♂️|🙍🏼‍♂️|🙍🏽‍♂️|🙅🏿‍♂️|🙅🏻‍♂️|🙅🏾‍♂️|🙅🏼‍♂️|🙅🏽‍♂️|🙆🏿‍♂️|🙆🏻‍♂️|🙆🏾‍♂️|🙆🏼‍♂️|🙆🏽‍♂️|💇🏿‍♂️|💇🏻‍♂️|💇🏾‍♂️|💇🏼‍♂️|💇🏽‍♂️|💆🏿‍♂️|💆🏻‍♂️|💆🏾‍♂️|💆🏼‍♂️|💆🏽‍♂️|🏌️‍♂️|🏌🏿‍♂️|🏌🏻‍♂️|🏌🏾‍♂️|🏌🏼‍♂️|🏌🏽‍♂️|💂🏿‍♂️|💂🏻‍♂️|💂🏾‍♂️|💂🏼‍♂️|💂🏽‍♂️|👨🏿‍⚕️|👨🏻‍⚕️|👨🏾‍⚕️|👨🏼‍⚕️|👨🏽‍⚕️|🧘🏿‍♂️|🧘🏻‍♂️|🧘🏾‍♂️|🧘🏼‍♂️|🧘🏽‍♂️|🧖🏿‍♂️|🧖🏻‍♂️|🧖🏾‍♂️|🧖🏼‍♂️|🧖🏽‍♂️|🤵🏿‍♂️|🤵🏻‍♂️|🤵🏾‍♂️|🤵🏼‍♂️|🤵🏽‍♂️|👨🏿‍⚖️|👨🏻‍⚖️|👨🏾‍⚖️|👨🏼‍⚖️|👨🏽‍⚖️|🤹🏿‍♂️|🤹🏻‍♂️|🤹🏾‍♂️|🤹🏼‍♂️|🤹🏽‍♂️|🧎🏿‍♂️|🧎🏻‍♂️|🧎🏾‍♂️|🧎🏼‍♂️|🧎🏽‍♂️|🏋️‍♂️|🏋🏿‍♂️|🏋🏻‍♂️|🏋🏾‍♂️|🏋🏼‍♂️|🏋🏽‍♂️|🧔🏻‍♂️|👱🏻‍♂️|🧙🏿‍♂️|🧙🏻‍♂️|🧙🏾‍♂️|🧙🏼‍♂️|🧙🏽‍♂️|🧔🏾‍♂️|👱🏾‍♂️|🧔🏼‍♂️|👱🏼‍♂️|🧔🏽‍♂️|👱🏽‍♂️|🚵🏿‍♂️|🚵🏻‍♂️|🚵🏾‍♂️|🚵🏼‍♂️|🚵🏽‍♂️|👨🏿‍✈️|👨🏻‍✈️|👨🏾‍✈️|👨🏼‍✈️|👨🏽‍✈️|🤾🏿‍♂️|🤾🏻‍♂️|🤾🏾‍♂️|🤾🏼‍♂️|🤾🏽‍♂️|🤽🏿‍♂️|🤽🏻‍♂️|🤽🏾‍♂️|🤽🏼‍♂️|🤽🏽‍♂️|👮🏿‍♂️|👮🏻‍♂️|👮🏾‍♂️|👮🏼‍♂️|👮🏽‍♂️|🙎🏿‍♂️|🙎🏻‍♂️|🙎🏾‍♂️|🙎🏼‍♂️|🙎🏽‍♂️|🙋🏿‍♂️|🙋🏻‍♂️|🙋🏾‍♂️|🙋🏼‍♂️|🙋🏽‍♂️|🚣🏿‍♂️|🚣🏻‍♂️|🚣🏾‍♂️|🚣🏼‍♂️|🚣🏽‍♂️|🏃🏿‍♂️|🏃🏻‍♂️|🏃🏾‍♂️|🏃🏼‍♂️|🏃🏽‍♂️|🤷🏿‍♂️|🤷🏻‍♂️|🤷🏾‍♂️|🤷🏼‍♂️|🤷🏽‍♂️|🧍🏿‍♂️|🧍🏻‍♂️|🧍🏾‍♂️|🧍🏼‍♂️|🧍🏽‍♂️|🦸🏿‍♂️|🦸🏻‍♂️|🦸🏾‍♂️|🦸🏼‍♂️|🦸🏽‍♂️|🦹🏿‍♂️|🦹🏻‍♂️|🦹🏾‍♂️|🦹🏼‍♂️|🦹🏽‍♂️|🏄🏿‍♂️|🏄🏻‍♂️|🏄🏾‍♂️|🏄🏼‍♂️|🏄🏽‍♂️|🏊🏿‍♂️|🏊🏻‍♂️|🏊🏾‍♂️|🏊🏼‍♂️|🏊🏽‍♂️|💁🏿‍♂️|💁🏻‍♂️|💁🏾‍♂️|💁🏼‍♂️|💁🏽‍♂️|🧛🏿‍♂️|🧛🏻‍♂️|🧛🏾‍♂️|🧛🏼‍♂️|🧛🏽‍♂️|🚶🏿‍♂️|🚶🏻‍♂️|🚶🏾‍♂️|🚶🏼‍♂️|🚶🏽‍♂️|👳🏿‍♂️|👳🏻‍♂️|👳🏾‍♂️|👳🏼‍♂️|👳🏽‍♂️|👰🏿‍♂️|👰🏻‍♂️|👰🏾‍♂️|👰🏼‍♂️|👰🏽‍♂️|🧜🏿‍♀️|🧜🏻‍♀️|🧜🏾‍♀️|🧜🏼‍♀️|🧜🏽‍♀️|🧜🏿‍♂️|🧜🏻‍♂️|🧜🏾‍♂️|🧜🏼‍♂️|🧜🏽‍♂️|🧑‍🤝‍🧑|🧑🏿‍✈️|🧑🏻‍✈️|🧑🏾‍✈️|🧑🏼‍✈️|🧑🏽‍✈️|🏳️‍⚧️|🚴🏿‍♀️|🚴🏻‍♀️|🚴🏾‍♀️|🚴🏼‍♀️|🚴🏽‍♀️|⛹️‍♀️|⛹🏿‍♀️|⛹🏻‍♀️|⛹🏾‍♀️|⛹🏼‍♀️|⛹🏽‍♀️|🙇🏿‍♀️|🙇🏻‍♀️|🙇🏾‍♀️|🙇🏼‍♀️|🙇🏽‍♀️|🤸🏿‍♀️|🤸🏻‍♀️|🤸🏾‍♀️|🤸🏼‍♀️|🤸🏽‍♀️|🧗🏿‍♀️|🧗🏻‍♀️|🧗🏾‍♀️|🧗🏼‍♀️|🧗🏽‍♀️|👷🏿‍♀️|👷🏻‍♀️|👷🏾‍♀️|👷🏼‍♀️|👷🏽‍♀️|🧔🏿‍♀️|👱🏿‍♀️|🕵️‍♀️|🕵🏿‍♀️|🕵🏻‍♀️|🕵🏾‍♀️|🕵🏼‍♀️|🕵🏽‍♀️|🧝🏿‍♀️|🧝🏻‍♀️|🧝🏾‍♀️|🧝🏼‍♀️|🧝🏽‍♀️|🤦🏿‍♀️|🤦🏻‍♀️|🤦🏾‍♀️|🤦🏼‍♀️|🤦🏽‍♀️|🧚🏿‍♀️|🧚🏻‍♀️|🧚🏾‍♀️|🧚🏼‍♀️|🧚🏽‍♀️|🙍🏿‍♀️|🙍🏻‍♀️|🙍🏾‍♀️|🙍🏼‍♀️|🙍🏽‍♀️|🙅🏿‍♀️|🙅🏻‍♀️|🙅🏾‍♀️|🙅🏼‍♀️|🙅🏽‍♀️|🙆🏿‍♀️|🙆🏻‍♀️|🙆🏾‍♀️|🙆🏼‍♀️|🙆🏽‍♀️|💇🏿‍♀️|💇🏻‍♀️|💇🏾‍♀️|💇🏼‍♀️|💇🏽‍♀️|💆🏿‍♀️|💆🏻‍♀️|💆🏾‍♀️|💆🏼‍♀️|💆🏽‍♀️|🏌️‍♀️|🏌🏿‍♀️|🏌🏻‍♀️|🏌🏾‍♀️|🏌🏼‍♀️|🏌🏽‍♀️|💂🏿‍♀️|💂🏻‍♀️|💂🏾‍♀️|💂🏼‍♀️|💂🏽‍♀️|👩🏿‍⚕️|👩🏻‍⚕️|👩🏾‍⚕️|👩🏼‍⚕️|👩🏽‍⚕️|🧘🏿‍♀️|🧘🏻‍♀️|🧘🏾‍♀️|🧘🏼‍♀️|🧘🏽‍♀️|🧖🏿‍♀️|🧖🏻‍♀️|🧖🏾‍♀️|🧖🏼‍♀️|🧖🏽‍♀️|🤵🏿‍♀️|🤵🏻‍♀️|🤵🏾‍♀️|🤵🏼‍♀️|🤵🏽‍♀️|👩🏿‍⚖️|👩🏻‍⚖️|👩🏾‍⚖️|👩🏼‍⚖️|👩🏽‍⚖️|🤹🏿‍♀️|🤹🏻‍♀️|🤹🏾‍♀️|🤹🏼‍♀️|🤹🏽‍♀️|🧎🏿‍♀️|🧎🏻‍♀️|🧎🏾‍♀️|🧎🏼‍♀️|🧎🏽‍♀️|🏋️‍♀️|🏋🏿‍♀️|🏋🏻‍♀️|🏋🏾‍♀️|🏋🏼‍♀️|🏋🏽‍♀️|🧔🏻‍♀️|👱🏻‍♀️|🧙🏿‍♀️|🧙🏻‍♀️|🧙🏾‍♀️|🧙🏼‍♀️|🧙🏽‍♀️|🧔🏾‍♀️|👱🏾‍♀️|🧔🏼‍♀️|👱🏼‍♀️|🧔🏽‍♀️|👱🏽‍♀️|🚵🏿‍♀️|🚵🏻‍♀️|🚵🏾‍♀️|🚵🏼‍♀️|🚵🏽‍♀️|👩🏿‍✈️|👩🏻‍✈️|👩🏾‍✈️|👩🏼‍✈️|👩🏽‍✈️|🤾🏿‍♀️|🤾🏻‍♀️|🤾🏾‍♀️|🤾🏼‍♀️|🤾🏽‍♀️|🤽🏿‍♀️|🤽🏻‍♀️|🤽🏾‍♀️|🤽🏼‍♀️|🤽🏽‍♀️|👮🏿‍♀️|👮🏻‍♀️|👮🏾‍♀️|👮🏼‍♀️|👮🏽‍♀️|🙎🏿‍♀️|🙎🏻‍♀️|🙎🏾‍♀️|🙎🏼‍♀️|🙎🏽‍♀️|🙋🏿‍♀️|🙋🏻‍♀️|🙋🏾‍♀️|🙋🏼‍♀️|🙋🏽‍♀️|🚣🏿‍♀️|🚣🏻‍♀️|🚣🏾‍♀️|🚣🏼‍♀️|🚣🏽‍♀️|🏃🏿‍♀️|🏃🏻‍♀️|🏃🏾‍♀️|🏃🏼‍♀️|🏃🏽‍♀️|🤷🏿‍♀️|🤷🏻‍♀️|🤷🏾‍♀️|🤷🏼‍♀️|🤷🏽‍♀️|🧍🏿‍♀️|🧍🏻‍♀️|🧍🏾‍♀️|🧍🏼‍♀️|🧍🏽‍♀️|🦸🏿‍♀️|🦸🏻‍♀️|🦸🏾‍♀️|🦸🏼‍♀️|🦸🏽‍♀️|🦹🏿‍♀️|🦹🏻‍♀️|🦹🏾‍♀️|🦹🏼‍♀️|🦹🏽‍♀️|🏄🏿‍♀️|🏄🏻‍♀️|🏄🏾‍♀️|🏄🏼‍♀️|🏄🏽‍♀️|🏊🏿‍♀️|🏊🏻‍♀️|🏊🏾‍♀️|🏊🏼‍♀️|🏊🏽‍♀️|💁🏿‍♀️|💁🏻‍♀️|💁🏾‍♀️|💁🏼‍♀️|💁🏽‍♀️|🧛🏿‍♀️|🧛🏻‍♀️|🧛🏾‍♀️|🧛🏼‍♀️|🧛🏽‍♀️|🚶🏿‍♀️|🚶🏻‍♀️|🚶🏾‍♀️|🚶🏼‍♀️|🚶🏽‍♀️|👳🏿‍♀️|👳🏻‍♀️|👳🏾‍♀️|👳🏼‍♀️|👳🏽‍♀️|👰🏿‍♀️|👰🏻‍♀️|👰🏾‍♀️|👰🏼‍♀️|👰🏽‍♀️|🧑🏿‍🎨|🧑🏻‍🎨|🧑🏾‍🎨|🧑🏼‍🎨|🧑🏽‍🎨|🧑🏿‍🚀|🧑🏻‍🚀|🧑🏾‍🚀|🧑🏼‍🚀|🧑🏽‍🚀|🧑🏿‍🍳|🧑🏻‍🍳|🧑🏾‍🍳|🧑🏼‍🍳|🧑🏽‍🍳|🧏‍♂️|🧏🏿‍♂|🧏🏻‍♂|🧏🏾‍♂|🧏🏼‍♂|🧏🏽‍♂|🧏‍♀️|🧏🏿‍♀|🧏🏻‍♀|🧏🏾‍♀|🧏🏼‍♀|🧏🏽‍♀|👁‍🗨️|👁️‍🗨|😶‍🌫️|🧑🏿‍🏭|🧑🏻‍🏭|🧑🏾‍🏭|🧑🏼‍🏭|🧑🏽‍🏭|🧑🏿‍🌾|🧑🏻‍🌾|🧑🏾‍🌾|🧑🏼‍🌾|🧑🏽‍🌾|🧑🏿‍🚒|🧑🏻‍🚒|🧑🏾‍🚒|🧑🏼‍🚒|🧑🏽‍🚒|🧑‍⚕️|🧑🏿‍⚕|🧑🏻‍⚕|🧑🏾‍⚕|🧑🏼‍⚕|🧑🏽‍⚕|❤️‍🔥|🧑‍⚖️|🧑🏿‍⚖|🧑🏻‍⚖|🧑🏾‍⚖|🧑🏼‍⚖|🧑🏽‍⚖|👨🏿‍🎨|👨🏻‍🎨|👨🏾‍🎨|👨🏼‍🎨|👨🏽‍🎨|👨🏿‍🚀|👨🏻‍🚀|👨🏾‍🚀|👨🏼‍🚀|👨🏽‍🚀|🧔‍♂️|🚴‍♂️|🚴🏿‍♂|🚴🏻‍♂|🚴🏾‍♂|🚴🏼‍♂|🚴🏽‍♂|👱‍♂️|⛹‍♂️|⛹️‍♂|⛹🏿‍♂|⛹🏻‍♂|⛹🏾‍♂|⛹🏼‍♂|⛹🏽‍♂|🙇‍♂️|🙇🏿‍♂|🙇🏻‍♂|🙇🏾‍♂|🙇🏼‍♂|🙇🏽‍♂|🤸‍♂️|🤸🏿‍♂|🤸🏻‍♂|🤸🏾‍♂|🤸🏼‍♂|🤸🏽‍♂|🧗‍♂️|🧗🏿‍♂|🧗🏻‍♂|🧗🏾‍♂|🧗🏼‍♂|🧗🏽‍♂|👷‍♂️|👷🏿‍♂|👷🏻‍♂|👷🏾‍♂|👷🏼‍♂|👷🏽‍♂|👨🏿‍🍳|👨🏻‍🍳|👨🏾‍🍳|👨🏼‍🍳|👨🏽‍🍳|👨🏿‍🦲|🧔🏿‍♂|👱🏿‍♂|👨🏿‍🦱|👨🏿‍🦰|👨🏿‍🦳|🕵‍♂️|🕵️‍♂|🕵🏿‍♂|🕵🏻‍♂|🕵🏾‍♂|🕵🏼‍♂|🕵🏽‍♂|🧝‍♂️|🧝🏿‍♂|🧝🏻‍♂|🧝🏾‍♂|🧝🏼‍♂|🧝🏽‍♂|🤦‍♂️|🤦🏿‍♂|🤦🏻‍♂|🤦🏾‍♂|🤦🏼‍♂|🤦🏽‍♂|👨🏿‍🏭|👨🏻‍🏭|👨🏾‍🏭|👨🏼‍🏭|👨🏽‍🏭|🧚‍♂️|🧚🏿‍♂|🧚🏻‍♂|🧚🏾‍♂|🧚🏼‍♂|🧚🏽‍♂|👨🏿‍🌾|👨🏻‍🌾|👨🏾‍🌾|👨🏼‍🌾|👨🏽‍🌾|👨🏿‍🍼|👨🏻‍🍼|👨🏾‍🍼|👨🏼‍🍼|👨🏽‍🍼|👨🏿‍🚒|👨🏻‍🚒|👨🏾‍🚒|👨🏼‍🚒|👨🏽‍🚒|🙍‍♂️|🙍🏿‍♂|🙍🏻‍♂|🙍🏾‍♂|🙍🏼‍♂|🙍🏽‍♂|🧞‍♂️|🙅‍♂️|🙅🏿‍♂|🙅🏻‍♂|🙅🏾‍♂|🙅🏼‍♂|🙅🏽‍♂|🙆‍♂️|🙆🏿‍♂|🙆🏻‍♂|🙆🏾‍♂|🙆🏼‍♂|🙆🏽‍♂|💇‍♂️|💇🏿‍♂|💇🏻‍♂|💇🏾‍♂|💇🏼‍♂|💇🏽‍♂|💆‍♂️|💆🏿‍♂|💆🏻‍♂|💆🏾‍♂|💆🏼‍♂|💆🏽‍♂|🏌‍♂️|🏌️‍♂|🏌🏿‍♂|🏌🏻‍♂|🏌🏾‍♂|🏌🏼‍♂|🏌🏽‍♂|💂‍♂️|💂🏿‍♂|💂🏻‍♂|💂🏾‍♂|💂🏼‍♂|💂🏽‍♂|👨‍⚕️|👨🏿‍⚕|👨🏻‍⚕|👨🏾‍⚕|👨🏼‍⚕|👨🏽‍⚕|🧘‍♂️|🧘🏿‍♂|🧘🏻‍♂|🧘🏾‍♂|🧘🏼‍♂|🧘🏽‍♂|👨🏿‍🦽|👨🏻‍🦽|👨🏾‍🦽|👨🏼‍🦽|👨🏽‍🦽|👨🏿‍🦼|👨🏻‍🦼|👨🏾‍🦼|👨🏼‍🦼|👨🏽‍🦼|🧖‍♂️|🧖🏿‍♂|🧖🏻‍♂|🧖🏾‍♂|🧖🏼‍♂|🧖🏽‍♂|🤵‍♂️|🤵🏿‍♂|🤵🏻‍♂|🤵🏾‍♂|🤵🏼‍♂|🤵🏽‍♂|👨‍⚖️|👨🏿‍⚖|👨🏻‍⚖|👨🏾‍⚖|👨🏼‍⚖|👨🏽‍⚖|🤹‍♂️|🤹🏿‍♂|🤹🏻‍♂|🤹🏾‍♂|🤹🏼‍♂|🤹🏽‍♂|🧎‍♂️|🧎🏿‍♂|🧎🏻‍♂|🧎🏾‍♂|🧎🏼‍♂|🧎🏽‍♂|🏋‍♂️|🏋️‍♂|🏋🏿‍♂|🏋🏻‍♂|🏋🏾‍♂|🏋🏼‍♂|🏋🏽‍♂|👨🏻‍🦲|🧔🏻‍♂|👱🏻‍♂|👨🏻‍🦱|👨🏻‍🦰|👨🏻‍🦳|🧙‍♂️|🧙🏿‍♂|🧙🏻‍♂|🧙🏾‍♂|🧙🏼‍♂|🧙🏽‍♂|👨🏿‍🔧|👨🏻‍🔧|👨🏾‍🔧|👨🏼‍🔧|👨🏽‍🔧|👨🏾‍🦲|🧔🏾‍♂|👱🏾‍♂|👨🏾‍🦱|👨🏾‍🦰|👨🏾‍🦳|👨🏼‍🦲|🧔🏼‍♂|👱🏼‍♂|👨🏼‍🦱|👨🏼‍🦰|👨🏼‍🦳|👨🏽‍🦲|🧔🏽‍♂|👱🏽‍♂|👨🏽‍🦱|👨🏽‍🦰|👨🏽‍🦳|🚵‍♂️|🚵🏿‍♂|🚵🏻‍♂|🚵🏾‍♂|🚵🏼‍♂|🚵🏽‍♂|👨🏿‍💼|👨🏻‍💼|👨🏾‍💼|👨🏼‍💼|👨🏽‍💼|👨‍✈️|👨🏿‍✈|👨🏻‍✈|👨🏾‍✈|👨🏼‍✈|👨🏽‍✈|🤾‍♂️|🤾🏿‍♂|🤾🏻‍♂|🤾🏾‍♂|🤾🏼‍♂|🤾🏽‍♂|🤽‍♂️|🤽🏿‍♂|🤽🏻‍♂|🤽🏾‍♂|🤽🏼‍♂|🤽🏽‍♂|👮‍♂️|👮🏿‍♂|👮🏻‍♂|👮🏾‍♂|👮🏼‍♂|👮🏽‍♂|🙎‍♂️|🙎🏿‍♂|🙎🏻‍♂|🙎🏾‍♂|🙎🏼‍♂|🙎🏽‍♂|🙋‍♂️|🙋🏿‍♂|🙋🏻‍♂|🙋🏾‍♂|🙋🏼‍♂|🙋🏽‍♂|🚣‍♂️|🚣🏿‍♂|🚣🏻‍♂|🚣🏾‍♂|🚣🏼‍♂|🚣🏽‍♂|🏃‍♂️|🏃🏿‍♂|🏃🏻‍♂|🏃🏾‍♂|🏃🏼‍♂|🏃🏽‍♂|👨🏿‍🔬|👨🏻‍🔬|👨🏾‍🔬|👨🏼‍🔬|👨🏽‍🔬|🤷‍♂️|🤷🏿‍♂|🤷🏻‍♂|🤷🏾‍♂|🤷🏼‍♂|🤷🏽‍♂|👨🏿‍🎤|👨🏻‍🎤|👨🏾‍🎤|👨🏼‍🎤|👨🏽‍🎤|🧍‍♂️|🧍🏿‍♂|🧍🏻‍♂|🧍🏾‍♂|🧍🏼‍♂|🧍🏽‍♂|👨🏿‍🎓|👨🏻‍🎓|👨🏾‍🎓|👨🏼‍🎓|👨🏽‍🎓|🦸‍♂️|🦸🏿‍♂|🦸🏻‍♂|🦸🏾‍♂|🦸🏼‍♂|🦸🏽‍♂|🦹‍♂️|🦹🏿‍♂|🦹🏻‍♂|🦹🏾‍♂|🦹🏼‍♂|🦹🏽‍♂|🏄‍♂️|🏄🏿‍♂|🏄🏻‍♂|🏄🏾‍♂|🏄🏼‍♂|🏄🏽‍♂|🏊‍♂️|🏊🏿‍♂|🏊🏻‍♂|🏊🏾‍♂|🏊🏼‍♂|🏊🏽‍♂|👨🏿‍🏫|👨🏻‍🏫|👨🏾‍🏫|👨🏼‍🏫|👨🏽‍🏫|👨🏿‍💻|👨🏻‍💻|👨🏾‍💻|👨🏼‍💻|👨🏽‍💻|💁‍♂️|💁🏿‍♂|💁🏻‍♂|💁🏾‍♂|💁🏼‍♂|💁🏽‍♂|🧛‍♂️|🧛🏿‍♂|🧛🏻‍♂|🧛🏾‍♂|🧛🏼‍♂|🧛🏽‍♂|🚶‍♂️|🚶🏿‍♂|🚶🏻‍♂|🚶🏾‍♂|🚶🏼‍♂|🚶🏽‍♂|👳‍♂️|👳🏿‍♂|👳🏻‍♂|👳🏾‍♂|👳🏼‍♂|👳🏽‍♂|👰‍♂️|👰🏿‍♂|👰🏻‍♂|👰🏾‍♂|👰🏼‍♂|👰🏽‍♂|👨🏿‍🦯|👨🏻‍🦯|👨🏾‍🦯|👨🏼‍🦯|👨🏽‍🦯|🧟‍♂️|🧑🏿‍🔧|🧑🏻‍🔧|🧑🏾‍🔧|🧑🏼‍🔧|🧑🏽‍🔧|👯‍♂️|🤼‍♂️|❤️‍🩹|🧜‍♀️|🧜🏿‍♀|🧜🏻‍♀|🧜🏾‍♀|🧜🏼‍♀|🧜🏽‍♀|🧜‍♂️|🧜🏿‍♂|🧜🏻‍♂|🧜🏾‍♂|🧜🏼‍♂|🧜🏽‍♂|🧑🏿‍🎄|🧑🏻‍🎄|🧑🏾‍🎄|🧑🏼‍🎄|🧑🏽‍🎄|🧑🏿‍💼|🧑🏻‍💼|🧑🏾‍💼|🧑🏼‍💼|🧑🏽‍💼|🧑🏿‍🦲|🧑🏿‍🦱|🧑🏿‍🦰|🧑🏿‍🦳|🧑🏿‍🍼|🧑🏻‍🍼|🧑🏾‍🍼|🧑🏼‍🍼|🧑🏽‍🍼|🧑🏿‍🦽|🧑🏻‍🦽|🧑🏾‍🦽|🧑🏼‍🦽|🧑🏽‍🦽|🧑🏿‍🦼|🧑🏻‍🦼|🧑🏾‍🦼|🧑🏼‍🦼|🧑🏽‍🦼|🧑🏻‍🦲|🧑🏻‍🦱|🧑🏻‍🦰|🧑🏻‍🦳|🧑🏾‍🦲|🧑🏾‍🦱|🧑🏾‍🦰|🧑🏾‍🦳|🧑🏼‍🦲|🧑🏼‍🦱|🧑🏼‍🦰|🧑🏼‍🦳|🧑🏽‍🦲|🧑🏽‍🦱|🧑🏽‍🦰|🧑🏽‍🦳|🧑🏿‍🦯|🧑🏻‍🦯|🧑🏾‍🦯|🧑🏼‍🦯|🧑🏽‍🦯|🧑‍✈️|🧑🏿‍✈|🧑🏻‍✈|🧑🏾‍✈|🧑🏼‍✈|🧑🏽‍✈|🏴‍☠️|🐻‍❄️|🏳️‍🌈|🧑🏿‍🔬|🧑🏻‍🔬|🧑🏾‍🔬|🧑🏼‍🔬|🧑🏽‍🔬|🧑🏿‍🎤|🧑🏻‍🎤|🧑🏾‍🎤|🧑🏼‍🎤|🧑🏽‍🎤|🧑🏿‍🎓|🧑🏻‍🎓|🧑🏾‍🎓|🧑🏼‍🎓|🧑🏽‍🎓|🧑🏿‍🏫|🧑🏻‍🏫|🧑🏾‍🏫|🧑🏼‍🏫|🧑🏽‍🏫|🧑🏿‍💻|🧑🏻‍💻|🧑🏾‍💻|🧑🏼‍💻|🧑🏽‍💻|🏳‍⚧️|🏳️‍⚧|👩🏿‍🎨|👩🏻‍🎨|👩🏾‍🎨|👩🏼‍🎨|👩🏽‍🎨|👩🏿‍🚀|👩🏻‍🚀|👩🏾‍🚀|👩🏼‍🚀|👩🏽‍🚀|🧔‍♀️|🚴‍♀️|🚴🏿‍♀|🚴🏻‍♀|🚴🏾‍♀|🚴🏼‍♀|🚴🏽‍♀|👱‍♀️|⛹‍♀️|⛹️‍♀|⛹🏿‍♀|⛹🏻‍♀|⛹🏾‍♀|⛹🏼‍♀|⛹🏽‍♀|🙇‍♀️|🙇🏿‍♀|🙇🏻‍♀|🙇🏾‍♀|🙇🏼‍♀|🙇🏽‍♀|🤸‍♀️|🤸🏿‍♀|🤸🏻‍♀|🤸🏾‍♀|🤸🏼‍♀|🤸🏽‍♀|🧗‍♀️|🧗🏿‍♀|🧗🏻‍♀|🧗🏾‍♀|🧗🏼‍♀|🧗🏽‍♀|👷‍♀️|👷🏿‍♀|👷🏻‍♀|👷🏾‍♀|👷🏼‍♀|👷🏽‍♀|👩🏿‍🍳|👩🏻‍🍳|👩🏾‍🍳|👩🏼‍🍳|👩🏽‍🍳|👩🏿‍🦲|🧔🏿‍♀|👱🏿‍♀|👩🏿‍🦱|👩🏿‍🦰|👩🏿‍🦳|🕵‍♀️|🕵️‍♀|🕵🏿‍♀|🕵🏻‍♀|🕵🏾‍♀|🕵🏼‍♀|🕵🏽‍♀|🧝‍♀️|🧝🏿‍♀|🧝🏻‍♀|🧝🏾‍♀|🧝🏼‍♀|🧝🏽‍♀|🤦‍♀️|🤦🏿‍♀|🤦🏻‍♀|🤦🏾‍♀|🤦🏼‍♀|🤦🏽‍♀|👩🏿‍🏭|👩🏻‍🏭|👩🏾‍🏭|👩🏼‍🏭|👩🏽‍🏭|🧚‍♀️|🧚🏿‍♀|🧚🏻‍♀|🧚🏾‍♀|🧚🏼‍♀|🧚🏽‍♀|👩🏿‍🌾|👩🏻‍🌾|👩🏾‍🌾|👩🏼‍🌾|👩🏽‍🌾|👩🏿‍🍼|👩🏻‍🍼|👩🏾‍🍼|👩🏼‍🍼|👩🏽‍🍼|👩🏿‍🚒|👩🏻‍🚒|👩🏾‍🚒|👩🏼‍🚒|👩🏽‍🚒|🙍‍♀️|🙍🏿‍♀|🙍🏻‍♀|🙍🏾‍♀|🙍🏼‍♀|🙍🏽‍♀|🧞‍♀️|🙅‍♀️|🙅🏿‍♀|🙅🏻‍♀|🙅🏾‍♀|🙅🏼‍♀|🙅🏽‍♀|🙆‍♀️|🙆🏿‍♀|🙆🏻‍♀|🙆🏾‍♀|🙆🏼‍♀|🙆🏽‍♀|💇‍♀️|💇🏿‍♀|💇🏻‍♀|💇🏾‍♀|💇🏼‍♀|💇🏽‍♀|💆‍♀️|💆🏿‍♀|💆🏻‍♀|💆🏾‍♀|💆🏼‍♀|💆🏽‍♀|🏌‍♀️|🏌️‍♀|🏌🏿‍♀|🏌🏻‍♀|🏌🏾‍♀|🏌🏼‍♀|🏌🏽‍♀|💂‍♀️|💂🏿‍♀|💂🏻‍♀|💂🏾‍♀|💂🏼‍♀|💂🏽‍♀|👩‍⚕️|👩🏿‍⚕|👩🏻‍⚕|👩🏾‍⚕|👩🏼‍⚕|👩🏽‍⚕|🧘‍♀️|🧘🏿‍♀|🧘🏻‍♀|🧘🏾‍♀|🧘🏼‍♀|🧘🏽‍♀|👩🏿‍🦽|👩🏻‍🦽|👩🏾‍🦽|👩🏼‍🦽|👩🏽‍🦽|👩🏿‍🦼|👩🏻‍🦼|👩🏾‍🦼|👩🏼‍🦼|👩🏽‍🦼|🧖‍♀️|🧖🏿‍♀|🧖🏻‍♀|🧖🏾‍♀|🧖🏼‍♀|🧖🏽‍♀|🤵‍♀️|🤵🏿‍♀|🤵🏻‍♀|🤵🏾‍♀|🤵🏼‍♀|🤵🏽‍♀|👩‍⚖️|👩🏿‍⚖|👩🏻‍⚖|👩🏾‍⚖|👩🏼‍⚖|👩🏽‍⚖|🤹‍♀️|🤹🏿‍♀|🤹🏻‍♀|🤹🏾‍♀|🤹🏼‍♀|🤹🏽‍♀|🧎‍♀️|🧎🏿‍♀|🧎🏻‍♀|🧎🏾‍♀|🧎🏼‍♀|🧎🏽‍♀|🏋‍♀️|🏋️‍♀|🏋🏿‍♀|🏋🏻‍♀|🏋🏾‍♀|🏋🏼‍♀|🏋🏽‍♀|👩🏻‍🦲|🧔🏻‍♀|👱🏻‍♀|👩🏻‍🦱|👩🏻‍🦰|👩🏻‍🦳|🧙‍♀️|🧙🏿‍♀|🧙🏻‍♀|🧙🏾‍♀|🧙🏼‍♀|🧙🏽‍♀|👩🏿‍🔧|👩🏻‍🔧|👩🏾‍🔧|👩🏼‍🔧|👩🏽‍🔧|👩🏾‍🦲|🧔🏾‍♀|👱🏾‍♀|👩🏾‍🦱|👩🏾‍🦰|👩🏾‍🦳|👩🏼‍🦲|🧔🏼‍♀|👱🏼‍♀|👩🏼‍🦱|👩🏼‍🦰|👩🏼‍🦳|👩🏽‍🦲|🧔🏽‍♀|👱🏽‍♀|👩🏽‍🦱|👩🏽‍🦰|👩🏽‍🦳|🚵‍♀️|🚵🏿‍♀|🚵🏻‍♀|🚵🏾‍♀|🚵🏼‍♀|🚵🏽‍♀|👩🏿‍💼|👩🏻‍💼|👩🏾‍💼|👩🏼‍💼|👩🏽‍💼|👩‍✈️|👩🏿‍✈|👩🏻‍✈|👩🏾‍✈|👩🏼‍✈|👩🏽‍✈|🤾‍♀️|🤾🏿‍♀|🤾🏻‍♀|🤾🏾‍♀|🤾🏼‍♀|🤾🏽‍♀|🤽‍♀️|🤽🏿‍♀|🤽🏻‍♀|🤽🏾‍♀|🤽🏼‍♀|🤽🏽‍♀|👮‍♀️|👮🏿‍♀|👮🏻‍♀|👮🏾‍♀|👮🏼‍♀|👮🏽‍♀|🙎‍♀️|🙎🏿‍♀|🙎🏻‍♀|🙎🏾‍♀|🙎🏼‍♀|🙎🏽‍♀|🙋‍♀️|🙋🏿‍♀|🙋🏻‍♀|🙋🏾‍♀|🙋🏼‍♀|🙋🏽‍♀|🚣‍♀️|🚣🏿‍♀|🚣🏻‍♀|🚣🏾‍♀|🚣🏼‍♀|🚣🏽‍♀|🏃‍♀️|🏃🏿‍♀|🏃🏻‍♀|🏃🏾‍♀|🏃🏼‍♀|🏃🏽‍♀|👩🏿‍🔬|👩🏻‍🔬|👩🏾‍🔬|👩🏼‍🔬|👩🏽‍🔬|🤷‍♀️|🤷🏿‍♀|🤷🏻‍♀|🤷🏾‍♀|🤷🏼‍♀|🤷🏽‍♀|👩🏿‍🎤|👩🏻‍🎤|👩🏾‍🎤|👩🏼‍🎤|👩🏽‍🎤|🧍‍♀️|🧍🏿‍♀|🧍🏻‍♀|🧍🏾‍♀|🧍🏼‍♀|🧍🏽‍♀|👩🏿‍🎓|👩🏻‍🎓|👩🏾‍🎓|👩🏼‍🎓|👩🏽‍🎓|🦸‍♀️|🦸🏿‍♀|🦸🏻‍♀|🦸🏾‍♀|🦸🏼‍♀|🦸🏽‍♀|🦹‍♀️|🦹🏿‍♀|🦹🏻‍♀|🦹🏾‍♀|🦹🏼‍♀|🦹🏽‍♀|🏄‍♀️|🏄🏿‍♀|🏄🏻‍♀|🏄🏾‍♀|🏄🏼‍♀|🏄🏽‍♀|🏊‍♀️|🏊🏿‍♀|🏊🏻‍♀|🏊🏾‍♀|🏊🏼‍♀|🏊🏽‍♀|👩🏿‍🏫|👩🏻‍🏫|👩🏾‍🏫|👩🏼‍🏫|👩🏽‍🏫|👩🏿‍💻|👩🏻‍💻|👩🏾‍💻|👩🏼‍💻|👩🏽‍💻|💁‍♀️|💁🏿‍♀|💁🏻‍♀|💁🏾‍♀|💁🏼‍♀|💁🏽‍♀|🧛‍♀️|🧛🏿‍♀|🧛🏻‍♀|🧛🏾‍♀|🧛🏼‍♀|🧛🏽‍♀|🚶‍♀️|🚶🏿‍♀|🚶🏻‍♀|🚶🏾‍♀|🚶🏼‍♀|🚶🏽‍♀|👳‍♀️|👳🏿‍♀|👳🏻‍♀|👳🏾‍♀|👳🏼‍♀|👳🏽‍♀|👰‍♀️|👰🏿‍♀|👰🏻‍♀|👰🏾‍♀|👰🏼‍♀|👰🏽‍♀|👩🏿‍🦯|👩🏻‍🦯|👩🏾‍🦯|👩🏼‍🦯|👩🏽‍🦯|🧟‍♀️|👯‍♀️|🤼‍♀️|🧑‍🎨|🧑‍🚀|🐈‍⬛|🧑‍🍳|🧏‍♂|🧏‍♀|👁‍🗨|😮‍💨|😶‍🌫|😵‍💫|🧑‍🏭|👨‍👦|👨‍👧|👩‍👦|👩‍👧|🧑‍🌾|🧑‍🚒|🧑‍⚕|❤‍🔥|🧑‍⚖|\#️⃣|\*️⃣|0️⃣|1️⃣|2️⃣|3️⃣|4️⃣|5️⃣|6️⃣|7️⃣|8️⃣|9️⃣|👨‍🎨|👨‍🚀|👨‍🦲|🧔‍♂|🚴‍♂|👱‍♂|⛹‍♂|🙇‍♂|🤸‍♂|🧗‍♂|👷‍♂|👨‍🍳|👨‍🦱|🕵‍♂|🧝‍♂|🤦‍♂|👨‍🏭|🧚‍♂|👨‍🌾|👨‍🍼|👨‍🚒|🙍‍♂|🧞‍♂|🙅‍♂|🙆‍♂|💇‍♂|💆‍♂|🏌‍♂|💂‍♂|👨‍⚕|🧘‍♂|👨‍🦽|👨‍🦼|🧖‍♂|🤵‍♂|👨‍⚖|🤹‍♂|🧎‍♂|🏋‍♂|🧙‍♂|👨‍🔧|🚵‍♂|👨‍💼|👨‍✈|🤾‍♂|🤽‍♂|👮‍♂|🙎‍♂|🙋‍♂|👨‍🦰|🚣‍♂|🏃‍♂|👨‍🔬|🤷‍♂|👨‍🎤|🧍‍♂|👨‍🎓|🦸‍♂|🦹‍♂|🏄‍♂|🏊‍♂|👨‍🏫|👨‍💻|💁‍♂|🧛‍♂|🚶‍♂|👳‍♂|👨‍🦳|👰‍♂|👨‍🦯|🧟‍♂|🧑‍🔧|👯‍♂|🤼‍♂|❤‍🩹|🧜‍♀|🧜‍♂|🧑‍🎄|🧑‍💼|🧑‍🦲|🧑‍🦱|🧑‍🍼|🧑‍🦽|🧑‍🦼|🧑‍🦰|🧑‍🦳|🧑‍🦯|🧑‍✈|🏴‍☠|🐻‍❄|🏳‍🌈|🧑‍🔬|🐕‍🦺|🧑‍🎤|🧑‍🎓|🧑‍🏫|🧑‍💻|🏳‍⚧|👩‍🎨|👩‍🚀|👩‍🦲|🧔‍♀|🚴‍♀|👱‍♀|⛹‍♀|🙇‍♀|🤸‍♀|🧗‍♀|👷‍♀|👩‍🍳|👩‍🦱|🕵‍♀|🧝‍♀|🤦‍♀|👩‍🏭|🧚‍♀|👩‍🌾|👩‍🍼|👩‍🚒|🙍‍♀|🧞‍♀|🙅‍♀|🙆‍♀|💇‍♀|💆‍♀|🏌‍♀|💂‍♀|👩‍⚕|🧘‍♀|👩‍🦽|👩‍🦼|🧖‍♀|🤵‍♀|👩‍⚖|🤹‍♀|🧎‍♀|🏋‍♀|🧙‍♀|👩‍🔧|🚵‍♀|👩‍💼|👩‍✈|🤾‍♀|🤽‍♀|👮‍♀|🙎‍♀|🙋‍♀|👩‍🦰|🚣‍♀|🏃‍♀|👩‍🔬|🤷‍♀|👩‍🎤|🧍‍♀|👩‍🎓|🦸‍♀|🦹‍♀|🏄‍♀|🏊‍♀|👩‍🏫|👩‍💻|💁‍♀|🧛‍♀|🚶‍♀|👳‍♀|👩‍🦳|👰‍♀|👩‍🦯|🧟‍♀|👯‍♀|🤼‍♀|🅰️|🇦🇫|🇦🇱|🇩🇿|🇦🇸|🇦🇩|🇦🇴|🇦🇮|🇦🇶|🇦🇬|🇦🇷|🇦🇲|🇦🇼|🇦🇨|🇦🇺|🇦🇹|🇦🇿|🅱️|🇧🇸|🇧🇭|🇧🇩|🇧🇧|🇧🇾|🇧🇪|🇧🇿|🇧🇯|🇧🇲|🇧🇹|🇧🇴|🇧🇦|🇧🇼|🇧🇻|🇧🇷|🇮🇴|🇻🇬|🇧🇳|🇧🇬|🇧🇫|🇧🇮|🇰🇭|🇨🇲|🇨🇦|🇮🇨|🇨🇻|🇧🇶|🇰🇾|🇨🇫|🇪🇦|🇹🇩|🇨🇱|🇨🇳|🇨🇽|🇨🇵|🇨🇨|🇨🇴|🇰🇲|🇨🇬|🇨🇩|🇨🇰|🇨🇷|🇭🇷|🇨🇺|🇨🇼|🇨🇾|🇨🇿|🇨🇮|🇩🇰|🇩🇬|🇩🇯|🇩🇲|🇩🇴|🇪🇨|🇪🇬|🇸🇻|🇬🇶|🇪🇷|🇪🇪|🇸🇿|🇪🇹|🇪🇺|🇫🇰|🇫🇴|🇫🇯|🇫🇮|🇫🇷|🇬🇫|🇵🇫|🇹🇫|🇬🇦|🇬🇲|🇬🇪|🇩🇪|🇬🇭|🇬🇮|🇬🇷|🇬🇱|🇬🇩|🇬🇵|🇬🇺|🇬🇹|🇬🇬|🇬🇳|🇬🇼|🇬🇾|🇭🇹|🇭🇲|🇭🇳|🇭🇰|🇭🇺|🇮🇸|🇮🇳|🇮🇩|🇮🇷|🇮🇶|🇮🇪|🇮🇲|🇮🇱|🇮🇹|🇯🇲|🇯🇵|㊗️|🈷️|㊙️|🈂️|🇯🇪|🇯🇴|🇰🇿|🇰🇪|🇰🇮|🇽🇰|🇰🇼|🇰🇬|🇱🇦|🇱🇻|🇱🇧|🇱🇸|🇱🇷|🇱🇾|🇱🇮|🇱🇹|🇱🇺|🇲🇴|🇲🇬|🇲🇼|🇲🇾|🇲🇻|🇲🇱|🇲🇹|🇲🇭|🇲🇶|🇲🇷|🇲🇺|🇾🇹|🇲🇽|🇫🇲|🇲🇩|🇲🇨|🇲🇳|🇲🇪|🇲🇸|🇲🇦|🇲🇿|🤶🏿|🤶🏻|🤶🏾|🤶🏼|🤶🏽|🇲🇲|🇳🇦|🇳🇷|🇳🇵|🇳🇱|🇳🇨|🇳🇿|🇳🇮|🇳🇪|🇳🇬|🇳🇺|🇳🇫|🇰🇵|🇲🇰|🇲🇵|🇳🇴|👌🏿|👌🏻|👌🏾|👌🏼|👌🏽|🅾️|🇴🇲|🅿️|🇵🇰|🇵🇼|🇵🇸|🇵🇦|🇵🇬|🇵🇾|🇵🇪|🇵🇭|🇵🇳|🇵🇱|🇵🇹|🇵🇷|🇶🇦|🇷🇴|🇷🇺|🇷🇼|🇷🇪|🇼🇸|🇸🇲|🎅🏿|🎅🏻|🎅🏾|🎅🏼|🎅🏽|🇸🇦|🇸🇳|🇷🇸|🇸🇨|🇸🇱|🇸🇬|🇸🇽|🇸🇰|🇸🇮|🇸🇧|🇸🇴|🇿🇦|🇬🇸|🇰🇷|🇸🇸|🇪🇸|🇱🇰|🇧🇱|🇸🇭|🇰🇳|🇱🇨|🇲🇫|🇵🇲|🇻🇨|🇸🇩|🇸🇷|🇸🇯|🇸🇪|🇨🇭|🇸🇾|🇸🇹|🇹🇼|🇹🇯|🇹🇿|🇹🇭|🇹🇱|🇹🇬|🇹🇰|🇹🇴|🇹🇹|🇹🇦|🇹🇳|🇹🇷|🇹🇲|🇹🇨|🇹🇻|🇺🇲|🇻🇮|🇺🇬|🇺🇦|🇦🇪|🇬🇧|🇺🇳|🇺🇸|🇺🇾|🇺🇿|🇻🇺|🇻🇦|🇻🇪|🇻🇳|🇼🇫|🇪🇭|🇾🇪|🇿🇲|🇿🇼|🎟️|✈️|⚗️|⚛️|👼🏿|👼🏻|👼🏾|👼🏼|👼🏽|👶🏿|👶🏻|👶🏾|👶🏼|👶🏽|👇🏿|👇🏻|👇🏾|👇🏼|👇🏽|👈🏿|👈🏻|👈🏾|👈🏼|👈🏽|👉🏿|👉🏻|👉🏾|👉🏼|👉🏽|👆🏿|👆🏻|👆🏾|👆🏼|👆🏽|⚖️|🗳️|🏖️|🛏️|🛎️|☣️|◼️|✒️|▪️|👦🏿|👦🏻|👦🏾|👦🏼|👦🏽|🤱🏿|🤱🏻|🤱🏾|🤱🏼|🤱🏽|🏗️|🤙🏿|🤙🏻|🤙🏾|🤙🏼|🤙🏽|🏕️|🕯️|🗃️|🗂️|⛓️|☑️|✔️|♟️|🧒🏿|🧒🏻|🧒🏾|🧒🏼|🧒🏽|🐿️|Ⓜ️|🏙️|🗜️|👏🏿|👏🏻|👏🏾|👏🏼|👏🏽|🏛️|☁️|🌩️|⛈️|🌧️|🌨️|♣️|⚰️|☄️|🖱️|👷🏿|👷🏻|👷🏾|👷🏼|👷🏽|🎛️|©️|🛋️|💑🏿|💑🏻|💑🏾|💑🏼|💑🏽|🖍️|🤞🏿|🤞🏻|🤞🏾|🤞🏼|🤞🏽|⚔️|🗡️|🧏🏿|🧏🏻|🧏🏾|🧏🏼|🧏🏽|🏚️|🏜️|🏝️|🖥️|🕵️|🕵🏿|🕵🏻|🕵🏾|🕵🏼|🕵🏽|♦️|‼️|🕊️|↙️|↘️|⬇️|👂🏿|👂🏻|👂🏾|👂🏼|👂🏽|🦻🏿|🦻🏻|🦻🏾|🦻🏼|🦻🏽|✴️|✳️|⏏️|🧝🏿|🧝🏻|🧝🏾|🧝🏼|🧝🏽|✉️|⁉️|👁️|🧚🏿|🧚🏻|🧚🏾|🧚🏼|🧚🏽|♀️|⛴️|🗄️|🎞️|📽️|⚜️|💪🏿|💪🏻|💪🏾|💪🏼|💪🏽|🌫️|🙏🏿|🙏🏻|🙏🏾|🙏🏼|🙏🏽|🦶🏿|🦶🏻|🦶🏾|🦶🏼|🦶🏽|🍽️|🖋️|🖼️|☹️|⚱️|⚙️|👧🏿|👧🏻|👧🏾|👧🏼|👧🏽|💂🏿|💂🏻|💂🏾|💂🏼|💂🏽|⚒️|🛠️|🖐️|🖐🏿|🖐🏻|🖐🏾|🖐🏼|🖐🏽|🫰🏿|🫰🏻|🫰🏾|🫰🏼|🫰🏽|🤝🏿|🤝🏻|🤝🏾|🤝🏼|🤝🏽|❣️|🫶🏿|🫶🏻|🫶🏾|🫶🏼|🫶🏽|♥️|🕳️|🏇🏿|🏇🏻|🏇🏾|🏇🏼|🏇🏽|🌶️|♨️|🏘️|⛸️|🫵🏿|🫵🏻|🫵🏾|🫵🏼|🫵🏽|☝️|☝🏿|☝🏻|☝🏾|☝🏼|☝🏽|♾️|ℹ️|🕹️|⌨️|\#⃣|\*⃣|0⃣|1⃣|2⃣|3⃣|4⃣|5⃣|6⃣|7⃣|8⃣|9⃣|💏🏿|💏🏻|💏🏾|💏🏼|💏🏽|🏷️|⏮️|✝️|🤛🏿|🤛🏻|🤛🏾|🤛🏼|🤛🏽|↔️|⬅️|↪️|🗨️|🫲🏿|🫲🏻|🫲🏾|🫲🏼|🫲🏽|🦵🏿|🦵🏻|🦵🏾|🦵🏼|🦵🏽|🎚️|🖇️|🤟🏿|🤟🏻|🤟🏾|🤟🏼|🤟🏽|🧙🏿|🧙🏻|🧙🏾|🧙🏼|🧙🏽|♂️|🕺🏿|🕺🏻|🕺🏾|🕺🏼|🕺🏽|👨🏿|👨🏻|👨🏾|👨🏼|👨🏽|🕰️|⚕️|👬🏿|👬🏻|👬🏾|👬🏼|👬🏽|🧜🏿|🧜🏻|🧜🏾|🧜🏼|🧜🏽|🖕🏿|🖕🏻|🖕🏾|🖕🏼|🖕🏽|🎖️|🛥️|🏍️|🛣️|⛰️|✖️|💅🏿|💅🏻|💅🏾|💅🏼|💅🏽|🏞️|⏭️|🥷🏿|🥷🏻|🥷🏾|🥷🏼|🥷🏽|👃🏿|👃🏻|👃🏾|👃🏼|👃🏽|🛢️|🗝️|👴🏿|👴🏻|👴🏾|👴🏼|👴🏽|👵🏿|👵🏻|👵🏾|👵🏼|👵🏽|🧓🏿|🧓🏻|🧓🏾|🧓🏼|🧓🏽|🕉️|👊🏿|👊🏻|👊🏾|👊🏼|👊🏽|👐🏿|👐🏻|👐🏾|👐🏼|👐🏽|☦️|🖌️|🫳🏿|🫳🏻|🫳🏾|🫳🏼|🫳🏽|🫴🏿|🫴🏻|🫴🏾|🫴🏼|🫴🏽|🤲🏿|🤲🏻|🤲🏾|🤲🏼|🤲🏽|〽️|🛳️|⏸️|☮️|🖊️|✏️|🚴🏿|🚴🏻|🚴🏾|🚴🏼|🚴🏽|⛹️|⛹🏿|⛹🏻|⛹🏾|⛹🏼|⛹🏽|🙇🏿|🙇🏻|🙇🏾|🙇🏼|🙇🏽|🤸🏿|🤸🏻|🤸🏾|🤸🏼|🤸🏽|🧗🏿|🧗🏻|🧗🏾|🧗🏼|🧗🏽|🧑🏿|🧔🏿|👱🏿|🤦🏿|🤦🏻|🤦🏾|🤦🏼|🤦🏽|🙍🏿|🙍🏻|🙍🏾|🙍🏼|🙍🏽|🙅🏿|🙅🏻|🙅🏾|🙅🏼|🙅🏽|🙆🏿|🙆🏻|🙆🏾|🙆🏼|🙆🏽|💇🏿|💇🏻|💇🏾|💇🏼|💇🏽|💆🏿|💆🏻|💆🏾|💆🏼|💆🏽|🏌️|🏌🏿|🏌🏻|🏌🏾|🏌🏼|🏌🏽|🛌🏿|🛌🏻|🛌🏾|🛌🏼|🛌🏽|🧘🏿|🧘🏻|🧘🏾|🧘🏼|🧘🏽|🧖🏿|🧖🏻|🧖🏾|🧖🏼|🧖🏽|🕴️|🕴🏿|🕴🏻|🕴🏾|🕴🏼|🕴🏽|🤵🏿|🤵🏻|🤵🏾|🤵🏼|🤵🏽|🤹🏿|🤹🏻|🤹🏾|🤹🏼|🤹🏽|🧎🏿|🧎🏻|🧎🏾|🧎🏼|🧎🏽|🏋️|🏋🏿|🏋🏻|🏋🏾|🏋🏼|🏋🏽|🧑🏻|🧔🏻|👱🏻|🧑🏾|🧔🏾|👱🏾|🧑🏼|🧔🏼|👱🏼|🧑🏽|🧔🏽|👱🏽|🚵🏿|🚵🏻|🚵🏾|🚵🏼|🚵🏽|🤾🏿|🤾🏻|🤾🏾|🤾🏼|🤾🏽|🤽🏿|🤽🏻|🤽🏾|🤽🏼|🤽🏽|🙎🏿|🙎🏻|🙎🏾|🙎🏼|🙎🏽|🙋🏿|🙋🏻|🙋🏾|🙋🏼|🙋🏽|🚣🏿|🚣🏻|🚣🏾|🚣🏼|🚣🏽|🏃🏿|🏃🏻|🏃🏾|🏃🏼|🏃🏽|🤷🏿|🤷🏻|🤷🏾|🤷🏼|🤷🏽|🧍🏿|🧍🏻|🧍🏾|🧍🏼|🧍🏽|🏄🏿|🏄🏻|🏄🏾|🏄🏼|🏄🏽|🏊🏿|🏊🏻|🏊🏾|🏊🏼|🏊🏽|🛀🏿|🛀🏻|🛀🏾|🛀🏼|🛀🏽|💁🏿|💁🏻|💁🏾|💁🏼|💁🏽|🚶🏿|🚶🏻|🚶🏾|🚶🏼|🚶🏽|👳🏿|👳🏻|👳🏾|👳🏼|👳🏽|🫅🏿|🫅🏻|🫅🏾|🫅🏼|🫅🏽|👲🏿|👲🏻|👲🏾|👲🏼|👲🏽|👰🏿|👰🏻|👰🏾|👰🏼|👰🏽|⛏️|🤌🏿|🤌🏻|🤌🏾|🤌🏼|🤌🏽|🤏🏿|🤏🏻|🤏🏾|🤏🏼|🤏🏽|▶️|⏯️|👮🏿|👮🏻|👮🏾|👮🏼|👮🏽|🫃🏿|🫃🏻|🫃🏾|🫃🏼|🫃🏽|🫄🏿|🫄🏻|🫄🏾|🫄🏼|🫄🏽|🤰🏿|🤰🏻|🤰🏾|🤰🏼|🤰🏽|🤴🏿|🤴🏻|🤴🏾|🤴🏼|🤴🏽|👸🏿|👸🏻|👸🏾|👸🏼|👸🏽|🖨️|🏎️|☢️|🛤️|🤚🏿|🤚🏻|🤚🏾|🤚🏼|🤚🏽|✊🏿|✊🏻|✊🏾|✊🏼|✊🏽|✋🏿|✋🏻|✋🏾|✋🏼|✋🏽|🙌🏿|🙌🏻|🙌🏾|🙌🏼|🙌🏽|⏺️|♻️|❤️|®️|🎗️|⛑️|◀️|🤜🏿|🤜🏻|🤜🏾|🤜🏼|🤜🏽|🗯️|➡️|⤵️|↩️|⤴️|🫱🏿|🫱🏻|🫱🏾|🫱🏼|🫱🏽|🗞️|🏵️|🛰️|✂️|🤳🏿|🤳🏻|🤳🏾|🤳🏼|🤳🏽|☘️|🛡️|⛩️|🛍️|🤘🏿|🤘🏻|🤘🏾|🤘🏼|🤘🏽|⛷️|☠️|🛩️|☺️|🏔️|🏂🏿|🏂🏻|🏂🏾|🏂🏼|🏂🏽|❄️|☃️|♠️|❇️|🗣️|🕷️|🕸️|🗓️|🗒️|🏟️|☪️|✡️|⏹️|⏱️|🎙️|☀️|🌥️|🌦️|🌤️|🕶️|🦸🏿|🦸🏻|🦸🏾|🦸🏼|🦸🏽|🦹🏿|🦹🏻|🦹🏾|🦹🏼|🦹🏽|☎️|🌡️|👎🏿|👎🏻|👎🏾|👎🏼|👎🏽|👍🏿|👍🏻|👍🏾|👍🏼|👍🏽|⏲️|🌪️|🖲️|™️|⚧️|☂️|⛱️|↕️|↖️|↗️|⬆️|🧛🏿|🧛🏻|🧛🏾|🧛🏼|🧛🏽|✌️|✌🏿|✌🏻|✌🏾|✌🏼|✌🏽|🖖🏿|🖖🏻|🖖🏾|🖖🏼|🖖🏽|⚠️|🗑️|👋🏿|👋🏻|👋🏾|👋🏼|👋🏽|〰️|☸️|🏳️|◻️|▫️|🌬️|👫🏿|👫🏻|👫🏾|👫🏼|👫🏽|💃🏿|💃🏻|💃🏾|💃🏼|💃🏽|👩🏿|👩🏻|👩🏾|👩🏼|👩🏽|🧕🏿|🧕🏻|🧕🏾|🧕🏼|🧕🏽|👭🏿|👭🏻|👭🏾|👭🏼|👭🏽|🗺️|✍️|✍🏿|✍🏻|✍🏾|✍🏼|✍🏽|☯️|🇦🇽|🥇|🥈|🥉|🆎|🏧|🅰|♒|♈|🔙|🅱|🆑|🆒|♋|♑|🎄|🔚|🆓|♊|🆔|🉑|🈸|🉐|🏯|㊗|🈹|🎎|🈚|🈁|🈷|🈵|🈶|🈺|🈴|🏣|🈲|🈯|㊙|🈂|🔰|🈳|♌|♎|🤶|🆕|🆖|🆗|👌|🔛|🅾|⛎|🅿|♓|🔜|🆘|♐|🎅|♏|🗽|🦖|🔝|♉|🗼|🆙|🆚|♍|🧮|🪗|🩹|🎟|🚡|✈|🛬|🛫|⏰|⚗|👽|👾|🚑|🏈|🏺|🫀|⚓|💢|😠|👿|😧|🐜|📶|😰|🚛|🎨|😲|⚛|🛺|🚗|🥑|🪓|👶|👼|🍼|🐤|🚼|👇|👈|👉|👆|🎒|🥓|🦡|🏸|🥯|🛄|🥖|⚖|🦲|🩰|🎈|🗳|🍌|🪕|🏦|📊|💈|⚾|🧺|🏀|🦇|🛁|🔋|🏖|😁|🫘|🐻|💓|🦫|🛏|🍺|🪲|🔔|🫑|🔕|🛎|🍱|🧃|🚲|👙|🧢|☣|🐦|🎂|🦬|🫦|⚫|🏴|🖤|⬛|◾|◼|✒|▪|🔲|🌼|🐡|📘|🔵|💙|🟦|🫐|🐗|💣|🦴|🔖|📑|📚|🪃|🍾|💐|🏹|🥣|🎳|🥊|👦|🧠|🍞|🤱|🧱|🌉|💼|🩲|🔆|🥦|💔|🧹|🟤|🤎|🟫|🧋|🫧|🪣|🐛|🏗|🚅|🎯|🌯|🚌|🚏|👤|👥|🧈|🦋|🌵|📅|🤙|🐪|📷|📸|🏕|🕯|🍬|🥫|🛶|🗃|📇|🗂|🎠|🎏|🪚|🥕|🏰|🐈|🐱|😹|😼|⛓|🪑|📉|📈|💹|☑|✔|✅|🧀|🏁|🍒|🌸|♟|🌰|🐔|🧒|🚸|🐿|🍫|🥢|⛪|🚬|🎦|Ⓜ|🎪|🏙|🌆|🗜|🎬|👏|🏛|🍻|🥂|📋|🔃|📕|📪|📫|🌂|☁|🌩|⛈|🌧|🌨|🤡|♣|👝|🧥|🪳|🍸|🥥|⚰|🪙|🥶|💥|☄|🧭|💽|🖱|🎊|😖|😕|🚧|👷|🎛|🏪|🍚|🍪|🍳|©|🪸|🛋|🔄|💑|🐄|🐮|🤠|🦀|🖍|💳|🌙|🦗|🏏|🐊|🥐|❌|❎|🤞|🎌|⚔|👑|🩼|😿|😢|🔮|🥒|🥤|🧁|🥌|🦱|➰|💱|🍛|🍮|🛃|🥩|🌀|🗡|🍡|🏿|💨|🧏|🌳|🦌|🚚|🏬|🏚|🏜|🏝|🖥|🕵|♦|💠|🔅|😞|🥸|➗|🤿|🪔|💫|🧬|🦤|🐕|🐶|💵|🐬|🚪|🫥|🔯|➿|‼|🍩|🕊|↙|↘|⬇|😓|🔽|🐉|🐲|👗|🤤|🩸|💧|🥁|🦆|🥟|📀|📧|🦅|👂|🌽|🦻|🥚|🍆|✴|✳|🕣|🕗|⏏|🔌|🐘|🛗|🕦|🕚|🧝|🪹|✉|📩|💶|🌲|🐑|⁉|🤯|😑|👁|👀|😘|🥹|😋|😱|🤮|😵|🫤|🤭|🤕|😷|🧐|🫢|😮|🫣|🤨|🙄|😤|🤬|😂|🤒|😛|😶|🏭|🧚|🧆|🍂|👪|⏩|⏬|⏪|⏫|📠|😨|🪶|♀|🎡|⛴|🏑|🗄|📁|🎞|📽|🔥|🚒|🧯|🧨|🎆|🌓|🌛|🐟|🍥|🎣|🕠|🕔|⛳|🦩|🔦|🥿|🫓|⚜|💪|💾|🎴|😳|🪰|🥏|🛸|🌫|🌁|🙏|🫕|🦶|👣|🍴|🍽|🥠|⛲|🖋|🕟|🍀|🕓|🦊|🖼|🍟|🍤|🐸|🐥|☹|😦|⛽|🌕|🌝|⚱|🎲|🧄|⚙|💎|🧞|👻|🦒|👧|🥛|👓|🌎|🌏|🌍|🌐|🧤|🌟|🥅|🐐|👺|🥽|🦍|🎓|🍇|🍏|📗|🟢|💚|🥗|🟩|😬|😺|😸|😀|😃|😄|😅|😆|💗|💂|🦮|🎸|🍔|🔨|⚒|🛠|🪬|🐹|🖐|🫰|👜|🤝|🐣|🎧|🪦|🙉|💟|❣|🫶|♥|💘|💝|💲|🟰|🦔|🚁|🌿|🌺|👠|🚄|⚡|🥾|🛕|🦛|🕳|⭕|🍯|🐝|🪝|🚥|🐎|🐴|🏇|🏥|☕|🌭|🥵|🌶|♨|🏨|⌛|⏳|🏠|🏡|🏘|💯|😯|🛖|🧊|🍨|🏒|⛸|🪪|📥|📨|🫵|☝|♾|ℹ|🔤|🔡|🔠|🔢|🔣|🎃|🫙|👖|🃏|🕹|🕋|🦘|🔑|⌨|🔟|🛴|👘|💏|💋|😽|😗|😚|😙|🔪|🪁|🥝|🪢|🐨|🥼|🏷|🥍|🪜|🐞|💻|🔷|🔶|🌗|🌜|⏮|✝|🍃|🥬|📒|🤛|↔|⬅|↪|🛅|🗨|🫲|🦵|🍋|🐆|🎚|💡|🚈|🏻|🔗|🖇|🦁|💄|🚮|🦎|🦙|🦞|🔒|🔐|🔏|🚂|🍭|🪘|🧴|🪷|😭|📢|🤟|🏩|💌|🪫|🧳|🫁|🤥|🧙|🪄|🧲|🔍|🔎|🀄|♂|🦣|👨|🕺|🥭|🕰|🦽|👞|🗾|🍁|🥋|🧉|🍖|🦾|🦿|⚕|🏾|🏼|🏽|📣|🍈|🫠|📝|👬|🕎|🚹|🧜|🚇|🦠|🎤|🔬|🖕|🪖|🎖|🌌|🚐|➖|🪞|🪩|🗿|📱|📴|📲|🤑|💰|💸|🐒|🐵|🚝|🥮|🎑|🕌|🦟|🛥|🛵|🏍|🦼|🛣|🗻|⛰|🚠|🚞|🐁|🐭|🪤|👄|🎥|✖|🍄|🎹|🎵|🎶|🎼|🔇|💅|📛|🏞|🤢|🧿|👔|🤓|🪺|🪆|😐|🌑|🌚|📰|⏭|🌃|🕤|🕘|🥷|🚳|⛔|🚯|📵|🔞|🚷|🚭|🚱|👃|📓|📔|🔩|🐙|🍢|🏢|👹|🛢|🗝|👴|👵|🧓|🫒|🕉|🚘|🚍|👊|🚔|🚖|🩱|🕜|🕐|🧅|📖|📂|👐|📭|📬|💿|📙|🟠|🧡|🟧|🦧|☦|🦦|📤|🦉|🐂|🦪|📦|📄|📃|📟|🖌|🫳|🌴|🫴|🤲|🥞|🐼|📎|🪂|🦜|〽|🎉|🥳|🛳|🛂|⏸|🐾|☮|🍑|🦚|🥜|🍐|🖊|✏|🐧|😔|🫂|👯|🤼|🎭|😣|🧑|🧔|🚴|👱|⛹|🙇|🤸|🧗|🤦|🤺|🙍|🙅|🙆|💇|💆|🏌|🛌|🧘|🧖|🕴|🤵|🤹|🧎|🏋|🚵|🤾|🤽|🙎|🙋|🚣|🏃|🤷|🧍|🏄|🏊|🛀|💁|🚶|👳|🫅|👲|👰|🧫|⛏|🛻|🥧|🐖|🐷|🐽|💩|💊|🤌|🤏|🎍|🍍|🏓|🍕|🪅|🪧|🛐|▶|⏯|🛝|🥺|🪠|➕|🚓|🚨|👮|🐩|🎱|🍿|🏤|📯|📮|🍲|🚰|🥔|🪴|🍗|💷|🫗|😾|😡|📿|🫃|🫄|🤰|🥨|🤴|👸|🖨|🚫|🟣|💜|🟪|👛|📌|🧩|🐇|🐰|🦝|🏎|📻|🔘|☢|🚃|🛤|🌈|🤚|✊|✋|🙌|🐏|🐀|🪒|🧾|⏺|♻|🍎|🔴|🧧|❗|🦰|❤|🏮|❓|🟥|🔻|🔺|®|😌|🎗|🔁|🔂|⛑|🚻|◀|💞|🦏|🎀|🍙|🍘|🤜|🗯|➡|⤵|↩|⤴|🫱|💍|🛟|🪐|🍠|🤖|🪨|🚀|🧻|🗞|🎢|🛼|🤣|🐓|🌹|🏵|📍|🏉|🎽|👟|😥|🧷|🦺|⛵|🍶|🧂|🫡|🥪|🥻|🛰|📡|🦕|🎷|🧣|🏫|✂|🦂|🪛|📜|🦭|💺|🙈|🌱|🤳|🕢|🕖|🪡|🥘|☘|🦈|🍧|🌾|🛡|⛩|🚢|🌠|🛍|🛒|🍰|🩳|🚿|🦐|🔀|🤫|🤘|🕡|🕕|🛹|⛷|🎿|💀|☠|🦨|🛷|😴|😪|🙁|🙂|🎰|🦥|🛩|🔹|🔸|😻|☺|😇|😍|🥰|😈|🤗|😊|😎|🥲|😏|🐌|🐍|🤧|🏔|🏂|❄|☃|⛄|🧼|⚽|🧦|🍦|🥎|♠|🍝|❇|🎇|✨|💖|🙊|🔊|🔈|🔉|🗣|💬|🚤|🕷|🕸|🗓|🗒|🐚|🧽|🥄|🚙|🏅|🐳|🦑|😝|🏟|⭐|🤩|☪|✡|🚉|🍜|🩺|⏹|🛑|⏱|📏|🍓|🎙|🥙|☀|⛅|🌥|🌦|🌤|🌞|🌻|🕶|🌅|🌄|🌇|🦸|🦹|🍣|🚟|🦢|💦|🕍|💉|👕|🌮|🥡|🫔|🎋|🍊|🚕|🍵|🫖|📆|🧸|☎|📞|🔭|📺|🕥|🕙|🎾|⛺|🧪|🌡|🤔|🩴|💭|🧵|🕞|🕒|👎|👍|🎫|🐅|🐯|⏲|😫|🚽|🍅|👅|🧰|🦷|🪥|🎩|🌪|🖲|🚜|™|🚆|🚊|🚋|⚧|🚩|📐|🔱|🧌|🚎|🏆|🍹|🐠|🎺|🌷|🥃|🦃|🐢|🕧|🕛|🐫|🕝|💕|🕑|☂|⛱|☔|😒|🦄|🔓|↕|↖|↗|⬆|🙃|🔼|🧛|🚦|📳|✌|📹|🎮|📼|🎻|🌋|🏐|🖖|🧇|🌘|🌖|⚠|🗑|⌚|🐃|🚾|🔫|🌊|🍉|👋|〰|🌒|🌔|🙀|😩|💒|🐋|🛞|☸|♿|🦯|⚪|❕|🏳|💮|🦳|🤍|⬜|◽|◻|❔|▫|🔳|🥀|🎐|🌬|🪟|🍷|😉|😜|🐺|👩|👫|💃|🧕|👢|👚|👒|👡|👭|🚺|🪵|🥴|🗺|🪱|😟|🎁|🔧|✍|🩻|🧶|🥱|🟡|💛|🟨|💴|☯|🪀|🤪|🦓|🤐|🧟|💤)");

    private static readonly Regex CustomEmoteRegex = new(@"<a?:\w*:\d*>");

    public static bool Check(IMessage message, AutoModConfig config, DiscordSocketClient _)
    {
        if (config.Limit == null)
            return false;

        if (string.IsNullOrEmpty(message.Content))
            return false;

        var customEmotes = CustomEmoteRegex.Matches(message.Content).Count;

        // Skip normal emote check if possible.
        if (customEmotes > config.Limit)
            return true;

        var emotes = EmoteRegex.Matches(message.Content).Count;

        return emotes + customEmotes > config.Limit;
    }
}
