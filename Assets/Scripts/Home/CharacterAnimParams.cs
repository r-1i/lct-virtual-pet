namespace Home
{
    /// <summary>
    /// Parameter names of Character_Base.controller (built by Tools → Character → Create Base Animator). Code that
    /// drives the character's animations uses these, through CharacterAppearance.Animator.
    /// Bools = looping states that last while the flag is on; triggers = one-shots from Any State, back to Idle after.
    /// </summary>
    public static class CharacterAnimParams
    {
        public const string Dancing = "isDancing";
        public const string Talking = "isTalking";
        public const string Working = "isWorking";
        public const string StomachAche = "isStomachAche";

        public const string Greet = "Greet";
        public const string Joy = "Joy";
        public const string Play = "Play";
        public const string Eat = "Eat";
    }
}
