// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System.Collections.Generic;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Api;
using VexFlowSharp.Skia;

namespace VexFlowSharp.Tests.Api
{
    [TestFixture]
    [Category("EasyScore")]
    [Category("Phase5")]
    public class EasyScoreTests
    {
        // ── Helper ────────────────────────────────────────────────────────────

        private static (SkiaRenderContext ctx, Factory factory, EasyScore score)
            CreateScore(int width = 400, int height = 150)
        {
            var ctx = new SkiaRenderContext(width, height);
            var factory = new Factory(ctx, width, height);
            var score = factory.EasyScore();
            return (ctx, factory, score);
        }

        // ── Unit tests ────────────────────────────────────────────────────────

        [Test]
        public void ParsesSingleNote()
        {
            var (ctx, _, score) = CreateScore();
            using (ctx)
            {
                var notes = score.Notes("C4/q");
                Assert.AreEqual(1, notes.Count, "Should produce 1 StaveNote");
                // "q" is the EasyScore input; tables normalizes it to "4"
                Assert.AreEqual("4", notes[0].GetDuration(), "Duration should be '4' (normalized from 'q')");
                var keys = notes[0].GetKeys();
                Assert.AreEqual(1, keys.Length, "Should have 1 key");
                Assert.AreEqual("c/4", keys[0], "Key should be 'c/4'");
            }
        }

        [Test]
        public void ParsesChord()
        {
            var (ctx, _, score) = CreateScore();
            using (ctx)
            {
                var notes = score.Notes("(C4 E4 G4)/q");
                Assert.AreEqual(1, notes.Count, "Chord should produce 1 StaveNote");
                var keys = notes[0].GetKeys();
                Assert.AreEqual(3, keys.Length, "Chord should have 3 keys");
                Assert.AreEqual("c/4", keys[0], "First key should be 'c/4'");
                Assert.AreEqual("e/4", keys[1], "Second key should be 'e/4'");
                Assert.AreEqual("g/4", keys[2], "Third key should be 'g/4'");
            }
        }

        [Test]
        public void RollingDuration()
        {
            var (ctx, _, score) = CreateScore();
            using (ctx)
            {
                var notes = score.Notes("C4/q, D4, E4");
                Assert.AreEqual(3, notes.Count, "Should produce 3 StaveNotes");
                // All three inherit /q which normalizes to "4"
                Assert.AreEqual("4", notes[0].GetDuration(), "C4 should have duration '4' (normalized from 'q')");
                Assert.AreEqual("4", notes[1].GetDuration(), "D4 should inherit duration '4'");
                Assert.AreEqual("4", notes[2].GetDuration(), "E4 should inherit duration '4'");
            }
        }

        [Test]
        public void ParsesAccidental()
        {
            var (ctx, _, score) = CreateScore();
            using (ctx)
            {
                var notes = score.Notes("C#4/q");
                Assert.AreEqual(1, notes.Count, "Should produce 1 StaveNote");
                var keys = notes[0].GetKeys();
                Assert.AreEqual("c#/4", keys[0], "Key should include accidental: 'c#/4'");
            }
        }

        [Test]
        public void ParsesFourNotes()
        {
            var (ctx, _, score) = CreateScore(600, 200);
            using (ctx)
            {
                var notes = score.Notes("C4/q, D4, E4, F4");
                Assert.AreEqual(4, notes.Count, "Should produce 4 StaveNotes");
                Assert.AreEqual("c/4", notes[0].GetKeys()[0]);
                Assert.AreEqual("d/4", notes[1].GetKeys()[0]);
                Assert.AreEqual("e/4", notes[2].GetKeys()[0]);
                Assert.AreEqual("f/4", notes[3].GetKeys()[0]);
            }
        }

        [Test]
        public void ParsesBassClef()
        {
            var (ctx, _, score) = CreateScore();
            using (ctx)
            {
                var notes = score.Notes("C3/q", new NoteOptions { Clef = "bass" });
                Assert.AreEqual(1, notes.Count, "Should produce 1 StaveNote");
                Assert.AreEqual("c/3", notes[0].GetKeys()[0]);
            }
        }

        [Test]
        [Category("ImageCompare")]
        [Explicit("Pixel comparison — requires reference image generation")]
        public void EasyScoreGrandStaffRenders()
        {
            // EasyScore grand staff: score.notes('C4/q, D4, E4, F4') for treble and bass
            // Compare against plan_05_04_easyscore_grandstaff.png
            Assert.Fail("Reference image not yet generated — run gen-reference-images.mjs first");
        }
    }
}
