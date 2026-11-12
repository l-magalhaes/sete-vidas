using NUnit.Framework;
using SeteVidas.Audio;

/// <summary>Diretor de música (Docs/Audio/plano-musicas.md): quem vence quando várias coisas acontecem juntas.</summary>
public class MusicRulesTests
{
    static MusicRules.Situation S() { return new MusicRules.Situation(); }

    [Test]
    public void NothingHappening_PlaysTheLevel()
    {
        Assert.AreEqual(MusicCue.Explore, MusicRules.Pick(S()));
    }

    [Test]
    public void Mountain_UntilTheJump()
    {
        var s = S(); s.OnMountain = true;
        Assert.AreEqual(MusicCue.Mountain, MusicRules.Pick(s));
    }

    [Test]
    public void Stealth_OnlyWhenTheCatIsObscuredInTheShadow()
    {
        var a = S(); a.Obscured = true;
        var b = S(); b.Obscured = true; b.InSanctuary = true;
        Assert.AreEqual(MusicCue.Stealth, MusicRules.Pick(a));
        Assert.AreEqual(MusicCue.Stealth, MusicRules.Pick(b), "Na sombra vence o santuário");
        Assert.AreEqual(MusicCue.Explore, MusicRules.Pick(S()), "Fora da sombra, a música da fase");
    }

    [Test]
    public void Combat_BeatsStealth()
    {
        var s = S(); s.Obscured = true; s.AnyCombat = true;
        Assert.AreEqual(MusicCue.Combat, MusicRules.Pick(s));
    }

    [Test]
    public void Boss_BeatsCombat_AndTheTomeSceneBeatsEverything()
    {
        var s = S(); s.AnyCombat = true; s.BossFighting = true;
        Assert.AreEqual(MusicCue.Boss, MusicRules.Pick(s));
        s.CurseScene = true;
        Assert.AreEqual(MusicCue.Curse, MusicRules.Pick(s));
    }

    [Test]
    public void AfterTheBoss_Silence_UntilTheTome()
    {
        var s = S(); s.BossOutro = true;
        Assert.AreEqual(MusicCue.None, MusicRules.Pick(s));
    }

    [Test]
    public void Combat_FadesInAndOutSmoothly_ButQuickly()
    {
        // pedido do usuário: a entrada do combate não pode ser brusca, e a saída não pode demorar
        float fadeIn = MusicRules.FadeTime(MusicCue.Explore, MusicCue.Combat);
        float fadeOut = MusicRules.FadeTime(MusicCue.Combat, MusicCue.Explore);
        Assert.That(fadeIn, Is.InRange(1f, 1.5f));
        Assert.That(fadeOut, Is.InRange(1f, 1.5f));
    }
}
