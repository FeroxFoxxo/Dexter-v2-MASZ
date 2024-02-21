using Bot.Data;
using Bot.Extensions;
using Discord;
using Levels.Data;
using Levels.Enums;
using Levels.Extensions;
using Microsoft.Extensions.Logging;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Globalization;
using System.Text.RegularExpressions;
using Color = SixLabors.ImageSharp.Color;
using Image = SixLabors.ImageSharp.Image;
using IOPath = System.IO.Path;

namespace Levels.Models;

public static partial class Rankcard
{
    private const int Widthmain = 1000;
    private const int Height = 450;
    private const int Pfpside = 350;
    private const int NormalMargin = 25;
    private const int LevelHeight = (Pfpside - 3 * NormalMargin) / 2;
    private const int MaxTitleXpWidth = Widthmain / 2;

    private const int MiniLabelWidth = 80;
    private const int LabelIntrusionPixels = 0;
    private const int LabelHeight = 60;
    private const int TypeLabelWidth = 175;
    private const int LabelMiniMargin = 10;
    private const int LabelBaselineDeltaTitle = 8;
    private const int LabelBaselineDeltaNormal = 3;

    private const int BarMarginVertical = 9;
    private const int BarMarginHorizontal = 125;
    private const int BarHeight = 75 - 3 * BarMarginVertical;
    public static readonly Size RankCardSize = new(Widthmain + Pfpside, Height);

    private static readonly Size TitleSize = new(RankCardSize.Width - 2 * NormalMargin, LabelHeight);
    private static readonly Size LevelsSize = new(Widthmain - 2 * NormalMargin, Pfpside - 2 * NormalMargin);
    private static Rectangle TitleRect(UserRankcardConfig config) => new(config.TitleOffset + NormalMargin, TitleSize);

    private static Rectangle LevelsRect(UserRankcardConfig config) =>
        new(config.LevelOffset + NormalMargin, LevelsSize);

    private static int PfpMargin(UserRankcardConfig config) =>
        (int)(Pfpside * (1 - (config.PfpRadiusFactor < 0.1 ? 0.1 : config.PfpRadiusFactor))) / 2;

    private static Size PfpSize(UserRankcardConfig config) => new(Pfpside - 2 * PfpMargin(config));

    private static Rectangle PfpRect(UserRankcardConfig config) =>
        new(config.PfpOffset + PfpMargin(config), PfpSize(config));

    private static LevelRect MainLevel(Rectangle container) => new(container);
    private static LevelRect MainHybridLevel(Rectangle container) => new(container, LevelRect.LevelBarType.HybridMain);

    private static LevelRect SecondaryHybridLevel(Rectangle container) =>
        new(container, LevelRect.LevelBarType.HybridSecondary);

    private static Rectangle LevelLabelRect(UserRankcardConfig config) => new(config.TitleOffsetX + NormalMargin,
        config.TitleOffsetY + NormalMargin, MiniLabelWidth + LabelIntrusionPixels, LabelHeight);

    private static Rectangle LevelTextRect(UserRankcardConfig config) => new(
        config.TitleOffsetX + MiniLabelWidth + NormalMargin, config.TitleOffsetY + NormalMargin,
        MaxTitleXpWidth - MiniLabelWidth, LabelHeight + LabelBaselineDeltaTitle);

    private static Rectangle NameRect(UserRankcardConfig config) => new(config.TitleOffsetX + NormalMargin,
        config.TitleOffsetY + NormalMargin, RankCardSize.Width - 2 * NormalMargin,
        LabelHeight + LabelBaselineDeltaNormal);

    public static async Task<Image> RenderRankCard(IUser user, CalculatedGuildUserLevel ul,
        UserRankcardConfig rankcardConfig, GuildUserLevelRepository levelsRepo, SettingsRepository configRepo,
        ILogger logger)
    {
        await configRepo.GetAppSettings();
        var fontPath = IOPath.Join(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Fonts", "rankcardfont.ttf");

        FontCollection fontCollection = new();
        fontCollection.Add(fontPath);
        var fontfamily = fontCollection.Families.First();

        Font fontTitle = new(fontfamily, 55);
        Font fontDefault = new(fontfamily, 40);
        Font fontMini = new(fontfamily, 22);

        List<RankcardLevelData> levelsData = [];
        int totallevel;
        var totalxp = ul.TotalXp;

        string totallevelstr;

        var allUsers = levelsRepo.GetAllLevelsInGuild(ul.GuildId).ToList();
        var rank = 1;
        var txtrank = 1;
        var vcrank = 1;
        allUsers.ForEach(u =>
        {
            if (u.TotalXp > totalxp)
                rank++;
            if (u.TextXp > ul.TextXp)
                txtrank++;
            if (u.VoiceXp > ul.VoiceXp)
                vcrank++;
        });

        var levelsContainer = LevelsRect(rankcardConfig);
        levelsData.Add(new RankcardLevelData(ul.TotalXp, MainLevel(levelsContainer), ul.Config!));

        levelsData[0].Rank = rank;
        levelsData[0].XpType = "Level";
        totallevel = levelsData[0].Level;
        totallevelstr = $"({ul.TotalXp.ToUnit()} XP)";

        if (rankcardConfig.RankcardFlags.HasFlag(RankcardFlags.ShowHybrid))
        {
            levelsData.Add(new RankcardLevelData(ul.TextXp > ul.VoiceXp ? ul.TextXp : ul.VoiceXp,
                MainHybridLevel(levelsContainer), ul.Config!, true));
            levelsData.Add(new RankcardLevelData(ul.TextXp > ul.VoiceXp ? ul.VoiceXp : ul.TextXp,
                SecondaryHybridLevel(levelsContainer), ul.Config!, true));

            if (ul.TextXp > ul.VoiceXp)
            {
                levelsData[1].Rank = txtrank;
                levelsData[1].XpType = "Txt";
                levelsData[2].Rank = vcrank;
                levelsData[2].XpType = "VC";
            }
            else
            {
                levelsData[1].Rank = vcrank;
                levelsData[1].XpType = "VC";
                levelsData[2].Rank = txtrank;
                levelsData[2].XpType = "Txt";
            }
        }

        Func<IImageProcessingContext, IImageProcessingContext> bgTransform;
        Func<IImageProcessingContext, IImageProcessingContext> pfpTransform;

        var xpColor = rankcardConfig.XpColor.ColorFromArgb();
        var offColor = rankcardConfig.OffColor.ColorFromArgb();
        var lvlBgColor = rankcardConfig.LevelBgColor.ColorFromArgb();

        Image bg = null;
        try
        {
            if (rankcardConfig.Background.StartsWith("http"))
                throw new FileLoadException();

            var fileName = IOPath.Join(LevelsImageRepository.GetDefaultBackgroundDir(),
                (rankcardConfig.Background ?? "default") + ".png");
            bg = Image.Load(File.ReadAllBytes(fileName));
            bg.Mutate(ctx => ctx.Resize(RankCardSize));
            bgTransform = g => g.DrawImage(bg, 1);
        }
        catch (FileNotFoundException)
        {
            var bgc = LevelRegex().IsMatch(rankcardConfig.Background)
                ? (0xff000000 | uint.Parse(rankcardConfig.Background[^6..], NumberStyles.HexNumber))
                .ColorFromArgb()
                : (0xff000000 | rankcardConfig.XpColor).ColorFromArgb();
            bgTransform = g => g.BackgroundColor(bgc);
        }
        catch (FileLoadException)
        {
            using HttpClient client = new();

            logger.LogInformation(rankcardConfig.Background);
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Get, rankcardConfig.Background);
                var resp = await client.SendAsync(req);
                if (!resp.IsSuccessStatusCode)
                    throw new HttpRequestException(
                        "Received unsuccessful response to rankcard background image request.\n" +
                        $"Code: {(int)resp.StatusCode} ({resp.ReasonPhrase})\n" +
                        $"Headers: {string.Join("\n".PadRight("\nHeaders: ".Length), resp.Headers.Select(h => $"{h.Key}={string.Join(", ", h.Value)}"))}\n" +
                        $"Content: {resp.Content}");
                bg = Image.Load(await resp.Content.ReadAsByteArrayAsync());
                bg.Mutate(ctx => ctx.Resize(RankCardSize));
                bgTransform = g => g.DrawImage(bg, 1);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                logger.LogError(e, e.Message);
                Console.ForegroundColor = ConsoleColor.White;
                bgTransform = g => g.BackgroundColor((0xff000000 | rankcardConfig.XpColor).ColorFromArgb());
            }
        }

        var rectPfp = PfpRect(rankcardConfig);

        Image pfp = null;
        using (HttpClient client = new())
        {
            try
            {
                var dataArr = await client.GetByteArrayAsync(user.GetAvatarOrDefaultUrl(size: 512));
                pfp = Image.Load(dataArr);
                pfp.Mutate(ctx => ctx.Resize(rectPfp.Size));

                pfpTransform = rankcardConfig.RankcardFlags.HasFlag(RankcardFlags.ClipPfp)
                    ? g => g.Clip(new PathBuilder().AddArc(rectPfp, 0, 0, 360).Build(),
                        clipg => clipg.DrawImage(pfp, new Point(rectPfp.Left, rectPfp.Top), 1)
                    )
                    : g => g.DrawImage(pfp, new Point(rectPfp.Left, rectPfp.Top), 1);
            }
            catch (HttpRequestException)
            {
                pfpTransform = g => g.Draw(new SolidPen(xpColor, 5),
                    new EllipsePolygon(rectPfp.Left, rectPfp.Top, rectPfp.Width, rectPfp.Height));
            }
        }

        var rectLevelLabel = LevelLabelRect(rankcardConfig);
        var rectLevelText = LevelTextRect(rankcardConfig);

        Image result = new Image<Rgba32>(RankCardSize.Width, RankCardSize.Height);
        result.Mutate(g =>
        {
            var offset = TextMeasurer.MeasureBounds(totallevel.ToString(), new TextOptions(fontTitle));
            const int margin = 5;

            g = bgTransform(g);

            g = g.Fill(lvlBgColor, TitleRect(rankcardConfig))
                .DrawTextInRect("LEVEL", rectLevelLabel, fontMini, offColor, HorizontalAlignment.Right,
                    VerticalAlignment.Bottom)
                .DrawTextInRect(totallevel.ToString(), rectLevelText, fontTitle, xpColor, HorizontalAlignment.Left,
                    VerticalAlignment.Bottom)
                .DrawTextInRect(totallevelstr,
                    new Rectangle(rectLevelText.X + (int)offset.Width + margin, rectLevelText.Y,
                        Widthmain / 2 - MiniLabelWidth - margin - (int)offset.Width,
                        LabelHeight + LabelBaselineDeltaNormal),
                    fontDefault, xpColor, HorizontalAlignment.Left, VerticalAlignment.Bottom);

            g = g.DrawLevels(fontTitle, fontDefault, fontMini, levelsData, rankcardConfig);

            bg?.Dispose();

            const int pfpmargin = 3;
            if (rankcardConfig.RankcardFlags.HasFlag(RankcardFlags.PfpBackground))
                g = g.Fill(0xff3f3f3f.ColorFromArgb()
                    , new EllipsePolygon(rectPfp.X + rectPfp.Width / 2, rectPfp.Y + rectPfp.Height / 2, rectPfp.Width + 2 * pfpmargin, rectPfp.Height + 2 * pfpmargin));

            if (rankcardConfig.RankcardFlags.HasFlag(RankcardFlags.DisplayPfp))
                g = pfpTransform(g);

            pfp?.Dispose();

            g = g.DrawTextInRect(user.Username, NameRect(rankcardConfig), fontDefault,
                offColor,
                HorizontalAlignment.Right, VerticalAlignment.Bottom);
        });
        return result;
    }

    private static IImageProcessingContext DrawLevels(this IImageProcessingContext g, Font fontTitle, Font fontDefault,
        Font fontMini, IEnumerable<RankcardLevelData> levels, UserRankcardConfig prefs)
    {
        var xpColor = prefs.XpColor.ColorFromArgb();
        var offColor = prefs.OffColor.ColorFromArgb();

        foreach (var ld in levels)
        {
            if (ld is null) continue;
            var barRect = ld.Rects.Bar(1);
            var levelPath = Graphics.RoundedRect(ld.Rects.FullRect, ld.Rects.FullRect.Height / 3);
            var barWholePath = Graphics.RoundedRect(barRect, barRect.Height / 2);
            var barXpgPath = Graphics.RoundedRect(ld.Rects.Bar(ld.Percent), barRect.Height / 2);
            var barInnerClipPath =
                Graphics.RoundedRect(new Rectangle(barRect.X + 2, barRect.Y + 2, barRect.Width - 4, barRect.Height - 4),
                    barRect.Height / 2 - 2);
            var levelRenderArea = levelPath.Clip(barWholePath);

            g = g.Fill(0xe0000000.ColorFromArgb(), barWholePath)
                .Clip(barInnerClipPath,
                    gclip => gclip.Fill(xpColor, barXpgPath))
                .Fill(prefs.LevelBgColor.ColorFromArgb(), levelRenderArea)
                .DrawTextInRect(ld.XpType, ld.Rects.TypeLabel, fontTitle, offColor, HorizontalAlignment.Center,
                    VerticalAlignment.Center);

            if (ld.Rects.RankLabel != default)
                g = g.DrawTextInRect("RANK", ld.Rects.RankLabel, fontMini, offColor, HorizontalAlignment.Right,
                    VerticalAlignment.Bottom);

            if (ld.Rects.RankText != default)
                g = g.DrawTextInRect($"#{ld.Rank}", ld.Rects.RankText, fontTitle, offColor, HorizontalAlignment.Left,
                    VerticalAlignment.Bottom);

            g = g.DrawTextInRect(ld.Level.ToString(), ld.Rects.CurrentLevel, fontTitle, xpColor,
                HorizontalAlignment.Center, VerticalAlignment.Center);
            if (ld.Rects.NextLevel != default)
                g = g.DrawTextInRect((ld.Level + 1).ToString(), ld.Rects.NextLevel, fontTitle, xpColor,
                    HorizontalAlignment.Center, VerticalAlignment.Center);

            var textXpTarget = ld.Rects.ExpText;
            if (!ld.IsHybrid && prefs.RankcardFlags.HasFlag(RankcardFlags.InsetMainXp))
            {
                ld.IsHybrid = true;
                textXpTarget = barRect;
            }

            if (ld.IsHybrid)
            {
                var overXpColor = xpColor.GetBrightness() < 0.5 ? Color.White : Color.Black;
                g = g.DrawTextInRect(ld.XpExpr, textXpTarget, fontDefault, xpColor, HorizontalAlignment.Center,
                        VerticalAlignment.Bottom)
                    .Clip(barXpgPath,
                        gclip => gclip.DrawTextInRect(ld.XpExpr, textXpTarget, fontDefault, overXpColor,
                            HorizontalAlignment.Center, VerticalAlignment.Bottom));
            }
            else
            {
                g = g.DrawTextInRect(ld.XpExpr, textXpTarget, fontDefault, xpColor, HorizontalAlignment.Right,
                    VerticalAlignment.Bottom);
            }
        }

        return g;
    }

    internal class LevelRect
    {
        public enum LevelBarType
        {
            Normal,
            HybridMain,
            HybridSecondary
        }

        public Func<float, Rectangle> Bar;
        public Rectangle CurrentLevel;
        public Rectangle ExpText;
        public Rectangle FullRect;
        public LevelBarType Leveltype;
        public Rectangle NextLevel;
        public Rectangle RankLabel;
        public Rectangle RankText;
        public Rectangle TypeLabel;

        public LevelRect(Rectangle container, LevelBarType leveltype = LevelBarType.Normal)
        {
            var originX = container.X;
            var originY = container.Y;
            int width;
            int barwidth;

            Leveltype = leveltype;
            if (leveltype == LevelBarType.Normal)
            {
                width = container.Width;
                barwidth = width - 2 * BarMarginHorizontal;

                NextLevel = new Rectangle(originX + width - BarMarginHorizontal,
                    originY + LevelHeight - BarHeight - BarMarginVertical, BarMarginHorizontal,
                    BarHeight + 2 * BarMarginVertical);
            }
            else
            {
                width = (container.Width - NormalMargin) / 2;
                if (leveltype == LevelBarType.HybridSecondary)
                    originX += width + NormalMargin;
                originY += (container.Height + NormalMargin) / 2;
                barwidth = width - BarMarginHorizontal - LabelMiniMargin;

                NextLevel = default;
            }

            FullRect = new Rectangle(originX, originY, width, LevelHeight);

            Bar = p => new Rectangle(originX + BarMarginHorizontal,
                originY + LevelHeight - BarHeight - BarMarginVertical
                , (int)(barwidth * p), BarHeight);
            CurrentLevel = new Rectangle(originX, originY + LevelHeight - BarHeight - BarMarginVertical,
                BarMarginHorizontal, BarHeight + 2 * BarMarginVertical);
            TypeLabel = new Rectangle(originX + LabelMiniMargin, originY + LabelMiniMargin, TypeLabelWidth,
                LabelHeight);
            RankLabel = new Rectangle(originX + TypeLabelWidth, originY, MiniLabelWidth + LabelIntrusionPixels,
                LabelHeight);
            RankText = new Rectangle(originX + MiniLabelWidth + TypeLabelWidth, originY,
                width * 2 / 3 - MiniLabelWidth - TypeLabelWidth - NormalMargin, LabelHeight + LabelBaselineDeltaTitle);
            ExpText = leveltype == LevelBarType.Normal
                ? new Rectangle(originX + width / 3, originY, width * 2 / 3 - LabelMiniMargin, LabelHeight)
                : Bar(1);
        }
    }

    internal class RankcardLevelData(long xp, LevelRect rects, GuildLevelConfig guildConfig, bool isHybrid = false) : LevelData(
        xp, guildConfig)
    {
        public bool IsHybrid = isHybrid;

        public int Rank;

        public LevelRect Rects = rects;
        public string XpType = "";
        public float Percent => ResidualXp / (float)LevelXp;
        public string XpExpr => $"{ResidualXp.ToUnit()} / {LevelXp.ToUnit()}{(IsHybrid ? "" : " XP")}";
    }

    [GeneratedRegex(@"^(#|0x)?[0-9A-F]{6}$", RegexOptions.IgnoreCase, "en-NZ")]
    private static partial Regex LevelRegex();
}
