namespace ShikiShiro
{
    public static class WaveCombatRules
    {
        public static int TrashCount(int wave, int poolCap)
        {
            int size = 16 + wave * 8;
            if (poolCap < 1)
            {
                poolCap = 1;
            }

            return size < poolCap ? size : poolCap;
        }

        public static int BossCount(int wave)
        {
            return wave >= 3 ? 3 : 1;
        }

        public static bool CanStartBossRound(int wave, int spawnedBossWave)
        {
            return wave >= 1 && spawnedBossWave != wave;
        }
    }
}
