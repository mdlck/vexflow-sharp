// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Api;
using VexFlowSharp.Common.Formatting;
using VexFlowSharp.Skia;
using VexFlowSharp.Tests.Infrastructure;

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

        private static string ReferenceImagesDir
        {
            get
            {
                string assemblyDir = Path.GetDirectoryName(
                    typeof(EasyScoreTests).Assembly.Location)!;
                return Path.GetFullPath(
                    Path.Combine(assemblyDir, "../../../reference-images"));
            }
        }

        private static string RefPath(string filename) =>
            Path.Combine(ReferenceImagesDir, filename);

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
        public void ParsesXNoteheadTypes()
        {
            var (ctx, _, score) = CreateScore();
            using (ctx)
            {
                var notes = score.Notes("C4/q/x, D4/q//X");

                Assert.That(notes, Has.Count.EqualTo(2));
                Assert.That(notes[0].GetNoteType(), Is.EqualTo("x"));
                Assert.That(notes[1].GetNoteType(), Is.EqualTo("X"));
                Assert.That(((StaveNote)notes[0]).GetNoteHeads()[0].GetGlyphCode(), Is.EqualTo("noteheadXBlack"));
                Assert.That(((StaveNote)notes[1]).GetNoteHeads()[0].GetGlyphCode(), Is.EqualTo("noteheadXBlack"));
            }
        }

        [Test]
        public void ParsesGhostNotesIntoReturnedNoteList()
        {
            var (ctx, _, score) = CreateScore();
            using (ctx)
            {
                var notes = score.Notes("C4/q, D4/q/g, E4/q");

                Assert.That(notes, Has.Count.EqualTo(3));
                Assert.That(notes[0], Is.TypeOf<StaveNote>());
                Assert.That(notes[1], Is.TypeOf<GhostNote>());
                Assert.That(notes[2], Is.TypeOf<StaveNote>());
                Assert.That(notes[1].GetTicks(), Is.EqualTo(new Fraction(Tables.RESOLUTION / 4, 1)));

                var voice = score.Voice(notes, new VoiceOptions { Time = "3/4" });
                Assert.That(voice.GetTickables(), Has.Count.EqualTo(3));
                Assert.That(voice.GetTotalTicks(), Is.EqualTo(new Fraction(Tables.RESOLUTION * 3 / 4, 1)));
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
        public void SetDefaults_AppliesStemClefAndVoiceTime()
        {
            var (ctx, _, score) = CreateScore();
            using (ctx)
            {
                score.Set(new EasyScoreDefaults
                {
                    Clef = "bass",
                    Stem = "down",
                    Time = "3/8",
                });

                var notes = score.Notes("C3/q");
                var voice = score.Voice(notes);

                Assert.That(notes[0].GetKeys()[0], Is.EqualTo("c/3"));
                Assert.That(notes[0].GetStemDirection(), Is.EqualTo(Stem.DOWN));
                Assert.That(voice.GetTotalTicks(), Is.EqualTo(new Fraction(Tables.RESOLUTION * 3 / 8, 1)));
            }
        }

        [Test]
        public void Voice_AcceptsTimeStringAndSoftmaxFactor()
        {
            var (ctx, _, score) = CreateScore();
            using (ctx)
            {
                var notes = score.Notes("C4/8, D4, E4");

                var voice = score.Voice(notes, new VoiceOptions
                {
                    Time = "3/8",
                    SoftmaxFactor = 2,
                });

                Assert.That(voice.GetTotalTicks(), Is.EqualTo(new Fraction(Tables.RESOLUTION * 3 / 8, 1)));
                Assert.That(voice.GetTickables(), Has.Count.EqualTo(3));
                Assert.That(voice.Softmax(notes[0].GetTicks().Value()), Is.GreaterThan(0));
            }
        }

        [Test]
        public void Voice_AcceptsV5NestedOptions()
        {
            var (ctx, _, score) = CreateScore();
            using (ctx)
            {
                var notes = score.Notes("C4/8, D4, E4");

                var voice = score.Voice(notes, new VoiceOptions
                {
                    Time = "3/8",
                    Options = new VoiceFormattingOptions { SoftmaxFactor = 3 },
                });

                Assert.That(voice.GetTotalTicks(), Is.EqualTo(new Fraction(Tables.RESOLUTION * 3 / 8, 1)));
                Assert.That(voice.Softmax(notes[0].GetTicks().Value()), Is.GreaterThan(0));
            }
        }

        [Test]
        public void BeamAndTuplet_ReturnNotesForFluentComposition()
        {
            var (ctx, _, score) = CreateScore();
            using (ctx)
            {
                var notes = score.Notes("C4/8, D4, E4")
                    .Cast<StemmableNote>()
                    .ToList();

                var beamed = score.Beam(notes, new FactoryBeamOptions { AutoStem = true });
                var tupleted = score.Tuplet(beamed, new TupletOptions { NumNotes = 3, NotesOccupied = 2 });

                Assert.That(beamed, Is.SameAs(notes));
                Assert.That(tupleted, Is.SameAs(notes));
                Assert.That(notes, Has.All.Matches<StemmableNote>(note => note.HasBeam()));
                Assert.That(notes, Has.All.Matches<StemmableNote>(note => note.GetTuplet() is Tuplet));
            }
        }

        [Test]
        public void NoteOptions_SetElementIdAndClasses()
        {
            var (ctx, _, score) = CreateScore();
            using (ctx)
            {
                var notes = score.Notes("C4/q[id=\"note-1\", class=\"red,bold\"]");

                Assert.That(notes[0].GetId(), Is.EqualTo("note-1"));
                Assert.That(notes[0].GetAttribute("id"), Is.EqualTo("note-1"));
                Assert.That(notes[0].HasClass("red"), Is.True);
                Assert.That(notes[0].HasClass("bold"), Is.True);
                Assert.That(notes[0].GetAttribute("class"), Is.EqualTo("red bold"));
            }
        }

        [Test]
        public void NoteOptions_AddArticulations()
        {
            var (ctx, _, score) = CreateScore();
            using (ctx)
            {
                var notes = score.Notes("C4/q[articulations=\"staccato.below,tenuto,accent.above\"]");

                var articulations = notes[0].GetModifiers().OfType<Articulation>().ToArray();

                Assert.That(articulations, Has.Length.EqualTo(3));
                Assert.That(articulations[0].Type, Is.EqualTo("a."));
                Assert.That(articulations[0].GetPosition(), Is.EqualTo(ModifierPosition.Below));
                Assert.That(articulations[1].Type, Is.EqualTo("a-"));
                Assert.That(articulations[1].GetPosition(), Is.EqualTo(ModifierPosition.Above));
                Assert.That(articulations[2].Type, Is.EqualTo("a>"));
                Assert.That(articulations[2].GetPosition(), Is.EqualTo(ModifierPosition.Above));
            }
        }

        [Test]
        public void NoteOptions_AddFingerings()
        {
            var (ctx, _, score) = CreateScore();
            using (ctx)
            {
                var notes = score.Notes("(C4 E4 G4)/q[fingerings=\"1.left,2.above,3.right\"]");

                var fingerings = notes[0].GetModifiers().OfType<FretHandFinger>().ToArray();

                Assert.That(fingerings, Has.Length.EqualTo(3));
                Assert.That(fingerings[0].GetFretHandFinger(), Is.EqualTo("1"));
                Assert.That(fingerings[0].GetPosition(), Is.EqualTo(ModifierPosition.Left));
                Assert.That(fingerings[0].GetIndex(), Is.EqualTo(0));
                Assert.That(fingerings[1].GetFretHandFinger(), Is.EqualTo("2"));
                Assert.That(fingerings[1].GetPosition(), Is.EqualTo(ModifierPosition.Above));
                Assert.That(fingerings[1].GetIndex(), Is.EqualTo(1));
                Assert.That(fingerings[2].GetFretHandFinger(), Is.EqualTo("3"));
                Assert.That(fingerings[2].GetPosition(), Is.EqualTo(ModifierPosition.Right));
                Assert.That(fingerings[2].GetIndex(), Is.EqualTo(2));
            }
        }

        [Test]
        public void NoteOptions_AddOrnaments()
        {
            var (ctx, _, score) = CreateScore();
            using (ctx)
            {
                var notes = score.Notes("C4/q[ornaments=\"tr.above,turn.below.delayed\"]");

                var ornaments = notes[0].GetModifiers().OfType<Ornament>().ToArray();

                Assert.That(ornaments, Has.Length.EqualTo(2));
                Assert.That(ornaments[0].Type, Is.EqualTo("tr"));
                Assert.That(ornaments[0].GetPosition(), Is.EqualTo(ModifierPosition.Above));
                Assert.That(ornaments[0].Delayed, Is.False);
                Assert.That(ornaments[1].Type, Is.EqualTo("turn"));
                Assert.That(ornaments[1].GetPosition(), Is.EqualTo(ModifierPosition.Below));
                Assert.That(ornaments[1].Delayed, Is.True);
            }
        }

        [Test]
        public void NoteOptions_AddAnnotations()
        {
            var (ctx, _, score) = CreateScore();
            using (ctx)
            {
                var notes = score.Notes("C4/q[annotations=\"lyric.below.left,mark.above.centerStem\"]");

                var annotations = notes[0].GetModifiers().OfType<Annotation>().ToArray();

                Assert.That(annotations, Has.Length.EqualTo(2));
                Assert.That(annotations[0].GetText(), Is.EqualTo("lyric"));
                Assert.That(annotations[0].GetVerticalJustification(), Is.EqualTo(AnnotationVerticalJustify.BOTTOM));
                Assert.That(annotations[0].GetJustification(), Is.EqualTo(AnnotationHorizontalJustify.LEFT));
                Assert.That(annotations[1].GetText(), Is.EqualTo("mark"));
                Assert.That(annotations[1].GetVerticalJustification(), Is.EqualTo(AnnotationVerticalJustify.TOP));
                Assert.That(annotations[1].GetJustification(), Is.EqualTo(AnnotationHorizontalJustify.CENTER_STEM));
            }
        }

        [Test]
        public void NoteOptions_AddSingularAnnotation()
        {
            var (ctx, _, score) = CreateScore();
            using (ctx)
            {
                var notes = score.Notes("C4/q[annotation=\"cue.bottom.right\"]");

                var annotation = notes[0].GetModifiers().OfType<Annotation>().Single();

                Assert.That(annotation.GetText(), Is.EqualTo("cue"));
                Assert.That(annotation.GetVerticalJustification(), Is.EqualTo(AnnotationVerticalJustify.BOTTOM));
                Assert.That(annotation.GetJustification(), Is.EqualTo(AnnotationHorizontalJustify.RIGHT));
            }
        }

        [Test]
        public void Parse_ReturnsResultAndHonorsThrowOnError()
        {
            var (ctx, _, score) = CreateScore();
            using (ctx)
            {
                var valid = score.Parse("C4/q");
                var invalid = score.Parse("C4/q, nope");

                Assert.That(valid.Success, Is.True);
                Assert.That(invalid.Success, Is.False);
                Assert.That(invalid.ErrorPos, Is.GreaterThanOrEqualTo(0));

                score.ThrowOnError = true;
                Assert.Throws<VexFlowException>(() => score.Parse("C4/q, nope"));
            }
        }

        [Test]
        public void AddCommitHook_RunsForEachParsedNote()
        {
            var (ctx, _, score) = CreateScore();
            using (ctx)
            {
                var hookCount = 0;
                var returned = score.AddCommitHook((_, note) =>
                {
                    hookCount++;
                    note.AddClass("hooked");
                });

                var notes = score.Notes("C4/q, D4");

                Assert.That(returned, Is.SameAs(score));
                Assert.That(hookCount, Is.EqualTo(2));
                Assert.That(notes, Has.All.Matches<StaveNote>(note => note.HasClass("hooked")));
            }
        }

        [Test]
        public void FactoryEasyScore_AcceptsV5ConstructionOptions()
        {
            using var ctx = new SkiaRenderContext(400, 150);
            var factory = new Factory(ctx, 400, 150);
            var hookCount = 0;

            var score = factory.EasyScore(new EasyScoreOptions
            {
                Defaults = new EasyScoreDefaults { Clef = "bass", Time = "3/8" },
                ThrowOnError = true,
                CommitHooks = new List<System.Action<Builder, StemmableNote>>
                {
                    (_, note) =>
                    {
                        hookCount++;
                        note.SetAttribute("id", "from-hook");
                    },
                },
            });

            var notes = score.Notes("C3/q");
            var voice = score.Voice(notes);

            Assert.That(((StaveNote)notes[0]).GetKeys()[0], Is.EqualTo("c/3"));
            Assert.That(notes[0].GetAttribute("id"), Is.EqualTo("from-hook"));
            Assert.That(hookCount, Is.EqualTo(1));
            Assert.That(voice.GetTotalTicks(), Is.EqualTo(new Fraction(Tables.RESOLUTION * 3 / 8, 1)));
            Assert.Throws<VexFlowException>(() => score.Parse("C4/q, nope"));
        }

        [Test]
        public void SetOptions_AppliesV5ConstructionOptionsAfterCreation()
        {
            var (ctx, _, score) = CreateScore();
            using (ctx)
            {
                var hookCount = 0;

                var returned = score.SetOptions(new EasyScoreOptions
                {
                    Defaults = new EasyScoreDefaults { Clef = "bass", Stem = "down", Time = "2/4" },
                    ThrowOnError = true,
                    CommitHooks = new List<System.Action<Builder, StemmableNote>>
                    {
                        (_, note) =>
                        {
                            hookCount++;
                            note.AddClass("from-set-options");
                        },
                    },
                });

                var notes = score.Notes("C3/q");
                var voice = score.Voice(notes);

                Assert.That(returned, Is.SameAs(score));
                Assert.That(notes[0].GetKeys()[0], Is.EqualTo("c/3"));
                Assert.That(notes[0].GetStemDirection(), Is.EqualTo(Stem.DOWN));
                Assert.That(notes[0].HasClass("from-set-options"), Is.True);
                Assert.That(hookCount, Is.EqualTo(1));
                Assert.That(voice.GetTotalTicks(), Is.EqualTo(new Fraction(Tables.RESOLUTION * 2 / 4, 1)));
                Assert.Throws<VexFlowException>(() => score.Parse("C4/q, nope"));
            }
        }

        [Test]
        public void SetContext_UpdatesUnderlyingFactory()
        {
            var (ctx, factory, score) = CreateScore();
            using (ctx)
            using (var nextContext = new SkiaRenderContext(400, 150))
            {
                var returned = score.SetContext(nextContext);
                var notes = score.Notes("C4/q");

                Assert.That(returned, Is.SameAs(score));
                Assert.That(factory.GetContext(), Is.SameAs(nextContext));
                Assert.That(notes[0].CheckContext(), Is.SameAs(nextContext));
            }
        }

        [Test]
        [Category("ImageCompare")]
        public void EasyScoreGrandStaffRenders()
        {
            using var ctx = new SkiaRenderContext(600, 250);
            ctx.SetFillStyle("#FFFFFF");
            ctx.FillRect(0, 0, 600, 250);
            var factory = new Factory(ctx, 600, 250);
            var score = factory.EasyScore();

            var trebleNotes = score.Notes("C4/q, D4, E4, F4");
            score.Set(new EasyScoreDefaults { Clef = "bass" });
            var bassNotes = score.Notes("C3/q, D3, E3, F3", new NoteOptions { Clef = "bass" });

            var trebleVoice = score.Voice(trebleNotes);
            var bassVoice = score.Voice(bassNotes);
            var system = factory.System(new SystemOptions { X = 10, Y = 10, Width = 570 });
            system.AddStave(new SystemStave { Voices = new List<Voice> { trebleVoice } })
                .AddClef("treble").AddTimeSignature("4/4");
            system.AddStave(new SystemStave { Voices = new List<Voice> { bassVoice } })
                .AddClef("bass").AddTimeSignature("4/4");
            system.AddConnector("singleLeft");
            system.AddConnector("brace");

            factory.Draw();

            string refPath = RefPath("easyscore_grandstaff-vexflow.png");
            Assert.That(File.Exists(refPath), Is.True, $"Reference PNG missing: {refPath}");

            byte[] actual = ctx.ToPng();
            byte[] reference = File.ReadAllBytes(refPath);
            ImageComparisonAssert.AssertImagesMatch(actual, reference, thresholdPercent: ImageComparison.CrossEngineThresholdPercent);
        }
    }
}
