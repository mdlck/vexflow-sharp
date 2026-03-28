using NUnit.Framework;
using VexFlowSharp;

namespace VexFlowSharp.Tests.TableTests
{
    [TestFixture]
    [Category("Music")]
    public class MusicTests
    {
        private Music _music = null!;

        [SetUp]
        public void SetUp()
        {
            _music = new Music();
        }

        // ── GetNoteParts ──────────────────────────────────────────────────────

        [Test]
        public void GetNoteParts_Sharp_ParsesCorrectly()
        {
            var parts = _music.GetNoteParts("c#");
            Assert.That(parts.Root,        Is.EqualTo("c"));
            Assert.That(parts.Accidental,  Is.EqualTo("#"));
        }

        [Test]
        public void GetNoteParts_Natural_ParsesCorrectly()
        {
            var parts = _music.GetNoteParts("d");
            Assert.That(parts.Root,        Is.EqualTo("d"));
            Assert.That(parts.Accidental,  Is.EqualTo(""));
        }

        [Test]
        public void GetNoteParts_DoubleFlat_ParsesCorrectly()
        {
            var parts = _music.GetNoteParts("abb");
            Assert.That(parts.Root,        Is.EqualTo("a"));
            Assert.That(parts.Accidental,  Is.EqualTo("bb"));
        }

        [Test]
        public void GetNoteParts_TooLong_Throws()
        {
            Assert.Throws<VexFlowException>(() => _music.GetNoteParts("c###"));
        }

        [Test]
        public void GetNoteParts_Invalid_Throws()
        {
            var ex = Assert.Throws<VexFlowException>(() => _music.GetNoteParts("x"));
            Assert.That(ex!.Code, Is.EqualTo("BadArguments"));
        }

        // ── GetKeyParts ───────────────────────────────────────────────────────

        [Test]
        public void GetKeyParts_Minor_ParsesCorrectly()
        {
            var parts = _music.GetKeyParts("Cm");
            Assert.That(parts.Root,        Is.EqualTo("c"));
            Assert.That(parts.Accidental,  Is.EqualTo(""));
            Assert.That(parts.Type,        Is.EqualTo("m"));
        }

        [Test]
        public void GetKeyParts_Major_DefaultsToM()
        {
            var parts = _music.GetKeyParts("G");
            Assert.That(parts.Root,  Is.EqualTo("g"));
            Assert.That(parts.Type,  Is.EqualTo("M"));
        }

        [Test]
        public void GetKeyParts_SharpMinor_ParsesCorrectly()
        {
            var parts = _music.GetKeyParts("F#m");
            Assert.That(parts.Root,        Is.EqualTo("f"));
            Assert.That(parts.Accidental,  Is.EqualTo("#"));
            Assert.That(parts.Type,        Is.EqualTo("m"));
        }

        // ── GetNoteValue ──────────────────────────────────────────────────────

        [Test]
        public void GetNoteValue_C_Returns0()
        {
            Assert.That(_music.GetNoteValue("c"), Is.EqualTo(0));
        }

        [Test]
        public void GetNoteValue_G_Returns7()
        {
            Assert.That(_music.GetNoteValue("g"), Is.EqualTo(7));
        }

        [Test]
        public void GetNoteValue_FSharp_Returns6()
        {
            Assert.That(_music.GetNoteValue("f#"), Is.EqualTo(6));
        }

        [Test]
        public void GetNoteValue_Invalid_Throws()
        {
            var ex = Assert.Throws<VexFlowException>(() => _music.GetNoteValue("z"));
            Assert.That(ex!.Code, Is.EqualTo("BadArguments"));
        }

        // ── GetScaleTones ─────────────────────────────────────────────────────

        [Test]
        public void GetScaleTones_CMajor_ReturnsCorrectScale()
        {
            var tones = _music.GetScaleTones(0, Music.Scales["major"]);
            CollectionAssert.AreEqual(new[] { 0, 2, 4, 5, 7, 9, 11 }, tones);
        }

        [Test]
        public void GetScaleTones_AMinor_ReturnsCorrectScale()
        {
            // A = 9; minor intervals [2,1,2,2,1,2,2]
            var tones = _music.GetScaleTones(9, Music.Scales["minor"]);
            CollectionAssert.AreEqual(new[] { 9, 11, 0, 2, 4, 5, 7 }, tones);
        }

        // ── GetCanonicalNoteName ──────────────────────────────────────────────

        [Test]
        public void GetCanonicalNoteName_0_ReturnsC()
        {
            Assert.That(_music.GetCanonicalNoteName(0), Is.EqualTo("c"));
        }

        [Test]
        public void GetCanonicalNoteName_6_ReturnsFSharp()
        {
            Assert.That(_music.GetCanonicalNoteName(6), Is.EqualTo("f#"));
        }

        [Test]
        public void GetCanonicalNoteName_OutOfRange_Throws()
        {
            Assert.Throws<VexFlowException>(() => _music.GetCanonicalNoteName(12));
        }

        // ── CreateScaleMap ────────────────────────────────────────────────────

        [Test]
        public void CreateScaleMap_GMajor_FSharpInMap()
        {
            var map = _music.CreateScaleMap("G");
            // G major has F#
            Assert.That(map.ContainsKey("f"), Is.True);
            Assert.That(map["f"], Is.EqualTo("f#"));
        }

        [Test]
        public void CreateScaleMap_CMajor_AllNaturals()
        {
            var map = _music.CreateScaleMap("C");
            foreach (var root in Music.Roots)
            {
                Assert.That(map.ContainsKey(root), Is.True);
                // All naturals end with 'n'
                Assert.That(map[root].EndsWith("n"), Is.True,
                    $"Expected {root} to map to a natural note, got {map[root]}");
            }
        }
    }
}
