using Verse;

namespace ActivateDependencies
{
    public static class Strings
    {
        public const string ID = "kathanon.ActivateDependencies";
        public const string Prefix = ID + ".";

        public static readonly string Active          = (Prefix + "Active"         ).Translate();
        public static readonly string Activate        = (Prefix + "Activate"       ).Translate();
        public static readonly string ActivateAll     = (Prefix + "ActivateAll"    ).Translate();

        public static readonly string ClickActivate   = (Prefix + "ClickActivate"  ).Translate();
        public static readonly string ClickDeactivate = (Prefix + "ClickDeactivate").Translate();

        public static readonly string CoreClickWeb = "ModClickToGoToWebsite".Translate();
        public static readonly string CoreClickSel = "ModClickToSelect"     .Translate();
    }
}
