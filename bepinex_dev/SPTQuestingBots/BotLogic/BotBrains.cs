using System.Collections.Generic;
using System.Linq;

namespace QuestingBots.BotLogic
{
    public static class BotBrains
    {
        public static string PMC { get { return "PMC"; } }
        public static string Cultist { get { return "SectantWarrior"; } }
        public static string Knight { get { return "Knight"; } }
        public static string Rogue { get { return "ExUsec"; } }
        public static string Tagilla { get { return "Tagilla"; } }
        public static string Killa { get { return "Killa"; } }

        public static IEnumerable<string> Scavs => new string[] { "Assault", "CursAssault" };
        public static IEnumerable<string> BossesNormal => new string[] { "BossBully","BossSanitar", "Tagilla", "BossGluhar", "BossKojaniy", "SectantPriest" };
        public static IEnumerable<string> FollowersGoons => new string[] { "BigPipe", "BirdEye" };
        public static IEnumerable<string> FollowersNormal => new string[] { "FollowerBully", "FollowerSanitar", "TagillaFollower", "FollowerGluharAssault", "FollowerGluharProtect", "FollowerGluharScout" };

        public static IEnumerable<string> AllGoons => FollowersGoons.Concat(new[] { Knight });
        public static IEnumerable<string> BossesWithoutGoons => BossesNormal.Concat(new[] { Tagilla, Killa });
        public static IEnumerable<string> BossesWithKnight => BossesWithoutGoons.Concat(new[] { Knight });

        public static IEnumerable<string> AllNormalBots => Scavs.Concat(new[] { PMC, Rogue, Cultist });
        public static IEnumerable<string> AllFollowers => FollowersNormal.Concat(FollowersGoons);
        public static IEnumerable<string> AllBossesWithFollowers => BossesWithKnight.Concat(AllFollowers);
        public static IEnumerable<string> AllBots => AllNormalBots.Concat(AllBossesWithFollowers);
    }
}
