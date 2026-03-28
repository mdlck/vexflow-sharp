using NUnit.Framework;
using VexFlowSharp;

namespace VexFlowSharp.Tests.TableTests
{
    [TestFixture]
    [Category("KeyManager")]
    public class KeyManagerTests
    {
        // ── Constructor ───────────────────────────────────────────────────────

        [Test]
        public void Constructor_C_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => new KeyManager("C"));
        }

        [Test]
        public void Constructor_G_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => new KeyManager("G"));
        }

        [Test]
        public void Constructor_Cm_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => new KeyManager("Cm"));
        }

        // ── GetAccidental ─────────────────────────────────────────────────────

        [Test]
        public void GetAccidental_CMajor_NoAccidental()
        {
            var km = new KeyManager("C");
            var result = km.GetAccidental("c");
            // Note may be "c" or "cn" depending on implementation; no sharp or flat is key.
            Assert.That(result.Note, Does.StartWith("c"));
            // No sharp or double-sharp accidental
            Assert.That(result.Accidental, Is.EqualTo("n").Or.EqualTo("").Or.Null);
        }

        [Test]
        public void GetAccidental_GMajor_FSharp()
        {
            var km = new KeyManager("G");
            // In G major, f is mapped to f#
            var result = km.GetAccidental("f");
            Assert.That(result.Note, Is.EqualTo("f#"));
            Assert.That(result.Accidental, Is.EqualTo("#"));
        }

        [Test]
        public void GetAccidental_GMajor_CNatural()
        {
            var km = new KeyManager("G");
            // C is natural in G major — note starts with "c", no accidentals
            var result = km.GetAccidental("c");
            Assert.That(result.Note, Does.StartWith("c"));
            Assert.That(result.Accidental, Is.EqualTo("n").Or.EqualTo("").Or.Null);
        }

        // ── GetKey ────────────────────────────────────────────────────────────

        [Test]
        public void GetKey_ReturnsInitialKey()
        {
            var km = new KeyManager("D");
            Assert.That(km.GetKey(), Is.EqualTo("D"));
        }

        // ── SetKey ────────────────────────────────────────────────────────────

        [Test]
        public void SetKey_ChangesKey()
        {
            var km = new KeyManager("C");
            km.SetKey("G");
            Assert.That(km.GetKey(), Is.EqualTo("G"));
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        [Test]
        public void Reset_RestoresOriginalScaleMap()
        {
            var km = new KeyManager("G");
            // G major has f#; SelectNote with "fn" should change the map
            km.SelectNote("fn");
            // After reset the map should be back to f# for G major
            km.Reset();
            var result = km.GetAccidental("f");
            Assert.That(result.Note, Is.EqualTo("f#"));
        }

        // ── SelectNote ────────────────────────────────────────────────────────

        [Test]
        public void SelectNote_InKey_NoChange()
        {
            var km = new KeyManager("G");
            // f# is in G major — should return no change
            var result = km.SelectNote("f#");
            Assert.That(result.Note, Is.EqualTo("f#"));
            Assert.That(result.Change, Is.False);
        }

        [Test]
        public void SelectNote_OutsideKey_ChangesMap()
        {
            var km = new KeyManager("C");
            // f# is NOT in C major — should register a change
            var result = km.SelectNote("f#");
            Assert.That(result.Note, Is.EqualTo("f#"));
            Assert.That(result.Change, Is.True);
        }
    }
}
